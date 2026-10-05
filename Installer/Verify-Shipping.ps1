param([string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice')
$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot
$taskInfo=Get-Content -LiteralPath (Join-Path $taskProject 'Info.json') -Raw -Encoding UTF8|ConvertFrom-Json
$taskExe=Join-Path $taskProject ('dist\GhostifyOverlay-Setup-'+$taskInfo.Version+'.exe')
$taskZip=Join-Path $taskProject ('dist\GhostifyOverlay-'+$taskInfo.Version+'.zip')
$taskRelease=Join-Path $taskProject 'bin\Release\GhostifyOverlay'
$taskWork=Join-Path $taskProject ('Backups\ShippingVerify-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $taskWork -Force|Out-Null
Add-Type -AssemblyName System.IO.Compression
$taskAssembly=[Reflection.Assembly]::LoadFrom($taskExe)
function Read-Resource([string]$name){
    $taskStream=$taskAssembly.GetManifestResourceStream($name);$taskMemory=New-Object IO.MemoryStream
    try{$taskStream.CopyTo($taskMemory);,$taskMemory.ToArray()}finally{$taskMemory.Dispose();$taskStream.Dispose()}
}
function Hash-Bytes([byte[]]$bytes){$taskSha=[Security.Cryptography.SHA256]::Create();try{[BitConverter]::ToString($taskSha.ComputeHash($bytes)).Replace('-','')}finally{$taskSha.Dispose()}}
$taskManifest=[Text.Encoding]::UTF8.GetString((Read-Resource 'Payload.json'))|ConvertFrom-Json
if($taskManifest.Version -ne $taskInfo.Version -or $taskManifest.Files.Count -ne 14){throw 'Shipping manifest version or file count differs'}
$taskEmbedded=Read-Resource 'Payload.zip'
if((Hash-Bytes $taskEmbedded) -ne (Get-FileHash -LiteralPath $taskZip).Hash){throw 'Standalone ZIP differs from the embedded EXE payload'}
$taskMemory=New-Object IO.MemoryStream(,$taskEmbedded)
$taskArchive=New-Object IO.Compression.ZipArchive($taskMemory,[IO.Compression.ZipArchiveMode]::Read)
try{
    if($taskArchive.Entries.Count -ne $taskManifest.Files.Count){throw 'Embedded ZIP entry count differs'}
    foreach($taskFile in $taskManifest.Files){
        if($taskFile.Path -match '(^|/)UserData(/|$)'){throw 'User data was packaged'}
        $taskEntry=$taskArchive.GetEntry('GhostifyOverlay/'+$taskFile.Path)
        if(!$taskEntry){throw ('Missing embedded file: '+$taskFile.Path)}
        $taskStream=$taskEntry.Open();$taskData=New-Object IO.MemoryStream
        try{$taskStream.CopyTo($taskData);$taskHash=Hash-Bytes $taskData.ToArray()}finally{$taskStream.Dispose();$taskData.Dispose()}
        $taskDiskHash=(Get-FileHash -LiteralPath (Join-Path $taskRelease $taskFile.Path)).Hash
        if($taskHash -ne $taskFile.Sha256 -or $taskHash -ne $taskDiskHash){throw ('Shipping hash mismatch: '+$taskFile.Path)}
        'PASS embedded / ZIP / Release SHA256 '+$taskFile.Path
    }
}finally{$taskArchive.Dispose();$taskMemory.Dispose()}
$taskReport=Join-Path $taskWork 'Verify.json'
$taskArguments='--verify "'+$GameDir+'" "'+$taskReport+'"'
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList $taskArguments -WindowStyle Hidden -Wait -PassThru
$taskResult=Get-Content -LiteralPath $taskReport -Raw -Encoding UTF8|ConvertFrom-Json
if($taskProcess.ExitCode -ne 0 -or !$taskResult.VerifiedGameBuild){throw 'Shipping EXE could not verify the installed alpha SDK'}
'PASS shipping EXE read-only verification recognizes installed 3.4.0 alpha'
if(Get-Process -Name 'A Dance of Fire and Ice' -ErrorAction SilentlyContinue){
    $taskFixture=Join-Path $taskWork 'Blocked-game 한글 경로';New-Item -ItemType Directory -Path $taskFixture|Out-Null
    [IO.File]::WriteAllText((Join-Path $taskFixture 'A Dance of Fire and Ice.exe'),'fixture')
    $taskUmm=Join-Path $taskFixture 'A Dance of Fire and Ice_Data\Managed\UnityModManager'
    New-Item -ItemType Directory -Path $taskUmm -Force|Out-Null
    Copy-Item -LiteralPath (Join-Path $GameDir 'A Dance of Fire and Ice_Data\Managed\UnityModManager\UnityModManager.dll') -Destination $taskUmm
    [IO.File]::WriteAllText((Join-Path $taskUmm 'Params.xml'),'<Param><ModParams/></Param>')
    $taskBefore=@(Get-ChildItem -LiteralPath $taskFixture -Recurse -File|ForEach-Object {(Get-FileHash -LiteralPath $_.FullName).Hash})
    $taskReport=Join-Path $taskWork 'Blocked.json'
    $taskArguments='--install "'+$taskFixture+'" "'+$taskReport+'" "'+(Join-Path $taskWork 'Backups')+'"'
    $taskProcess=Start-Process -FilePath $taskExe -ArgumentList $taskArguments -WindowStyle Hidden -Wait -PassThru
    $taskResult=Get-Content -LiteralPath $taskReport -Raw -Encoding UTF8|ConvertFrom-Json
    if($taskProcess.ExitCode -ne 1 -or $taskResult.Success -or $taskResult.Message -notmatch '종료'){throw 'Shipping EXE did not block an active game'}
    $taskAfter=@(Get-ChildItem -LiteralPath $taskFixture -Recurse -File|ForEach-Object {(Get-FileHash -LiteralPath $_.FullName).Hash})
    if(($taskBefore -join ',') -ne ($taskAfter -join ',') -or (Test-Path -LiteralPath (Join-Path $taskFixture 'Mods'))){throw 'Blocked EXE installation modified its fixture'}
    'PASS shipping EXE blocks an active game and leaves its Unicode fixture byte-for-byte unchanged'
}else{'Active-game block check skipped: no user game is running.'}
'Shipping verification finished; actual game installation was not modified.'
