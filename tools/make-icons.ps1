# Draws a mod's OWN icons (its tab icons) from its list and packs them into a sheet of its own,
# beside the list: <name>.lucide.txt -> <name>.png + <name>.txt. ModsLibCS1.props embeds
# <project>\Resources\PanelIcons.png + .txt, and Atlas merges them into the library's atlas, where a
# tab asks for them as "icon-" + the name. The library's own sheet holds only what every mod uses.
#
#   powershell -File <mod>\ModsLibCS1\tools\make-icons.ps1 -Spec <mod>\<project>\Resources\PanelIcons.lucide.txt
#
# The list: one icon per line, its name, then each of its SVG paths (Lucide's, 24-unit grid), all
# separated by " | "; '#' starts a comment line. A circle or rect is written as a path first.
# Drawn as the tab icons always were (SpriteMaker.RunIcons): 24 px, white, stroke 2.5 units.
# Deterministic: run it again with nothing changed and git shows no diff. Look at the PNG before
# committing it, and credit the icons in the mod's own notices.

param([Parameter(Mandatory = $true)][string]$Spec)

$ErrorActionPreference = 'Stop'
$Spec = (Resolve-Path $Spec).Path
$file = [IO.Path]::GetFileName($Spec)
if ($file -notmatch '^(.+)\.lucide\.txt$') { throw "the list's name must end in .lucide.txt: $Spec" }
$dir = Split-Path -Parent $Spec
$png = Join-Path $dir "$($Matches[1]).png"
$txt = Join-Path $dir "$($Matches[1]).txt"

Add-Type -Path (Join-Path $PSScriptRoot 'SpriteMaker.cs') -ReferencedAssemblies System.Drawing

$before = if (Test-Path $png) { (Get-FileHash $png).Hash } else { '' }
$summary = [SpriteMaker]::RunIcons($Spec, $png, $txt)
$after = (Get-FileHash $png).Hash
$state = if ($before -eq $after) { 'UNCHANGED' } else { 'changed' }
Write-Host "icons: $summary -> $png ($state)"
