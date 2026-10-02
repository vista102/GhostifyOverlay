$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$root=Join-Path $PSScriptRoot ('test-fixture-ui-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root | Out-Null
$csc='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$info=Get-Content -LiteralPath (Join-Path $project 'Info.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$payload=Join-Path $project ('dist\GhostifyOverlay-'+$info.Version+'.zip')
$exe=Join-Path $root 'InstallerUiHarness.exe'
& $csc /nologo /target:exe /langversion:latest /main:GhostifySetup.InstallerUiTests "/out:$exe" /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll /r:System.Xml.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll "/resource:$payload,Payload.zip" ('/resource:'+(Join-Path $PSScriptRoot 'obj\Payload.json')+',Payload.json') ('/resource:'+(Join-Path $PSScriptRoot 'Assets\Mascot.png')+',Mascot.png') ('/resource:'+(Join-Path $PSScriptRoot 'Assets\Ghostify.ico')+',Brand.ico') (Join-Path $PSScriptRoot 'Program.cs') (Join-Path $PSScriptRoot 'InstallerEngine.cs') (Join-Path $PSScriptRoot 'InstallerUiTests.cs')
if($LASTEXITCODE -ne 0){throw 'Installer UI test compilation failed'}
# Exercise our actual UI class and Windows Forms message loop, without writing to the game.
& $exe $root
if($LASTEXITCODE -ne 0){throw 'Installer UI tests failed'}
'Fixture: '+$root
