using System.Numerics;

namespace Engine.Assets;

public enum GltfTextureFilter : byte { Nearest, Bilinear }

public static class GltfTextureSampler
{
    public static Vector4 Sample(in GltfImageAsset image, Vector2 uv, GltfTextureFilter filter, Vector4 fallback)
    {
        if (image.Width <= 0 || image.Height <= 0 || image.Rgba.Length < image.Width * image.Height * 4) return fallback;
        float x = Math.Clamp(uv.X, 0f, 1f) * (image.Width - 1);
        float y = Math.Clamp(1f - uv.Y, 0f, 1f) * (image.Height - 1);
        if (filter == GltfTextureFilter.Nearest) return Read(image, (int)MathF.Round(x), (int)MathF.Round(y));
        int x0 = (int)MathF.Floor(x), y0 = (int)MathF.Floor(y);
        int x1 = Math.Min(x0 + 1, image.Width - 1), y1 = Math.Min(y0 + 1, image.Height - 1);
        Vector4 a = Read(image, x0, y0), b = Read(image, x1, y0), c = Read(image, x0, y1), d = Read(image, x1, y1);
        Vector4 top = Vector4.Lerp(a, b, x - x0);
        Vector4 bottom = Vector4.Lerp(c, d, x - x0);
        return Vector4.Lerp(top, bottom, y - y0);
    }

    private static Vector4 Read(in GltfImageAsset image, int x, int y)
    {
        int i = (y * image.Width + x) * 4;
        return new Vector4(image.Rgba[i] / 255f, image.Rgba[i + 1] / 255f, image.Rgba[i + 2] / 255f, image.Rgba[i + 3] / 255f);
    }
}
