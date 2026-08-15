using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using System.Text.Json;
using StbImageSharp;

namespace Engine.Assets;

public static class GltfLoader
{
    public static bool TryLoad(string path, ModelHandle handle, out GltfModelAsset asset, out string error)
    {
        asset = default;
        error = string.Empty;
        try
        {
            byte[] file = File.ReadAllBytes(path);
            string baseDirectory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? string.Empty;
            if (!TryReadContainer(file, out byte[] jsonBytes, out byte[]? binary, out error)) return false;
            if (jsonBytes.Length >= 3 && jsonBytes[0] == 0xEF && jsonBytes[1] == 0xBB && jsonBytes[2] == 0xBF) jsonBytes = jsonBytes[3..];
            using JsonDocument document = JsonDocument.Parse(jsonBytes);
            return TryDecode(document.RootElement, baseDirectory, binary, handle, Path.GetFileNameWithoutExtension(path).ToLowerInvariant(), out asset, out error);
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or ArgumentException)
        {
            error = $"{path}: {ex.Message}";
            return false;
        }
    }

    private static bool TryReadContainer(byte[] file, out byte[] json, out byte[]? binary, out string error)
    {
        json = Array.Empty<byte>();
        binary = null;
        error = string.Empty;
        if (file.Length >= 12 && BinaryPrimitives.ReadUInt32LittleEndian(file) == 0x46546C67)
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(4)) != 2) { error = "GLB version must be 2."; return false; }
            int offset = 12;
            while (offset + 8 <= file.Length)
            {
                int length = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(offset)));
                uint type = BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(offset + 4));
                offset += 8;
                if (length < 0 || offset + length > file.Length) { error = "GLB chunk exceeds file bounds."; return false; }
                if (type == 0x4E4F534A) json = file.AsSpan(offset, length).ToArray();
                else if (type == 0x004E4942) binary = file.AsSpan(offset, length).ToArray();
                offset += length;
            }
            if (json.Length == 0) { error = "GLB JSON chunk is missing."; return false; }
            return true;
        }

        json = file;
        return true;
    }

    private static bool TryDecode(JsonElement root, string baseDirectory, byte[]? glbBinary, ModelHandle handle, string stableId, out GltfModelAsset asset, out string error)
    {
        asset = default;
        error = string.Empty;
        if (!StringEquals(root, "asset", "version", "2.0")) { error = "glTF asset.version must be 2.0."; return false; }
        if (!TryReadBuffers(root, baseDirectory, glbBinary, out byte[][] buffers, out error)) return false;

        JsonElement accessors = GetArray(root, "accessors");
        JsonElement views = GetArray(root, "bufferViews");
        JsonElement meshes = GetArray(root, "meshes");
        if (meshes.GetArrayLength() == 0) { error = "At least one mesh is required."; return false; }

        List<GltfVertex> vertices = new();
        List<int> indices = new();
        List<GltfPrimitiveRange> primitives = new();
        foreach (JsonElement primitive in meshes[0].GetProperty("primitives").EnumerateArray())
        {
            if (primitive.GetProperty("mode").GetInt32() is not 4) { error = "Only TRIANGLES primitive mode is supported."; return false; }
            JsonElement attributes = primitive.GetProperty("attributes");
            if (!attributes.TryGetProperty("POSITION", out JsonElement positionAccessor)) { error = "POSITION is required."; return false; }
            int positionIndex = positionAccessor.GetInt32();
            int count = accessors[positionIndex].GetProperty("count").GetInt32();
            Vector3[] positions = ReadVector3(accessors, views, buffers, positionIndex, count, out error);
            if (error.Length != 0) return false;
            Vector3[] normals = attributes.TryGetProperty("NORMAL", out JsonElement normal) ? ReadVector3(accessors, views, buffers, normal.GetInt32(), count, out error) : new Vector3[count];
            if (error.Length != 0) return false;
            Vector2[] uvs = attributes.TryGetProperty("TEXCOORD_0", out JsonElement uv) ? ReadVector2(accessors, views, buffers, uv.GetInt32(), count, out error) : new Vector2[count];
            if (error.Length != 0) return false;
            Vector4[] weights = attributes.TryGetProperty("WEIGHTS_0", out JsonElement weight) ? ReadVector4(accessors, views, buffers, weight.GetInt32(), count, out error) : new Vector4[count];
            if (error.Length != 0) return false;
            Vector4[] jointWeights = attributes.TryGetProperty("JOINTS_0", out JsonElement joint) ? ReadVector4(accessors, views, buffers, joint.GetInt32(), count, out error) : new Vector4[count];
            if (error.Length != 0) return false;
            int firstVertex = vertices.Count;
            for (int i = 0; i < count; i++) vertices.Add(new GltfVertex(positions[i], normals[i], uvs[i], weights[i], jointWeights[i]));
            if (!primitive.TryGetProperty("indices", out JsonElement indexAccessor)) { error = "Indexed primitives are required."; return false; }
            int[] primitiveIndices = ReadIndices(accessors, views, buffers, indexAccessor.GetInt32(), out error);
            if (error.Length != 0) return false;
            int firstIndex = indices.Count;
            for (int i = 0; i < primitiveIndices.Length; i++) indices.Add(primitiveIndices[i] + firstVertex);
            primitives.Add(new GltfPrimitiveRange(firstIndex, primitiveIndices.Length, primitive.TryGetProperty("material", out JsonElement material) ? material.GetInt32() : -1));
        }

        GltfMaterialData[] materials = ReadMaterials(root);
        GltfNode[] nodes = ReadNodes(root, out error);
        if (error.Length != 0) return false;
        int[] joints = ReadJointIndices(root);
        GltfAnimationClip[] animations = ReadAnimations(root, out error);
        if (error.Length != 0) return false;
        GltfImageAsset[] images = ReadImages(root, baseDirectory, buffers, views, out error);
        if (error.Length != 0) return false;
        GltfAnimationTrack[] tracks = ReadAnimationTracks(root, accessors, views, buffers, out error);
        if (error.Length != 0) return false;
        asset = new GltfModelAsset(handle, stableId, vertices.ToArray(), indices.ToArray(), primitives.ToArray(), materials, nodes, joints, Array.Empty<GltfAnimationChannel>(), animations) { Images = images, Tracks = tracks };
        return true;
    }

    private static GltfImageAsset[] ReadImages(JsonElement root, string baseDirectory, byte[][] buffers, JsonElement views, out string error)
    {
        error = string.Empty; JsonElement array = GetArray(root, "images"); GltfImageAsset[] result = new GltfImageAsset[array.GetArrayLength()]; int i = 0;
        foreach (JsonElement image in array.EnumerateArray())
        {
            string mime = image.TryGetProperty("mimeType", out JsonElement m) ? m.GetString() ?? string.Empty : string.Empty;
            byte[] encoded;
            if (image.TryGetProperty("uri", out JsonElement uri))
            {
                string value = uri.GetString() ?? string.Empty;
                if (value.StartsWith("data:", StringComparison.Ordinal)) { int comma = value.IndexOf(','); mime = mime.Length == 0 ? value[5..comma].Split(';')[0] : mime; encoded = Convert.FromBase64String(value[(comma + 1)..]); }
                else { encoded = File.ReadAllBytes(Path.Combine(baseDirectory, value)); if (mime.Length == 0) mime = value.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || value.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ? "image/jpeg" : "image/png"; }
            }
            else if (image.TryGetProperty("bufferView", out JsonElement view))
            {
                JsonElement v = views[view.GetInt32()]; int buffer = v.GetProperty("buffer").GetInt32(); int offset = v.TryGetProperty("byteOffset", out JsonElement o) ? o.GetInt32() : 0; int length = v.GetProperty("byteLength").GetInt32(); encoded = buffers[buffer].AsSpan(offset, length).ToArray();
            }
            else { error = "Image source is missing."; return []; }
            if (mime is not "image/png" and not "image/jpeg") { error = $"Unsupported image MIME type '{mime}'."; return []; }
            try { ImageResult decoded = ImageResult.FromMemory(encoded, ColorComponents.RedGreenBlueAlpha); result[i] = new GltfImageAsset($"image-{i}", mime, decoded.Data, decoded.Width, decoded.Height); }
            catch (Exception ex) { error = $"Image {i} decode failed: {ex.Message}"; return []; }
            i++;
        }
        return result;
    }

    private static GltfAnimationTrack[] ReadAnimationTracks(JsonElement root, JsonElement accessors, JsonElement views, byte[][] buffers, out string error)
    {
        error = string.Empty; List<GltfAnimationTrack> result = new();
        foreach (JsonElement animation in GetArray(root, "animations").EnumerateArray())
        {
            JsonElement samplers = animation.TryGetProperty("samplers", out JsonElement s) ? s : default;
            if (!animation.TryGetProperty("channels", out JsonElement channels)) continue;
            foreach (JsonElement channel in channels.EnumerateArray())
            {
                int samplerIndex = channel.GetProperty("sampler").GetInt32(); JsonElement sampler = samplers[samplerIndex]; string interpolation = sampler.TryGetProperty("interpolation", out JsonElement ip) ? ip.GetString() ?? "LINEAR" : "LINEAR";
                if (interpolation != "LINEAR") { error = "Only LINEAR animation interpolation is supported."; return []; }
                int input = sampler.GetProperty("input").GetInt32(), output = sampler.GetProperty("output").GetInt32(); float[] times = ReadScalar(accessors, views, buffers, input, out error); if (error.Length != 0) return [];
                int node = channel.GetProperty("target").GetProperty("node").GetInt32(); string path = channel.GetProperty("target").GetProperty("path").GetString() ?? string.Empty; byte pathId = path == "translation" ? (byte)0 : path == "rotation" ? (byte)1 : path == "scale" ? (byte)2 : (byte)255; if (pathId == 255) { error = "Unsupported animation channel path."; return []; }
                Vector4[] values = ReadVector(accessors, views, buffers, output, times.Length, path == "rotation" ? 4 : 3, out error); if (error.Length != 0) return [];
                result.Add(new GltfAnimationTrack(node, pathId, 1, times, values));
            }
        }
        return result.ToArray();
    }

    private static float[] ReadScalar(JsonElement accessors, JsonElement views, byte[][] buffers, int accessorIndex, out string error)
    {
        Vector4[] values = ReadVector(accessors, views, buffers, accessorIndex, accessors[accessorIndex].GetProperty("count").GetInt32(), 1, out error); float[] result = new float[values.Length]; for (int i = 0; i < result.Length; i++) result[i] = values[i].X; return result;
    }

    private static bool TryReadBuffers(JsonElement root, string baseDirectory, byte[]? glb, out byte[][] buffers, out string error)
    {
        error = string.Empty;
        JsonElement array = GetArray(root, "buffers");
        buffers = new byte[array.GetArrayLength()][];
        int index = 0;
        foreach (JsonElement buffer in array.EnumerateArray())
        {
            int length = buffer.GetProperty("byteLength").GetInt32();
            if (buffer.TryGetProperty("uri", out JsonElement uri))
            {
                string value = uri.GetString() ?? string.Empty;
                if (value.StartsWith("data:application/octet-stream;base64,", StringComparison.Ordinal)) buffers[index] = Convert.FromBase64String(value[(value.IndexOf(',') + 1)..]);
                else buffers[index] = File.ReadAllBytes(Path.Combine(baseDirectory, value));
            }
            else if (index == 0 && glb is not null) buffers[index] = glb;
            else { error = "Buffer URI is missing."; return false; }
            if (buffers[index].Length < length) { error = "Buffer is shorter than byteLength."; return false; }
            index++;
        }
        return true;
    }

    private static Vector3[] ReadVector3(JsonElement accessors, JsonElement views, byte[][] buffers, int accessorIndex, int count, out string error)
        => ReadVector(accessors, views, buffers, accessorIndex, count, 3, out error).Select(static v => new Vector3(v.X, v.Y, v.Z)).ToArray();

    private static Vector2[] ReadVector2(JsonElement accessors, JsonElement views, byte[][] buffers, int accessorIndex, int count, out string error)
        => ReadVector(accessors, views, buffers, accessorIndex, count, 2, out error).Select(static v => new Vector2(v.X, v.Y)).ToArray();

    private static Vector4[] ReadVector4(JsonElement accessors, JsonElement views, byte[][] buffers, int accessorIndex, int count, out string error)
        => ReadVector(accessors, views, buffers, accessorIndex, count, 4, out error).ToArray();

    private static Vector4[] ReadVector(JsonElement accessors, JsonElement views, byte[][] buffers, int accessorIndex, int count, int components, out string error)
    {
        error = string.Empty;
        JsonElement accessor = accessors[accessorIndex];
        if (accessor.GetProperty("componentType").GetInt32() != 5126) { error = "Only FLOAT vertex attributes are supported."; return Array.Empty<Vector4>(); }
        string expected = components == 1 ? "SCALAR" : $"VEC{components}";
        if (accessor.GetProperty("type").GetString() != expected) { error = "Accessor type does not match attribute."; return Array.Empty<Vector4>(); }
        (byte[] data, int offset, int stride) = ViewData(accessor, views, buffers, accessorIndex, components * 4, out error);
        Vector4[] result = new Vector4[count];
        for (int i = 0; i < count; i++) { int p = offset + i * stride; result[i] = new Vector4(components > 0 ? BitConverter.ToSingle(data, p) : 0, components > 1 ? BitConverter.ToSingle(data, p + 4) : 0, components > 2 ? BitConverter.ToSingle(data, p + 8) : 0, components > 3 ? BitConverter.ToSingle(data, p + 12) : 0); }
        return result;
    }

    private static int[] ReadIndices(JsonElement accessors, JsonElement views, byte[][] buffers, int accessorIndex, out string error)
    {
        error = string.Empty;
        JsonElement accessor = accessors[accessorIndex];
        int component = accessor.GetProperty("componentType").GetInt32();
        int size = component == 5121 ? 1 : component == 5123 ? 2 : component == 5125 ? 4 : 0;
        if (size == 0 || accessor.GetProperty("type").GetString() != "SCALAR") { error = "Indices must be unsigned byte, short, or int scalars."; return Array.Empty<int>(); }
        (byte[] data, int offset, int stride) = ViewData(accessor, views, buffers, accessorIndex, size, out error);
        int count = accessor.GetProperty("count").GetInt32();
        int[] result = new int[count];
        for (int i = 0; i < count; i++) { int p = offset + i * stride; result[i] = size == 1 ? data[p] : size == 2 ? BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(p)) : checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(p))); }
        return result;
    }

    private static (byte[] Data, int Offset, int Stride) ViewData(JsonElement accessor, JsonElement views, byte[][] buffers, int accessorIndex, int elementSize, out string error)
    {
        error = string.Empty;
        if (!accessor.TryGetProperty("bufferView", out JsonElement viewProperty)) { error = "Sparse accessors are unsupported."; return (Array.Empty<byte>(), 0, elementSize); }
        JsonElement view = views[viewProperty.GetInt32()];
        int bufferIndex = view.GetProperty("buffer").GetInt32();
        int offset = view.TryGetProperty("byteOffset", out JsonElement viewOffset) ? viewOffset.GetInt32() : 0;
        offset += accessor.TryGetProperty("byteOffset", out JsonElement accessorOffset) ? accessorOffset.GetInt32() : 0;
        int stride = view.TryGetProperty("byteStride", out JsonElement byteStride) ? byteStride.GetInt32() : elementSize;
        if (stride < elementSize || offset < 0 || offset >= buffers[bufferIndex].Length) { error = "Accessor range is invalid."; return (Array.Empty<byte>(), 0, stride); }
        return (buffers[bufferIndex], offset, stride);
    }

    private static GltfMaterialData[] ReadMaterials(JsonElement root)
    {
        JsonElement array = GetArray(root, "materials");
        GltfMaterialData[] result = new GltfMaterialData[array.GetArrayLength()];
        int i = 0;
        foreach (JsonElement material in array.EnumerateArray())
        {
            JsonElement pbr = material.GetProperty("pbrMetallicRoughness");
            Vector4 color = ReadVector4Property(pbr, "baseColorFactor", Vector4.One);
            float metallic = pbr.TryGetProperty("metallicFactor", out JsonElement metal) ? metal.GetSingle() : 1f;
            float roughness = pbr.TryGetProperty("roughnessFactor", out JsonElement rough) ? rough.GetSingle() : 1f;
            Vector3 emissive = ReadVector3Property(material, "emissiveFactor", Vector3.Zero);
            result[i++] = new GltfMaterialData(color, metallic, roughness, TextureIndex(pbr, "baseColorTexture"), TextureIndex(material, "normalTexture"), TextureIndex(material, "occlusionTexture"), TextureIndex(material, "emissiveTexture"), emissive);
        }
        return result;
    }

    private static GltfNode[] ReadNodes(JsonElement root, out string error)
    {
        error = string.Empty;
        JsonElement array = GetArray(root, "nodes");
        GltfNode[] result = new GltfNode[array.GetArrayLength()];
        int i = 0;
        foreach (JsonElement node in array.EnumerateArray())
        {
            if (node.TryGetProperty("matrix", out _)) { error = "Node matrix transforms are unsupported; use TRS properties."; return Array.Empty<GltfNode>(); }
            result[i++] = new GltfNode(-1, ReadVector3Property(node, "translation", Vector3.Zero), ReadQuaternion(node), ReadVector3Property(node, "scale", Vector3.One), node.TryGetProperty("mesh", out JsonElement mesh) ? mesh.GetInt32() : -1, node.TryGetProperty("skin", out JsonElement skin) ? skin.GetInt32() : -1);
        }
        return result;
    }

    private static int[] ReadJointIndices(JsonElement root)
    {
        JsonElement skins = GetArray(root, "skins");
        List<int> result = new();
        foreach (JsonElement skin in skins.EnumerateArray()) foreach (JsonElement joint in skin.GetProperty("joints").EnumerateArray()) result.Add(joint.GetInt32());
        return result.ToArray();
    }

    private static GltfAnimationClip[] ReadAnimations(JsonElement root, out string error)
    {
        error = string.Empty;
        JsonElement array = GetArray(root, "animations");
        GltfAnimationClip[] result = new GltfAnimationClip[array.GetArrayLength()];
        int i = 0;
        foreach (JsonElement animation in array.EnumerateArray())
        {
            if (animation.TryGetProperty("samplers", out JsonElement samplers)) foreach (JsonElement sampler in samplers.EnumerateArray()) if (sampler.TryGetProperty("interpolation", out JsonElement interpolation) && interpolation.GetString() is not (null or "LINEAR")) { error = "Only LINEAR animation interpolation is supported."; return Array.Empty<GltfAnimationClip>(); }
            result[i] = new GltfAnimationClip(animation.TryGetProperty("name", out JsonElement name) ? name.GetString() ?? $"clip-{i}" : $"clip-{i}", 0f, 0, animation.TryGetProperty("channels", out JsonElement channels) ? channels.GetArrayLength() : 0);
            i++;
        }
        return result;
    }

    private static int TextureIndex(JsonElement parent, string name) => parent.TryGetProperty(name, out JsonElement texture) && texture.TryGetProperty("index", out JsonElement index) ? index.GetInt32() : -1;
    private static Quaternion ReadQuaternion(JsonElement node) { Vector4 value = ReadVector4Property(node, "rotation", new Vector4(0, 0, 0, 1)); return new Quaternion(value.X, value.Y, value.Z, value.W); }
    private static Vector4 ReadVector4Property(JsonElement parent, string name, Vector4 fallback) { if (!parent.TryGetProperty(name, out JsonElement value)) return fallback; float[] v = value.EnumerateArray().Select(static x => x.GetSingle()).ToArray(); return new Vector4(v.Length > 0 ? v[0] : fallback.X, v.Length > 1 ? v[1] : fallback.Y, v.Length > 2 ? v[2] : fallback.Z, v.Length > 3 ? v[3] : fallback.W); }
    private static Vector3 ReadVector3Property(JsonElement parent, string name, Vector3 fallback) { Vector4 value = ReadVector4Property(parent, name, new Vector4(fallback, 0)); return new Vector3(value.X, value.Y, value.Z); }
    private static JsonElement GetArray(JsonElement root, string name) => root.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Array ? value : JsonDocument.Parse("[]").RootElement;
    private static bool StringEquals(JsonElement root, string parent, string property, string expected) => root.TryGetProperty(parent, out JsonElement value) && value.TryGetProperty(property, out JsonElement actual) && actual.GetString() == expected;
}
