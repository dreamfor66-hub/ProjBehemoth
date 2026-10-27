Add-Type -AssemblyName System.Drawing
$references=[AppDomain]::CurrentDomain.GetAssemblies() | Where-Object {$_.GetName().Name -match 'System.Drawing|System.Private.Windows|System.Runtime$'} | Select-Object -ExpandProperty Location
Add-Type -ReferencedAssemblies $references -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public static class RestoreApprovedArt {
 static void Copy(Graphics g,Bitmap input,Rectangle region){g.CompositingMode=CompositingMode.SourceCopy;g.DrawImage(input,region,region,GraphicsUnit.Pixel);}
 static void Overlay(string source,string path,Rectangle region){using(var src=new Bitmap(source))using(var output=new Bitmap(src.Width,src.Height,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(output)){g.Clear(Color.Transparent);Copy(g,src,region);output.Save(path,ImageFormat.Png);}}
 public static void Build(string original,string wallEdit,string doorEdit,string output){
  var front=new Rectangle(698,181,282,403);var left=new Rectangle(282,0,137,655);var right=new Rectangle(1284,228,102,408);
  using(var source=new Bitmap(original))using(var edit=new Bitmap(wallEdit))using(var walls=new Bitmap(source)){
   using(var g=Graphics.FromImage(walls)){Copy(g,edit,front);Copy(g,edit,left);}walls.Save(output+"B1-Walls.png",ImageFormat.Png);
  }
  Overlay(original,output+"B1-Forward.png",front);Overlay(original,output+"B1-LeftClosed.png",left);
  Overlay(doorEdit,output+"B1-Left.png",left);Overlay(doorEdit,output+"B1-Right.png",right);
  for(int mask=0;mask<8;mask++)using(var walls=new Bitmap(output+"B1-Walls.png"))using(var g=Graphics.FromImage(walls)){
   g.CompositingMode=CompositingMode.SourceOver;
   string[] names={"Forward","Left","Right"};for(int i=0;i<3;i++)if((mask&(1<<i))!=0)using(var layer=new Bitmap(output+"B1-"+names[i]+".png"))g.DrawImageUnscaled(layer,0,0);
   walls.Save(output+"B1-Mask"+mask+".png",ImageFormat.Png);
   if(mask==7)walls.Save(output+"B1-AllOpen.png",ImageFormat.Png);
  }
 }
 public static void HeadOnly(string original,string edited,string output){using(var source=new Bitmap(original))using(var edit=new Bitmap(edited))using(var g=Graphics.FromImage(source)){Copy(g,edit,new Rectangle(500,45,390,300));source.Save(output,ImageFormat.Png);}}
 public static long OutsideChanges(string a,string b,Rectangle[] regions){long changed=0;using(var x=new Bitmap(a))using(var y=new Bitmap(b)){for(int j=0;j<x.Height;j++)for(int i=0;i<x.Width;i++){bool inside=false;foreach(var r in regions)if(r.Contains(i,j)){inside=true;break;}if(!inside&&x.GetPixel(i,j).ToArgb()!=y.GetPixel(i,j).ToArgb())changed++;}}return changed;}
}
'@

