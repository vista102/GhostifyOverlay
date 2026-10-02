param([string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice',[switch]$SkipModBuild)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
if(!$SkipModBuild){& (Join-Path $project 'Build.ps1') -GameDir $GameDir}
$info=Get-Content -LiteralPath (Join-Path $project 'Info.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$release=(Resolve-Path -LiteralPath (Join-Path $project 'bin\Release\GhostifyOverlay')).Path
$output=Join-Path $project 'dist'
$work=Join-Path $PSScriptRoot 'obj'
New-Item -ItemType Directory -Path $output,$work -Force | Out-Null
$files=@(Get-ChildItem -LiteralPath $release -Recurse -File)
if($files.Count -ne 12){throw 'Unexpected mod Release files'}
$manifest=@{Version=$info.Version;Files=@($files | ForEach-Object {
    $relative=$_.FullName.Substring($release.Length).TrimStart('\','/').Replace('\','/')
    if($relative.StartsWith('UserData',[StringComparison]::OrdinalIgnoreCase)){throw 'UserData cannot be packaged'}
    @{Path=$relative;Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}
})}
$manifestPath=Join-Path $work 'Payload.json'
[IO.File]::WriteAllText($manifestPath,($manifest | ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false))
$payload=Join-Path $work ('Payload-'+[Guid]::NewGuid().ToString('N')+'.zip')
Add-Type -AssemblyName System.IO.Compression
$payloadStream=[IO.File]::Create($payload)
$archive=[IO.Compression.ZipArchive]::new($payloadStream,[IO.Compression.ZipArchiveMode]::Create,$false)
try{
    foreach($file in $files){
        $relative=$file.FullName.Substring($release.Length).TrimStart('\','/').Replace('\','/')
        $entry=$archive.CreateEntry('GhostifyOverlay/'+$relative,[IO.Compression.CompressionLevel]::Optimal)
        $entry.LastWriteTime=[DateTimeOffset]::new(2000,1,1,0,0,0,[TimeSpan]::Zero)
        $input=[IO.File]::OpenRead($file.FullName);$destination=$entry.Open()
        try{$input.CopyTo($destination)}finally{$input.Dispose();$destination.Dispose()}
    }
}finally{$archive.Dispose();$payloadStream.Dispose()}
# ICO is a format conversion of the supplied image; the PNG itself is unchanged.
Add-Type -AssemblyName System.Drawing
$mascot=Join-Path $PSScriptRoot 'Assets\Mascot.png'
$icon=Join-Path $PSScriptRoot 'Assets\Ghostify.ico'
$source=[Drawing.Bitmap]::new($mascot)
$images=@()
try{
    foreach($size in @(16,24,32,48,64,128,256)){
        $bitmap=[Drawing.Bitmap]::new($size,$size)
        $graphics=[Drawing.Graphics]::FromImage($bitmap)
        $memory=[IO.MemoryStream]::new()
        try{
            $graphics.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.Clear([Drawing.Color]::White)
            $graphics.DrawImage($source,0,0,$size,$size)
            $bitmap.Save($memory,[Drawing.Imaging.ImageFormat]::Png)
            $images+=@{Size=$size;Bytes=$memory.ToArray()}
        }finally{$graphics.Dispose();$bitmap.Dispose();$memory.Dispose()}
    }
}finally{$source.Dispose()}
$iconStream=[IO.File]::Create($icon);$writer=[IO.BinaryWriter]::new($iconStream)
try{
    $writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$images.Count)
    $offset=6+16*$images.Count
    foreach($image in $images){
        $dimension=if($image.Size -eq 256){0}else{$image.Size}
        $writer.Write([byte]$dimension);$writer.Write([byte]$dimension);$writer.Write([byte]0);$writer.Write([byte]0)
        $writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$image.Bytes.Length);$writer.Write([uint32]$offset)
        $offset+=$image.Bytes.Length
    }
    foreach($image in $images){$writer.Write([byte[]]$image.Bytes)}
}finally{$writer.Dispose();$iconStream.Dispose()}
$csc='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
if(!(Test-Path -LiteralPath $csc)){throw 'Visual Studio Build Tools C# compiler is required'}
$exe=Join-Path $output ('GhostifyOverlay-Setup-'+$info.Version+'.exe')
$assemblyInfo=Join-Path $work 'AssemblyInfo.cs'
$assemblyVersion=$info.Version+'.0'
[IO.File]::WriteAllText($assemblyInfo,('[assembly: System.Reflection.AssemblyTitle("Ghostify Overlay Setup")][assembly: System.Reflection.AssemblyProduct("Ghostify Overlay")][assembly: System.Reflection.AssemblyVersion("'+$assemblyVersion+'")][assembly: System.Reflection.AssemblyFileVersion("'+$assemblyVersion+'")]'),[Text.UTF8Encoding]::new($false))
$appManifest=Join-Path $work 'app.manifest'
$manifestSource=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'app.manifest'))
[IO.File]::WriteAllText($appManifest,([regex]::Replace($manifestSource,'version="\d+\.\d+\.\d+\.\d+"','version="'+$assemblyVersion+'"')),[Text.UTF8Encoding]::new($false))
$arguments=@('/nologo','/target:winexe','/platform:anycpu','/optimize+','/deterministic+','/langversion:latest',('/out:'+$exe),('/win32icon:'+$icon),('/win32manifest:'+$appManifest),
 '/r:System.dll','/r:System.Core.dll','/r:System.Drawing.dll','/r:System.Windows.Forms.dll','/r:System.Xml.dll','/r:System.Web.Extensions.dll','/r:System.IO.Compression.dll','/r:System.IO.Compression.FileSystem.dll',
 ('/resource:'+$payload+',Payload.zip'),('/resource:'+$manifestPath+',Payload.json'),('/resource:'+$mascot+',Mascot.png'),('/resource:'+$icon+',Brand.ico'),
 $assemblyInfo,(Join-Path $PSScriptRoot 'InstallerEngine.cs'),(Join-Path $PSScriptRoot 'Program.cs'))
& $csc $arguments
if($LASTEXITCODE -ne 0){throw 'Installer compilation failed'}
$zip=Join-Path $output ('GhostifyOverlay-'+$info.Version+'.zip')
Copy-Item -LiteralPath $payload -Destination $zip -Force
$checksums=@($exe,$zip) | ForEach-Object {(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+[IO.Path]::GetFileName($_)}
[IO.File]::WriteAllLines((Join-Path $output 'SHA256SUMS.txt'),$checksums,[Text.UTF8Encoding]::new($false))
'Installer: '+$exe
'SHA256: '+(Get-FileHash -LiteralPath $exe).Hash
