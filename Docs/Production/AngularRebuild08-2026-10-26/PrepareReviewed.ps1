Add-Type -AssemblyName System.Drawing
$refs=[AppDomain]::CurrentDomain.GetAssemblies() | Where-Object {$_.GetName().Name -match 'System.Drawing|System.Private.Windows|System.Runtime$'} | Select-Object -ExpandProperty Location
Add-Type -ReferencedAssemblies $refs -TypeDefinition @'
using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;
public static class LocalEquipment08 {
 static GraphicsPath Path(int[] a){var p=new GraphicsPath();var pts=new Point[a.Length/2];for(int i=0;i<pts.Length;i++)pts[i]=new Point(a[2*i],a[2*i+1]);p.AddPolygon(pts);return p;}
 public static long[] Patch(string original,string edited,string output,int[] shield,int[] arm,int[][] bands){
  using(var a=new Bitmap(original))using(var b=new Bitmap(edited))using(var o=new Bitmap(a.Width,a.Height,PixelFormat.Format32bppArgb))using(var sp=Path(shield))using(var ap=Path(arm))using(var bp=new GraphicsPath()){
   if(a.Size!=b.Size)throw new Exception("Cannot fit or rotate generated shield: source canvas differs");
   foreach(var band in bands)using(var p=Path(band))bp.AddPath(p,false);
   long armChanged=0,bodyChanged=0;
   for(int y=0;y<a.Height;y++)for(int x=0;x<a.Width;x++){
    var src=a.GetPixel(x,y);bool inArm=ap.IsVisible(x,y),inBand=bp.IsVisible(x,y)&&inArm;
    bool edit=(sp.IsVisible(x,y)&&!inArm)||inBand;
    var c=edit?b.GetPixel(x,y):src;
    if(c.A==0)c=Color.FromArgb(0,0,0,0);
    o.SetPixel(x,y,c);
    if(src.A>220&&inArm&&!inBand&&src.ToArgb()!=c.ToArgb())armChanged++;
    if(src.A>220&&!edit&&src.ToArgb()!=c.ToArgb())bodyChanged++;
   }
   o.Save(output,ImageFormat.Png);return new[]{armChanged,bodyChanged};
  }
 }
 public static int Clean(string input,string output,int width,int height){int noise=0;using(var a=new Bitmap(input))using(var o=new Bitmap(width,height,PixelFormat.Format32bppArgb)){
  for(int y=0;y<Math.Min(a.Height,height);y++)for(int x=0;x<Math.Min(a.Width,width);x++){var c=a.GetPixel(x,y);if(c.A==0||(y>a.Height-22&&c.R>c.G*2+40&&c.R>c.B*2+40)){if(c.A!=0)noise++;c=Color.FromArgb(0,0,0,0);}o.SetPixel(x,y,c);}o.Save(output,ImageFormat.Png);}return noise;}
 public static void Axis(string input,string output){using(var a=new Bitmap(input))using(var b=new Bitmap(a.Width,a.Height))using(var g=Graphics.FromImage(b))using(var p=new Pen(Color.FromArgb(40,190,240),3)){g.Clear(Color.FromArgb(110,113,107));g.DrawImageUnscaled(a,0,0);g.DrawLine(p,1260,198,863,877);b.Save(output,ImageFormat.Png);}}
}
'@
$folder=$PSScriptRoot
# Latest user steering: finish Warrior with the original arms/shield. All generated shield edits are unselected.
foreach($side in @('Front','Rear')){Copy-Item -LiteralPath (Join-Path $folder ('Restored/Warrior-'+$side+'.png')) -Destination (Join-Path $folder ('Reviewed/Warrior-'+$side+'.png'))}
$barb=[LocalEquipment08]::Clean((Join-Path $folder 'Sources/BarbarianRear08.png'),(Join-Path $folder 'Reviewed/Barbarian-Rear.png'),1448,1086)
$wizard=[LocalEquipment08]::Clean((Join-Path $folder 'Sources/WizardRear08.png'),(Join-Path $folder 'Reviewed/Wizard-Rear.png'),1448,1086)
Copy-Item -LiteralPath (Join-Path $folder 'Restored/Wizard-Front.png') -Destination (Join-Path $folder 'Reviewed/Wizard-Front.png')
[LocalEquipment08]::Axis((Join-Path $folder 'Reviewed/Barbarian-Rear.png'),(Join-Path $folder 'Axe-Axis-Review.png'))
[pscustomobject]@{warriorFrontMatchesRestored=((Get-FileHash (Join-Path $folder 'Restored/Warrior-Front.png')).Hash -eq (Get-FileHash (Join-Path $folder 'Reviewed/Warrior-Front.png')).Hash);warriorRearMatchesRestored=((Get-FileHash (Join-Path $folder 'Restored/Warrior-Rear.png')).Hash -eq (Get-FileHash (Join-Path $folder 'Reviewed/Warrior-Rear.png')).Hash);removedBarbarianFooterNoise=$barb;removedWizardFooterNoise=$wizard;note='Latest user instruction: Warrior rollback only; no generated shield or arm is applied.'}|ConvertTo-Json | Set-Content -LiteralPath (Join-Path $folder 'OriginalBodyLocks.json') -Encoding utf8
