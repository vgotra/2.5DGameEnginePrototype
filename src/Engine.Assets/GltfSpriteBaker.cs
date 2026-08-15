using System.Numerics;

namespace Engine.Assets;

public static class GltfSpriteBaker
{
    public static bool TryBake(in GltfModelAsset model, in GltfSpriteBakeSettings settings, out GltfSpriteAtlas atlas, out string error)
    {
        atlas = default;
        error = string.Empty;
        if (settings.FrameWidth <= 0 || settings.FrameHeight <= 0 || settings.Directions <= 0 || settings.FramesPerClip <= 0)
        {
            error = "Sprite bake dimensions and counts must be positive.";
            return false;
        }
        if (model.Vertices.Length == 0 || model.Indices.Length == 0)
        {
            error = "Model has no indexed geometry.";
            return false;
        }

        int width = checked(settings.FrameWidth * settings.Directions);
        int height = checked(settings.FrameHeight * settings.FramesPerClip);
        byte[] pixels = new byte[checked(width * height * 4)];
        Vector3 min = new(float.MaxValue);
        Vector3 max = new(float.MinValue);
        for (int i = 0; i < model.Vertices.Length; i++)
        {
            min = Vector3.Min(min, model.Vertices[i].Position);
            max = Vector3.Max(max, model.Vertices[i].Position);
        }
        Vector3 extent = Vector3.Max(max - min, new Vector3(0.001f));
        for (int direction = 0; direction < settings.Directions; direction++)
        {
            float angle = direction * MathF.Tau / settings.Directions;
            float sin = MathF.Sin(angle);
            float cos = MathF.Cos(angle);
            Vector2[] projected = new Vector2[model.Vertices.Length];
            GltfPose pose = default;
            for (int i = 0; i < model.Vertices.Length; i++)
            {
                GltfVertex vertex = model.Vertices[i];
                Vector3 source = vertex.Position;
                Vector3 p = source - min;
                float x = p.X * cos - p.Z * sin;
                float y = p.Y;
                projected[i] = new Vector2(
                    (x / MathF.Max(extent.X, extent.Z) + 0.5f) * (settings.FrameWidth - 1),
                    (1f - y / extent.Y) * (settings.FrameHeight - 1));
            }
            for (int frame = 0; frame < settings.FramesPerClip; frame++)
            {
                bool hasPose = model.Tracks.Length > 0 && GltfPoseEvaluator.TryEvaluate(in model, model.Tracks, settings.FramesPerSecond <= 0 ? 0f : frame / (float)settings.FramesPerSecond, out pose, out _);
                if (hasPose)
                    for (int i = 0; i < model.Vertices.Length; i++)
                    {
                        GltfVertex vertex = model.Vertices[i];
                        Vector3 source = vertex.Position;
                        float total = vertex.Weights.X + vertex.Weights.Y + vertex.Weights.Z + vertex.Weights.W;
                        if (total > 0f && pose.JointMatrices.Length > 0)
                            source = Skin(vertex.Position, vertex.Joints.X, vertex.Weights.X, pose.JointMatrices) + Skin(vertex.Position, vertex.Joints.Y, vertex.Weights.Y, pose.JointMatrices) + Skin(vertex.Position, vertex.Joints.Z, vertex.Weights.Z, pose.JointMatrices) + Skin(vertex.Position, vertex.Joints.W, vertex.Weights.W, pose.JointMatrices);
                        else if (pose.NodeTransforms.Length > 0) source = Vector3.Transform(source, pose.NodeTransforms[0]);
                        Vector3 p = source - min;
                        float x = p.X * cos - p.Z * sin;
                        projected[i] = new Vector2((x / MathF.Max(extent.X, extent.Z) + 0.5f) * (settings.FrameWidth - 1), (1f - p.Y / extent.Y) * (settings.FrameHeight - 1));
                    }
                int originX = direction * settings.FrameWidth;
                int originY = frame * settings.FrameHeight;
                for (int p = 0; p < model.Primitives.Length; p++)
                {
                    GltfPrimitiveRange primitive = model.Primitives[p];
                    GltfMaterialData material = primitive.MaterialIndex >= 0 && primitive.MaterialIndex < model.Materials.Length ? model.Materials[primitive.MaterialIndex] : new GltfMaterialData(Vector4.One, 0f, 1f, -1, -1, -1, -1, Vector3.Zero);
                    for (int i = primitive.FirstIndex; i + 2 < primitive.FirstIndex + primitive.IndexCount; i += 3)
                    {
                        int a = model.Indices[i];
                        int b = model.Indices[i + 1];
                        int c = model.Indices[i + 2];
                        Rasterize(projected[a], projected[b], projected[c], model.Vertices[a].TexCoord, model.Vertices[b].TexCoord, model.Vertices[c].TexCoord, originX, originY, settings.FrameWidth, settings.FrameHeight, material, model.Images, settings, pixels, width);
                    }
                }
            }
        }
        GltfSpriteFrame[] frames = new GltfSpriteFrame[settings.Directions * settings.FramesPerClip];
        int frameIndex = 0;
        for (int frame = 0; frame < settings.FramesPerClip; frame++)
            for (int direction = 0; direction < settings.Directions; direction++)
                frames[frameIndex] = new GltfSpriteFrame(frameIndex++, direction,
                    new Vector2((float)(direction * settings.FrameWidth) / width, (float)(frame * settings.FrameHeight) / height),
                    new Vector2((float)settings.FrameWidth / width, (float)settings.FrameHeight / height), Vector2.Zero);
        atlas = new GltfSpriteAtlas(model.StableId, pixels, width, height, settings.FrameWidth, settings.FrameHeight, settings.Directions * settings.FramesPerClip, settings.Directions)
        { Clip = settings.Clip, FramesPerSecond = settings.FramesPerSecond, Frames = frames };
        return true;
    }

    private static Vector3 Skin(Vector3 position, float joint, float weight, Matrix4x4[] matrices)
        => weight <= 0f || joint < 0f || joint >= matrices.Length ? Vector3.Zero : Vector3.Transform(position, matrices[(int)joint]) * weight;

    private static void Rasterize(Vector2 a, Vector2 b, Vector2 c, Vector2 uva, Vector2 uvb, Vector2 uvc, int originX, int originY, int frameWidth, int frameHeight, GltfMaterialData material, GltfImageAsset[] images, GltfSpriteBakeSettings settings, byte[] pixels, int atlasWidth)
    {
        int minX = Math.Max(0, (int)MathF.Floor(MathF.Min(a.X, MathF.Min(b.X, c.X))));
        int maxX = Math.Min(frameWidth - 1, (int)MathF.Ceiling(MathF.Max(a.X, MathF.Max(b.X, c.X))));
        int minY = Math.Max(0, (int)MathF.Floor(MathF.Min(a.Y, MathF.Min(b.Y, c.Y))));
        int maxY = Math.Min(frameHeight - 1, (int)MathF.Ceiling(MathF.Max(a.Y, MathF.Max(b.Y, c.Y))));
        float area = Edge(a, b, c);
        if (MathF.Abs(area) < 0.0001f) return;
        for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++)
        {
            Vector2 point = new(x + 0.5f, y + 0.5f);
            float w0 = Edge(b, c, point) / area;
            float w1 = Edge(c, a, point) / area;
            float w2 = Edge(a, b, point) / area;
            if (w0 < 0 || w1 < 0 || w2 < 0) continue;
            Vector2 uv = uva * w0 + uvb * w1 + uvc * w2;
            Vector4 color = material.BaseColor;
            if (material.BaseColorTexture >= 0 && material.BaseColorTexture < images.Length)
                color *= GltfTextureSampler.Sample(in images[material.BaseColorTexture], uv, settings.TextureFilter, Vector4.One);
            float lighting = 0.75f + 0.25f * Math.Clamp(1f - material.Roughness * 0.25f + material.Metallic * 0.1f, 0f, 1f);
            color = new Vector4(color.X * lighting + material.EmissiveColor.X, color.Y * lighting + material.EmissiveColor.Y, color.Z * lighting + material.EmissiveColor.Z, color.W);
            if (color.W < settings.AlphaThreshold) continue;
            int index = ((originY + y) * atlasWidth + originX + x) * 4;
            pixels[index] = ToByte(color.X); pixels[index + 1] = ToByte(color.Y); pixels[index + 2] = ToByte(color.Z); pixels[index + 3] = ToByte(color.W);
        }
    }

    private static float Edge(Vector2 a, Vector2 b, Vector2 p) => (p.X - a.X) * (b.Y - a.Y) - (p.Y - a.Y) * (b.X - a.X);
    private static byte ToByte(float value) => (byte)Math.Clamp((int)MathF.Round(value * 255f), 0, 255);
}
