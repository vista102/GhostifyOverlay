param([string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice')
$ErrorActionPreference = 'Stop'
$gameRoot = (Resolve-Path -LiteralPath $GameDir).Path
$modsRoot = (Resolve-Path -LiteralPath (Join-Path $gameRoot 'Mods')).Path
$target = (Resolve-Path -LiteralPath (Join-Path $modsRoot 'DonQuixoteOverlay')).Path
$source = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot 'bin\Release\GhostifyOverlay')).Path
if (!(Split-Path $target -Parent).Equals($modsRoot,[StringComparison]::OrdinalIgnoreCase)) {throw 'Unexpected installed mod path'}
if ((Get-Item -LiteralPath $target).Attributes -band [IO.FileAttributes]::ReparsePoint) {throw 'Installed mod is redirected'}
if (@(Get-Process -Name 'A Dance of Fire and Ice' -ErrorAction SilentlyContinue).Count -gt 0) {throw 'Close the game before replacing the loaded assembly'}
$installedInfo = Get-Content -LiteralPath (Join-Path $target 'Info.json') -Raw | ConvertFrom-Json
$newInfo = Get-Content -LiteralPath (Join-Path $source 'Info.json') -Raw | ConvertFrom-Json
$expectedInfo = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Info.json') -Raw | ConvertFrom-Json
if ($installedInfo.Id -ne 'DonQuixoteOverlay' -or $newInfo.Id -ne 'DonQuixoteOverlay' -or $newInfo.Version -ne $expectedInfo.Version) {throw 'Unexpected mod identity/version'}
$files = @(Get-ChildItem -LiteralPath $source -Recurse -File)
if ($files.Count -ne 12) {throw 'Unexpected Release contents'}
$userData = Join-Path $target 'UserData'
$dataHashes = @()
if (Test-Path -LiteralPath $userData) {
    $dataHashes = @(Get-ChildItem -LiteralPath $userData -Recurse -File | ForEach-Object { [pscustomobject]@{Path=$_.FullName;Hash=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash} })
}
$koreaTime = [TimeZoneInfo]::ConvertTimeFromUtc([DateTime]::UtcNow,[TimeZoneInfo]::FindSystemTimeZoneById('Korea Standard Time'))
$backup = Join-Path $PSScriptRoot ('Backups\Hotfix-'+$koreaTime.ToString('yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $backup | Out-Null
$paramsPath = Join-Path $gameRoot 'A Dance of Fire and Ice_Data\Managed\UnityModManager\Params.xml'
Copy-Item -LiteralPath $paramsPath -Destination (Join-Path $backup 'UMM-Params.xml')
# Preserve the previous assembly when its filename changes with the project name.
foreach ($name in @($installedInfo.AssemblyName, [IO.Path]::ChangeExtension($installedInfo.AssemblyName, '.pdb'))) {
    $previousFile = Join-Path $target $name
    if (Test-Path -LiteralPath $previousFile) {
        $previousBackup = Join-Path $backup ('PreviousBuild\'+$name)
        New-Item -ItemType Directory -Path (Split-Path $previousBackup -Parent) -Force | Out-Null
        Copy-Item -LiteralPath $previousFile -Destination $previousBackup
    }
}
$hashes = @()
foreach ($file in $files) {
    $relative = $file.FullName.Substring($source.Length).TrimStart('\','/')
    $destination = [IO.Path]::GetFullPath((Join-Path $target $relative))
    if (!$destination.StartsWith($target+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or $relative.StartsWith('UserData',[StringComparison]::OrdinalIgnoreCase)) {throw 'Release file is outside allowed install paths'}
    $hashes += [pscustomobject]@{Path=$relative;Sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash}
    $backupFile = Join-Path $backup ('PreviousBuild\'+$relative)
    if (Test-Path -LiteralPath $destination) {
        New-Item -ItemType Directory -Path (Split-Path $backupFile -Parent) -Force | Out-Null
        Copy-Item -LiteralPath $destination -Destination $backupFile
    }
}
foreach ($file in $files) {
    $relative = $file.FullName.Substring($source.Length).TrimStart('\','/')
    $destination = Join-Path $target $relative
    New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
}
foreach ($entry in $hashes) {
    if ((Get-FileHash -LiteralPath (Join-Path $target $entry.Path) -Algorithm SHA256).Hash -ne $entry.Sha256) {throw ('Installed hash mismatch: '+$entry.Path)}
}
foreach ($entry in $dataHashes) {
    if ((Get-FileHash -LiteralPath $entry.Path -Algorithm SHA256).Hash -ne $entry.Hash) {throw 'Existing user data changed'}
}
$params = [xml]::new(); $params.PreserveWhitespace=$true; $params.Load($paramsPath)
$overlay = $params.SelectSingleNode('/Param/ModParams/Mod[@Id="DonQuixoteOverlay"]')
if (!$overlay) {throw 'Installed mod UMM entry is missing'}
$overlay.SetAttribute('Enabled','true')
$original = $params.SelectSingleNode('/Param/ModParams/Mod[@Id="DonQuixote"]')
if ($original) {$original.SetAttribute('Enabled','false')}
$params.Save($paramsPath)
$receipt = [pscustomobject]@{
    UpdatedAtKst=$koreaTime.ToString('yyyy-MM-dd HH:mm:ss'); PreviousVersion=$installedInfo.Version; Version=$newInfo.Version
    Target=$target; Backup=$backup; Files=$hashes; PreservedUserFiles=$dataHashes.Count
}
$receipt | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $backup 'installation.json') -Encoding UTF8
'Updated '+$installedInfo.Version+' -> '+$newInfo.Version+'; '+$hashes.Count+' installed file hashes verified; '+$dataHashes.Count+' user files preserved.'
'UMM: Ghostify Overlay ON, DonQuixote OFF.'
'Backup: '+$backup
