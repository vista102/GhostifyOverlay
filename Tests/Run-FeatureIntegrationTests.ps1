$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$managed='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice\A Dance of Fire and Ice_Data\Managed'
$work=Join-Path $project ('Backups/Features-0.4.14-20261027/Harness-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work -Force|Out-Null
$sources=@('src/Mod/Application/Input/ChatterFilter.cs','src/Mod/Application/Visibility/VisibilityLease.cs','src/Mod/Domain/Configuration/ChatterSettings.cs','src/Mod/Integration/Interop/ModInterop.cs','src/Mod/Integration/Adofai/Input/ChatterRuntime.cs','src/Mod/Integration/Adofai/Input/ChatterAsyncInputPatch.cs','src/Mod/Integration/Adofai/Input/ChatterSyncInputPatch.cs','src/Mod/Integration/Adofai/Input/RawGhostInputPatch.cs','src/Mod/Integration/Unity/NativeSongTitleVisibility.cs','src/Mod/Features/Overlay/OverlayController.Typography.cs','src/Mod/Presentation/Settings/SettingsWindow.Typography.cs','Tests/FeatureIntegrationHarness.cs')|ForEach-Object{Join-Path $project $_}
$compiler='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$harness=Join-Path $work 'FeatureIntegrationTests.dll'
$harmony=Join-Path $managed 'UnityModManager/0Harmony.dll'
& $compiler /nologo /target:library /langversion:latest /r:System.Core.dll "/r:$harmony" "/out:$harness" $sources
if($LASTEXITCODE -ne 0){throw 'Feature harness compilation failed'}
[Reflection.Assembly]::LoadFrom($harmony)|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom($harness)
try{$results=$assembly.GetType('FeatureIntegrationTests',$true).GetMethod('Run').Invoke($null,@())}catch{throw $_.Exception.ToString()}
$results
'Feature integration assertions passed: '+$results.Count
