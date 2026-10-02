$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$root=Join-Path $PSScriptRoot ('test-fixture-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root | Out-Null
$csc='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$ummSource=Join-Path $root 'FakeUmm.cs'
[IO.File]::WriteAllText($ummSource,'[assembly:System.Reflection.AssemblyVersion("0.33.0.0")] public class FakeUmm {}')
$umm=Join-Path $root 'FakeUmm.dll'
& $csc /nologo /target:library "/out:$umm" $ummSource
if($LASTEXITCODE -ne 0){throw 'UMM fixture compilation failed'}
$sleeperSource=Join-Path $root 'Sleeper.cs'
[IO.File]::WriteAllText($sleeperSource,'public static class Sleeper { public static void Main(){System.Threading.Thread.Sleep(3500);} }')
$sleeper=Join-Path $root 'Sleeper.exe'
& $csc /nologo /target:exe "/out:$sleeper" $sleeperSource
if($LASTEXITCODE -ne 0){throw 'Process fixture compilation failed'}
$harness=Join-Path $root 'InstallerHarness.dll'
& $csc /nologo /target:library /langversion:latest "/out:$harness" /r:System.Core.dll /r:System.Web.Extensions.dll /r:System.Xml.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll (Join-Path $PSScriptRoot 'InstallerEngine.cs') (Join-Path $PSScriptRoot 'InstallerTests.cs')
if($LASTEXITCODE -ne 0){throw 'Installer test compilation failed'}
$assembly=[Reflection.Assembly]::LoadFrom($harness)
$info=Get-Content -LiteralPath (Join-Path $project 'Info.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$payload=Join-Path $project ('dist\GhostifyOverlay-'+$info.Version+'.zip')
$manifest=Join-Path $PSScriptRoot 'obj\Payload.json'
$testArgs=[object[]]@($root,$payload,$manifest,$umm,$sleeper)
for($i=0;$i -lt $testArgs.Length;$i++){$testArgs[$i]=$testArgs[$i].PSObject.BaseObject}
try{$results=$assembly.GetType('GhostifySetup.InstallerTests',$true).GetMethod('Run').Invoke($null,$testArgs)}catch{throw $_.Exception.ToString()}
$results
'Installer assertions passed: '+$results.Count
# Run the exact shipping EXE backend against a separate, disposable game fixture.
$game=$assembly.GetType('GhostifySetup.InstallerTests',$true).GetMethod('Game').Invoke($null,@('shipping-exe'))
$exe=Join-Path $project ('dist\GhostifyOverlay-Setup-'+$info.Version+'.exe')
$report=Join-Path $root 'EXE-result.json'
$backup=Join-Path $root 'EXE-backups'
$arguments='--install "'+$game+'" "'+$report+'" "'+$backup+'"'
$process=Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
$result=Get-Content -LiteralPath $report -Raw -Encoding UTF8 | ConvertFrom-Json
if($process.ExitCode -ne 0 -or !$result.Success){throw ('Shipping EXE install failed: '+$result.Message)}
'PASS shipping EXE installs its embedded payload into a Unicode game fixture'
$data=Join-Path $game 'Mods\DonQuixoteOverlay\UserData'
New-Item -ItemType Directory -Path $data | Out-Null
foreach($name in @('settings.json','layout.json','keyviewer.json','keyviewer-counts.json')){
    [IO.File]::WriteAllText((Join-Path $data $name),('saved user settings '+$name))
    [IO.File]::WriteAllText((Join-Path $data ($name+'.bak')),('saved user backup '+$name))
}
$before=@(Get-ChildItem -LiteralPath $data -File | ForEach-Object {@{Name=$_.Name;Hash=(Get-FileHash -LiteralPath $_.FullName).Hash}})
$updateReport=Join-Path $root 'EXE-update-result.json'
$process=Start-Process -FilePath $exe -ArgumentList ('--install "'+$game+'" "'+$updateReport+'" "'+$backup+'"') -WindowStyle Hidden -PassThru -Wait
$result=Get-Content -LiteralPath $updateReport -Raw -Encoding UTF8 | ConvertFrom-Json
if($process.ExitCode -ne 0 -or !$result.Success){throw ('Shipping EXE update failed: '+$result.Message)}
foreach($file in $before){
    if((Get-FileHash -LiteralPath (Join-Path $data $file.Name)).Hash -ne $file.Hash){throw 'Shipping EXE changed user settings'}
    if((Get-FileHash -LiteralPath (Join-Path $result.Backup ('previous-mod\UserData\'+$file.Name))).Hash -ne $file.Hash){throw 'Shipping EXE backup is incomplete'}
}
'PASS shipping EXE updates and backs up all eight user files without changing them'
$failureReport=Join-Path $root 'EXE-invalid-result.json'
$process=Start-Process -FilePath $exe -ArgumentList ('--install "'+$root+'" "'+$failureReport+'" "'+$backup+'"') -WindowStyle Hidden -PassThru -Wait
$result=Get-Content -LiteralPath $failureReport -Raw -Encoding UTF8 | ConvertFrom-Json
if($process.ExitCode -ne 1 -or $result.Success -or [string]::IsNullOrWhiteSpace($result.Message)){throw 'Shipping EXE invalid-path handling failed'}
'PASS shipping EXE reports invalid paths without opening an error window'
'Fixture: '+$root
