param()
$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot
$taskCsc='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$taskOutput=Join-Path $PSScriptRoot ('ColorRowHarness-'+[Guid]::NewGuid().ToString('N')+'.dll')
& $taskCsc /nologo /target:library /langversion:latest /r:System.Core.dll "/out:$taskOutput" (Join-Path $taskProject 'SettingsWindow.Colors.cs') (Join-Path $PSScriptRoot 'ColorRowHarness.cs')
if($LASTEXITCODE -ne 0){throw 'Color row harness compilation failed'}
$taskAssembly=[Reflection.Assembly]::LoadFrom($taskOutput)
$taskResults=$taskAssembly.GetType('DonQuixoteOverlay.SettingsWindow',$true).GetMethod('TestColorRows').Invoke($null,@())
$taskResults
'Color row assertions passed: '+$taskResults.Count
