param([switch]$Rebuild)
$repoRoot = Split-Path $PSScriptRoot -Parent
$mcpDir = Join-Path $repoRoot "tools\mcp"
$vulkanDir = Join-Path $mcpDir "mcp-Vulkan"
$vulkanBuild = Join-Path $vulkanDir "vulkan\build\index.js"
$csharpDir = Join-Path $mcpDir "csharp-language-server"
$csharpProj = Join-Path $csharpDir "src\CsharpMcp\CsharpMcp.csproj"
$csharpDll = Join-Path $csharpDir "src\CsharpMcp\bin\Debug\net10.0\CsharpMcp.dll"

if (-not (Get-Command git -ErrorAction SilentlyContinue)) { throw "git was not found on PATH." }
if (-not (Get-Command node -ErrorAction SilentlyContinue)) { throw "node was not found on PATH. Install Node.js LTS from https://nodejs.org/" }
if (-not (Get-Command npm -ErrorAction SilentlyContinue)) { throw "npm was not found on PATH. Install Node.js LTS from https://nodejs.org/" }

if (-not (Test-Path $vulkanDir)) {
    git clone --depth 1 https://github.com/gpx1000/mcp-Vulkan.git $vulkanDir
}
if (-not (Test-Path $vulkanBuild) -or $Rebuild) {
    & git -C $vulkanDir pull --ff-only
    Push-Location (Join-Path $vulkanDir "vulkan")
    try { npm install; npm run build } finally { Pop-Location }
    if ($LASTEXITCODE -ne 0) { throw "mcp-Vulkan build failed." }
}

Write-Host "mcp-Vulkan ready: $vulkanBuild"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "dotnet was not found on PATH; skipping csharp-language-server setup."
    return
}

if (-not (Test-Path $csharpDir)) {
    git clone --depth 1 https://github.com/jgauffin/csharp-language-server.git $csharpDir
}
if (-not (Test-Path $csharpDll) -or $Rebuild) {
    & git -C $csharpDir pull --ff-only
    & dotnet build $csharpProj -p:ManagePackageVersionsCentrally=false
    if ($LASTEXITCODE -ne 0) { throw "csharp-language-server build failed." }
}

Write-Host "csharp-language-mcp ready: $csharpDll"
