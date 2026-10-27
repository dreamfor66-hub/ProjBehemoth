$projectRoot='E:\Github\Proj_Behemoth'
$report=Join-Path $projectRoot 'Docs\Production\AngularWorld-2026-10-26'
$dotnet='C:\Program Files\Unity\Editor\6000.0.59f2\Editor\Data\NetCoreRuntime\dotnet.exe'
$compiler='C:\Program Files\Unity\Editor\6000.0.59f2\Editor\Data\DotNetSdkRoslyn\csc.dll'
foreach($name in @('DungeonSlash','Assembly-CSharp-Editor')){
 [xml]$project=Get-Content (Join-Path $projectRoot ($name+'.csproj')) -Raw
 $ns=[Xml.XmlNamespaceManager]::new($project.NameTable);$ns.AddNamespace('p','http://schemas.microsoft.com/developer/msbuild/2003')
 $defs=$project.SelectSingleNode('//p:DefineConstants',$ns).InnerText
 $output=Join-Path $report ($name+'.dll')
 $argsList=[Collections.Generic.List[string]]::new()
 foreach($arg in @('/nologo','/target:library','/nostdlib+','/langversion:9.0')){$argsList.Add($arg)}
 $argsList.Add('/define:'+$defs)
 $argsList.Add('/out:"'+$output+'"')
 if($name -eq 'Assembly-CSharp-Editor'){$argsList.Add('/reference:"'+(Join-Path $report 'DungeonSlash.dll')+'"')}
 foreach($node in $project.SelectNodes('//p:Reference/p:HintPath',$ns)){
  $path=$node.InnerText;if(![IO.Path]::IsPathRooted($path)){$path=Join-Path $projectRoot $path}
  if($name -eq 'Assembly-CSharp-Editor' -and [IO.Path]::GetFileName($path) -eq 'DungeonSlash.dll'){$path=Join-Path $report 'DungeonSlash.dll'}
  if(Test-Path -LiteralPath $path){$argsList.Add('/reference:"'+$path+'"')}
 }
 $files=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
 foreach($node in $project.SelectNodes('//p:Compile',$ns)){$path=Join-Path $projectRoot $node.Include;if(Test-Path -LiteralPath $path){[void]$files.Add($path)}}
 $folder=if($name -eq 'DungeonSlash'){'Assets\Scripts'}else{'Assets\Editor'}
 foreach($file in Get-ChildItem (Join-Path $projectRoot $folder) -Recurse -Filter '*.cs'){[void]$files.Add($file.FullName)}
 foreach($path in $files){$argsList.Add('"'+$path+'"')}
 $response=Join-Path $report ($name+'.rsp');[IO.File]::WriteAllLines($response,$argsList)
 & $dotnet $compiler ('@'+$response) 2>&1 | Tee-Object (Join-Path $report ($name+'-Compile.txt'))
 if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
}
