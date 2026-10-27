Add-Type -AssemblyName System.Drawing
$drawingReferences=[AppDomain]::CurrentDomain.GetAssemblies() | Where-Object {$_.GetName().Name -match 'System.Drawing|System.Private.Windows|System.Runtime$'} | Select-Object -ExpandProperty Location
Add-Type -ReferencedAssemblies $drawingReferences -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public static class AngularReviewImages {
 public static void Doors(string source,string output,string name,float x,float y,float w,float h){
  using(var src=new Bitmap(source))using(var dest=new Bitmap(src.Width,src.Height,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(dest)){
   g.Clear(Color.Transparent);g.CompositingMode=CompositingMode.SourceCopy;
   var rect=new Rectangle((int)(src.Width*x),(int)(src.Height*y),(int)(src.Width*w),(int)(src.Height*h));
   g.DrawImage(src,rect,rect,GraphicsUnit.Pixel);dest.Save(output,ImageFormat.Png);
  }
 }
 public static void Sheet(string[] paths,string[] labels,string output){
  int cols=4,cell=300,rows=(paths.Length+cols-1)/cols;
  using(var canvas=new Bitmap(cols*cell,rows*340))using(var g=Graphics.FromImage(canvas))using(var font=new Font("Malgun Gothic",13))using(var ink=new SolidBrush(Color.FromArgb(228,222,204))){
   g.Clear(Color.FromArgb(48,50,47));g.InterpolationMode=InterpolationMode.HighQualityBicubic;
   for(int i=0;i<paths.Length;i++){
    int x=(i%cols)*cell,y=(i/cols)*340;
    using(var src=new Bitmap(paths[i])){
     float scale=Math.Min(280f/src.Width,280f/src.Height);
     g.DrawImage(src,x+10+(280-src.Width*scale)/2,y+10+(280-src.Height*scale)/2,src.Width*scale,src.Height*scale);
    }
    g.DrawString(labels[i],font,ink,new RectangleF(x+8,y+294,284,42));
   }
   canvas.Save(output,ImageFormat.Png);
  }
 }
 public static void Composite(string walls,string[] doors,int mask,string output){
  using(var canvas=new Bitmap(walls))using(var g=Graphics.FromImage(canvas)){
   g.CompositingMode=CompositingMode.SourceOver;
   for(int i=0;i<3;i++)if((mask&(1<<i))!=0)using(var layer=new Bitmap(doors[i]))g.DrawImageUnscaled(layer,0,0);
   canvas.Save(output,ImageFormat.Png);
  }
 }
}
'@
$root='E:\Github\Proj_Behemoth'
$folder=Join-Path $root 'Docs\Production\AngularWorld-2026-10-26'
$art=Join-Path $root 'Assets\Art\World\Angular'
for($floor=1;$floor -le 3;$floor++){
 $source=Join-Path $art ('Backdrops\B'+$floor+'-AllOpen.png')
 if(!(Test-Path $source)){continue}
 [AngularReviewImages]::Doors($source,(Join-Path $art ('Backdrops\B'+$floor+'-Forward.png')),'Forward',.39,.10,.22,.61)
 [AngularReviewImages]::Doors($source,(Join-Path $art ('Backdrops\B'+$floor+'-Left.png')),'Left',.06,.08,.20,.65)
 [AngularReviewImages]::Doors($source,(Join-Path $art ('Backdrops\B'+$floor+'-Right.png')),'Right',.74,.08,.20,.65)
 $doors=[string[]]@('Forward','Left','Right' | ForEach-Object {Join-Path $art ('Backdrops\B'+$floor+'-'+$_+'.png')})
 foreach($mask in @(0,1,2,3,4,5,6,7)){
  [AngularReviewImages]::Composite((Join-Path $art ('Backdrops\B'+$floor+'-Walls.png')),$doors,$mask,(Join-Path $folder ('B'+$floor+'-Mask'+$mask+'.png')))
 }
}
$roster=Get-Content (Join-Path $folder 'Roster.json') -Raw | ConvertFrom-Json
foreach($floor in @(1,2,3,0)){
 $items=@($roster | Where-Object {$_.floor -eq $floor -and (Test-Path (Join-Path $art ('Monsters\'+$_.id+'.png')))})
 if($items.Count -eq 0){continue}
 $paths=[string[]]@($items | ForEach-Object {Join-Path $art ('Monsters\'+$_.id+'.png')})
 $labels=[string[]]@($items | ForEach-Object {$_.name})
 [AngularReviewImages]::Sheet($paths,$labels,(Join-Path $folder ('Lineup-B'+$floor+'.png')))
}
