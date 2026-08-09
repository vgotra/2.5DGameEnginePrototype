namespace Engine.Rendering;

public readonly struct PngImage
{
    public readonly byte[] Data;
    public readonly int Width;
    public readonly int Height;

    public PngImage(byte[] data, int width, int height)
    {
        Data = data;
        Width = width;
        Height = height;
    }
}
