using System.Runtime.InteropServices;

namespace Engine.Platform.Win32;

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    public int X;
    public int Y;
}
