using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Platform.Win32;

namespace IsometricSandbox.Game;

public sealed class Win32TileRenderer : IDisposable
{
    private readonly Win32Window _window;
    private readonly nint _windowDc;
    private nint _bufferDc;
    private nint _bufferBitmap;
    private nint _previousBitmap;
    private readonly POINT[] _points = new POINT[4];
    private readonly nint _backgroundBrush = CreateSolidBrush(ColorRef(18, 22, 32));
    private readonly nint _tileBrush = CreateSolidBrush(ColorRef(255, 255, 255));
    private readonly nint _blackPen = CreatePen(0, 1, 0);
    private int _width, _height;
    public Win32TileRenderer(Win32Window window) { _window = window; _windowDc = GetDC(window.Handle); }
    public void Draw(TileMap map, IsometricCamera camera, Vector2 player, float jumpHeight = 0)
    {
        RECT area = new(); GetClientRect(_window.Handle, ref area);
        int width = Math.Max(1, area.Right - area.Left), height = Math.Max(1, area.Bottom - area.Top);
        EnsureBackbuffer(width, height);
        FillRect(_bufferDc, ref area, _backgroundBrush);
        float halfWidth = map.TileWidth * 0.5f;
        float halfHeight = (camera.Isometric ? map.TileHeight : map.TileWidth) * 0.5f;
        for (int y = 0; y < map.Height; y++)
        for (int x = 0; x < map.Width; x++)
        {
            Vector2 center = camera.WorldToScreen(map.TileToWorld(x, y), map);
            if (center.X < -map.TileWidth || center.X > width + map.TileWidth || center.Y < -halfHeight * 2 || center.Y > height + halfHeight * 2) continue;
            if (camera.Isometric) DrawDiamond(_bufferDc, center, halfWidth, halfHeight);
            else DrawBox(_bufferDc, center, halfWidth, halfHeight);
        }
        if (camera.Isometric) DrawDiamond(_bufferDc, camera.WorldToScreen(player, map) - new Vector2(0, jumpHeight), 20, 10);
        else DrawBox(_bufferDc, camera.WorldToScreen(player, map) - new Vector2(0, jumpHeight), 20, 20);
        BitBlt(_windowDc, 0, 0, width, height, _bufferDc, 0, 0, 0x00CC0020);
        ValidateRect(_window.Handle, IntPtr.Zero);
    }
    private void EnsureBackbuffer(int width, int height)
    {
        if (_bufferDc != 0 && _width == width && _height == height) return;
        // Draw off-screen and copy once with BitBlt; direct window drawing flickers
        // because Windows can erase the client area between draw operations.
        if (_bufferDc != 0) { SelectObject(_bufferDc, _previousBitmap); DeleteObject(_bufferBitmap); DeleteDC(_bufferDc); }
        _bufferDc = CreateCompatibleDC(_windowDc); _bufferBitmap = CreateCompatibleBitmap(_windowDc, width, height); _previousBitmap = SelectObject(_bufferDc, _bufferBitmap); _width = width; _height = height;
    }
    private void DrawDiamond(nint dc, Vector2 center, float halfWidth, float halfHeight)
    {
        _points[0] = new((int)center.X, (int)(center.Y - halfHeight)); _points[1] = new((int)(center.X + halfWidth), (int)center.Y); _points[2] = new((int)center.X, (int)(center.Y + halfHeight)); _points[3] = new((int)(center.X - halfWidth), (int)center.Y);
        nint previousPen = SelectObject(dc, _blackPen);
        Polygon(dc, _points, 4, _tileBrush);
        SelectObject(dc, previousPen);
    }
    private void DrawBox(nint dc, Vector2 center, float halfWidth, float halfHeight)
    {
        nint previousPen = SelectObject(dc, _blackPen);
        nint previousBrush = SelectObject(dc, _tileBrush);
        Rectangle(dc, (int)(center.X - halfWidth), (int)(center.Y - halfHeight), (int)(center.X + halfWidth), (int)(center.Y + halfHeight));
        SelectObject(dc, previousPen);
        SelectObject(dc, previousBrush);
    }
    public void Dispose() { if (_bufferDc != 0) { SelectObject(_bufferDc, _previousBitmap); DeleteObject(_bufferBitmap); DeleteDC(_bufferDc); } DeleteObject(_backgroundBrush); DeleteObject(_tileBrush); DeleteObject(_blackPen); ReleaseDC(_window.Handle, _windowDc); }
    private static uint ColorRef(byte r, byte g, byte b) => (uint)(r | (g << 8) | (b << 16));
    [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X, Y; public POINT(int x, int y) { X = x; Y = y; } }
    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern nint GetDC(nint window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(nint window, nint dc);
    [DllImport("user32.dll")] private static extern bool GetClientRect(nint window, ref RECT rect);
    [DllImport("user32.dll")] private static extern bool ValidateRect(nint window, nint rect);
    [DllImport("user32.dll")] private static extern int FillRect(nint dc, ref RECT rect, nint brush);
    [DllImport("gdi32.dll")] private static extern nint CreateSolidBrush(uint color);
    [DllImport("gdi32.dll")] private static extern nint CreatePen(int style, int width, uint color);
    [DllImport("gdi32.dll")] private static extern nint CreateCompatibleDC(nint dc);
    [DllImport("gdi32.dll")] private static extern nint CreateCompatibleBitmap(nint dc, int width, int height);
    [DllImport("gdi32.dll")] private static extern nint SelectObject(nint dc, nint objectHandle);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(nint dc);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(nint destination, int x, int y, int width, int height, nint source, int sourceX, int sourceY, uint operation);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(nint objectHandle);
    [DllImport("gdi32.dll")] private static extern bool Polygon(nint dc, POINT[] points, int count, nint brush);
    [DllImport("gdi32.dll")] private static extern bool Rectangle(nint dc, int left, int top, int right, int bottom);
}
