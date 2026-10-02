$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$source=Get-Content -LiteralPath (Join-Path (Split-Path $PSScriptRoot) 'UiComponents.cs') -Raw -Encoding UTF8
# Preview the actual production pixel generator and dimensions without starting Unity.
$method=[regex]::Match($source,'private static Color\[\] SwitchPixels\(int width, int height\) \{[\s\S]*?return pixels;\s*\}').Value
$constants=[regex]::Match($source,'private const int SwitchWidth[^;]+;').Value+[regex]::Match($source,'private const float SwitchInset[^;]+;').Value+[regex]::Match($source,'internal const float SwitchTravel[^;]+;').Value
if(!$method -or !$constants){throw 'Production switch geometry missing'}
$header=@'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public struct Color { public float a; public Color(float r,float g,float b,float alpha) {a=alpha;} }
public static class Mathf { public static float Max(float a,float b){return Math.Max(a,b);} public static float Abs(float a){return Math.Abs(a);} public static float Sqrt(float a){return (float)Math.Sqrt(a);} public static float Clamp01(float a){return Math.Max(0,Math.Min(1,a));} }
public static class GhostifySwitchPreview {
'@
$renderer=@'
static Bitmap Raster(int width,int height,System.Drawing.Color tint){
 var pixels=SwitchPixels(width,height);var bitmap=new Bitmap(width,height,PixelFormat.Format32bppArgb);
 for(int y=0;y<height;y++)for(int x=0;x<width;x++)bitmap.SetPixel(x,height-y-1,System.Drawing.Color.FromArgb((int)Math.Round(pixels[y*width+x].a*tint.A),tint.R,tint.G,tint.B));
 return bitmap;
}
public static void Save(string path){
 using(var output=new Bitmap(620,170))using(var g=Graphics.FromImage(output))using(var font=new Font("Segoe UI",13)){
  g.Clear(System.Drawing.Color.FromArgb(250,250,250));g.InterpolationMode=InterpolationMode.HighQualityBicubic;
  for(int i=0;i<2;i++){
   bool enabled=i==1;float x=60+310*i,y=47,s=3;
   using(var track=Raster(SwitchWidth*4,SwitchHeight*4,enabled?System.Drawing.Color.FromArgb(255,202,58):System.Drawing.Color.FromArgb(214,216,219)))
   using(var thumb=Raster(SwitchThumbSize*4,SwitchThumbSize*4,System.Drawing.Color.White))
   using(var shadow=Raster(SwitchThumbSize*4,SwitchThumbSize*4,System.Drawing.Color.FromArgb(46,0,0,0))){
    g.DrawImage(track,x,y,SwitchWidth*s,SwitchHeight*s);
    float tx=x+(SwitchWidth*.5f+(enabled?SwitchTravel:-SwitchTravel)-SwitchThumbSize*.5f)*s,ty=y+SwitchInset*s;
    g.DrawImage(shadow,tx,ty+s,SwitchThumbSize*s,SwitchThumbSize*s);
    g.DrawImage(thumb,tx,ty,SwitchThumbSize*s,SwitchThumbSize*s);
   }
   g.DrawString(enabled?"ON":"OFF",font,Brushes.DimGray,x,15);
  }
  output.Save(path,ImageFormat.Png);
 }
}
}
'@
Add-Type -TypeDefinition ($header+$constants+$method+$renderer) -ReferencedAssemblies System.Drawing.dll
[GhostifySwitchPreview]::Save((Join-Path $PSScriptRoot 'Toggle-0.2.8.png'))
'Preview rendered from production SwitchPixels and size/position constants; not a Unity screenshot.'
