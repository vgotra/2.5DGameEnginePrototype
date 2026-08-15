param(
    [string]$SourceDirectory = (Join-Path $PSScriptRoot '..\assets\gltf'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\assets\generated\game'),
    [int]$FrameWidth = 64,
    [int]$FrameHeight = 64,
    [int]$Directions = 8,
    [int]$FramesPerClip = 8
)

$ErrorActionPreference = 'Stop'
$source = [IO.Path]::GetFullPath($SourceDirectory)
$output = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $output | Out-Null
$manifest = Join-Path $output 'game-bake.json'

if (-not (Test-Path -LiteralPath $source -PathType Container)) {
    '{"version":1,"sourceFormat":"glTF 2.0","assets":[]}' | Set-Content -LiteralPath $manifest -Encoding UTF8
    exit 0
}

& (Join-Path $PSScriptRoot 'BuildGltfSpriteManifest.ps1') -SourceDirectory $source -OutputManifest $manifest -FrameWidth $FrameWidth -FrameHeight $FrameHeight -Directions $Directions -FramesPerClip $FramesPerClip
if ($LASTEXITCODE -ne 0) { throw 'glTF manifest generation failed.' }

# Atlas files are optional until source assets are available to the offline raster baker.
# Preserve deterministic manifest output and package any pre-baked atlases beside it.
Get-ChildItem -LiteralPath $source -File -Include '*.png' | Sort-Object Name | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $output $_.Name) -Force
}
