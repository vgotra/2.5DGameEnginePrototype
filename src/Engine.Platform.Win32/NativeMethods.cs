using System.Runtime.InteropServices;

namespace Engine.Platform.Win32;

/// <summary>
/// Win32 P/Invoke surface, source-generated via [LibraryImport] for AOT/trim-safety
/// and to remove runtime marshalling stubs. UTF-16 string APIs use StringMarshalling.Utf16
/// (the default under [DllImport(CharSet.Unicode]); LibraryImport defaults to UTF-8).
/// SetLastError is omitted because no caller queries Marshal.GetLastPInvokeError().
/// </summary>
internal static partial class NativeMethods
{
    // ---- user32.dll ----

    [LibraryImport("user32.dll")]
    internal static partial int GetSystemMetrics(int index);

    [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint CreateWindowEx(
        uint exStyle,
        string className,
        string windowName,
        uint style,
        int x,
        int y,
        int width,
        int height,
        nint parent,
        nint menu,
        nint instance,
        nint param);

    [LibraryImport("user32.dll")]
    internal static partial int DestroyWindow(nint handle);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ShowWindow(nint handle, int command);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowTextW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetWindowText(nint handle, string title);

    // Message-pump exports: PeekMessage, DispatchMessage, and CallWindowProc exist
    // in user32.dll only as their A and W variants (WinUser.h expands them via
    // #ifdef UNICODE); there is no bare literal export. TranslateMessage is the
    // one exception - it is a literal-only export with no A/W variant. The old
    // [DllImport] with the default Ansi charset silently appended 'A' and bound
    // to the A variants; [LibraryImport] uses the EntryPoint verbatim with no
    // suffix fallback, so we pin PeekMessageA/DispatchMessageA/CallWindowProcA
    // to preserve the original Ansi binding. (MSG has no text fields; the A/W
    // distinction is moot for our WndProc, but exact preservation is the
    // roadmap mandate.)
    [LibraryImport("user32.dll", EntryPoint = "PeekMessageA")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool PeekMessage(out MSG message, nint handle, uint min, uint max, uint remove);

    [LibraryImport("user32.dll", EntryPoint = "TranslateMessage")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool TranslateMessage(ref MSG message);

    [LibraryImport("user32.dll", EntryPoint = "DispatchMessageA")]
    internal static partial nint DispatchMessage(ref MSG message);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsWindow(nint handle);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    internal static partial nint SetWindowLongPtr(nint handle, int index, nint value);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    internal static partial nint GetWindowLongPtr(nint handle, int index);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetWindowRect(nint handle, out RECT rect);

    [LibraryImport("user32.dll")]
    internal static partial nint MonitorFromWindow(nint handle, uint flags);

    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetMonitorInfo(nint monitor, ref MONITORINFO info);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetWindowPos(
        nint handle,
        nint insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [LibraryImport("user32.dll", EntryPoint = "CallWindowProcA")]
    internal static partial nint CallWindowProc(nint previousProcedure, nint handle, uint message, nint wParam, nint lParam);

    [LibraryImport("user32.dll")]
    internal static partial void PostQuitMessage(int exitCode);

    [LibraryImport("user32.dll")]
    internal static partial nint GetForegroundWindow();

    [LibraryImport("user32.dll")]
    internal static partial short GetAsyncKeyState(int key);

    // ---- kernel32.dll ----

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint GetModuleHandle(string? name);
}