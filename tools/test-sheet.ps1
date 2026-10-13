# Offline checks of the sprite sheet, the skins' sprite names and the choice-row fit rule.
# The three files are pure (no Unity), so their SOURCE is compiled here; no game needed.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools\test-sheet.ps1
#
# What it guards: the table parses and matches the PNG; every sprite a skin names is in the
# sheet (a missing one would make that skin fall back to Vanilla in-game); nothing in the sheet
# is unused; a broken table is refused, never half-read; the fit rule does what it says.

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$src = Join-Path $root 'src\UI'
Add-Type -Path (Join-Path $src 'SheetTable.cs'), (Join-Path $src 'SkinSprites.cs'), (Join-Path $src 'ChoiceFit.cs')
Add-Type -AssemblyName System.Drawing

$failed = 0
function Check([string]$name, [bool]$ok, [string]$detail = "") {
    if ($ok) { Write-Host "ok    $name  $detail" }
    else { Write-Host "FAIL  $name  $detail"; $script:failed++ }
}

function Parse([string]$text) {
    $err = $null
    $t = [ModsLibCS1.UI.SheetTable]::Parse($text, [ref]$err)
    return @{ Table = $t; Error = $err }
}

# ---- the real sheet --------------------------------------------------------------------------

$txt = [IO.File]::ReadAllText((Join-Path $root 'sprites\sheet.txt'))
$p = Parse $txt
Check "sheet.txt parses" ($null -ne $p.Table) "$($p.Error)"
$table = $p.Table

$img = [System.Drawing.Image]::FromFile((Join-Path $root 'sprites\sheet.png'))
Check "sheet.png matches the table's size" ($img.Width -eq $table.Width -and $img.Height -eq $table.Height) "$($img.Width)x$($img.Height) vs $($table.Width)x$($table.Height)"
$img.Dispose()

$used = @{}
foreach ($skin in 'SeeThrough', 'GameBlue', 'Frosted') {
    $sprites = [ModsLibCS1.UI.SkinSprites]::$skin.Invoke()
    $missing = @($sprites.All() | Where-Object { -not $table.Contains($_) })
    Check "$skin names only sprites in the sheet" ($missing.Count -eq 0) ($missing -join ', ')
    Check "$skin is drawn from the library's sheet" $sprites.OwnSheet
    foreach ($n in $sprites.All()) { $used[$n] = $true }
}

# Tab icons ("icon-" + a Lucide name) are offered to the mods, which name the ones they want.
$unused = @($table.Entries | Where-Object { -not $used.ContainsKey($_.Name) -and -not $_.Name.StartsWith("icon-") } | ForEach-Object { $_.Name })
Check "every sprite in the sheet is used by a skin (or is a tab icon)" ($unused.Count -eq 0) ($unused -join ', ')

$v = [ModsLibCS1.UI.SkinSprites]::Vanilla()
Check "Vanilla draws from the game's atlas" (-not $v.OwnSheet)
$ours = @($v.All() | Where-Object { $table.Contains($_) })
Check "Vanilla names none of the library's sprites" ($ours.Count -eq 0) ($ours -join ', ')

# Overlap: two sprites sharing pixels would show bits of each other.
$overlap = @()
$e = $table.Entries
for ($i = 0; $i -lt $e.Count; $i++) {
    for ($j = $i + 1; $j -lt $e.Count; $j++) {
        $a = $e[$i]; $b = $e[$j]
        if ($a.X -lt $b.X + $b.Width -and $b.X -lt $a.X + $a.Width -and $a.Y -lt $b.Y + $b.Height -and $b.Y -lt $a.Y + $a.Height) {
            $overlap += "$($a.Name)/$($b.Name)"
        }
    }
}
Check "no two sprites overlap" ($overlap.Count -eq 0) ($overlap -join ', ')

# ---- a broken table is refused -----------------------------------------------------------------

$bad = @{
    'no sheet size'          = "a 0 0 4 4 0 0 0 0"
    'sprite outside'         = "sheet 16 16`na 10 10 8 8 0 0 0 0"
    'border with no middle'  = "sheet 16 16`na 0 0 8 8 4 4 0 0"
    'name twice'             = "sheet 16 16`na 0 0 4 4 0 0 0 0`na 4 0 4 4 0 0 0 0"
    'too few numbers'        = "sheet 16 16`na 0 0 4 4 0 0 0"
    'not a number'           = "sheet 16 16`na 0 0 4 x 0 0 0 0"
    'empty'                  = ""
}
foreach ($k in $bad.Keys) {
    $r = Parse $bad[$k]
    Check "refused: $k" ($null -eq $r.Table -and $r.Error) "$($r.Error)"
}

$ok = Parse "# comment`r`nsheet 16 16`r`n`r`na 0 0 8 8 3 3 3 3`r`nb 8 0 8 1 0 0 0 0"
Check "CRLF, comments and blank lines read" ($null -ne $ok.Table -and $ok.Table.Entries.Count -eq 2 -and $ok.Table.Contains('b'))

# ---- the choice-row fit rule ---------------------------------------------------------------

$F = [ModsLibCS1.UI.ChoiceFit]
$w = $F::SegmentWidths([float[]]@(10, 60, 100.2))
Check "a short name still gets the minimum segment" ($w[0] -eq $F::MinSegment) "$($w -join ', ')"
Check "a long name gets its width plus padding, rounded up" ($w[2] -eq [math]::Ceiling(100.2 + $F::SegmentPadding)) "$($w[2])"
$strip = $F::StripWidth($w)
$expected = 2 * $F::StripPadding + 2 * $F::Gap + $w[0] + $w[1] + $w[2]
Check "strip width = segments + gaps + padding" ($strip -eq $expected) "$strip"
Check "fits exactly at the strip's width" ($F::Fits([float[]]@(10, 60, 100.2), [float]$strip))
Check "doesn't fit one unit narrower" (-not $F::Fits([float[]]@(10, 60, 100.2), [float]($strip - 1)))
Check "no choices never fit (no empty strip)" (-not $F::Fits([float[]]@(), [float]1000))

# Where the buttons start: at the column, reaching into it for a short name, or a dropdown (-1).
# Cloud style in the 600-wide panel (2026-10-13): 62+70+46 -> a 270 strip, 241 beside a 303 column.
$cloud = [float[]]@(62, 70, 46)
$cloudStrip = $F::StripWidth($F::SegmentWidths($cloud))
Check "fits beside the column: starts at it" ($F::Start($cloud, [float]200, [float]544, [float]110) -eq 200)
Check "too wide beside it: ends at the right, reaching into the column" ($F::Start($cloud, [float]303, [float]544, [float]110) -eq (544 - $cloudStrip)) "$($F::Start($cloud, [float]303, [float]544, [float]110))"
Check "a name too long to leave room: a dropdown" ($F::Start($cloud, [float]303, [float]544, [float](544 - $cloudStrip - $F::NameRoom + 1)) -eq -1)
Check "exactly the name's room: buttons" ($F::Start($cloud, [float]303, [float]544, [float](544 - $cloudStrip - $F::NameRoom)) -ge 0)
Check "no choices: a dropdown" ($F::Start([float[]]@(), [float]0, [float]1000, [float]0) -eq -1)

Write-Host ""
if ($failed -gt 0) { Write-Host "$failed FAILED"; exit 1 } else { Write-Host "all passed" }
