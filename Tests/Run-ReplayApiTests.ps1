param([string]$Compiler='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe')
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('GhostifyOverlay-ReplayApi-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixture|Out-Null
try {
    $harness=Join-Path $fixture 'ReplayApiBridgeTests.dll'
    $sources=@(
        (Join-Path $project 'src/Mod/Application/Replays/ReplayRunState.cs'),
        (Join-Path $project 'src/Mod/Integration/TUFReplay/ReplayApiBridge.cs'),
        (Join-Path $PSScriptRoot 'ReplayApiBridgeHarness.cs')
    )
    & $Compiler /nologo /target:library /langversion:latest /r:System.Core.dll "/out:$harness" $sources
    if($LASTEXITCODE -ne 0){throw 'ReplayApi bridge tests could not compile'}
    $assembly=[Reflection.Assembly]::Load([IO.File]::ReadAllBytes($harness))
    $results=$assembly.GetType('ReplayApiBridgeHarness',$true).GetMethod('Run').Invoke($null,@())
    $results
    'ReplayApi bridge assertions passed: '+$results.Count
} finally {
    Remove-Item -LiteralPath $fixture -Recurse -Force
}
