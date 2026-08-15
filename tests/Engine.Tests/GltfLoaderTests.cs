using System.Numerics;
using System.Text;
using Engine.Assets;

namespace Engine.Tests;

internal static class GltfLoaderTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(Loader_ReadsEmbeddedMinimalGltf), Loader_ReadsEmbeddedMinimalGltf),
        new(nameof(ManifestReader_ReadsBakeMetadata), ManifestReader_ReadsBakeMetadata),
        new(nameof(SpriteBaker_IsDeterministicAndWritesPixels), SpriteBaker_IsDeterministicAndWritesPixels),
        new(nameof(TextureSampler_ClampsAndInterpolates), TextureSampler_ClampsAndInterpolates),
        new(nameof(PoseEvaluator_InterpolatesTrack), PoseEvaluator_InterpolatesTrack)
        ,new(nameof(Manifest_RejectsUnsortedOrInvalidEntries), Manifest_RejectsUnsortedOrInvalidEntries)
    ];

    private static void Loader_ReadsEmbeddedMinimalGltf()
    {
        byte[] bytes = new byte[42];
        WriteFloat(bytes, 0, 0); WriteFloat(bytes, 4, 0); WriteFloat(bytes, 8, 0);
        WriteFloat(bytes, 12, 1); WriteFloat(bytes, 16, 0); WriteFloat(bytes, 20, 0);
        WriteFloat(bytes, 24, 0); WriteFloat(bytes, 28, 1); WriteFloat(bytes, 32, 0);
        bytes[36] = 0; bytes[37] = 0; bytes[38] = 1; bytes[39] = 0; bytes[40] = 2; bytes[41] = 0;
        string uri = "data:application/octet-stream;base64," + Convert.ToBase64String(bytes);
        string json = $$"""{"asset":{"version":"2.0"},"buffers":[{"byteLength":42,"uri":"{{uri}}"}],"bufferViews":[{"buffer":0,"byteOffset":0,"byteLength":36},{"buffer":0,"byteOffset":36,"byteLength":6}],"accessors":[{"bufferView":0,"componentType":5126,"count":3,"type":"VEC3"},{"bufferView":1,"componentType":5123,"count":3,"type":"SCALAR"}],"meshes":[{"primitives":[{"attributes":{"POSITION":0},"indices":1,"mode":4}]}]}""";
        string path = Path.Combine(Path.GetTempPath(), "minimal-gameplay.gltf");
        File.WriteAllText(path, json, Encoding.UTF8);
        try
        {
            TestAssert.True(GltfLoader.TryLoad(path, new ModelHandle(1), out GltfModelAsset asset, out string error), error);
            TestAssert.True(asset.Vertices.Length == 3 && asset.Indices.Length == 3, "minimal glTF geometry decoded");
            TestAssert.True(asset.Vertices[1].Position == Vector3.UnitX, "vertex positions decoded");
        }
        finally { File.Delete(path); }
    }

    private static void ManifestReader_ReadsBakeMetadata()
    {
        string path = Path.Combine(Path.GetTempPath(), "game-bake.json");
        File.WriteAllText(path, "{\"version\":1,\"assets\":[{\"id\":\"rogue\",\"source\":\"rogue.gltf\",\"atlas\":\"rogue.png\",\"bake\":{\"directions\":8,\"frameWidth\":64,\"frameHeight\":64,\"atlasWidth\":512,\"atlasHeight\":512,\"clips\":[{\"name\":\"idle\",\"firstFrame\":0,\"frameCount\":64,\"framesPerSecond\":12}]}}]}");
        try
        {
            TestAssert.True(GltfBakeManifestReader.TryRead(path, out GltfBakeEntry[] entries, out string error), error);
            TestAssert.True(entries.Length == 1 && entries[0].Directions == 8 && entries[0].Clips[0].FrameCount == 64, "bake metadata is available to the asset library");
        }
        finally { File.Delete(path); }
    }

    private static void SpriteBaker_IsDeterministicAndWritesPixels()
    {
        GltfVertex[] vertices = [new(new Vector3(-1, 0, 0), Vector3.UnitZ, Vector2.Zero, Vector4.Zero, Vector4.Zero), new(new Vector3(1, 0, 0), Vector3.UnitZ, Vector2.UnitX, Vector4.Zero, Vector4.Zero), new(new Vector3(0, 1, 0), Vector3.UnitZ, Vector2.UnitY, Vector4.Zero, Vector4.Zero)];
        GltfModelAsset model = new(new ModelHandle(2), "triangle", vertices, [0, 1, 2], [new GltfPrimitiveRange(0, 3, 0)], [new GltfMaterialData(new Vector4(1, 0, 0, 1), 0, 1, -1, -1, -1, -1, Vector3.Zero)], [], [], [], []);
        GltfSpriteBakeSettings settings = new(16, 16, 4, 2, 12);
        TestAssert.True(GltfSpriteBaker.TryBake(in model, in settings, out GltfSpriteAtlas first, out string error), error);
        TestAssert.True(GltfSpriteBaker.TryBake(in model, in settings, out GltfSpriteAtlas second, out error), error);
        TestAssert.True(first.Width == 64 && first.Height == 32 && first.Rgba.AsSpan().SequenceEqual(second.Rgba), "sprite bake is deterministic");
        TestAssert.True(first.Rgba.Any(static value => value != 0), "sprite bake writes pixels");
        TestAssert.True(first.Frames.Length == 8 && first.Frames[0].UvScale.X == 0.25f, "sprite frame UV metadata is deterministic");
    }

    private static void TextureSampler_ClampsAndInterpolates()
    {
        GltfImageAsset image = new("test", "image/raw", [255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 255, 255], 2, 2);
        Vector4 nearest = GltfTextureSampler.Sample(in image, new(-1, 0), GltfTextureFilter.Nearest, Vector4.One);
        Vector4 linear = GltfTextureSampler.Sample(in image, new(0.5f, 0.5f), GltfTextureFilter.Bilinear, Vector4.Zero);
        TestAssert.True(nearest.Z > 0.9f && nearest.X < 0.1f, "texture sampling clamps UVs");
        TestAssert.True(linear.X > 0.4f && linear.Y > 0.4f && linear.Z > 0.4f, "bilinear texture sampling interpolates texels");
    }

    private static void PoseEvaluator_InterpolatesTrack()
    {
        GltfNode[] nodes = [new(-1, Vector3.Zero, Quaternion.Identity, Vector3.One, -1, -1)];
        GltfModelAsset model = new(new ModelHandle(3), "pose", [], [], [], [], nodes, [], [], []);
        GltfAnimationTrack[] tracks = [new(0, 0, 1, [0, 1], [Vector4.Zero, new Vector4(2, 0, 0, 0)])];
        TestAssert.True(GltfPoseEvaluator.TryEvaluate(in model, tracks, 0.5f, out GltfPose pose, out string error), error);
        TestAssert.True(MathF.Abs(pose.NodeTransforms[0].M41 - 1f) < 0.001f, "pose evaluator interpolates translation");
    }

    private static void Manifest_RejectsUnsortedOrInvalidEntries()
    {
        GltfBakeEntry[] entries = [new("z", "z.gltf", "z.png", 16, 16, 1, 16, 16, [new GltfBakeClip("idle", 0, 1, 12)]), new("a", "a.gltf", "a.png", 16, 16, 1, 16, 16, [])];
        TestAssert.True(!GltfBakeManifestReader.Validate(entries, out _), "manifest ordering is deterministic and validated");
        GltfBakeEntry[] valid = [entries[1]];
        TestAssert.True(GltfBakeManifestReader.Validate(valid, out string error), error);
    }

    private static void WriteFloat(byte[] bytes, int offset, float value) => BitConverter.GetBytes(value).CopyTo(bytes, offset);
}
