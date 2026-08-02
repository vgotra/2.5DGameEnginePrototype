param([switch]$Rebuild)
$repoRoot = Split-Path $PSScriptRoot -Parent
$serverDir = Join-Path $repoRoot "tools\mcp\mcp-Vulkan"
$buildDir = Join-Path $serverDir "vulkan\build\index.js"

if (-not (Get-Command git -ErrorAction SilentlyContinue)) { throw "git was not found on PATH." }
if (-not (Get-Command node -ErrorAction SilentlyContinue)) { throw "node was not found on PATH. Install Node.js LTS from https://nodejs.org/" }
if (-not (Get-Command npm -ErrorAction SilentlyContinue)) { throw "npm was not found on PATH. Install Node.js LTS from https://nodejs.org/" }

if (-not (Test-Path $serverDir)) {
    git clone --depth 1 https://github.com/gpx1000/mcp-Vulkan.git $serverDir
}
if (-not (Test-Path $buildDir) -or $Rebuild) {
    & git -C $serverDir pull --ff-only
    Push-Location (Join-Path $serverDir "vulkan")
    try { npm install; npm run build } finally { Pop-Location }
    if ($LASTEXITCODE -ne 0) { throw "mcp-Vulkan build failed." }
}

Write-Host "mcp-Vulkan ready: $buildDir"
