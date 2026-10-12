# Draws the See-through and Game blue sprites and packs them into sprites/sheet.png + sheet.txt.
# Deterministic: run it again with nothing changed and git shows no diff.
#
#   .\tools\make-sprites.ps1
#
# Look at the PNG before committing it.

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root 'sprites'
New-Item -ItemType Directory -Force $out | Out-Null

Add-Type -Path (Join-Path $PSScriptRoot 'SpriteMaker.cs') -ReferencedAssemblies System.Drawing

$png = Join-Path $out 'sheet.png'
$txt = Join-Path $out 'sheet.txt'
$before = if (Test-Path $png) { (Get-FileHash $png).Hash } else { '' }

$summary = [SpriteMaker]::Run($png, $txt)

$after = (Get-FileHash $png).Hash
$state = if ($before -eq $after) { 'UNCHANGED' } else { 'changed' }
Write-Host "sprites: $summary ($state)"
