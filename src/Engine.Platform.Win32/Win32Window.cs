using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Platform.Win32;

public sealed class Win32Window : IGameWindow
{
    private const int WM_CLOSE = 0x0010;
    private const int WM_DESTROY = 0x0002;
    private const int WM_QUIT = 0x0012;
    private const int WM_SYSCOMMAND = 0x0112;
    private const int WM_SIZE = 0x0005;
    private const int SC_CLOSE = 0xF060;
    private const int GWL_STYLE = -16;
    private const uint WS_VISIBLE = 0x10000000;
    private const uint WS_POPUP = 0x80000000;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint MONITOR_DEFAULTTONEAREST = 2;
    private readonly nint _handle;
    private readonly nint _moduleHandle;
    private readonly WndProcDelegate _windowProcedure;
    private nint _previousProcedure;
    private bool _closed;
    private bool _fullscreen;
    private RECT _windowedRect;
    private long _windowedStyle;

    public Vector2 Size { get; private set; }
    public NativeWindowSurface NativeSurface => new(PlatformKind.Win32, _handle, 0, _moduleHandle);
    public bool ShouldClose => _closed;
    public bool Fullscreen => _fullscreen;
    public void SetTitle(string title) => SetWindowText(_handle, title);
    public void Close() { _closed = true; if (_handle != 0) DestroyWindow(_handle); }

    public void SetFullscreen(bool fullscreen)
    {
        if (fullscreen == _fullscreen) return;
        if (fullscreen)
        {
            GetWindowRect(_handle, out _windowedRect);
            _windowedStyle = GetWindowLongPtr(_handle, GWL_STYLE);
            nint monitor = MonitorFromWindow(_handle, MONITOR_DEFAULTTONEAREST);
            MONITORINFO info = new() { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
            if (GetMonitorInfo(monitor, ref info))
            {
                SetWindowLongPtr(_handle, GWL_STYLE, unchecked((nint)(WS_VISIBLE | WS_POPUP)));
                SetWindowPos(_handle, 0, info.rcMonitor.Left, info.rcMonitor.Top, info.rcMonitor.Right - info.rcMonitor.Left, info.rcMonitor.Bottom - info.rcMonitor.Top, SWP_FRAMECHANGED);
            }
        }
        else
        {
            SetWindowLongPtr(_handle, GWL_STYLE, (nint)_windowedStyle);
            SetWindowPos(_handle, 0, _windowedRect.Left, _windowedRect.Top, _windowedRect.Right - _windowedRect.Left, _windowedRect.Bottom - _windowedRect.Top, SWP_FRAMECHANGED);
        }
        _fullscreen = fullscreen;
    }

    public Win32Window(int width, int height, string title)
    {
        _moduleHandle = GetModuleHandle(null);
        int x = Math.Max(0, (GetSystemMetrics(SM_CXSCREEN) - width) / 2);
        int y = Math.Max(0, (GetSystemMetrics(SM_CYSCREEN) - height) / 2);
        _handle = CreateWindowEx(0, "STATIC", title, 0x10CF0000, x, y, width, height, 0, 0, _moduleHandle, 0);
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
        if (message == WM_SIZE)
        {
            int width = (int)(lParam & 0xFFFF);
            int height = (int)((lParam >> 16) & 0xFFFF);
            if (width > 0 && height > 0) Size = new(width, height);
        }
        return CallWindowProc(_previousProcedure, handle, message, wParam, lParam);
    }

    [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X; public int Y; }
    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MONITORINFO { public uint cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags; }
    [StructLayout(LayoutKind.Sequential)] private struct MSG { public nint hwnd; public uint message; public nint wParam; public nint lParam; public uint time; public POINT point; }
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
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
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern long GetWindowLongPtr(nint handle, long index);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint handle, out RECT rect);
    [DllImport("user32.dll")] private static extern nint MonitorFromWindow(nint handle, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(nint monitor, ref MONITORINFO info);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(nint handle, nint insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern nint CallWindowProc(nint previousProcedure, nint handle, uint message, nint wParam, nint lParam);
    [DllImport("user32.dll")] private static extern void PostQuitMessage(int exitCode);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern nint GetModuleHandle(string? name);
}
