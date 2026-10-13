$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot
$taskFixture=Join-Path $taskProject ('Backups\Feedback-0.5.0\Umm-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $taskFixture -Force|Out-Null
$taskCsc='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$taskHarness=Join-Path $taskFixture 'UmmHarness.dll'
$taskSources=@('UmmSettingsGeometry.cs','UmmSettingsHost.cs','UmmSettingsInput.cs','SettingsWindow.Umm.cs','Tests/UmmHostHarness.cs')|ForEach-Object{Join-Path $taskProject $_}
& $taskCsc /nologo /target:library /langversion:latest /r:System.Core.dll "/out:$taskHarness" $taskSources
if($LASTEXITCODE -ne 0){throw 'UMM harness compilation failed'}
$taskAssembly=[Reflection.Assembly]::LoadFrom($taskHarness)
$taskResults=$taskAssembly.GetType('DonQuixoteOverlay.UmmHostHarness',$true).GetMethod('Run').Invoke($null,@())
$taskResults
'UMM host assertions passed: '+$taskResults.Count
