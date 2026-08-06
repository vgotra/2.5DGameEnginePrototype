using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Rendering.Vulkan;

[StructLayout(LayoutKind.Sequential)]
public struct CameraPushConstants
{
    public Vector2 Viewport;
}
