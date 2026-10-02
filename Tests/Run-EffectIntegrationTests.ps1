param([string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice')
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$managed=Join-Path $GameDir 'A Dance of Fire and Ice_Data\Managed'
foreach($dir in @($managed,(Join-Path $managed 'UnityModManager'))){foreach($dll in Get-ChildItem -LiteralPath $dir -Filter '*.dll'){try{[Reflection.Assembly]::LoadFrom($dll.FullName)|Out-Null}catch{}}}
$mod=[Reflection.Assembly]::LoadFrom((Join-Path $project 'bin\Release\GhostifyOverlay\GhostifyOverlay.dll'))
$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$script:passed=0
function T([string]$name){$mod.GetType('DonQuixoteOverlay.'+$name,$true)}
function Invoke-Static($type,[string]$name,[object[]]$values){
    for($i=0;$i -lt $values.Count;$i++){if($null -ne $values[$i]){$values[$i]=$values[$i].PSObject.BaseObject}}
    $method=$null
    foreach($candidate in $type.GetMethods($flags)|Where-Object {$_.Name -eq $name -and $_.GetParameters().Count -eq $values.Count}) {
        $parameters=$candidate.GetParameters();$match=$true
        for($i=0;$i -lt $values.Count;$i++){if($null -ne $values[$i] -and !$parameters[$i].ParameterType.IsInstanceOfType($values[$i])){$match=$false}}
        if($match){$method=$candidate;break}
    }
    if($null -eq $method){throw ('Missing compatible method '+$type.Name+'.'+$name)}
    try{$method.Invoke($null,$values)}catch{throw $_.Exception.ToString()}
}
function Check($condition,[string]$name){if(!$condition){throw ('FAIL '+$name)};$script:passed++;'PASS '+$name}
$origin=Join-Path (Split-Path $project) 'DonQuixote'
$source=[IO.File]::ReadAllText((Join-Path $origin 'GameplayPatches.cs')).Replace('namespace DonQuixote {','namespace DonQuixoteOverlay {')
Check ($source -ceq [IO.File]::ReadAllText((Join-Path $project 'GameplayPatches.cs'))) 'effect patches match original code except independent namespace'
$restore=[IO.File]::ReadAllText((Join-Path $origin 'GameStateRestoration.cs'))
$begin=$restore.IndexOf('    internal static class EffectRestoration {');$end=$restore.IndexOf('    public sealed class GameStateLifetime')
Check ([IO.File]::ReadAllText((Join-Path $project 'EffectRestoration.cs')).Contains($restore.Substring($begin,$end-$begin))) 'effect state restoration implementation matches original'
$originalSettings=[IO.File]::ReadAllText((Join-Path $origin 'Settings.cs'));$newSettings=[IO.File]::ReadAllText((Join-Path $project 'Settings.cs'))
$begin=$originalSettings.IndexOf('    public sealed class EffectsSettings');$end=$originalSettings.IndexOf('    [Serializable]', $begin)
Check ($newSettings.Contains($originalSettings.Substring($begin,$end-$begin))) 'effect fields and defaults match original settings model'
$fixture=Join-Path $PSScriptRoot ('effect-storage-'+[Guid]::NewGuid().ToString('N'))
Invoke-Static (T 'SettingsStore') 'Initialize' @($fixture)|Out-Null
$settingsPath=Join-Path $fixture 'UserData\settings.json'
[IO.File]::WriteAllText($settingsPath,'{"SchemaVersion":1,"Judgments":{"HideXPerfect":true,"TextSizeScale":0.83},"Input":{"AllowedKeys":["Return"]},"Overlay":{"FullAttempts":{"legacy":31},"FullProgressAttempts":{"legacy":43}}}')
$settings=Invoke-Static (T 'SettingsStore') 'Load' @()
Check ($settings.SchemaVersion -eq 2 -and $null -ne $settings.Effects -and !$settings.Effects.Enabled -and $settings.Effects.MoveTrackMax -eq 30) 'legacy schema adds effects safely with original defaults'
Check ($settings.Judgments.HideXPerfect -and [Math]::Abs($settings.Judgments.TextSizeScale-.83) -lt .00001 -and $settings.Input.AllowedKeys[0] -eq 'Return' -and $settings.Overlay.FullAttempts['legacy'] -eq 31 -and $settings.Overlay.FullProgressAttempts['legacy'] -eq 43) 'legacy judgment size, allowed keys and attempt records remain intact'
$settings.Effects=$null;Invoke-Static (T 'SettingsNormalization') 'Normalize' @($settings)|Out-Null
Check ($null -ne $settings.Effects -and $settings.Effects.FilterExcludeList.Count -eq 0) 'null effect settings recovered independently'
$settings.Effects.FilterExcludeList=$null;$settings.Effects.MoveTrackMax=-3
Invoke-Static (T 'SettingsNormalization') 'Normalize' @($settings)|Out-Null
Check ($settings.Effects.FilterExcludeList.Count -eq 0 -and $settings.Effects.MoveTrackMax -eq 0) 'missing filter list and negative tile count normalize safely'
$settings.Effects.MoveTrackMax=15000;$settings.Effects.FilterExcludeList.Add('');$settings.Effects.FilterExcludeList.Add('  ');$settings.Effects.FilterExcludeList.Add('Sepia')
Invoke-Static (T 'SettingsNormalization') 'Normalize' @($settings)|Out-Null
Check ($settings.Effects.MoveTrackMax -eq 9999 -and $settings.Effects.FilterExcludeList.Count -eq 1 -and $settings.Effects.FilterExcludeList[0] -eq 'Sepia') 'original persistence bounds and nonempty exclusions preserved'
foreach($name in @('Enabled','DisableFilter','DisableBloom','DisableFlash','DisableHallOfMirrors','DisableScreenShake')){$settings.Effects.GetType().GetField($name).SetValue($settings.Effects,$true)}
$settings.Effects.MoveTrackLimitEnabled=$false;$settings.Effects.MoveTrackMax=0
Invoke-Static (T 'SettingsStore') 'Save' @($settings)|Out-Null
$loaded=Invoke-Static (T 'SettingsStore') 'Load' @()
Check ($loaded.Effects.Enabled -and $loaded.Effects.DisableFilter -and $loaded.Effects.DisableBloom -and $loaded.Effects.DisableFlash -and $loaded.Effects.DisableHallOfMirrors -and $loaded.Effects.DisableScreenShake -and !$loaded.Effects.MoveTrackLimitEnabled -and $loaded.Effects.MoveTrackMax -eq 0 -and $loaded.Effects.FilterExcludeList[0] -eq 'Sepia') 'all effect toggles, zero bypass and exclusions survive save/reload'
Check ($loaded.Overlay.FullAttempts['legacy'] -eq 31 -and $loaded.Overlay.FullProgressAttempts['legacy'] -eq 43 -and [Math]::Abs($loaded.Judgments.TextSizeScale-.83) -lt .00001) 'effect saves preserve fractional judgment size and cumulative attempt records'
[IO.File]::WriteAllText($settingsPath,'{broken')
$recovered=Invoke-Static (T 'SettingsStore') 'Load' @()
Check ($null -ne $recovered.Effects -and @(Get-ChildItem (Split-Path $settingsPath) -Filter 'settings.json.corrupt-*').Count -eq 1 -and $recovered.Overlay.FullAttempts['legacy'] -eq 31) 'corrupt effect settings backed up and legacy attempt records recovered'
Add-Type -Path (Join-Path $PSScriptRoot 'ILReader.cs') -ReferencedAssemblies 'System.Core'
$reader=[LifecycleTests].GetMethod('Calls',$flags)
Check (@($reader.Invoke($null,@((T 'SettingsWindow').GetMethod('Build',$flags)))|Where-Object {$_.DeclaringType -eq (T 'SettingsWindow') -and $_.Name -eq 'ValueSlider'}).Count -eq 1) 'judgment settings page uses the verified shared value slider'
$disableCalls=$reader.Invoke($null,@((T 'Main').GetMethod('Disable',$flags)))
$cleanupCalls=@($mod.GetTypes()|Where-Object {$_.FullName -like 'DonQuixoteOverlay.Main+*'}|ForEach-Object {$_.GetMethods($flags)|Where-Object {$_.Name -like '*Disable*' -and $null -ne $_.GetMethodBody()}|ForEach-Object {$reader.Invoke($null,@($_.PSObject.BaseObject))}})
Check (@($cleanupCalls|Where-Object {$_.DeclaringType -eq (T 'EffectRestoration') -and $_.Name -eq 'RestoreFilters'}).Count -eq 1 -and @($cleanupCalls|Where-Object {$_.DeclaringType -eq (T 'EffectRestoration') -and $_.Name -eq 'RestoreCameras'}).Count -eq 1) 'mod shutdown independently restores filters and cameras'
$toggleCalls=$reader.Invoke($null,@((T 'Main').GetMethod('Toggle',$flags)))
Check (@($toggleCalls|Where-Object {$_.Name -eq 'AddComponent' -and $_.IsGenericMethod -and $_.GetGenericArguments()[0] -eq (T 'EffectLifetime')}).Count -eq 1) 'mod root owns the scene/disable restoration lifetime'
Check (@($toggleCalls|Where-Object {$_.DeclaringType -eq (T 'PatchRegistry') -and $_.Name -eq 'Apply'}).Count -eq 1 -and @($cleanupCalls|Where-Object {$_.DeclaringType -eq (T 'PatchRegistry') -and $_.Name -eq 'RemoveAll'}).Count -eq 1) 'activation and shutdown use original grouped owner-scoped patch lifecycle'
$registry=T 'PatchRegistry';$items=@(Invoke-Static $registry 'Catalog' @())
$groups=[Array]::CreateInstance((T 'PatchGroup'),$items.Count)
for($i=0;$i -lt $items.Count;$i++){$groups.SetValue($items[$i].PSObject.BaseObject,$i)}
Invoke-Static $registry 'ValidateCatalog' @($mod,$groups)|Out-Null
Check ($groups.Count -eq 7) 'patch catalog covers current overlay plus six effect feature groups'
$optional=@($groups|Where-Object {!$_.GetType().GetField('Required',$flags).GetValue($_)})
Check ($optional.Count -eq 6 -and $groups[0].GetType().GetField('Types',$flags).GetValue($groups[0]).Count -eq 21) 'existing 21 patches keep required behavior and new effect groups remain optional'
$owner='qkddn.DonQuixoteOverlay.effects-integration';$foreignOwner=$owner+'.foreign'
$harmony=[HarmonyLib.Harmony]::new($owner);$foreign=[HarmonyLib.Harmony]::new($foreignOwner)
try {
    foreach($group in $optional){$key=$group.GetType().GetField('Key',$flags).GetValue($group);Check (Invoke-Static $registry 'ApplyGroup' @($harmony,$group)) ('installed game effect patch group '+$key)}
    $target=[HarmonyLib.AccessTools]::Method([ffxBloomPlus],'StartEffect',[Type[]]@([scrPlanet]),$null)
    $prefix=(T 'DisableBloomPatch').GetMethod('Prefix',$flags)
    $foreign.Patch($target,[HarmonyLib.HarmonyMethod]::new($prefix),$null,$null,$null)|Out-Null
    Invoke-Static $registry 'RemoveAll' @($harmony)|Out-Null
    Check ([HarmonyLib.Harmony]::GetPatchInfo($target).Owners -contains $foreignOwner -and [HarmonyLib.Harmony]::GetPatchInfo($target).Owners -notcontains $owner) 'owner-scoped cleanup preserves a foreign mod sharing our effect method'
    $bad=[Activator]::CreateInstance((T 'PatchGroup'),$flags,$null,[object[]]@('controlled-failure','controlled optional probe',$false,[Type[]]@((T 'DisableShakePatch')),[Func[string]]{'controlled missing API'}),$null)
    Check (!(Invoke-Static $registry 'ApplyGroup' @($harmony,$bad)) -and !(Invoke-Static $registry 'IsAvailable' @('controlled-failure'))) 'optional API failure reports unavailable without aborting overlay'
    $rollback=[Activator]::CreateInstance((T 'PatchGroup'),$flags,$null,[object[]]@('controlled-rollback','controlled partial group',$false,[Type[]]@((T 'DisableShakePatch'),(T 'AsyncInputPatch')),$null),$null)
    Check (!(Invoke-Static $registry 'ApplyGroup' @($harmony,$rollback))) 'optional partial patch failure returns safely'
    Check (@([HarmonyLib.Harmony]::GetAllPatchedMethods()|Where-Object {[HarmonyLib.Harmony]::GetPatchInfo($_).Owners -contains $owner}).Count -eq 0) 'partial group rollback removes newly created patches'
} finally {Invoke-Static $registry 'RemoveAll' @($harmony)|Out-Null;$foreign.UnpatchAll($foreignOwner)}
Check (@([HarmonyLib.Harmony]::GetAllPatchedMethods()|Where-Object {[HarmonyLib.Harmony]::GetPatchInfo($_).Owners -contains $owner -or [HarmonyLib.Harmony]::GetPatchInfo($_).Owners -contains $foreignOwner}).Count -eq 0) 'all effect integration test patches removed without native calls'
'Effect integration assertions passed: '+$script:passed
