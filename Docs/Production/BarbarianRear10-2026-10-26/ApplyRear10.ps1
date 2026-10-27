Add-Type -AssemblyName System.Drawing
$refs=[AppDomain]::CurrentDomain.GetAssemblies() | Where-Object {$_.GetName().Name -match 'System.Drawing|System.Private.Windows|System.Runtime$'} | Select-Object -ExpandProperty Location
Add-Type -ReferencedAssemblies $refs -TypeDefinition @'
using System;using System.Drawing;using System.Drawing.Imaging;
public static class BarbarianRear10 {
 public static void Save(string input,string output){using(var a=new Bitmap(input))using(var b=new Bitmap(1448,1086,PixelFormat.Format32bppArgb)){
  if(Math.Abs(a.Width-1448)>3||Math.Abs(a.Height-1086)>3)throw new Exception("Unexpected canvas dimensions");
  for(int y=0;y<Math.Min(a.Height,b.Height);y++)for(int x=0;x<Math.Min(a.Width,b.Width);x++){var c=a.GetPixel(x,y);if(c.A==0||(y>a.Height-22&&c.R>c.G*2+40&&c.R>c.B*2+40))c=Color.FromArgb(0,0,0,0);b.SetPixel(x,y,c);}b.Save(output,ImageFormat.Png);
 }}
}
'@
$folder=$PSScriptRoot
$final=Join-Path $folder 'Barbarian-Rear10.png'
[BarbarianRear10]::Save((Join-Path $folder 'Sources/BarbarianRear10.png'),$final)
Copy-Item -LiteralPath $final -Destination 'E:/Github/Proj_Behemoth/Assets/Art/World/Angular/Heroes/Barbarian-Rear.png'
$locks=Get-Content -LiteralPath (Join-Path $folder 'ProtectedAssets.json') -Raw | ConvertFrom-Json
$checks=foreach($asset in $locks){[pscustomobject]@{name=$asset.name;unchanged=((Get-FileHash -LiteralPath $asset.path).Hash -eq $asset.sha256)}}
if(@($checks | Where-Object {-not $_.unchanged}).Count -gt 0){throw 'An unrelated character changed'}
[pscustomobject]@{onlyInstalledAsset='Assets/Art/World/Angular/Heroes/Barbarian-Rear.png';matchesReviewed=((Get-FileHash -LiteralPath $final).Hash -eq (Get-FileHash -LiteralPath 'E:/Github/Proj_Behemoth/Assets/Art/World/Angular/Heroes/Barbarian-Rear.png').Hash);protected=$checks}|ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $folder 'Applied-Check.json') -Encoding utf8

