$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$managed='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice\A Dance of Fire and Ice_Data\Managed'
$work=Join-Path $project ('Backups/UX-0.5.0/Preset-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work -Force|Out-Null
$sources=@('src/Mod/Domain/Presets/KeyBindingPreset.cs','src/Mod/Application/Presets/KeyPresetService.cs','src/Mod/Application/Persistence/ISettingsRepository.cs','src/Mod/Infrastructure/Persistence/IJsonStorage.cs','src/Mod/Infrastructure/Persistence/AtomicJsonStorage.cs','src/Mod/Infrastructure/Persistence/JsonSettingsRepository.cs','src/Mod/Infrastructure/Persistence/KeyPresetStore.cs','src/Mod/Infrastructure/Persistence/KeyViewerColorConverter.cs','src/Mod/Features/KeyViewer/Configuration/KeyViewerSetting.cs','src/Mod/Features/KeyViewer/Configuration/KeyViewerCounterPalette.cs','src/Mod/Features/KeyViewer/Layout/KeyviewerStyle.cs','src/Mod/Features/KeyViewer/Layout/FootKeyviewerStyle.cs','src/Mod/Presentation/Theme/DQColors.cs','Tests/KeyPresetTests.cs')|ForEach-Object{Join-Path $project $_}
$refs=@('Newtonsoft.Json.dll','UnityEngine.dll','UnityEngine.CoreModule.dll','Unity.TextMeshPro.dll','netstandard.dll')|ForEach-Object{'/r:'+(Join-Path $managed $_)}
$output=Join-Path $work 'KeyPresetTests.dll'
$compiler='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
& $compiler /nologo /target:library /langversion:latest /r:System.Core.dll $refs "/out:$output" $sources
if($LASTEXITCODE -ne 0){throw 'Preset tests failed to compile'}
foreach($name in @('Newtonsoft.Json.dll','UnityEngine.CoreModule.dll','UnityEngine.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $managed $name))|Out-Null}
$assembly=[Reflection.Assembly]::LoadFrom($output)
$results=$assembly.GetType('KeyPresetTests',$true).GetMethod('Run').Invoke($null,[object[]]@([string]$work))
$results
'Key preset assertions passed: '+$results.Count
