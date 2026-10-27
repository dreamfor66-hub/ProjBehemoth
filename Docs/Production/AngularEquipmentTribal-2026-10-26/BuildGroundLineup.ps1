Add-Type -AssemblyName System.Drawing
$references=[AppDomain]::CurrentDomain.GetAssemblies() | Where-Object {$_.GetName().Name -match 'System.Drawing|System.Private.Windows|System.Runtime$'} | Select-Object -ExpandProperty Location
Add-Type -ReferencedAssemblies $references -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public static class GroundLineup03 {
 public static Rectangle Bounds(Bitmap source){int left=source.Width,top=source.Height,right=-1,bottom=-1;for(int y=0;y<source.Height;y++)for(int x=0;x<source.Width;x++)if(source.GetPixel(x,y).A>220){left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);}return Rectangle.FromLTRB(left,top,right+1,bottom+1);}
 public static string[] Build(string[] paths,string[] labels,float[] heights,int[] bodyTops,string output,bool gray,int ground,float referenceHeight){
  var rows=new string[paths.Length];
  using(var sheet=new Bitmap(2040,700))using(var g=Graphics.FromImage(sheet))using(var font=new Font("Malgun Gothic",16))using(var small=new Font("Malgun Gothic",13))using(var pen=new Pen(Color.FromArgb(215,194,145),2)){
   g.Clear(gray?Color.FromArgb(124,125,119):Color.FromArgb(35,39,32));g.InterpolationMode=InterpolationMode.HighQualityBicubic;
   g.DrawLine(pen,15,ground,2025,ground);
   for(int i=0;i<paths.Length;i++)using(var source=new Bitmap(paths[i])){
    var bounds=Bounds(source);int top=bodyTops[i]>=0?bodyTops[i]:bounds.Top;var scale=referenceHeight*heights[i]/(bounds.Bottom-top);
    float x=i*340+170-(bounds.Left+bounds.Width/2f)*scale,y=ground-(bounds.Bottom)*scale;
    g.DrawImage(source,x,y,source.Width*scale,source.Height*scale);
    g.DrawLine(pen,i*340+150,ground,i*340+190,ground);
    g.DrawString(labels[i],font,Brushes.White,i*340+22,ground+14);
    g.DrawString("비교 배율 "+heights[i].ToString("0.00"),small,Brushes.White,i*340+22,ground+48);
    rows[i]=labels[i]+": sourceFootY="+bounds.Bottom+", renderedFootY="+(y+bounds.Bottom*scale).ToString("0.000")+", scale="+scale.ToString("0.00000");
   }
   g.DrawString("지면 · 실제 발바닥 최저점 기준 / 표시 배율은 비교용 가정",small,Brushes.White,20,ground+80);
   sheet.Save(output,ImageFormat.Png);
  }return rows;
 }
}
'@
$project='E:\Github\Proj_Behemoth'
$folder=Join-Path $project 'Docs\Production\AngularEquipmentTribal-2026-10-26'
$config=Get-Content (Join-Path $folder 'LineupScale.json') -Raw | ConvertFrom-Json
$paths=[string[]]@($config.characters | ForEach-Object {Join-Path $project $_.source})
$labels=[string[]]@($config.characters | ForEach-Object {$_.label})
$heights=[single[]]@($config.characters | ForEach-Object {$_.relativeHeight})
$bodyTops=[int[]]@($config.characters | ForEach-Object {if($null -ne $_.bodyTopY){$_.bodyTopY}else{-1}})
foreach($tone in @('Dark','Gray')){
 $rows=[GroundLineup03]::Build($paths,$labels,$heights,$bodyTops,(Join-Path $folder ('GroundLineup-'+$tone+'.png')),($tone -eq 'Gray'),$config.groundY,$config.warriorSilhouetteHeight)
 $rows | Set-Content (Join-Path $folder 'GroundAlignment.txt') -Encoding utf8
}

