using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Platform.Win32;

public sealed class Win32Window : IGameWindow
{
    private const int WM_CLOSE = 0x0010;
    private const int WM_DESTROY = 0x0002;
    private const int WM_QUIT = 0x0012;
    private const int WM_SYSCOMMAND = 0x0112;
    private const int SC_CLOSE = 0xF060;
    private readonly nint _handle;
    private readonly WndProcDelegate _windowProcedure;
    private nint _previousProcedure;
    private bool _closed;

    public Vector2 Size { get; private set; }
    public nint Handle => _handle;
    public nint ModuleHandle { get; }
    public bool ShouldClose => _closed;
    public void SetTitle(string title) => SetWindowText(_handle, title);
    public void Close() { _closed = true; if (_handle != 0) DestroyWindow(_handle); }

    public Win32Window(int width, int height, string title)
    {
        ModuleHandle = GetModuleHandle(null);
        _handle = CreateWindowEx(0, "STATIC", title, 0x10CF0000, 100, 100, width, height, 0, 0, ModuleHandle, 0);
        if (_handle == 0) throw new InvalidOperationException("Unable to create Win32 window.");
        _windowProcedure = WindowProcedure;
        _previousProcedure = SetWindowLongPtr(_handle, -4, Marshal.GetFunctionPointerForDelegate(_windowProcedure));
        Size = new(width, height);
        ShowWindow(_handle, 5);
    }

    public void PumpEvents()
    {
        // Drain all pending messages before simulation so input and close events
        // are handled promptly by the fixed-step loop.
        while (PeekMessage(out MSG message, 0, 0, 0, 1))
        {
            if (message.message is WM_CLOSE or WM_DESTROY or WM_QUIT) _closed = true;
            if (message.message == WM_SYSCOMMAND && (message.wParam.ToInt64() & 0xFFF0) == SC_CLOSE) { Close(); continue; }
            TranslateMessage(ref message);
            DispatchMessage(ref message);
        }
        if (_handle != 0 && !IsWindow(_handle)) _closed = true;
    }

    public void Dispose()
    {
        if (_handle != 0 && IsWindow(_handle)) DestroyWindow(_handle);
    }

    private nint WindowProcedure(nint handle, uint message, nint wParam, nint lParam)
    {
        // Intercept close messages explicitly; relying on the STATIC class procedure
        // does not provide a reliable signal to the game loop.
        if (message == WM_CLOSE) { _closed = true; DestroyWindow(handle); return 0; }
        if (message == WM_DESTROY) { _closed = true; PostQuitMessage(0); return 0; }
        return CallWindowProc(_previousProcedure, handle, message, wParam, lParam);
    }

    [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X; public int Y; }
    [StructLayout(LayoutKind.Sequential)] private struct MSG { public nint hwnd; public uint message; public nint wParam; public nint lParam; public uint time; public POINT point; }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern nint CreateWindowEx(uint exStyle, string className, string windowName, uint style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint param);
    [DllImport("user32.dll")] private static extern bool DestroyWindow(nint handle);
    [DllImport("user32.dll")] private static extern bool ShowWindow(nint handle, int command);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool SetWindowText(nint handle, string title);
    [DllImport("user32.dll")] private static extern bool PeekMessage(out MSG message, nint handle, uint min, uint max, uint remove);
    [DllImport("user32.dll")] private static extern bool TranslateMessage(ref MSG message);
    [DllImport("user32.dll")] private static extern nint DispatchMessage(ref MSG message);
    [DllImport("user32.dll")] private static extern bool IsWindow(nint handle);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate nint WndProcDelegate(nint handle, uint message, nint wParam, nint lParam);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern nint SetWindowLongPtr(nint handle, int index, nint value);
    [DllImport("user32.dll")] private static extern nint CallWindowProc(nint previousProcedure, nint handle, uint message, nint wParam, nint lParam);
    [DllImport("user32.dll")] private static extern void PostQuitMessage(int exitCode);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern nint GetModuleHandle(string? name);
}
