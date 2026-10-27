Add-Type -AssemblyName System.Drawing
$folder=$PSScriptRoot
$project='E:/Github/Proj_Behemoth'
$heroes=Join-Path $project 'Assets/Art/World/Angular/Heroes'
# Warrior is the user-requested rollback. Generated Warrior08 variants are never installed.
foreach($name in @('Warrior-Front','Warrior-Rear','Barbarian-Rear','Wizard-Front','Wizard-Rear')){
 Copy-Item -LiteralPath (Join-Path $folder ('Reviewed/'+$name+'.png')) -Destination (Join-Path $heroes ($name+'.png'))
}
$cropCode=Get-Content -LiteralPath (Join-Path $project 'Docs/Production/AngularWorld-2026-10-26/CropSelection.ps1') -Raw
Invoke-Expression $cropCode.Substring(0,$cropCode.IndexOf("$"+'root='))
[UpperBodySelection]::Crop((Join-Path $heroes 'Warrior-Front.png'),(Join-Path $project 'Assets/Art/Characters/Selection/SelectionWarrior.png'),130,60,850,850)
[UpperBodySelection]::Crop((Join-Path $heroes 'Wizard-Front.png'),(Join-Path $project 'Assets/Art/Characters/Selection/SelectionWizard.png'),130,25,800,850)
$portraitCode=Get-Content -LiteralPath (Join-Path $project 'Docs/Production/AngularPartyRefine-2026-10-26/ApplyArt.ps1') -Raw
Invoke-Expression $portraitCode.Substring(0,$portraitCode.IndexOf("$"+'project='))
[PartyArt05]::Portrait((Join-Path $heroes 'Wizard-Front.png'),(Join-Path $heroes 'Wizard-Portrait.png'),350,70,470)
$locks=Get-Content -LiteralPath (Join-Path $folder 'UnchangedAssets.json') -Raw | ConvertFrom-Json
$checks=foreach($lock in $locks){[pscustomobject]@{name=$lock.name;unchanged=((Get-FileHash -LiteralPath $lock.path).Hash -eq $lock.sha256)}}
[pscustomobject]@{unchangedAssets=$checks;wizardFrontExactRestoration=((Get-FileHash -LiteralPath (Join-Path $heroes 'Wizard-Front.png')).Hash -eq (Get-FileHash -LiteralPath (Join-Path $folder 'Restored/Wizard-Front.png')).Hash);warriorFrontExactRollback=((Get-FileHash -LiteralPath (Join-Path $heroes 'Warrior-Front.png')).Hash -eq (Get-FileHash -LiteralPath (Join-Path $folder 'Restored/Warrior-Front.png')).Hash);warriorRearExactRollback=((Get-FileHash -LiteralPath (Join-Path $heroes 'Warrior-Rear.png')).Hash -eq (Get-FileHash -LiteralPath (Join-Path $folder 'Restored/Warrior-Rear.png')).Hash)}|ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $folder 'Applied-Invariants.json') -Encoding utf8
