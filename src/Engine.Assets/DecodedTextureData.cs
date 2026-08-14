using System.Runtime.InteropServices;
using Engine.Rendering;

namespace Engine.Assets;

public unsafe struct DecodedTextureData : IDisposable
{
    private nint _memory;
    public int Width;
    public int Height;
    public TextureFilter Filter;

    public bool IsValid => _memory != 0;
    public int ByteLength => checked(Width * Height * 4);

    public static DecodedTextureData FromPng(PngImage image, TextureFilter filter)
    {
        int length = checked(image.Width * image.Height * 4);
        nint memory = (nint)NativeMemory.Alloc((nuint)length);
        Marshal.Copy(image.Data, 0, memory, length);
        return new DecodedTextureData { _memory = memory, Width = image.Width, Height = image.Height, Filter = filter };
    }

    public ReadOnlySpan<byte> AsSpan()
    {
        if (_memory == 0) return ReadOnlySpan<byte>.Empty;
        return new ReadOnlySpan<byte>((void*)_memory, ByteLength);
    }

    public void Dispose()
    {
        if (_memory == 0) return;
        NativeMemory.Free((void*)_memory);
        _memory = 0;
        Width = 0;
        Height = 0;
    }
}
