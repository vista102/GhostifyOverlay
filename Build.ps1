param([string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice')
$ErrorActionPreference='Stop'
$msbuild='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe'
& $msbuild (Join-Path $PSScriptRoot 'GhostifyOverlay.csproj') /t:Rebuild /p:Configuration=Release "/p:GameDir=$GameDir" /p:FrameworkPathOverride=C:\Windows\Microsoft.NET\Framework64\v4.0.30319 /v:minimal /nologo /fl "/flp:logfile=$PSScriptRoot\build.log;verbosity=minimal"
if ($LASTEXITCODE -ne 0) { throw 'Release build failed' }
