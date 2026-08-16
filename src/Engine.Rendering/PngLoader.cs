using System.IO;
using StbImageSharp;

namespace Engine.Rendering;

public static class PngLoader
{
    public static PngImage? Decode(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"[assets] missing {path} — using fallback");
            return null;
        }

        try
        {
            ImageResult image = ImageResult.FromMemory(File.ReadAllBytes(path), ColorComponents.RedGreenBlueAlpha);
            Console.WriteLine($"[assets] loaded {path} ({image.Width}x{image.Height})");
            return new PngImage(image.Data, image.Width, image.Height);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[assets] failed to load {path}: {ex.Message} — using fallback");
            return null;
        }
    }

    internal static TextureHandle? Load(IRenderer renderer, string path, TextureFilter filter = TextureFilter.Nearest)
    {
        PngImage? image = Decode(path);
        if (!image.HasValue) return null;
        return renderer.UploadTexture(image.Value.Data, image.Value.Width, image.Value.Height, filter);
    }

}
