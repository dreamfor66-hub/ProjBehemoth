Add-Type -AssemblyName System.Drawing
$root='E:\Github\Proj_Behemoth'
$report=Join-Path $root 'Docs\Production\AngularCharacterRefine-2026-10-26'
$names=@('Warrior','Rogue','Barbarian','Samurai','Monk','Wizard')
foreach($tone in @('Dark','Gray')){
 $sheet=[Drawing.Bitmap]::new(1440,450)
 $g=[Drawing.Graphics]::FromImage($sheet)
 $g.Clear($(if($tone -eq 'Dark'){[Drawing.Color]::FromArgb(35,39,32)}else{[Drawing.Color]::FromArgb(120,121,115)}))
 $g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
 $font=[Drawing.Font]::new('Malgun Gothic',15)
 for($i=0;$i -lt 6;$i++){
  $path=if($i -eq 2){Join-Path $root 'Assets\Art\Concepts\BarbarianBearPelt02.png'}else{Join-Path $root ('Assets\Art\Trials\Angular01\StyleCorrections03\'+$names[$i]+'.png')}
  $image=[Drawing.Image]::FromFile($path)
  $scale=[Math]::Min(232.0/$image.Width,390.0/$image.Height)
  $w=[single]($image.Width*$scale);$h=[single]($image.Height*$scale)
  $g.DrawImage($image,[single]($i*240+(240-$w)/2),[single](400-$h),$w,$h)
  $g.DrawString($(if($i -eq 2){'Barbarian · 곰가죽 시안'}else{$names[$i]}),$font,[Drawing.Brushes]::White,[single]($i*240+10),[single]414)
  $image.Dispose()
 }
 $sheet.Save((Join-Path $report ('BearLineup-'+$tone+'.png')),[Drawing.Imaging.ImageFormat]::Png)
 $font.Dispose();$g.Dispose();$sheet.Dispose()
}

