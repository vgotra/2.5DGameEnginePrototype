using System.Runtime.CompilerServices;

namespace Engine.Rendering.Vulkan;

/// <summary>
/// Minimal growable array with an explicit count, used on per-frame accumulation paths in place of
/// <see cref="System.Collections.Generic.List{T}"/>. Growth is amortized (doubles), so steady-state
/// <see cref="Add"/> is a single store + increment with no allocation; <see cref="AsSpan"/> exposes the
/// live region for zero-copy iteration and upload (see Conventions/HotPath.md).
/// </summary>
public sealed class GrowableBuffer<T>
{
    private T[] _items = Array.Empty<T>();

    public int Count { get; private set; }

    public void EnsureCapacity(int capacity)
    {
        if (capacity <= _items.Length) return;
        int newSize = _items.Length == 0 ? Math.Max(capacity, 4) : Math.Max(capacity, _items.Length * 2);
        Array.Resize(ref _items, newSize);
    }

    public void Clear() => Count = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        if ((uint)Count >= (uint)_items.Length) EnsureCapacity(Count + 1);
        _items[Count++] = item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan() => _items.AsSpan(0, Count);
}
