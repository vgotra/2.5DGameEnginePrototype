using System.Numerics;

namespace Engine.Assets;

public readonly record struct ModelHandle(int Value);
public readonly record struct GltfMaterialData(Vector4 BaseColor, float Metallic, float Roughness, int BaseColorTexture, int NormalTexture, int OcclusionTexture, int EmissiveTexture, Vector3 EmissiveColor);
public readonly record struct GltfVertex(Vector3 Position, Vector3 Normal, Vector2 TexCoord, Vector4 Weights, Vector4 Joints);
public readonly record struct GltfPrimitiveRange(int FirstIndex, int IndexCount, int MaterialIndex);
public readonly record struct GltfNode(int Parent, Vector3 Translation, Quaternion Rotation, Vector3 Scale, int Mesh, int Skin);
public readonly record struct GltfAnimationChannel(int Node, byte Path, byte Interpolation, int FirstKey, int KeyCount);
public readonly record struct GltfAnimationClip(string Name, float DurationSeconds, int FirstChannel, int ChannelCount);
public readonly record struct GltfImageAsset(string Id, string MimeType, byte[] Rgba, int Width, int Height);
public readonly record struct GltfTextureAsset(string Id, int ImageIndex);
public readonly record struct GltfAnimationSampler(int InputAccessor, int OutputAccessor, byte Interpolation);
public readonly record struct GltfSkinData(int[] Joints, Matrix4x4[] InverseBindMatrices);
public readonly record struct GltfModelAsset(ModelHandle Handle, string StableId, GltfVertex[] Vertices, int[] Indices, GltfPrimitiveRange[] Primitives, GltfMaterialData[] Materials, GltfNode[] Nodes, int[] Joints, GltfAnimationChannel[] Channels, GltfAnimationClip[] Animations)
{
    public GltfImageAsset[] Images { get; init; } = [];
    public GltfTextureAsset[] Textures { get; init; } = [];
    public GltfAnimationSampler[] Samplers { get; init; } = [];
    public GltfSkinData[] Skins { get; init; } = [];
    public GltfAnimationTrack[] Tracks { get; init; } = [];
}
public readonly record struct GltfSpriteBakeSettings(int FrameWidth, int FrameHeight, int Directions, int FramesPerClip, int FramesPerSecond)
{
    public string Clip { get; init; } = "default";
    public GltfTextureFilter TextureFilter { get; init; } = GltfTextureFilter.Nearest;
    public float AlphaThreshold { get; init; } = 0.01f;
}
public readonly record struct GltfSpriteFrame(int FrameIndex, int Direction, Vector2 UvOffset, Vector2 UvScale, Vector2 Anchor);
public readonly record struct GltfSpriteAtlas(string StableId, byte[] Rgba, int Width, int Height, int FrameWidth, int FrameHeight, int FrameCount, int Directions)
{
    public string Clip { get; init; } = "default";
    public int FramesPerSecond { get; init; }
    public GltfSpriteFrame[] Frames { get; init; } = [];
    public Vector2 Anchor { get; init; }
}
