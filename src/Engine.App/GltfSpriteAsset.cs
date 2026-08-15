using System.Numerics;
using Engine.Rendering;

namespace Engine.App;

public readonly record struct GltfAssetId(string Value);
public readonly record struct SpriteAnimationClip(string Name, int FirstFrame, int FrameCount, int FramesPerSecond);
public readonly record struct GltfSpriteAsset(GltfAssetId Id, string Source, string Atlas, int FrameWidth, int FrameHeight, int FrameCount)
{
    public int Directions { get; init; } = 1;
    public SpriteAnimationClip[] Clips { get; init; } = [];
}
public readonly record struct GltfCookedCharacter(GltfAssetId Id, TextureHandle Atlas, int FrameWidth, int FrameHeight, int FrameCount, int Directions, SpriteAnimationClip[] Clips, Vector2[] UvOffsets, Vector2[] UvScales);

public sealed class GltfSpriteManifest
{
    private readonly Dictionary<GltfAssetId, GltfSpriteAsset> _assets = new();

    public void Add(in GltfSpriteAsset asset) => _assets[asset.Id] = asset;

    public bool TryGet(GltfAssetId id, out GltfSpriteAsset asset) => _assets.TryGetValue(id, out asset);
}
