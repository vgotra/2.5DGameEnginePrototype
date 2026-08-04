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
    private const int SIZE_MINIMIZED = 1;
    private const int SC_CLOSE = 0xF060;
    private const int GWL_STYLE = -16;
    private const int GWL_WNDPROC = -4;
    private const uint WS_VISIBLE = 0x10000000;
    private const uint WS_POPUP = 0x80000000;
    private const uint WS_OVERLAPPED = 0x00000000;
    private const uint WS_CAPTION = 0x00C00000;
    private const uint WS_SYSMENU = 0x00080000;
    private const uint WS_THICKFRAME = 0x00040000;
    private const uint WS_MINIMIZEBOX = 0x00020000;
    private const uint WS_MAXIMIZEBOX = 0x00010000;
    private const uint WS_CLIPSIBLINGS = 0x04000000;
    private const uint WS_CLIPCHILDREN = 0x02000000;
    private const uint WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
    private const int SW_SHOW = 5;
    private const long DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint MONITOR_DEFAULTTONEAREST = 2;
    private readonly nint _handle;
    private readonly nint _moduleHandle;
    private readonly WndProcDelegate _windowProcedure;
    private nint _previousProcedure;
    private bool _closed;
    private bool _fullscreen;
    private bool _minimized;
    private RECT _windowedRect;
    private nint _windowedStyle;

    public Vector2 Size { get; private set; }
    public NativeWindowSurface NativeSurface => new(PlatformKind.Win32, _handle, 0, _moduleHandle);
    public bool ShouldClose => _closed;
    public bool Fullscreen => _fullscreen;
    public bool IsMinimized => _minimized;
    public void SetTitle(string title) => NativeMethods.SetWindowText(_handle, title);
    public void Close() { _closed = true; if (_handle != 0) NativeMethods.DestroyWindow(_handle); }

    public void SetFullscreen(bool fullscreen)
    {
        if (fullscreen == _fullscreen) return;
        if (fullscreen)
        {
            NativeMethods.GetWindowRect(_handle, out _windowedRect);
            _windowedStyle = NativeMethods.GetWindowLongPtr(_handle, GWL_STYLE);
            nint monitor = NativeMethods.MonitorFromWindow(_handle, MONITOR_DEFAULTTONEAREST);
            MONITORINFO info = new() { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
            if (NativeMethods.GetMonitorInfo(monitor, ref info))
            {
                NativeMethods.SetWindowLongPtr(_handle, GWL_STYLE, unchecked((nint)(WS_VISIBLE | WS_POPUP)));
                NativeMethods.SetWindowPos(_handle, 0, info.rcMonitor.Left, info.rcMonitor.Top, info.rcMonitor.Right - info.rcMonitor.Left, info.rcMonitor.Bottom - info.rcMonitor.Top, SWP_FRAMECHANGED);
            }
        }
        else
        {
            NativeMethods.SetWindowLongPtr(_handle, GWL_STYLE, _windowedStyle);
            NativeMethods.SetWindowPos(_handle, 0, _windowedRect.Left, _windowedRect.Top, _windowedRect.Right - _windowedRect.Left, _windowedRect.Bottom - _windowedRect.Top, SWP_FRAMECHANGED);
        }
        _fullscreen = fullscreen;
    }

    public Win32Window(int width, int height, string title)
    {
        NativeMethods.SetProcessDpiAwarenessContext((nint)DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        _moduleHandle = NativeMethods.GetModuleHandle(null);
        int x = Math.Max(0, (NativeMethods.GetSystemMetrics(SM_CXSCREEN) - width) / 2);
        int y = Math.Max(0, (NativeMethods.GetSystemMetrics(SM_CYSCREEN) - height) / 2);
        _handle = NativeMethods.CreateWindowEx(0, "STATIC", title, WS_CLIPSIBLINGS | WS_CLIPCHILDREN | WS_OVERLAPPEDWINDOW | WS_VISIBLE, x, y, width, height, 0, 0, _moduleHandle, 0);
        if (_handle == 0) throw new InvalidOperationException("Unable to create Win32 window.");
        _windowProcedure = WindowProcedure;
        _previousProcedure = NativeMethods.SetWindowLongPtr(_handle, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_windowProcedure));
        Size = new(width, height);
        NativeMethods.ShowWindow(_handle, SW_SHOW);
    }

    public void PumpEvents()
    {
        while (NativeMethods.PeekMessage(out MSG message, 0, 0, 0, 1))
        {
            if (message.message is WM_CLOSE or WM_DESTROY or WM_QUIT) _closed = true;
            if (message.message == WM_SYSCOMMAND && (message.wParam.ToInt64() & 0xFFF0) == SC_CLOSE) { Close(); continue; }
            NativeMethods.TranslateMessage(ref message);
            NativeMethods.DispatchMessage(ref message);
        }
        if (_handle != 0 && !NativeMethods.IsWindow(_handle)) _closed = true;
    }

    public void Dispose()
    {
        if (_handle != 0 && NativeMethods.IsWindow(_handle)) NativeMethods.DestroyWindow(_handle);
    }

    private nint WindowProcedure(nint handle, uint message, nint wParam, nint lParam)
    {
        if (message == WM_CLOSE) { _closed = true; NativeMethods.DestroyWindow(handle); return 0; }
        if (message == WM_DESTROY) { _closed = true; NativeMethods.PostQuitMessage(0); return 0; }
        if (message == WM_SIZE)
        {
            _minimized = wParam.ToInt64() == SIZE_MINIMIZED;
            int width = (int)(lParam & 0xFFFF);
            int height = (int)((lParam >> 16) & 0xFFFF);
            if (width > 0 && height > 0) Size = new(width, height);
        }
        return NativeMethods.CallWindowProc(_previousProcedure, handle, message, wParam, lParam);
    }

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate nint WndProcDelegate(nint handle, uint message, nint wParam, nint lParam);
}