using System.Runtime.CompilerServices;

namespace Engine.Rendering.Vulkan;

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
