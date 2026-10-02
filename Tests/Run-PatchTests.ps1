param([string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice')
$ErrorActionPreference='Stop'
$managed=Join-Path $GameDir 'A Dance of Fire and Ice_Data\Managed'
foreach($dir in @($managed,(Join-Path $managed 'UnityModManager'))) {
    Get-ChildItem -LiteralPath $dir -Filter *.dll | ForEach-Object {try {[Reflection.Assembly]::LoadFrom($_.FullName)|Out-Null} catch {}}
}
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path (Split-Path $PSScriptRoot) 'bin\Release\GhostifyOverlay\GhostifyOverlay.dll'))
$owner='qkddn.DonQuixoteOverlay.offline-test'
$harmony=New-Object HarmonyLib.Harmony($owner)
try {
    $patchTypes=@($assembly.GetTypes()|Where-Object {$_.GetCustomAttributes([HarmonyLib.HarmonyPatch],$false).Count -gt 0})
    if($patchTypes.Count -ne 28) {throw "Unexpected patch class count: $($patchTypes.Count)"}
    $targetNames=New-Object 'System.Collections.Generic.HashSet[string]'
    $created=0; $unavailable=0
    foreach($type in $patchTypes) {
        $info=$type.GetCustomAttributes([HarmonyLib.HarmonyPatch],$false)[0].info
        $target=[HarmonyLib.AccessTools]::Method($info.declaringType,$info.methodName,$info.argumentTypes,$null)
        if($null -eq $target) {throw "Missing game API for $($type.Name)"}
        $targetNames.Add($target.DeclaringType.FullName+'.'+$target.Name)|Out-Null
        "PASS target metadata $($type.Name)"
        try {
            $methods=$harmony.CreateClassProcessor($type).Patch()
            if($methods.Count -eq 0) {throw "No target for $($type.Name)"}
            $created++; "PASS patch creation $($type.Name)"
        } catch {
            if($_.Exception.ToString() -notmatch 'ECall methods must be packaged into a system module') {throw}
            $unavailable++; "UNVERIFIED outside Unity: $($type.Name) (CLR rejects Unity ECall)"

        }
    }
    if($targetNames.Count -ne 27) {throw "Unexpected unique target count: $($targetNames.Count)"}
    "Target metadata passed: 28 classes / 27 targets; patch creation passed: $created; unavailable outside Unity: $unavailable"
    # Exercise the manual hook lease prefixes without invoking any native hook/game method.
    $flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
    $hookType=$assembly.GetType('DonQuixoteOverlay.KeyViewerContents.AsyncInputHook',$true)
    $gameType=[AsyncInputManager]
    $manualTargets=@(
        @{Target=[HarmonyLib.AccessTools]::Method($gameType,'ToggleHook');Prefix='TogglePrefix'},
        @{Target=[HarmonyLib.AccessTools]::PropertyGetter($gameType,'isActive');Prefix='ActivePrefix'}
    )
    foreach($type in @($gameType)+$gameType.GetNestedTypes($flags)) {
        foreach($method in $type.GetMethods($flags -bor [Reflection.BindingFlags]::DeclaredOnly)) {
            $parameters=$method.GetParameters()
            if($method.ReturnType -eq [void] -and $parameters.Count -eq 1 -and $parameters[0].ParameterType -eq [SkyHook.SkyHookEvent]) {
                $manualTargets+=@{Target=$method;Prefix='ListenerPrefix'}
            }
        }
    }
    if($manualTargets.Count -lt 3){throw 'Missing async hook lease targets'}
    $manualCreated=0;$manualUnavailable=0
    foreach($pair in $manualTargets) {
        if($null -eq $pair.Target){throw 'Missing manual hook target'}
        $prefix=$hookType.GetMethod($pair.Prefix,$flags)
        try {
            $harmony.Patch($pair.Target,[HarmonyLib.HarmonyMethod]::new($prefix),$null,$null,$null)|Out-Null
            if(!([HarmonyLib.Harmony]::GetPatchInfo($pair.Target).Prefixes.PatchMethod -contains $prefix)){throw 'Hook lease prefix was not installed'}
            $manualCreated++;"PASS manual hook prefix $($pair.Prefix) -> $($pair.Target.Name)"
        } catch {
            if($_.Exception.ToString() -notmatch 'ECall methods must be packaged into a system module'){throw}
            $manualUnavailable++;"UNVERIFIED outside Unity: manual hook $($pair.Prefix) (CLR rejects Unity ECall)"
        }
    }
    "Manual hook targets: $($manualTargets.Count); patch creation passed: $manualCreated; unavailable outside Unity: $manualUnavailable"
} catch { Write-Output $_.Exception.ToString(); throw } finally {$harmony.UnpatchAll($owner)}
$remaining=@([HarmonyLib.Harmony]::GetAllPatchedMethods()|Where-Object {[HarmonyLib.Harmony]::GetPatchInfo($_).Owners -contains $owner})
if($remaining.Count -ne 0) {throw 'Patch cleanup failed'}
'PASS all test patches removed; native game methods were not executed'
