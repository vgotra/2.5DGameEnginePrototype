using System.Numerics;
using Engine.App;
using Engine.Rendering;

namespace IsometricSandbox.Game.Rendering;

public static class ProceduralTextures
{
    private const int TextureChannelCount = 4;
    private const int UkraineFlagTextureSize = 16;
    private const int DefaultBlobTextureSize = 24;
    private const float PixelCenterOffset = 0.5f;
    private const float BlobEdgeSoftness = 1.5f;
    private const byte OpaqueAlpha = byte.MaxValue;

    public static TextureHandle UkraineFlag(RenderContext renderer)
    {
        byte[] rgba = new byte[UkraineFlagTextureSize * UkraineFlagTextureSize * TextureChannelCount];
        for (int y = 0; y < UkraineFlagTextureSize; y++)
        {
            bool isBlue = y < UkraineFlagTextureSize / 2;
            for (int x = 0; x < UkraineFlagTextureSize; x++)
            {
                int pixelOffset = (y * UkraineFlagTextureSize + x) * TextureChannelCount;
                rgba[pixelOffset] = isBlue ? (byte)0 : OpaqueAlpha;
                rgba[pixelOffset + 1] = isBlue ? (byte)87 : (byte)215;
                rgba[pixelOffset + 2] = isBlue ? (byte)183 : (byte)0;
                rgba[pixelOffset + 3] = OpaqueAlpha;
            }
        }
        return renderer.UploadTexture(rgba, UkraineFlagTextureSize, UkraineFlagTextureSize, TextureFilter.Nearest);
    }

    public static TextureHandle Blob(RenderContext renderer, Vector4 color, int size = DefaultBlobTextureSize)
    {
        byte[] rgba = new byte[size * size * TextureChannelCount];
        Vector2 center = new(size * PixelCenterOffset, size * PixelCenterOffset);
        float radius = size * PixelCenterOffset;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + PixelCenterOffset, y + PixelCenterOffset), center);
                float alpha = Math.Clamp(radius - distance, 0f, BlobEdgeSoftness) / BlobEdgeSoftness;
                int pixelOffset = (y * size + x) * TextureChannelCount;
                rgba[pixelOffset] = (byte)(color.X * OpaqueAlpha);
                rgba[pixelOffset + 1] = (byte)(color.Y * OpaqueAlpha);
                rgba[pixelOffset + 2] = (byte)(color.Z * OpaqueAlpha);
                rgba[pixelOffset + 3] = (byte)(alpha * OpaqueAlpha);
            }
        }
        return renderer.UploadTexture(rgba, size, size, TextureFilter.Nearest);
    }
}
