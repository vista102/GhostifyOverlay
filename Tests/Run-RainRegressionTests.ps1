param([string]$OriginalGhostPath)
$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot
$taskCsc='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$taskOutput=Join-Path $PSScriptRoot ('RainHarness-'+[Guid]::NewGuid().ToString('N')+'.dll')
$taskSources=@('Rain.cs','RainPool.cs','RainManager.cs','RawRain.cs','KeyViewerMetrics.cs')|ForEach-Object {Join-Path $taskProject ('KeyViewerContents\'+$_)}
$taskSources+=Join-Path $PSScriptRoot 'RainRegressionHarness.cs'
& $taskCsc /nologo /target:library /langversion:latest "/out:$taskOutput" /r:System.Core.dll $taskSources
if($LASTEXITCODE -ne 0){throw 'Rain harness compilation failed'}
$taskAssembly=[Reflection.Assembly]::LoadFrom($taskOutput)
$taskResults=$taskAssembly.GetType('DonQuixoteOverlay.KeyViewerContents.RainRegressionHarness',$true).GetMethod('Run').Invoke($null,@())
$taskResults
'Rain regression assertions passed: '+$taskResults.Count