param([string]$Compiler = "glslc")
$shaderRoot = Join-Path $PSScriptRoot "..\assets\shaders"
if (-not (Get-Command $Compiler -ErrorAction SilentlyContinue)) { throw "Shader compiler '$Compiler' was not found. Install Vulkan SDK or glslc." }
& $Compiler -fshader-stage=vertex (Join-Path $shaderRoot "shape.vert.glsl") -o (Join-Path $shaderRoot "shape.vert.spv")
& $Compiler -fshader-stage=fragment (Join-Path $shaderRoot "shape.frag.glsl") -o (Join-Path $shaderRoot "shape.frag.spv")
if ($LASTEXITCODE -ne 0) { throw "Shader compilation failed." }
