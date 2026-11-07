param([string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice')
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$managed=Join-Path $GameDir 'A Dance of Fire and Ice_Data\Managed'
$work=Join-Path ([IO.Path]::GetTempPath()) ('Ghostify-KeyViewerReplay-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work -Force|Out-Null
$sources=@('src/Mod/Application/Replays/ReplayRunState.cs','src/Mod/Features/KeyViewer/Input/KeyViewer.Input.cs','src/Mod/Features/KeyViewer/Input/KeyViewer.Replay.cs','src/Mod/Features/KeyViewer/Input/KeyTransitionState.cs','src/Mod/Features/KeyViewer/Input/KeyEventClock.cs','src/Mod/Features/KeyViewer/Input/KeyKpsWindow.cs','src/Mod/Features/KeyViewer/Rain/RawRain.cs','src/Mod/Infrastructure/Persistence/KeyCountData.cs','Tests/KeyViewerReplayHarness.cs')|ForEach-Object{Join-Path $project $_}
$compiler='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$harness=Join-Path $work 'KeyViewerReplayTests.dll'
$json=Join-Path $managed 'Newtonsoft.Json.dll'
& $compiler /nologo /target:library /langversion:latest /r:System.Core.dll "/r:$json" ('/r:'+(Join-Path $managed 'netstandard.dll')) "/out:$harness" $sources
if($LASTEXITCODE -ne 0){throw 'Key viewer replay harness compilation failed'}
[Reflection.Assembly]::LoadFrom($json)|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom($harness)
try{$results=$assembly.GetType('KeyViewerReplayHarness',$true).GetMethod('Run').Invoke($null,@())}catch{throw $_.Exception.ToString()}
$results
'Key viewer replay assertions passed: '+$results.Count
