param(
    [Parameter(Mandatory = $true)][string]$SourceDirectory,
    [Parameter(Mandatory = $true)][string]$OutputManifest,
    [int]$FrameWidth = 64,
    [int]$FrameHeight = 64,
    [int]$Directions = 8,
    [int]$FramesPerClip = 8,
    [string]$Clip = 'idle'
)

$ErrorActionPreference = 'Stop'
if ($FrameWidth -le 0 -or $FrameHeight -le 0 -or $Directions -le 0 -or $FramesPerClip -le 0) { throw 'Bake dimensions and counts must be positive.' }
$files = Get-ChildItem -LiteralPath $SourceDirectory -File | Where-Object { $_.Extension -in '.gltf', '.glb' } | Sort-Object Name
$assets = @()

foreach ($file in $files) {
    if ($file.Extension -eq '.gltf') {
        $document = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
        if ($null -eq $document.asset -or $document.asset.version -ne '2.0') {
            throw "Unsupported glTF version in $($file.Name)"
        }
        $meshCount = @($document.meshes).Count
        $animationCount = @($document.animations).Count
        $skinCount = @($document.skins).Count
    } else {
        throw "Binary GLB inspection requires the offline baker implementation: $($file.Name)"
    }

    $assets += [ordered]@{
        id = [IO.Path]::GetFileNameWithoutExtension($file.Name).ToLowerInvariant()
        source = $file.Name
        atlas = [IO.Path]::GetFileNameWithoutExtension($file.Name) + '.png'
        meshCount = $meshCount
        animationCount = $animationCount
        skinCount = $skinCount
        frameWidth = 0
        frameHeight = 0
        frameCount = 0
        bake = [ordered]@{
            directions = $Directions
            frameWidth = $FrameWidth
            frameHeight = $FrameHeight
            clips = @([ordered]@{ name = $Clip; firstFrame = 0; frameCount = $FramesPerClip * $Directions; framesPerSecond = 12 })
            atlasWidth = $FrameWidth * $Directions
            atlasHeight = $FrameHeight * $FramesPerClip
            anchor = 'bottom-center'
            projection = 'isometric'
        }
    }
}

$result = [ordered]@{
    version = 1
    sourceFormat = 'glTF 2.0'
    assets = $assets
}

$parent = Split-Path -Parent $OutputManifest
if ($parent) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutputManifest -Encoding UTF8
