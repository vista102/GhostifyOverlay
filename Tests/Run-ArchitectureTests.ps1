$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$managed='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice\A Dance of Fire and Ice_Data\Managed'
$json=Join-Path $managed 'Newtonsoft.Json.dll'
[Reflection.Assembly]::LoadFrom($json)|Out-Null
$fixture=Join-Path $project ('Backups\ArchitectureTests-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixture -Force|Out-Null
$sources=@(Get-ChildItem -LiteralPath (Join-Path $project 'src/Mod/Application') -Filter *.cs -Recurse -File|ForEach-Object FullName)
$sources+=@('IJsonStorage','AtomicJsonStorage','JsonSettingsRepository')|ForEach-Object{Join-Path $project ('src/Mod/Infrastructure/Persistence/'+$_+'.cs')}
$sources+=Join-Path $PSScriptRoot 'ArchitectureServiceTests.cs'
$compiler='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$harness=Join-Path $fixture 'ArchitectureTests.dll'
& $compiler /nologo /target:library /langversion:latest /r:System.Core.dll ('/r:'+(Join-Path $managed 'netstandard.dll')) "/r:$json" "/out:$harness" $sources
if($LASTEXITCODE -ne 0){throw 'Architecture service tests could not compile'}
$assembly=[Reflection.Assembly]::LoadFrom($harness)
$arguments=[object[]]@($fixture)
$arguments[0]=$arguments[0].PSObject.BaseObject
$results=$assembly.GetType('ArchitectureServiceTests',$true).GetMethod('Run').Invoke($null,$arguments)
$results
'Architecture service assertions passed: '+$results.Count
if(@($assembly.GetReferencedAssemblies().Name|Where-Object {$_ -match 'Unity|Harmony|Windows.Forms'}).Count){throw 'Application services depend on a runtime UI or patch engine'}
'PASS application services run without Unity, Harmony or Windows Forms'
