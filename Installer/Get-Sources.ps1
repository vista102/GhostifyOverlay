$installerSources=@(Get-ChildItem -LiteralPath (Join-Path (Split-Path $PSScriptRoot) 'src\Installer') -Filter *.cs -Recurse -File|Sort-Object FullName|ForEach-Object {$_.FullName})
if(!$installerSources.Count){throw 'Installer production sources are missing'}
