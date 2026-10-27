param([switch]$Preview,[string]$Revision="07")
Add-Type -AssemblyName System.Drawing
$references=[AppDomain]::CurrentDomain.GetAssemblies() | Where-Object {$_.GetName().Name -match 'System.Drawing|System.Private.Windows|System.Runtime$'} | Select-Object -ExpandProperty Location
Add-Type -ReferencedAssemblies $references -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public static class FixedArms06 {
 public static Point[] Points(int[] a){var p=new Point[a.Length/2];for(int i=0;i<p.Length;i++)p[i]=new Point(a[i*2],a[i*2+1]);return p;}
 public static GraphicsPath Path(int[] a){var p=new GraphicsPath();p.AddPolygon(Points(a));return p;}
 static PointF Map(float x,float y,float ox,float oy,float xx,float xy,float yx,float yy){return new PointF(ox+x*xx+y*yx,oy+x*xy+y*yy);}
 public static long Compose(string original,string shieldFile,string plainFile,string output,int[] old,int[] arm,float ox,float oy,float xx,float xy,float yx,float yy){
  using(var src=new Bitmap(original))using(var shield=new Bitmap(shieldFile))using(var plain=new Bitmap(plainFile))using(var body=new Bitmap(src.Width,src.Height,PixelFormat.Format32bppArgb))using(var result=new Bitmap(src.Width,src.Height,PixelFormat.Format32bppArgb))using(var oldPath=Path(old))using(var oldRegion=new Region(oldPath))using(var armPath=Path(arm))using(var armRegion=new Region(armPath)){
   using(var border=(GraphicsPath)oldPath.Clone())using(var pen=new Pen(Color.Black,28)){border.Widen(pen);oldRegion.Union(border);}
   for(int y=0;y<src.Height;y++)for(int x=0;x<src.Width;x++){var c=src.GetPixel(x,y);body.SetPixel(x,y,oldRegion.IsVisible(x,y)&&!armRegion.IsVisible(x,y)?Color.FromArgb(0,0,0,0):c);}
   PointF[] target={Map(0,0,ox,oy,xx,xy,yx,yy),Map(shield.Width,0,ox,oy,xx,xy,yx,yy),Map(0,shield.Height,ox,oy,xx,xy,yx,yy)};
   PointF[] panelTarget;if(src.Height>1200){panelTarget=new[]{Map(0,0,560.8f,508.1f,.4f,.02f,.04f,.35f),Map(plain.Width,0,560.8f,508.1f,.4f,.02f,.04f,.35f),Map(0,plain.Height,560.8f,508.1f,.4f,.02f,.04f,.35f)};}else{panelTarget=new[]{Map(0,0,105.95f,495,.37f,0,0,.375f),Map(plain.Width,0,105.95f,495,.37f,0,0,.375f),Map(0,plain.Height,105.95f,495,.37f,0,0,.375f)};}int[][] mounts={new[]{361,420,447,430,456,455,708,488,764,478,783,498,785,591,747,616,721,621,418,568,393,555,361,540},new[]{360,736,443,749,455,777,707,799,766,773,785,787,788,880,747,913,712,929,418,884,392,882,359,871}};using(var g=Graphics.FromImage(result)){g.Clear(Color.FromArgb(0,0,0,0));g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(plain,panelTarget);using(var mp=new GraphicsPath()){foreach(var m in mounts){var pp=Points(m);var mm=new PointF[pp.Length];for(int i=0;i<pp.Length;i++)mm[i]=Map(pp[i].X,pp[i].Y,ox,oy,xx,xy,yx,yy);mp.AddPolygon(mm);}g.SetClip(mp);g.DrawImage(shield,target);g.ResetClip();}g.DrawImageUnscaled(body,0,0);}
   // Original arm pixels are restored directly; only shield's leather loop fronts may cover them.
   for(int y=0;y<src.Height;y++)for(int x=0;x<src.Width;x++)if(armRegion.IsVisible(x,y)&&src.GetPixel(x,y).A>0)result.SetPixel(x,y,src.GetPixel(x,y));
   int[][] ribbons={new[]{427,447,550,455,710,503,722,521,715,622,567,590,419,562},new[]{426,768,550,775,712,806,720,833,713,926,562,901,418,878}};
   using(var ribbonPath=new GraphicsPath()){
    foreach(var band in ribbons){var points=Points(band);var mapped=new PointF[points.Length];for(int i=0;i<points.Length;i++)mapped[i]=Map(points[i].X,points[i].Y,ox,oy,xx,xy,yx,yy);ribbonPath.AddPolygon(mapped);}
    using(var loopRegion=new Region(ribbonPath)){
     loopRegion.Intersect(armRegion);
     using(var g=Graphics.FromImage(result)){g.SetClip(loopRegion,CombineMode.Replace);g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(shield,target);}
     long changed=0;for(int y=0;y<src.Height;y++)for(int x=0;x<src.Width;x++){var a=src.GetPixel(x,y);var b=result.GetPixel(x,y);if(armRegion.IsVisible(x,y)&&!loopRegion.IsVisible(x,y)&&a.A>220&&a.ToArgb()!=b.ToArgb())changed++;if(b.A==0)result.SetPixel(x,y,Color.FromArgb(0,0,0,0));}
     if(src.Height<1200){for(int yy2=1010;yy2<1055;yy2++)for(int xx2=170;xx2<225;xx2++)result.SetPixel(xx2,yy2,Color.FromArgb(0,0,0,0));}result.Save(output,ImageFormat.Png);return changed;
    }
   }
  }
 }
 public static void Book(string original,string edited,string output,Rectangle r){using(var src=new Bitmap(original))using(var edit=new Bitmap(edited)){for(int y=r.Top;y<r.Bottom;y++)for(int x=r.Left;x<r.Right;x++){var a=src.GetPixel(x,y);var b=edit.GetPixel(x,y);double t=Math.Min(1,Math.Min(Math.Min(x-r.Left,r.Right-1-x),Math.Min(y-r.Top,r.Bottom-1-y))/24.0);t=t*t*(3-2*t);double aa=a.A*(1-t),bb=b.A*t,alpha=aa+bb;if(alpha<.5){src.SetPixel(x,y,Color.FromArgb(0,0,0,0));continue;}src.SetPixel(x,y,Color.FromArgb((int)Math.Round(alpha),(int)Math.Round((a.R*aa+b.R*bb)/alpha),(int)Math.Round((a.G*aa+b.G*bb)/alpha),(int)Math.Round((a.B*aa+b.B*bb)/alpha)));}src.Save(output,ImageFormat.Png);}}
 public static int CleanSave(string input,string output){int count=0;using(var b=new Bitmap(input)){for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++){var c=b.GetPixel(x,y);if(c.A==0||(y>b.Height-45&&c.R>200&&c.G<45&&c.B<45)){if(c.A!=0)count++;b.SetPixel(x,y,Color.FromArgb(0,0,0,0));}}b.Save(output,ImageFormat.Png);}return count;}
}
'@
$project='E:\Github\Proj_Behemoth'
$folder=Join-Path $project 'Docs\Production\AngularPartyRefine-2026-10-26'
$heroes=Join-Path $project 'Assets\Art\World\Angular\Heroes'
if($Preview){$heroes=Join-Path $folder 'Review07\Preview';New-Item -ItemType Directory -Path $heroes -Force|Out-Null}
$mask=Get-Content (Join-Path $folder 'ArmMasks.json') -Raw|ConvertFrom-Json
$shield=Join-Path $folder 'Sources\ShieldInner06.png'
if($Revision -ne '06'){$shield=Join-Path $folder ('Sources\ShieldLoops'+$Revision+'.png')}
$plain=Join-Path $folder ('Sources\ShieldPlain'+$Revision+'.png')
$front=[FixedArms06]::Compose((Join-Path $folder 'ArmLocked\Warrior-Front.png'),$shield,$plain,(Join-Path $heroes 'Warrior-Front.png'),$mask.frontOldShield,$mask.frontArm,569.35,501.35,0.26,-0.071,0.09375,0.34375)
$rear=[FixedArms06]::Compose((Join-Path $folder 'ArmLocked\Warrior-Rear.png'),$shield,$plain,(Join-Path $heroes 'Warrior-Rear.png'),$mask.rearOldShield,$mask.rearArm,122.9,495,0.34,0,0,0.375)
$barbClean=[FixedArms06]::CleanSave((Join-Path $folder ('Sources\BarbarianRear'+$Revision+'.png')),(Join-Path $heroes 'Barbarian-Rear.png'))
if($Revision -eq '07'){
 $wizardClean=[FixedArms06]::CleanSave((Join-Path $folder 'Sources\WizardRear07.png'),(Join-Path $heroes 'Wizard-Rear.png'))
 [FixedArms06]::CleanSave((Join-Path $folder 'Sources\WizardFront07.png'),(Join-Path $heroes 'Wizard-Front.png'))|Out-Null
}else{$wizardClean=[FixedArms06]::CleanSave((Join-Path $folder ('Sources\WizardRear'+$Revision+'.png')),(Join-Path $heroes 'Wizard-Rear.png'))}
if(-not $Preview){
$cropScript=Get-Content (Join-Path $project 'Docs\Production\AngularWorld-2026-10-26\CropSelection.ps1') -Raw
if(-not ('UpperBodySelection' -as [type])){Invoke-Expression $cropScript.Substring(0,$cropScript.IndexOf("$"+'root='))}
[UpperBodySelection]::Crop((Join-Path $heroes 'Warrior-Front.png'),(Join-Path $project 'Assets\Art\Characters\Selection\SelectionWarrior.png'),130,60,850,850)
}
[pscustomobject]@{frontOriginalArmChangedOutsideLeatherLoops=$front;rearOriginalArmChangedOutsideLeatherLoops=$rear;barbarianRedEdgeNoiseRemoved=$barbClean;wizardRedEdgeNoiseRemoved=$wizardClean;pass=($front -eq 0 -and $rear -eq 0);note='Arm/hand coordinates and source pixels locked. Only the new shield and leather loop fronts are composited. No generated Warrior anatomy is used.'}|ConvertTo-Json|Set-Content (Join-Path $folder ('Arm-Invariants'+$Revision+'.json')) -Encoding utf8



