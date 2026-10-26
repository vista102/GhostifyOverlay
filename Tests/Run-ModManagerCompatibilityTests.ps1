param([string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice')
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$managed=Join-Path $GameDir 'A Dance of Fire and Ice_Data\Managed'
$ummDir=Join-Path $managed 'UnityModManager'
$modPath=Join-Path $project 'bin\Release\GhostifyOverlay\GhostifyOverlay.dll'
$passed=0
function Check($okay,[string]$label){if(!$okay){throw ('FAIL '+$label)};$script:passed++;'PASS '+$label}
$info=Get-Content -LiteralPath (Join-Path $project 'Info.json') -Raw -Encoding UTF8|ConvertFrom-Json
$compatibility=Get-Content -LiteralPath (Join-Path $project 'GameCompatibility.json') -Raw -Encoding UTF8|ConvertFrom-Json
$ummName=[Reflection.AssemblyName]::GetAssemblyName((Join-Path $ummDir 'UnityModManager.dll'))
Check ($info.ManagerVersion -eq '0.32.5' -and $compatibility.MinimumModManager -eq $info.ManagerVersion) 'mod and installer declare UMM 0.32.5'
Check ($ummName.Version -eq [version]'0.32.5.0') 'API checks use the real UMM 0.32.5 assembly'
foreach($directory in @($managed,$ummDir)){
    foreach($file in Get-ChildItem -LiteralPath $directory -Filter *.dll){try{[Reflection.Assembly]::LoadFrom($file.FullName)|Out-Null}catch{}}
}
$mod=[Reflection.Assembly]::LoadFrom($modPath)
$reference=@($mod.GetReferencedAssemblies()|Where-Object Name -eq 'UnityModManager')
Check ($reference.Count -eq 1 -and $reference[0].Version -eq $ummName.Version) 'shipping mod references UMM 0.32.5 rather than a newer version'
$harmony=@($mod.GetReferencedAssemblies()|Where-Object Name -eq '0Harmony')
$harmonyName=[Reflection.AssemblyName]::GetAssemblyName((Join-Path $ummDir '0Harmony.dll'))
Check ($harmony.Count -eq 1 -and $harmony[0].Version -eq $harmonyName.Version) 'Harmony reference matches the UMM installation'
$entry=$mod.GetType('DonQuixoteOverlay.Main',$true).GetMethod('Load')
Check ($entry.ReturnType -eq [bool] -and $entry.GetParameters().Count -eq 1 -and $entry.GetParameters()[0].ParameterType.Assembly.GetName().Version -eq $ummName.Version) 'stable UMM entry point resolves against 0.32.5'
$metadata=[dnlib.DotNet.ModuleDefMD]::Load($modPath)
try{
    $references=@($metadata.GetMemberRefs()|Where-Object {$_.DeclaringType.DefinitionAssembly.Name -eq 'UnityModManager'})
    Check ($references.Count -gt 0) 'UMM member reference inventory is nonempty'
    foreach($member in $references){
        $resolved=$mod.ManifestModule.ResolveMember([int]$member.MDToken.Raw)
        Check ($resolved.DeclaringType.Assembly.GetName().Version -eq $ummName.Version) ('real UMM 0.32.5 member resolves: '+$member.FullName)
    }
}finally{$metadata.Dispose()}
$tweaksPath=Join-Path $GameDir 'Mods/AdofaiTweaks/AdofaiTweaks.dll'
if(Test-Path -LiteralPath $tweaksPath){
    $tweaks=[Reflection.Assembly]::LoadFrom($tweaksPath)
    $flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
    $patches=$tweaks.GetType('AdofaiTweaks.Tweaks.HideUiElements.HideUiElementsPatches',$true)
    $settingsProperty=$patches.GetProperty('Settings',$flags)
    Check ($settingsProperty -ne $null -and $settingsProperty.GetGetMethod($true).IsStatic) 'installed Tweaks exposes the optional static UI settings adapter'
    $settingsType=$settingsProperty.PropertyType
    foreach($name in @('IsEnabled','RecordingMode','PlayingProfile','RecordingProfile')){
        Check ($null -ne $settingsType.GetProperty($name,$flags)) ('installed Tweaks UI option is readable: '+$name)
    }
    $profile=$settingsType.GetProperty('PlayingProfile',$flags).PropertyType
    foreach($name in @('HideEverything','HideTitle')){Check ($null -ne $profile.GetProperty($name,$flags)) ('installed Tweaks hide profile supports '+$name)}
}else{'Optional Tweaks metadata check skipped: standalone integration remains available.'}
'Mod manager compatibility assertions passed: '+$passed
