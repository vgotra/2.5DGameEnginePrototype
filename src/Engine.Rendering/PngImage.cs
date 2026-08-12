namespace Engine.Rendering;

public readonly struct PngImage(byte[] data, int width, int height)
{
    public readonly byte[] Data = data;
    public readonly int Width = width;
    public readonly int Height = height;
}
