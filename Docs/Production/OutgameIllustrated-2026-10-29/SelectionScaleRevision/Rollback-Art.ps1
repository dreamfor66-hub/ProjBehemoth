param(
    [ValidateSet('All','Barbarian','Monk','Wizard')][string]$Character = 'All',
    [switch]$CheckOnly,
    [switch]$Force
)
$ErrorActionPreference = 'Stop'
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Rollback-Manifest.json') -Raw | ConvertFrom-Json
$projectPath = [IO.Path]::GetFullPath($manifest.projectRoot)
$artPath = [IO.Path]::GetFullPath((Join-Path $projectPath 'Assets/Art/UI/Outgame')) + [IO.Path]::DirectorySeparatorChar
$selected = @($manifest.entries | Where-Object { $Character -eq 'All' -or $_.character -eq $Character })
if ($selected.Count -eq 0) { throw 'No matching backup entry.' }
# Validate every selected file before replacing any of them.
foreach ($entry in $selected) {
    $target = [IO.Path]::GetFullPath((Join-Path $projectPath $entry.relativePath))
    if (-not $target.StartsWith($artPath, [StringComparison]::OrdinalIgnoreCase)) { throw "Unexpected target: $target" }
    $backup = Join-Path $PSScriptRoot $entry.backupFile
    if ((Get-FileHash -LiteralPath $backup -Algorithm SHA256).Hash -ne $entry.beforeSha256) { throw "Backup changed: $backup" }
    $currentHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
    if (-not $Force -and $currentHash -ne $entry.installedSha256 -and $currentHash -ne $entry.beforeSha256) {
        throw "Later artwork edits detected: $target. Preserve them before choosing -Force."
    }
    if ((Get-FileHash -LiteralPath ($target + '.meta') -Algorithm SHA256).Hash -ne $entry.metaSha256) {
        throw "Sprite metadata changed: $target.meta. Review it before restoring artwork."
    }
}
if ($CheckOnly) { Write-Output "Rollback preflight passed for $($selected.Count) illustration(s); no files changed."; return }
foreach ($entry in $selected) {
    $target = Join-Path $projectPath $entry.relativePath
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $entry.backupFile) -Destination $target -Force
    if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $entry.beforeSha256) { throw "Restore verification failed: $target" }
    Write-Output "Restored $($entry.character) illustration."
}
# Existing editor imports the restored PNGs. Scene/UI data and sprite GUIDs stay intact.
$refreshRequest = Join-Path $projectPath 'Temp/AngularWorld.request'
if (Test-Path -LiteralPath (Split-Path $refreshRequest -Parent)) {
    if (-not (Test-Path -LiteralPath $refreshRequest)) { Set-Content -LiteralPath $refreshRequest -Value 'refresh' }
}
