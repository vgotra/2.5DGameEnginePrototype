using Engine.Assets;
using Engine.Rendering;
using Engine.Threading;

namespace Engine.Tests;

internal static class TextureAssetTests
{
    public static readonly TestCase[] Tests =
    [
        new(nameof(Request_SamePathReturnsSameHandle), Request_SamePathReturnsSameHandle),
        new(nameof(MissingTextureTransitionsToFailed), MissingTextureTransitionsToFailed),
        new(nameof(DecodedTextureOwnsUnmanagedBytes), DecodedTextureOwnsUnmanagedBytes)
    ];

    private static void Request_SamePathReturnsSameHandle()
    {
        using JobSystem jobs = new(1);
        using TextureAssetCatalog assets = new(jobs);
        TextureAssetHandle first = assets.Request(Path.Combine(Path.GetTempPath(), "missing-texture.png"));
        TextureAssetHandle second = assets.Request(Path.Combine(Path.GetTempPath(), ".", "missing-texture.png"));
        TestAssert.True(first == second, "normalized duplicate requests share a handle");
    }

    private static void MissingTextureTransitionsToFailed()
    {
        using JobSystem jobs = new(1);
        using TextureAssetCatalog assets = new(jobs);
        TextureAssetHandle handle = assets.Request(Path.Combine(Path.GetTempPath(), "missing-texture.png"));
        for (int i = 0; i < 1000 && assets.GetState(handle) is not TextureAssetState.Failed; i++) Thread.Yield();
        TestAssert.True(assets.GetState(handle) == TextureAssetState.Failed, "missing texture fails without blocking the caller");
    }

    private static void DecodedTextureOwnsUnmanagedBytes()
    {
        byte[] pixels = [1, 2, 3, 4];
        DecodedTextureData decoded = DecodedTextureData.FromPng(new PngImage(pixels, 1, 1), TextureFilter.Nearest);
        TestAssert.True(decoded.AsSpan().SequenceEqual(pixels), "decoded bytes are readable");
        decoded.Dispose();
        TestAssert.True(!decoded.IsValid, "decoded bytes are released");
    }
}
