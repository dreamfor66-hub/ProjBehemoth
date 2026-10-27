Add-Type -AssemblyName System.Drawing
$references=[AppDomain]::CurrentDomain.GetAssemblies() | Where-Object {$_.GetName().Name -match 'System.Drawing|System.Private.Windows|System.Runtime$'} | Select-Object -ExpandProperty Location
Add-Type -ReferencedAssemblies $references -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public static class EquipmentArt03 {
 public static void Patch(string original,string edited,string output,Rectangle[] regions){using(var src=new Bitmap(original))using(var edit=new Bitmap(edited)){foreach(var r in regions){int feather=(r.Y<100)?24:40;for(int y=r.Top;y<r.Bottom;y++)for(int x=r.Left;x<r.Right;x++){double d=Math.Min(Math.Min(x-r.Left,r.Right-1-x),Math.Min(y-r.Top,r.Bottom-1-y));double t=Math.Min(1.0,d/feather);t=t*t*(3-2*t);Color a=src.GetPixel(x,y),b=edit.GetPixel(x,y);double aa=a.A*(1-t),ba=b.A*t,alpha=aa+ba;if(alpha<0.5){src.SetPixel(x,y,Color.FromArgb(0,0,0,0));continue;}src.SetPixel(x,y,Color.FromArgb((int)Math.Round(alpha),(int)Math.Round((a.R*aa+b.R*ba)/alpha),(int)Math.Round((a.G*aa+b.G*ba)/alpha),(int)Math.Round((a.B*aa+b.B*ba)/alpha)));}}for(int y=0;y<src.Height;y++)for(int x=0;x<src.Width;x++)if(src.GetPixel(x,y).A==0)src.SetPixel(x,y,Color.FromArgb(0,0,0,0));src.Save(output,ImageFormat.Png);}}
 public static long Check(string original,string result,Rectangle[] regions){long changes=0;using(var a=new Bitmap(original))using(var b=new Bitmap(result)){for(int y=0;y<a.Height;y++)for(int x=0;x<a.Width;x++){bool inside=false;foreach(var r in regions)if(r.Contains(x,y)){inside=true;break;}if(!inside){Color ca=a.GetPixel(x,y),cb=b.GetPixel(x,y);if((ca.A!=0||cb.A!=0)&&ca.ToArgb()!=cb.ToArgb())changes++;}}}return changes;}
 public static void Portrait(string input,string output){using(var src=new Bitmap(input))using(var img=new Bitmap(640,640))using(var g=Graphics.FromImage(img)){g.Clear(Color.Transparent);g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(src,new Rectangle(0,0,640,640),new Rectangle(270,60,480,480),GraphicsUnit.Pixel);img.Save(output,ImageFormat.Png);}}
}
'@
$project='E:\Github\Proj_Behemoth'
$assets=Join-Path $project 'Assets'
$folder=Join-Path $project 'Docs\Production\AngularEquipmentTribal-2026-10-26'
$items=Get-Content (Join-Path $folder 'Inputs.json') -Raw | ConvertFrom-Json
$paths=@{};foreach($item in $items){$paths[$item.id]=$item.path}
$rear=Join-Path $assets 'Art\World\Angular\Heroes\Warrior-Rear.png'
$front=Join-Path $assets 'Art\World\Angular\Heroes\Warrior-Front.png'
$rearRegions=[Drawing.Rectangle[]]@([Drawing.Rectangle]::new(500,45,390,300))
$frontRegions=[Drawing.Rectangle[]]@([Drawing.Rectangle]::new(630,500,394,590))
[EquipmentArt03]::Patch((Join-Path $folder 'Before\Warrior-Rear.png'),$paths['WarriorRear03'],$rear,$rearRegions)
# Rejected grip: front and shield-side arm restored before strap edit.
Copy-Item -LiteralPath $paths['BarbarianTribal03'] -Destination (Join-Path $assets 'Art\Concepts\BarbarianBearPelt03.png')
$cropScript=Get-Content (Join-Path $project 'Docs\Production\AngularWorld-2026-10-26\CropSelection.ps1') -Raw
Invoke-Expression $cropScript.Substring(0,$cropScript.IndexOf("$"+'root='))
[UpperBodySelection]::Crop($front,(Join-Path $assets 'Art\Characters\Selection\SelectionWarrior.png'),130,60,850,850)
[EquipmentArt03]::Portrait($front,(Join-Path $assets 'Art\World\Angular\Heroes\Warrior-Portrait.png'))
$rearChanges=[EquipmentArt03]::Check((Join-Path $folder 'Before\Warrior-Rear.png'),$rear,$rearRegions)
$frontChanges=[EquipmentArt03]::Check((Join-Path $folder 'Before\Warrior-Front.png'),$front,$frontRegions)
[pscustomobject]@{rearChangesOutsideHeadAndShield=$rearChanges;frontChangesOutsideShieldArm=$frontChanges;pass=($rearChanges -eq 0 -and $frontChanges -eq 0)} | ConvertTo-Json | Set-Content (Join-Path $folder 'Pixel-Invariants.json') -Encoding utf8




