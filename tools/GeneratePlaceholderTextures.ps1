# Generates placeholder PNG textures into assets/textures/ so the asset pipeline
# is testable before you drop in your own art. Re-run anytime; your real art in
# assets/textures/ is never overwritten.
param(
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$dir = Join-Path $root 'assets\textures'
New-Item -ItemType Directory -Path $dir -Force | Out-Null

function New-Texture {
    param([string]$Name, [int]$Size, [System.Drawing.Color]$Color, [System.Drawing.Color]$Border)
    $path = Join-Path $dir "$Name.png"
    if ((Test-Path $path) -and -not $Force) { Write-Host "skip   $Name.png (exists)"; return }
    $bmp = New-Object System.Drawing.Bitmap($Size, $Size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
    $g.Clear($Color)
    if ($Border -ne [System.Drawing.Color]::Empty) {
        $pen = New-Object System.Drawing.Pen($Border, 2)
        $g.DrawRectangle($pen, 0, 0, $Size - 1, $Size - 1)
        $pen.Dispose()
    }
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Host "wrote  $Name.png"
}

function New-Flag {
    param([string]$Name, [int]$Size)
    $path = Join-Path $dir "$Name.png"
    if ((Test-Path $path) -and -not $Force) { Write-Host "skip   $Name.png (exists)"; return }
    $bmp = New-Object System.Drawing.Bitmap($Size, $Size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $blue = [System.Drawing.Color]::FromArgb(255, 0, 87, 183)
    $yellow = [System.Drawing.Color]::FromArgb(255, 255, 215, 0)
    $g.Clear($blue)
    $g.FillRectangle((New-Object System.Drawing.SolidBrush($yellow)), 0, [int]($Size / 2), $Size, [int]($Size / 2))
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Host "wrote  $Name.png"
}

New-Flag 'player' 64
New-Texture 'grass' 64 ([System.Drawing.Color]::FromArgb(255, 92, 168, 74)) ([System.Drawing.Color]::Empty)
New-Texture 'water' 64 ([System.Drawing.Color]::FromArgb(255, 52, 120, 190)) ([System.Drawing.Color]::Empty)
New-Texture 'tree' 64 ([System.Drawing.Color]::FromArgb(255, 34, 92, 44)) ([System.Drawing.Color]::Empty)
New-Texture 'bonfire' 64 ([System.Drawing.Color]::FromArgb(255, 220, 110, 30)) ([System.Drawing.Color]::Empty)
New-Texture 'wall' 64 ([System.Drawing.Color]::FromArgb(255, 90, 90, 90)) ([System.Drawing.Color]::Empty)
New-Texture 'deer' 64 ([System.Drawing.Color]::FromArgb(255, 72, 150, 88)) ([System.Drawing.Color]::Empty)
New-Texture 'rabbit' 64 ([System.Drawing.Color]::FromArgb(255, 220, 130, 155)) ([System.Drawing.Color]::Empty)
