Add-Type -AssemblyName System.Drawing
$references=[AppDomain]::CurrentDomain.GetAssemblies() | Where-Object {$_.GetName().Name -match 'System.Drawing|System.Private.Windows|System.Runtime$'} | Select-Object -ExpandProperty Location
Add-Type -ReferencedAssemblies $references -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public static class EquipmentStraps04 {
 public static Point[] P(int[] xy){var pts=new Point[xy.Length/2];for(int i=0;i<pts.Length;i++)pts[i]=new Point(xy[i*2],xy[i*2+1]);return pts;}
 public static long Patch(string original,string edited,string output,Point[][] polygons,int dx,int dy){long outside=0;using(var src=new Bitmap(original))using(var edit=new Bitmap(edited))using(var region=new Region()){region.MakeEmpty();foreach(var pts in polygons)using(var path=new GraphicsPath()){path.AddPolygon(pts);region.Union(path);}using(var result=new Bitmap(src)){for(int y=0;y<src.Height;y++)for(int x=0;x<src.Width;x++){result.SetPixel(x,y,src.GetPixel(x,y));if(region.IsVisible(x,y)){int sx=x-dx,sy=y-dy;if(sx>=0&&sy>=0&&sx<edit.Width&&sy<edit.Height)result.SetPixel(x,y,edit.GetPixel(sx,sy));}if(result.GetPixel(x,y).A==0)result.SetPixel(x,y,Color.FromArgb(0,0,0,0));if(!region.IsVisible(x,y)){var a=src.GetPixel(x,y);var b=result.GetPixel(x,y);if((a.A!=0||b.A!=0)&&a.ToArgb()!=b.ToArgb())outside++;}}result.Save(output,ImageFormat.Png);}}return outside;}
}
'@
$project='E:\Github\Proj_Behemoth'
$folder=Join-Path $project 'Docs\Production\AngularEquipmentTribal-2026-10-26'
$items=Get-Content (Join-Path $folder 'Inputs04.json') -Raw | ConvertFrom-Json
$paths=@{};foreach($item in $items){$paths[$item.id]=$item.path}
# Polygons follow the generated leather bands. Rear insert is translated -10,-12 to the ORIGINAL arm; no fist/arm pose is inserted.
$rearPolygons=[Drawing.Point[][]]@(
 [EquipmentStraps04]::P(@(240,586,268,588,424,646,406,696,239,642)),
 [EquipmentStraps04]::P(@(223,850,251,838,355,864,349,906,227,916))
)
$frontPolygons=[Drawing.Point[][]]@(
 [EquipmentStraps04]::P(@(693,649,718,624,795,606,818,606,826,647,797,654,722,680,703,700)),
 [EquipmentStraps04]::P(@(730,739,754,722,808,708,831,708,842,747,814,756,750,774))
)
$rear=Join-Path $project 'Assets\Art\World\Angular\Heroes\Warrior-Rear.png'
$front=Join-Path $project 'Assets\Art\World\Angular\Heroes\Warrior-Front.png'
$rearChanges=[EquipmentStraps04]::Patch((Join-Path $folder 'Before\Warrior-RearHelmet03.png'),$paths['WarriorRearStrap04'],$rear,$rearPolygons,-10,-12)
$frontChanges=[EquipmentStraps04]::Patch((Join-Path $folder 'Before\Warrior-Front.png'),$paths['WarriorFrontStrap04'],$front,$frontPolygons,0,0)
Copy-Item -LiteralPath $paths['BarbarianTribalPants04'] -Destination (Join-Path $project 'Assets\Art\Concepts\BarbarianBearPelt04.png')
$cropScript=Get-Content (Join-Path $project 'Docs\Production\AngularWorld-2026-10-26\CropSelection.ps1') -Raw
Invoke-Expression $cropScript.Substring(0,$cropScript.IndexOf("$"+'root='))
[UpperBodySelection]::Crop($front,(Join-Path $project 'Assets\Art\Characters\Selection\SelectionWarrior.png'),130,60,850,850)
# HUD portrait shows no shield and remains the exact existing image.
Copy-Item -LiteralPath (Join-Path $folder 'Before\Warrior-Portrait.png') -Destination (Join-Path $project 'Assets\Art\World\Angular\Heroes\Warrior-Portrait.png')
[UpperBodySelection]::Crop((Join-Path $project 'Assets\Art\Concepts\BarbarianBearPelt04.png'),(Join-Path $project 'Assets\Art\Concepts\BarbarianBearPelt04-Selection.png'),0,0,1024,1024)
[pscustomobject]@{rearVisibleChangesOutsideStraps=$rearChanges;frontVisibleChangesOutsideStraps=$frontChanges;pass=($rearChanges -eq 0 -and $frontChanges -eq 0);note='Fully transparent RGB normalized to zero. Original arm/hand poses retained; only two leather-band polygons inserted per view.'}|ConvertTo-Json|Set-Content (Join-Path $folder 'Pixel-Invariants04.json') -Encoding utf8

