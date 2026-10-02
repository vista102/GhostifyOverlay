param([string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice')
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$csc='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$fixture=Join-Path $PSScriptRoot ('effects-fixture-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixture | Out-Null
# Extract the production model instead of maintaining a test copy of its defaults.
$settings=[IO.File]::ReadAllText((Join-Path $project 'Settings.cs'))
$begin=$settings.IndexOf('    public sealed class EffectsSettings')
$end=$settings.IndexOf('    [Serializable]', $begin)
if($begin -lt 0 -or $end -le $begin){throw 'Production EffectsSettings section missing'}
$modelPath=Join-Path $fixture 'EffectsSettings.cs'
[IO.File]::WriteAllText($modelPath, 'using System.Collections.Generic; namespace DonQuixoteOverlay {'+$settings.Substring($begin,$end-$begin)+'}')
$output=Join-Path $fixture 'EffectHarness.dll'
$harmony=Join-Path $GameDir 'A Dance of Fire and Ice_Data\Managed\UnityModManager\0Harmony.dll'
[Reflection.Assembly]::LoadFrom($harmony)|Out-Null
$sources=@('GameplayPatches.cs','EffectRestoration.cs','EffectLifetime.cs','OwnedStateRegistry.cs','RuntimeStatus.cs') | ForEach-Object {Join-Path $project $_}
$sources+=@((Join-Path $project 'SettingsWindow.Effects.cs'),$modelPath,(Join-Path $PSScriptRoot 'EffectHarness.cs'),(Join-Path $PSScriptRoot 'EffectUiHarness.cs'))
& $csc /nologo /target:library /langversion:latest "/out:$output" /r:System.Core.dll "/r:$harmony" $sources
if($LASTEXITCODE -ne 0){throw 'Effect harness compilation failed'}
$harness=[Reflection.Assembly]::LoadFrom($output)
$results=$harness.GetType('DonQuixoteOverlay.EffectHarness',$true).GetMethod('Run').Invoke($null,@())
$results
'Effect production-code assertions passed: '+$results.Count
