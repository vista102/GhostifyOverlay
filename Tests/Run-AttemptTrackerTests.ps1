$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot
$taskSource=[IO.File]::ReadAllText((Join-Path $taskProject 'src/Mod/Features/Attempts/AttemptTracker.cs'))
$taskSource=$taskSource.Replace('using HarmonyLib;','').Replace('using TMPro;','').Replace('using UnityEngine.SceneManagement;','').Replace('using UnityEngine.TextCore.LowLevel;','')
$taskFixture=Join-Path $taskProject ('Backups\Feedback-0.4.8\Attempts-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $taskFixture -Force|Out-Null
$taskModel=Join-Path $taskFixture 'Tracker.cs'
[IO.File]::WriteAllText($taskModel,$taskSource,[Text.UTF8Encoding]::new($false))
$taskCsc='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$taskHarness=Join-Path $taskFixture 'AttemptHarness.dll'
$taskSources=@('src/Mod/Application/Attempts/LevelFingerprint.cs','src/Mod/Application/Attempts/AttemptRecordMigration.cs','src/Mod/Integration/Adofai/Metadata/AdofaiLevelIdentity.cs','Tests/LevelHashHarness.cs')|ForEach-Object{Join-Path $taskProject $_}
& $taskCsc /nologo /target:library /langversion:latest /r:System.Core.dll "/out:$taskHarness" $taskModel $taskSources (Join-Path $PSScriptRoot 'AttemptTrackerHarness.cs')
if($LASTEXITCODE -ne 0){throw 'Attempt tracker harness compilation failed'}
$taskAssembly=[Reflection.Assembly]::LoadFrom($taskHarness)
$taskResults=$taskAssembly.GetType('DonQuixoteOverlay.AttemptTrackerHarness',$true).GetMethod('Run').Invoke($null,@())
$taskResults
'Attempt tracker assertions passed: '+$taskResults.Count
