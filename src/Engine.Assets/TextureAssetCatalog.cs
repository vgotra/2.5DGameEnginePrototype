using Engine.Rendering;
using Engine.Threading;

namespace Engine.Assets;

public sealed class TextureAssetCatalog : IDisposable
{
    private readonly JobSystem _jobs;
    private readonly Dictionary<string, TextureAssetHandle> _handles = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();
    private Entry[] _entries = Array.Empty<Entry>();
    private bool _disposed;

    public TextureAssetCatalog(JobSystem jobs) => _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));

    public TextureAssetHandle Request(string path, TextureFilter filter = TextureFilter.Nearest)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        string key = Path.GetFullPath(path);
        lock (_sync)
        {
            if (_handles.TryGetValue(key, out TextureAssetHandle existing)) return existing;

            int index = _entries.Length;
            Array.Resize(ref _entries, index + 1);
            TextureAssetHandle handle = new(index);
            _handles.Add(key, handle);
            _entries[index] = new Entry { Path = key, Filter = filter, State = TextureAssetState.Queued };
            _entries[index].DecodeJob = _jobs.Run(() => Decode(index));
            return handle;
        }
    }

    public TextureAssetState GetState(TextureAssetHandle handle)
    {
        lock (_sync) return IsValid(handle) ? _entries[handle.Value].State : TextureAssetState.Failed;
    }

    public bool TryTakeDecoded(TextureAssetHandle handle, out DecodedTextureData data)
    {
        data = default;
        lock (_sync)
        {
            if (!IsValid(handle)) return false;
            ref Entry entry = ref _entries[handle.Value];
            if (entry.State != TextureAssetState.Decoded) return false;
            data = entry.Data;
            entry.Data = default;
            entry.State = TextureAssetState.Resident;
            return true;
        }
    }

    public void MarkFailed(TextureAssetHandle handle)
    {
        lock (_sync)
        {
            if (!IsValid(handle)) return;
            _entries[handle.Value].Data.Dispose();
            _entries[handle.Value].Data = default;
            _entries[handle.Value].State = TextureAssetState.Failed;
        }
    }

    private void Decode(int index)
    {
        string path;
        TextureFilter filter;
        lock (_sync)
        {
            ref Entry queued = ref _entries[index];
            queued.State = TextureAssetState.Decoding;
            path = queued.Path;
            filter = queued.Filter;
        }
        PngImage? image = PngLoader.Decode(path);
        DecodedTextureData decoded = default;
        if (image.HasValue) decoded = DecodedTextureData.FromPng(image.Value, filter);
        lock (_sync)
        {
            ref Entry entry = ref _entries[index];
            if (entry.State == TextureAssetState.Failed) { decoded.Dispose(); return; }
            if (!image.HasValue) { entry.State = TextureAssetState.Failed; return; }
            entry.Data = decoded;
            entry.State = TextureAssetState.Decoded;
        }
    }

    private bool IsValid(TextureAssetHandle handle) => (uint)handle.Value < (uint)_entries.Length;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        for (int i = 0; i < _entries.Length; i++) _entries[i].Data.Dispose();
        _entries = Array.Empty<Entry>();
        _handles.Clear();
    }

    private struct Entry
    {
        public string Path;
        public TextureFilter Filter;
        public TextureAssetState State;
        public JobHandle DecodeJob;
        public DecodedTextureData Data;
    }
}
