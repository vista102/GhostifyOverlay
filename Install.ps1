param([string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice')
$ErrorActionPreference = 'Stop'
$projectDir = $PSScriptRoot
$sourceDir = (Resolve-Path -LiteralPath (Join-Path $projectDir 'bin\Release\GhostifyOverlay')).Path
$gameRoot = (Resolve-Path -LiteralPath $GameDir).Path
$modsRoot = (Resolve-Path -LiteralPath (Join-Path $gameRoot 'Mods')).Path
$targetDir = [IO.Path]::GetFullPath((Join-Path $modsRoot 'DonQuixoteOverlay'))
$stageDir = [IO.Path]::GetFullPath((Join-Path $modsRoot ('.DonQuixoteOverlay-install-'+[Guid]::NewGuid().ToString('N'))))
foreach ($path in @($targetDir,$stageDir)) {
    if (!(Split-Path $path -Parent).Equals($modsRoot,[StringComparison]::OrdinalIgnoreCase)) { throw 'Install target is outside the game Mods directory' }
}
if ((Get-Item -LiteralPath $modsRoot).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Mods directory is redirected; inspect before installing' }
if (@(Get-Process -Name 'A Dance of Fire and Ice' -ErrorAction SilentlyContinue).Count -gt 0) { throw 'Close A Dance of Fire and Ice before installing' }
if (Test-Path -LiteralPath $targetDir) { throw 'Target already exists; inspect it before updating to preserve user data' }
$info = Get-Content -LiteralPath (Join-Path $sourceDir 'Info.json') -Raw | ConvertFrom-Json
$expectedInfo = Get-Content -LiteralPath (Join-Path $projectDir 'Info.json') -Raw | ConvertFrom-Json
if ($info.Id -ne 'DonQuixoteOverlay' -or $info.Version -ne $expectedInfo.Version) { throw 'Unexpected mod identity or version' }
$ummDir = Join-Path $gameRoot 'A Dance of Fire and Ice_Data\Managed\UnityModManager'
$ummVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $ummDir 'UnityModManager.dll')).Version
if ($ummVersion -lt [version]$info.ManagerVersion) { throw 'Unity Mod Manager is older than the build requirement' }
$paramsPath = Join-Path $ummDir 'Params.xml'
$params = [xml]::new()
$params.PreserveWhitespace = $true
$params.Load($paramsPath)
if ($null -eq $params.Param.ModParams) { throw 'Unexpected Unity Mod Manager settings schema' }
$files = @(Get-ChildItem -LiteralPath $sourceDir -Recurse -File)
if ($files.Count -ne 12) { throw 'Unexpected Release contents' }
$hashes = foreach ($file in $files) {
    $relative = $file.FullName.Substring($sourceDir.Length).TrimStart('\','/')
    if ($relative.StartsWith('UserData',[StringComparison]::OrdinalIgnoreCase)) { throw 'Release contains user data' }
    [pscustomobject]@{ Path=$relative; Sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash }
}
$koreaTime = [TimeZoneInfo]::ConvertTimeFromUtc([DateTime]::UtcNow,[TimeZoneInfo]::FindSystemTimeZoneById('Korea Standard Time'))
$backupDir = Join-Path $projectDir ('Backups\Install-'+$koreaTime.ToString('yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $backupDir | Out-Null
$paramsBackup = Join-Path $backupDir 'UMM-Params.xml'
Copy-Item -LiteralPath $paramsPath -Destination $paramsBackup
New-Item -ItemType Directory -Path $stageDir | Out-Null
foreach ($entry in $hashes) {
    $destination = Join-Path $stageDir $entry.Path
    New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $sourceDir $entry.Path) -Destination $destination
    if ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -ne $entry.Sha256) { throw ('Staged file mismatch: '+$entry.Path) }
}
# Both absolute paths were checked against the resolved Mods directory above.
Move-Item -LiteralPath $stageDir -Destination $targetDir
try {
    $old = $params.SelectSingleNode('/Param/ModParams/Mod[@Id="DonQuixote"]')
    if ($old) { $old.SetAttribute('Enabled','false') }
    $overlay = $params.SelectSingleNode('/Param/ModParams/Mod[@Id="DonQuixoteOverlay"]')
    if (!$overlay) {
        $overlay = $params.CreateElement('Mod')
        $overlay.SetAttribute('Id','DonQuixoteOverlay')
        $hotkey = $params.CreateElement('Hotkey')
        $code = $params.CreateElement('keyCode'); $code.InnerText='None'
        $modifiers = $params.CreateElement('modifiers'); $modifiers.InnerText='0'
        $hotkey.AppendChild($code) | Out-Null
        $hotkey.AppendChild($modifiers) | Out-Null
        $overlay.AppendChild($hotkey) | Out-Null
        $params.Param.ModParams.AppendChild($overlay) | Out-Null
    }
    $overlay.SetAttribute('Enabled','true')
    $params.Save($paramsPath)
} catch {
    Copy-Item -LiteralPath $paramsBackup -Destination $paramsPath -Force
    throw
}
foreach ($entry in $hashes) {
    if ((Get-FileHash -LiteralPath (Join-Path $targetDir $entry.Path) -Algorithm SHA256).Hash -ne $entry.Sha256) { throw ('Installed file mismatch: '+$entry.Path) }
}
$verified = [xml](Get-Content -LiteralPath $paramsPath -Raw)
if ($verified.SelectSingleNode('/Param/ModParams/Mod[@Id="DonQuixoteOverlay"]/@Enabled').Value -ne 'true') { throw 'Overlay was not enabled' }
$oldEnabled = $verified.SelectSingleNode('/Param/ModParams/Mod[@Id="DonQuixote"]/@Enabled')
if ($oldEnabled -and $oldEnabled.Value -ne 'false') { throw 'Original mod is still enabled' }
$receipt = [pscustomobject]@{
    InstalledAtKst=$koreaTime.ToString('yyyy-MM-dd HH:mm:ss'); Version=$info.Version
    Source=$sourceDir; Target=$targetDir; UmmVersion=$ummVersion.ToString(); UmmSettingsBackup=$paramsBackup
    OverlayEnabled=$true; OriginalEnabled=$false; Files=$hashes
}
$receipt | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $backupDir 'installation.json') -Encoding UTF8
'Installed Ghostify Overlay '+$info.Version+'; all '+$hashes.Count+' file hashes match Release.'
'UMM: Ghostify Overlay ON, DonQuixote OFF; other mod settings preserved.'
'Settings backup: '+$paramsBackup
'Target: '+$targetDir
