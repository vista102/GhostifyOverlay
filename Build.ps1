param([string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice', [string]$ModManagerDir = '')
$ErrorActionPreference='Stop'
if (!$ModManagerDir) { $ModManagerDir=Join-Path $GameDir 'A Dance of Fire and Ice_Data\Managed\UnityModManager' }
$info=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Info.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$compatibility=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'GameCompatibility.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($info.ManagerVersion -ne $compatibility.MinimumModManager) { throw 'Mod and installer UMM requirements differ' }
$ummVersion=[Reflection.AssemblyName]::GetAssemblyName((Join-Path $ModManagerDir 'UnityModManager.dll')).Version
if ($ummVersion.ToString(3) -ne $info.ManagerVersion) { throw ('Build against UMM '+$info.ManagerVersion+' using -ModManagerDir; found '+$ummVersion) }
$msbuild='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe'
& $msbuild (Join-Path $PSScriptRoot 'GhostifyOverlay.csproj') /t:Rebuild /p:Configuration=Release "/p:GameDir=$GameDir" "/p:ModManagerDir=$ModManagerDir" /p:FrameworkPathOverride=C:\Windows\Microsoft.NET\Framework64\v4.0.30319 /v:minimal /nologo /fl "/flp:logfile=$PSScriptRoot\build.log;verbosity=minimal"
if ($LASTEXITCODE -ne 0) { throw 'Release build failed' }
