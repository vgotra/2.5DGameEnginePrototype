Add-Type -AssemblyName System.Drawing
$output = Join-Path $PSScriptRoot '..\assets\fonts\splash-font.png'
New-Item -ItemType Directory -Force (Split-Path $output) | Out-Null
$bitmap = New-Object System.Drawing.Bitmap 1024, 384
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.Clear([System.Drawing.Color]::Transparent)
$font = New-Object System.Drawing.Font('Arial', 28, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
for ($i = 0; $i -lt 95; $i++) {
    $x = ($i % 16) * 64 + 8
    $y = [math]::Floor($i / 16) * 64 + 10
    $graphics.DrawString(([char]($i + 32)).ToString(), $font, [System.Drawing.Brushes]::White, $x, $y)
}
$bitmap.Save($output, [System.Drawing.Imaging.ImageFormat]::Png)
$font.Dispose()
$graphics.Dispose()
$bitmap.Dispose()
