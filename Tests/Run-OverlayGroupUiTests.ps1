$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$managed='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice\A Dance of Fire and Ice_Data\Managed'
$work=Join-Path $project ('Backups/UX-0.5.0/Ui-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work -Force|Out-Null
$sources=@(Get-ChildItem -LiteralPath (Join-Path $project 'src/Mod/Presentation/Settings') -Filter 'SettingsWindow*.cs'|ForEach-Object FullName)
$sources+=@('src/Mod/Presentation/Controls/UiButtonKind.cs','src/Mod/Presentation/Theme/DQColors.cs','src/Mod/Presentation/Theme/DQTypography.cs','src/Mod/Presentation/Theme/DQRadii.cs','src/Mod/Presentation/Theme/DQControlMetrics.cs','src/Mod/Domain/Configuration/OverlaySettings.cs','src/Mod/Domain/Configuration/LayoutData.cs','src/Mod/Domain/Configuration/ChatterSettings.cs','src/Mod/Domain/Configuration/DonQuixoteSettings.cs','src/Mod/Domain/Presets/KeyBindingPreset.cs','src/Mod/Application/Presets/KeyPresetService.cs','src/Mod/Application/Persistence/ISettingsRepository.cs','src/Mod/Infrastructure/Persistence/IJsonStorage.cs','src/Mod/Infrastructure/Persistence/AtomicJsonStorage.cs','src/Mod/Infrastructure/Persistence/JsonSettingsRepository.cs','src/Mod/Infrastructure/Persistence/KeyPresetStore.cs','src/Mod/Infrastructure/Persistence/KeyViewerColorConverter.cs','src/Mod/Infrastructure/Diagnostics/RuntimeStatus.cs','src/Mod/Features/KeyViewer/Configuration/KeyViewerSetting.cs','src/Mod/Features/KeyViewer/Configuration/KeyViewerCounterPalette.cs','src/Mod/Features/KeyViewer/Configuration/KeyViewerColors.cs','src/Mod/Features/KeyViewer/Layout/KeyviewerStyle.cs','src/Mod/Features/KeyViewer/Layout/FootKeyviewerStyle.cs','src/Mod/Features/KeyViewer/Layout/KeyViewerSlot.cs','src/Mod/Features/KeyViewer/Layout/KeyViewerMetrics.cs','src/Mod/Features/KeyViewer/Layout/KeyViewerGeometry.cs','src/Mod/Integration/Adofai/Input/KeyCaptureUiLease.cs','src/Mod/Features/LayoutEditing/LayoutEditorController.cs','src/Mod/Features/LayoutEditing/LayoutEditSnapshot.cs','src/Mod/Features/LayoutEditing/LayoutKeyRepeat.cs','src/Mod/Features/Overlay/OverlayPart.cs','src/Mod/Features/Overlay/OverlayGroup.cs','src/Mod/Features/Overlay/OverlayGroups.cs','src/Mod/Features/Overlay/OverlayTransforms.cs','Tests/SettingsUxHarness.cs','Tests/SettingsUxPreview.cs')|ForEach-Object{Join-Path $project $_}
$json=Join-Path $managed 'Newtonsoft.Json.dll'
$output=Join-Path $work 'SettingsUxTests.dll'
$compiler='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
& $compiler /nologo /target:library /langversion:latest /r:System.Core.dll ('/r:'+(Join-Path $managed 'netstandard.dll')) "/r:$json" "/out:$output" $sources
if($LASTEXITCODE -ne 0){throw 'Settings UX harness failed to compile'}
[Reflection.Assembly]::LoadFrom($json)|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom($output)
$results=$assembly.GetType('DonQuixoteOverlay.SettingsWindow',$true).GetMethod('Run').Invoke($null,[object[]]@([string]$work))
$results
'Settings UX assertions passed: '+$results.Count