# Run in pwsh: the native SkyHook mapper needs CollectionExtensions.
param([string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice',[switch]$CompatibilityOnly)
$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot
$taskManaged=Join-Path $GameDir 'A Dance of Fire and Ice_Data\Managed'
if(!$CompatibilityOnly){$taskNative=[Runtime.InteropServices.NativeLibrary]::Load((Join-Path $GameDir 'A Dance of Fire and Ice_Data\Plugins\x86_64\skyhook.dll'))}
foreach($taskDir in @($taskManaged,(Join-Path $taskManaged 'UnityModManager'))){foreach($taskFile in Get-ChildItem -LiteralPath $taskDir -Filter *.dll){try{[Reflection.Assembly]::LoadFrom($taskFile.FullName)|Out-Null}catch{}}}
$taskMod=[Reflection.Assembly]::LoadFrom((Join-Path $taskProject 'bin\Release\GhostifyOverlay\GhostifyOverlay.dll'))
$taskCsc='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$taskOutput=Join-Path $PSScriptRoot ('FeedbackV3Harness-'+[Guid]::NewGuid().ToString('N')+'.dll')
& $taskCsc /nologo /target:library /langversion:latest /r:System.Core.dll "/out:$taskOutput" (Join-Path $PSScriptRoot 'FeedbackV3Tests.cs')
if($LASTEXITCODE -ne 0){throw 'Feedback v3 harness compilation failed'}
$taskHarness=[Reflection.Assembly]::LoadFrom($taskOutput)
$taskMethod=if($CompatibilityOnly){'RunCompatibility'}else{'RunCore'}
try{$taskResults=$taskHarness.GetType('FeedbackV3Tests',$true).GetMethod($taskMethod).Invoke($null,@($taskMod))}catch{throw $_.Exception.ToString()}
$taskResults
'Feedback v3 '+$taskMethod+' assertions passed: '+$taskResults.Count
