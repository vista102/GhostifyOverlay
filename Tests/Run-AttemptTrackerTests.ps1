$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot
$taskSource=[IO.File]::ReadAllText((Join-Path $taskProject 'OverlayController.cs'))
$taskStart=$taskSource.IndexOf('    internal static class AttemptTracker')
$taskEnd=$taskSource.IndexOf('    public sealed class OverlayController')
if($taskStart -lt 0 -or $taskEnd -le $taskStart){throw 'Production tracker source boundary missing'}
$taskFixture=Join-Path $taskProject ('Backups\Feedback-0.4.8\Attempts-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $taskFixture -Force|Out-Null
$taskModel=Join-Path $taskFixture 'Tracker.cs'
[IO.File]::WriteAllText($taskModel,'using System;using System.IO;using UnityEngine;namespace DonQuixoteOverlay {'+$taskSource.Substring($taskStart,$taskEnd-$taskStart)+'}',[Text.UTF8Encoding]::new($false))
$taskCsc='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$taskHarness=Join-Path $taskFixture 'AttemptHarness.dll'
& $taskCsc /nologo /target:library /langversion:latest /r:System.Core.dll "/out:$taskHarness" $taskModel (Join-Path $PSScriptRoot 'AttemptTrackerHarness.cs')
if($LASTEXITCODE -ne 0){throw 'Attempt tracker harness compilation failed'}
$taskAssembly=[Reflection.Assembly]::LoadFrom($taskHarness)
$taskResults=$taskAssembly.GetType('DonQuixoteOverlay.AttemptTrackerHarness',$true).GetMethod('Run').Invoke($null,@())
$taskResults
'Attempt tracker assertions passed: '+$taskResults.Count
