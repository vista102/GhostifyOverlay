param([Parameter(Mandatory=$true)][string]$InputDirectory)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$fonts=[Drawing.Text.PrivateFontCollection]::new()
$fonts.AddFontFile((Join-Path (Split-Path $PSScriptRoot) 'Assets/GmarketSansTTFMedium.ttf'))
foreach($name in @('overlay','keyviewer','colors','presets')){
    $boxes=Get-Content -LiteralPath (Join-Path $InputDirectory ($name+'.json')) -Raw -Encoding UTF8|ConvertFrom-Json
    $bitmap=[Drawing.Bitmap]::new(960,720);$graphics=[Drawing.Graphics]::FromImage($bitmap)
    try{
        $graphics.Clear([Drawing.Color]::White);$graphics.SmoothingMode='AntiAlias';$graphics.TextRenderingHint='AntiAliasGridFit'
        foreach($box in $boxes){
            if($box.W -le 0 -or $box.H -le 0 -or $box.ClipW -le 0 -or $box.ClipH -le 0){continue}
            $state=$graphics.Save();$graphics.SetClip([Drawing.RectangleF]::new($box.ClipX,$box.ClipY,$box.ClipW,$box.ClipH))
            $rect=[Drawing.RectangleF]::new($box.X,$box.Y,$box.W,$box.H)
            try{
                if($box.Kind -in @('Surface','Button','Toggle','Input')){
                    $color=[Drawing.Color]::White
                    if($box.Fill){$hex=$box.Fill.TrimStart('#');$color=[Drawing.Color]::FromArgb([Convert]::ToInt32($hex.Substring(6,2),16),[Convert]::ToInt32($hex.Substring(0,2),16),[Convert]::ToInt32($hex.Substring(2,2),16),[Convert]::ToInt32($hex.Substring(4,2),16))}
                    if($box.Selected){$color=[Drawing.Color]::FromArgb(255,248,224)}
                    $brush=[Drawing.SolidBrush]::new($color);try{$graphics.FillRectangle($brush,$rect)}finally{$brush.Dispose()}
                    if($box.Kind -ne 'Surface'){$pen=[Drawing.Pen]::new([Drawing.Color]::FromArgb(205,205,205));try{$graphics.DrawRectangle($pen,$rect.X,$rect.Y,$rect.Width,$rect.Height)}finally{$pen.Dispose()}}
                    if($box.Kind -eq 'Toggle'){
                        $x=$rect.Right-60;$y=$rect.Top+($rect.Height-26)/2
                        $track=[Drawing.Drawing2D.GraphicsPath]::new();$track.AddArc($x,$y,26,26,90,180);$track.AddArc($x+20,$y,26,26,270,180);$track.CloseFigure()
                        $trackColor=if($box.On){[Drawing.Color]::FromArgb(255,201,57)}else{[Drawing.Color]::FromArgb(214,216,219)}
                        $brush=[Drawing.SolidBrush]::new($trackColor);try{$graphics.FillPath($brush,$track)}finally{$brush.Dispose();$track.Dispose()}
                        $thumbX=if($box.On){$x+23}else{$x+3};$graphics.FillEllipse([Drawing.Brushes]::White,$thumbX,($y+3),20,20)
                    }
                }
                if($box.Text){
                    $size=if($box.Font -gt 0){$box.Font}else{15}
                    $font=[Drawing.Font]::new($fonts.Families[0],[single]$size,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
                    $format=[Drawing.StringFormat]::new();$format.Alignment=if($box.Align -eq 'Center'){'Center'}else{'Near'};$format.LineAlignment='Center';$format.Trimming='EllipsisCharacter'
                    $textColor=[Drawing.Color]::Black
                    if($box.Kind -eq 'Text' -and $box.Fill){$hex=$box.Fill.TrimStart('#');$textColor=[Drawing.Color]::FromArgb([Convert]::ToInt32($hex.Substring(6,2),16),[Convert]::ToInt32($hex.Substring(0,2),16),[Convert]::ToInt32($hex.Substring(2,2),16),[Convert]::ToInt32($hex.Substring(4,2),16))}
                    $textBrush=[Drawing.SolidBrush]::new($textColor)
                    try{$graphics.DrawString($box.Text,$font,$textBrush,$rect,$format)}finally{$font.Dispose();$format.Dispose();$textBrush.Dispose()}
                }
            }finally{$graphics.Restore($state)}
        }
        $path=Join-Path $InputDirectory ($name+'.png');$bitmap.Save($path,[Drawing.Imaging.ImageFormat]::Png);$path
    }finally{$graphics.Dispose();$bitmap.Dispose()}
}
$fonts.Dispose()
