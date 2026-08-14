namespace Engine.Assets;

public enum TextureAssetState : byte
{
    Unrequested,
    Queued,
    Decoding,
    Decoded,
    Resident,
    Failed
}

public readonly record struct TextureAssetHandle(int Value)
{
    public bool IsValid => Value >= 0;
    public static TextureAssetHandle Invalid => new(-1);
}
