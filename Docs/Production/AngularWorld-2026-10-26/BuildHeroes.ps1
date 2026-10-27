Add-Type -AssemblyName System.Drawing
$target=Join-Path $PWD 'Art\World\Angular\Heroes'
New-Item -ItemType Directory -Force -Path $target | Out-Null
$backup=Join-Path $PWD '..\Docs\Production\AngularWorld-2026-10-26\Before\Heroes'
New-Item -ItemType Directory -Force -Path $backup | Out-Null
Copy-Item Data\Characters\*.asset $backup
Copy-Item Prefabs\Combat\PlayerView.prefab $backup
Copy-Item Scripts\Combat\Presentation\PlayerView.cs $backup
$copies='[{"id":"Rogue","source":"C:\\Users\\user\\.codex\\generated_images\\01a17187-2859-76e1-a2dc-0390b72989dc\\exec-ab7cff3c-c2c2-4139-a829-8eeb919405ee.png"},{"id":"Barbarian","source":"C:\\Users\\user\\.codex\\generated_images\\01a17187-2859-76e1-a2dc-0390b72989dc\\exec-b244240d-2f9d-443f-85c2-7e0677663ae0.png"},{"id":"Samurai","source":"C:\\Users\\user\\.codex\\generated_images\\01a17187-2859-76e1-a2dc-0390b72989dc\\exec-cfd14610-c4ec-47a2-9c25-881dd1103148.png"},{"id":"Monk","source":"C:\\Users\\user\\.codex\\generated_images\\01a17187-2859-76e1-a2dc-0390b72989dc\\exec-6b42d588-c1f7-4465-8a9c-c9d8c23ad4a1.png"},{"id":"Wizard","source":"C:\\Users\\user\\.codex\\generated_images\\01a17187-2859-76e1-a2dc-0390b72989dc\\exec-7c865ce1-7ab1-4113-88ce-a32a3d73cce4.png"}]' | ConvertFrom-Json
foreach($copy in $copies){Copy-Item -LiteralPath $copy.source -Destination (Join-Path $target ($copy.id+'-Rear.png'))}
Copy-Item Art\Trials\Angular01\Rear.png (Join-Path $target 'Warrior-Rear.png')
$names=@('Warrior','Rogue','Barbarian','Samurai','Monk','Wizard')
$boxes=@(@(270,60,480,480),@(340,0,500,500),@(250,40,530,530),@(240,50,510,510),@(240,80,530,530),@(250,25,510,510))
for($i=0;$i -lt $names.Count;$i++){
 $source=[Drawing.Image]::FromFile((Join-Path $PWD ('Art\Trials\Angular01\StyleCorrections03\'+$names[$i]+'.png')))
 $output=[Drawing.Bitmap]::new(640,640)
 $graphics=[Drawing.Graphics]::FromImage($output)
 $graphics.Clear([Drawing.Color]::Transparent)
 $graphics.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
 $box=$boxes[$i]
 $graphics.DrawImage($source,[Drawing.Rectangle]::new(0,0,640,640),[Drawing.Rectangle]::new($box[0],$box[1],$box[2],$box[3]),[Drawing.GraphicsUnit]::Pixel)
 $graphics.Dispose()
 $output.Save((Join-Path $target ($names[$i]+'-Portrait.png')),[Drawing.Imaging.ImageFormat]::Png)
 $output.Dispose();$source.Dispose()
}
