using System.Runtime.InteropServices;

namespace Engine.Platform.Win32;

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}
