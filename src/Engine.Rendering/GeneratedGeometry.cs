using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Rendering;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct ShapeVertex(Vector2 Position, Vector4 Color);
