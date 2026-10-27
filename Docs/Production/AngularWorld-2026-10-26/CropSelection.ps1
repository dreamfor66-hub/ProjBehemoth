Add-Type -AssemblyName System.Drawing
$drawingReferences=[AppDomain]::CurrentDomain.GetAssemblies() | Where-Object {$_.GetName().Name -match 'System.Drawing|System.Private.Windows|System.Runtime$'} | Select-Object -ExpandProperty Location
Add-Type -ReferencedAssemblies $drawingReferences -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public static class UpperBodySelection {
 public static void Crop(string input,string output,int x,int y,int w,int h) {
  using(var source=new Bitmap(input)) using(var canvas=new Bitmap(1536,896,PixelFormat.Format32bppArgb)) using(var g=Graphics.FromImage(canvas)) {
   g.Clear(Color.Transparent);g.CompositingMode=CompositingMode.SourceCopy;
   g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
   g.SetClip(new Rectangle(820,0,716,748));
   float scale=Math.Max(716f/w,736f/h);
   var destination=new RectangleF(820+(716-w*scale)/2,12,w*scale,h*scale);
   g.DrawImage(source,destination,new RectangleF(x,y,w,h),GraphicsUnit.Pixel);
   canvas.Save(output,ImageFormat.Png);
  }
 }
}
'@
$root='E:\Github\Proj_Behemoth\Assets'
$crops=@(
 @('Warrior',130,60,850,850),
 @('Rogue',220,0,720,780),
 @('Barbarian',60,40,850,850),
 @('Samurai',220,60,730,850),
 @('Monk',200,90,750,850),
 @('Wizard',130,25,800,850)
)
foreach($c in $crops){
 [UpperBodySelection]::Crop((Join-Path $root ('Art\Trials\Angular01\StyleCorrections03\'+$c[0]+'.png')),(Join-Path $root ('Art\Characters\Selection\Selection'+$c[0]+'.png')),$c[1],$c[2],$c[3],$c[4])
}
