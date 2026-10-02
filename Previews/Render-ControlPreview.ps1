$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$project=Split-Path $PSScriptRoot
$source=Get-Content -LiteralPath (Join-Path $project 'UiComponents.cs') -Raw -Encoding UTF8
# Render production alpha masks, including the fill and border's shared contour.
$methods=''
foreach($name in @('RoundedPixels','ClosePixels','SwitchPixels')){
    $method=[regex]::Match($source,'private static Color\[\] '+$name+'\([^)]*\) \{[\s\S]*?return pixels;\s*\}').Value
    if(!$method){throw ('Production pixel generator missing: '+$name)}
    $methods+=$method
}
$constants=[regex]::Match($source,'private const int SwitchWidth[^;]+;').Value+[regex]::Match($source,'private const float SwitchInset[^;]+;').Value+[regex]::Match($source,'internal const float SwitchTravel[^;]+;').Value
$header=@'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public struct Color { public float a; public Color(float r,float g,float b,float alpha) {a=alpha;} }
public static class Mathf {
 public static float Max(float a,float b){return Math.Max(a,b);}
 public static float Min(float a,float b){return Math.Min(a,b);}
 public static float Abs(float a){return Math.Abs(a);}
 public static float Sqrt(float a){return (float)Math.Sqrt(a);}
 public static float Clamp01(float a){return Math.Max(0,Math.Min(1,a));}
}
public static class GhostifyControlPreview {
'@
$renderer=@'
static Bitmap Raster(Color[] pixels,int width,int height,System.Drawing.Color tint){
 var bitmap=new Bitmap(width,height,PixelFormat.Format32bppArgb);
 for(int y=0;y<height;y++)for(int x=0;x<width;x++)bitmap.SetPixel(x,height-y-1,System.Drawing.Color.FromArgb((int)Math.Round(pixels[y*width+x].a*tint.A),tint.R,tint.G,tint.B));
 return bitmap;
}
static void Slice(Graphics g,Bitmap image,RectangleF target,float radius){
 float[] from={0,radius,64-radius,64};
 float[] toX={target.Left,target.Left+radius*1.5f,target.Right-radius*1.5f,target.Right};
 float[] toY={target.Top,target.Top+radius*1.5f,target.Bottom-radius*1.5f,target.Bottom};
 using(var attributes=new ImageAttributes()){
  attributes.SetWrapMode(WrapMode.Clamp);
  for(int y=0;y<3;y++)for(int x=0;x<3;x++){
   var destination=Rectangle.FromLTRB((int)Math.Round(toX[x]),(int)Math.Round(toY[y]),(int)Math.Round(toX[x+1]),(int)Math.Round(toY[y+1]));
   g.DrawImage(image,destination,from[x],from[y],from[x+1]-from[x],from[y+1]-from[y],GraphicsUnit.Pixel,attributes);
  }
 }
}
public static void Save(string path,string fontPath){
 using(var output=new Bitmap(960,274))using(var g=Graphics.FromImage(output))using(var fonts=new System.Drawing.Text.PrivateFontCollection()){
  fonts.AddFontFile(fontPath);
  g.Clear(System.Drawing.Color.FromArgb(250,250,250));g.InterpolationMode=InterpolationMode.HighQualityBicubic;
  using(var title=new Font(fonts.Families[0],23))using(var label=new Font(fonts.Families[0],19))using(var ink=new SolidBrush(System.Drawing.Color.FromArgb(41,41,41))){
   g.DrawString("Ghostify Overlay",title,ink,32,20);
   using(var close=Raster(ClosePixels(96),96,96,System.Drawing.Color.FromArgb(85,85,85)))g.DrawImage(close,895,17,36,36);
   for(int i=0;i<2;i++){
    bool enabled=i==1;var rect=new RectangleF(32,80+82*i,896,66);float s=1.5f;
    var fill=enabled?System.Drawing.Color.FromArgb(255,248,224):System.Drawing.Color.White;
    var border=enabled?System.Drawing.Color.FromArgb(153,255,202,58):System.Drawing.Color.FromArgb(56,136,136,136);
    using(var background=Raster(RoundedPixels(64,9,false),64,64,fill))using(var stroke=Raster(RoundedPixels(64,9,true),64,64,border)){
     Slice(g,background,rect,9);Slice(g,stroke,rect,9);
    }
    g.DrawString(enabled?"Overlay enabled":"Overlay disabled",label,ink,rect.Left+24,rect.Top+17);
    float tx=rect.Right-(SwitchRightMargin+SwitchWidth)*s,ty=rect.Top+(rect.Height-SwitchHeight*s)*.5f;
    using(var track=Raster(SwitchPixels(SwitchWidth*4,SwitchHeight*4),SwitchWidth*4,SwitchHeight*4,enabled?System.Drawing.Color.FromArgb(255,202,58):System.Drawing.Color.FromArgb(214,216,219)))
    using(var thumb=Raster(SwitchPixels(SwitchThumbSize*4,SwitchThumbSize*4),SwitchThumbSize*4,SwitchThumbSize*4,System.Drawing.Color.White))
    using(var shadow=Raster(SwitchPixels(SwitchThumbSize*4,SwitchThumbSize*4),SwitchThumbSize*4,SwitchThumbSize*4,System.Drawing.Color.FromArgb(46,0,0,0))){
     g.DrawImage(track,tx,ty,SwitchWidth*s,SwitchHeight*s);
     float x=tx+(SwitchWidth*.5f+(enabled?SwitchTravel:-SwitchTravel)-SwitchThumbSize*.5f)*s,y=ty+SwitchInset*s;
     g.DrawImage(shadow,x,y+s,SwitchThumbSize*s,SwitchThumbSize*s);g.DrawImage(thumb,x,y,SwitchThumbSize*s,SwitchThumbSize*s);
    }
   }
  }
  output.Save(path,ImageFormat.Png);
 }
}
}
'@
Add-Type -TypeDefinition ($header+$constants+$methods+$renderer) -ReferencedAssemblies System.Drawing.dll
[GhostifyControlPreview]::Save((Join-Path $PSScriptRoot 'Controls-0.2.9.png'),(Join-Path $project 'Assets\GoogleSans-Regular.ttf'))
'Production shape preview rendered; not a Unity screenshot.'
