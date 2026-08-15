using System.Text.Json;

namespace Engine.Assets;

public readonly record struct GltfBakeClip(string Name, int FirstFrame, int FrameCount, int FramesPerSecond);
public readonly record struct GltfBakeEntry(string Id, string Source, string Atlas, int FrameWidth, int FrameHeight, int Directions, int AtlasWidth, int AtlasHeight, GltfBakeClip[] Clips);

public static class GltfBakeManifestReader
{
    public static bool TryRead(string path, out GltfBakeEntry[] entries, out string error)
    {
        entries = Array.Empty<GltfBakeEntry>();
        error = string.Empty;
        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(path));
            JsonElement root = document.RootElement;
            if (!root.TryGetProperty("version", out JsonElement version) || version.GetInt32() != 1) { error = "Unsupported bake manifest version."; return false; }
            List<GltfBakeEntry> result = new();
            foreach (JsonElement item in root.GetProperty("assets").EnumerateArray())
            {
                JsonElement bake = item.GetProperty("bake");
                List<GltfBakeClip> clips = new();
                foreach (JsonElement clip in bake.GetProperty("clips").EnumerateArray()) clips.Add(new GltfBakeClip(clip.GetProperty("name").GetString() ?? string.Empty, clip.GetProperty("firstFrame").GetInt32(), clip.GetProperty("frameCount").GetInt32(), clip.GetProperty("framesPerSecond").GetInt32()));
                result.Add(new GltfBakeEntry(item.GetProperty("id").GetString() ?? string.Empty, item.GetProperty("source").GetString() ?? string.Empty, item.GetProperty("atlas").GetString() ?? string.Empty, bake.GetProperty("frameWidth").GetInt32(), bake.GetProperty("frameHeight").GetInt32(), bake.GetProperty("directions").GetInt32(), bake.GetProperty("atlasWidth").GetInt32(), bake.GetProperty("atlasHeight").GetInt32(), clips.ToArray()));
            }
            entries = result.ToArray();
            if (!Validate(entries, out error)) { entries = Array.Empty<GltfBakeEntry>(); return false; }
            return true;
        }
        catch (Exception ex) when (ex is IOException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            error = $"{path}: {ex.Message}";
            return false;
        }
    }

    public static bool Validate(ReadOnlySpan<GltfBakeEntry> entries, out string error)
    {
        error = string.Empty;
        string previous = string.Empty;
        for (int i = 0; i < entries.Length; i++)
        {
            GltfBakeEntry entry = entries[i];
            if (string.IsNullOrWhiteSpace(entry.Id) || string.IsNullOrWhiteSpace(entry.Source) || string.IsNullOrWhiteSpace(entry.Atlas)) { error = "Manifest entries require id, source, and atlas."; return false; }
            if (entry.FrameWidth <= 0 || entry.FrameHeight <= 0 || entry.Directions <= 0 || entry.AtlasWidth != entry.FrameWidth * entry.Directions || entry.AtlasHeight <= 0) { error = $"Manifest entry '{entry.Id}' has invalid dimensions."; return false; }
            if (i > 0 && string.CompareOrdinal(previous, entry.Id) >= 0) { error = "Manifest entries must be sorted by unique id."; return false; }
            previous = entry.Id;
            int lastFrame = -1;
            for (int clipIndex = 0; clipIndex < entry.Clips.Length; clipIndex++)
            {
                GltfBakeClip clip = entry.Clips[clipIndex];
                if (string.IsNullOrWhiteSpace(clip.Name) || clip.FirstFrame < 0 || clip.FrameCount <= 0 || clip.FramesPerSecond <= 0 || clip.FirstFrame < lastFrame) { error = $"Manifest entry '{entry.Id}' has invalid clip metadata."; return false; }
                lastFrame = clip.FirstFrame + clip.FrameCount;
            }
        }
        return true;
    }
}
