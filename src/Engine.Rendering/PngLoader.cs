using System.IO;
using StbImageSharp;

namespace Engine.Rendering;

public static class PngLoader
{
    public static TextureHandle? Load(IRenderer renderer, string path, TextureFilter filter = TextureFilter.Nearest)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"[assets] missing {path} — using fallback");
            return null;
        }

        try
        {
            ImageResult image = ImageResult.FromMemory(File.ReadAllBytes(path), ColorComponents.RedGreenBlueAlpha);
            TextureHandle handle = renderer.UploadTexture(image.Data, image.Width, image.Height, filter);
            Console.WriteLine($"[assets] loaded {path} ({image.Width}x{image.Height})");
            return handle;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[assets] failed to load {path}: {ex.Message} — using fallback");
            return null;
        }
    }
}
