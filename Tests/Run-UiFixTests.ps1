$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$csc='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$output=Join-Path $PSScriptRoot ('UiFixHarness-'+[Guid]::NewGuid().ToString('N')+'.dll')
$sources=@((Join-Path $project 'src/Mod/Features/KeyViewer/Presentation/KeyViewer.Layout.cs'),(Join-Path $project 'src/Mod/Features/KeyViewer/Layout/KeyViewerMetrics.cs'),(Join-Path $project 'src/Mod/Presentation/Controls/OverlayTextShadow.cs'),(Join-Path $project 'src/Mod/Integration/Adofai/Input/KeyCaptureUiLease.cs'),(Join-Path $PSScriptRoot 'UiFixHarness.cs'))
$sources+=@((Join-Path $project 'src/Mod/Features/KeyViewer/Layout/KeyViewerGeometry.cs'),(Join-Path $project 'src/Mod/Features/KeyViewer/Layout/KeyviewerStyle.cs'))
$sources+=Join-Path $project 'src/Mod/Features/KeyViewer/Layout/KeyViewerSlot.cs'
# Compile the production layout builder and shadow owner against inert native API stubs.
& $csc /nologo /target:library /langversion:latest "/out:$output" /r:System.Core.dll $sources
if($LASTEXITCODE -ne 0){throw 'UI harness compilation failed'}
$harness=[Reflection.Assembly]::LoadFrom($output)
$results=$harness.GetType('DonQuixoteOverlay.UiFixHarness',$true).GetMethod('Run').Invoke($null,@())
$results
'UI repair assertions passed: '+$results.Count
