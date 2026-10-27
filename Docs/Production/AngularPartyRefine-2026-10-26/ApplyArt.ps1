Add-Type -AssemblyName System.Drawing
$references=[AppDomain]::CurrentDomain.GetAssemblies() | Where-Object {$_.GetName().Name -match 'System.Drawing|System.Private.Windows|System.Runtime$'} | Select-Object -ExpandProperty Location
Add-Type -ReferencedAssemblies $references -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public static class PartyArt05 {
 public static void Save(string input,string output){using(var src=new Bitmap(input)){for(int y=0;y<src.Height;y++)for(int x=0;x<src.Width;x++)if(src.GetPixel(x,y).A==0)src.SetPixel(x,y,Color.FromArgb(0,0,0,0));src.Save(output,ImageFormat.Png);}}
 public static long Rogue(string input,string original,string output){long changed=0;using(var src=new Bitmap(input))using(var head=new Bitmap(original)){var region=new Rectangle(390,0,450,345);for(int y=region.Top;y<region.Bottom;y++)for(int x=region.Left;x<region.Right;x++)src.SetPixel(x,y,head.GetPixel(x,y));for(int y=0;y<src.Height;y++)for(int x=0;x<src.Width;x++)if(src.GetPixel(x,y).A==0&&!region.Contains(x,y))src.SetPixel(x,y,Color.FromArgb(0,0,0,0));for(int y=120;y<285;y++)for(int x=550;x<760;x++)if(src.GetPixel(x,y).ToArgb()!=head.GetPixel(x,y).ToArgb())changed++;src.Save(output,ImageFormat.Png);}return changed;}
 public static void Portrait(string input,string output,int x,int y,int size){using(var src=new Bitmap(input))using(var img=new Bitmap(640,640))using(var g=Graphics.FromImage(img)){g.Clear(Color.FromArgb(0,0,0,0));g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(src,new Rectangle(0,0,640,640),new Rectangle(x,y,size,size),GraphicsUnit.Pixel);img.Save(output,ImageFormat.Png);}}
}
'@
$project='E:\Github\Proj_Behemoth'
$folder=Join-Path $project 'Docs\Production\AngularPartyRefine-2026-10-26'
$heroes=Join-Path $project 'Assets\Art\World\Angular\Heroes'
$items=@(Get-Content (Join-Path $folder 'Inputs.json') -Raw|ConvertFrom-Json)+@(Get-Content (Join-Path $folder 'Inputs-Continuity.json') -Raw|ConvertFrom-Json)
$paths=@{};foreach($item in $items){$paths[$item.id]=$item.path}
# Apply only the reviewed revision, before creating any consuming crops.
& (Join-Path $folder 'ComposeFixedArms.ps1') -Revision '07'
$rogueChanges=[PartyArt05]::Rogue($paths['RogueFront05'],(Join-Path $folder 'Before\Rogue-Front.png'),(Join-Path $heroes 'Rogue-Front.png'))
# User approved this exact Barbarian front. Do not regenerate or retouch it.
Copy-Item -LiteralPath (Join-Path $project 'Assets\Art\Concepts\BarbarianBearPelt04.png') -Destination (Join-Path $heroes 'Barbarian-Front.png')
$cropScript=Get-Content (Join-Path $project 'Docs\Production\AngularWorld-2026-10-26\CropSelection.ps1') -Raw
Invoke-Expression $cropScript.Substring(0,$cropScript.IndexOf("$"+'root='))
$crops=@(@('Warrior',130,60,850,850),@('Rogue',70,0,880,850),@('Barbarian',0,0,1024,1024),@('Wizard',130,25,800,850))
foreach($c in $crops){[UpperBodySelection]::Crop((Join-Path $heroes ($c[0]+'-Front.png')),(Join-Path $project ('Assets\Art\Characters\Selection\Selection'+$c[0]+'.png')),$c[1],$c[2],$c[3],$c[4])}
[PartyArt05]::Portrait((Join-Path $heroes 'Barbarian-Front.png'),(Join-Path $heroes 'Barbarian-Portrait.png'),350,80,520)
[PartyArt05]::Portrait((Join-Path $heroes 'Wizard-Front.png'),(Join-Path $heroes 'Wizard-Portrait.png'),350,70,470)
[pscustomobject]@{rogueFaceRectangle='x550..759, y120..284';rogueFaceChangedPixels=$rogueChanges;rogueHeadRestored='x390..839, y0..344';barbarianApprovedFrontUnchanged=((Get-FileHash (Join-Path $heroes 'Barbarian-Front.png')).Hash -eq (Get-FileHash (Join-Path $project 'Assets\Art\Concepts\BarbarianBearPelt04.png')).Hash);pass=($rogueChanges -eq 0)}|ConvertTo-Json|Set-Content (Join-Path $folder 'Identity-Invariants.json') -Encoding utf8


