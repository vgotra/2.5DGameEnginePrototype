using System.Numerics;
using Engine.Rendering;

namespace IsometricSandbox.Game;

public static class ProceduralTextures
{
    public static TextureHandle UkraineFlag(IRenderer renderer)
    {
        const int size = 16;
        byte[] rgba = new byte[size * size * 4];
        for (int y = 0; y < size; y++)
        {
            bool blue = y < size / 2;
            for (int x = 0; x < size; x++)
            {
                int i = (y * size + x) * 4;
                rgba[i] = blue ? (byte)0 : (byte)255;
                rgba[i + 1] = blue ? (byte)87 : (byte)215;
                rgba[i + 2] = blue ? (byte)183 : (byte)0;
                rgba[i + 3] = 255;
            }
        }
        return renderer.UploadTexture(rgba, size, size, TextureFilter.Nearest);
    }

    public static TextureHandle Blob(IRenderer renderer, Vector4 color, int size = 24)
    {
        byte[] rgba = new byte[size * size * 4];
        Vector2 center = new(size * 0.5f, size * 0.5f);
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = Math.Clamp(radius - d, 0f, 1.5f) / 1.5f;
                int i = (y * size + x) * 4;
                rgba[i] = (byte)(color.X * 255f);
                rgba[i + 1] = (byte)(color.Y * 255f);
                rgba[i + 2] = (byte)(color.Z * 255f);
                rgba[i + 3] = (byte)(alpha * 255f);
            }
        }
        return renderer.UploadTexture(rgba, size, size, TextureFilter.Nearest);
    }
}
