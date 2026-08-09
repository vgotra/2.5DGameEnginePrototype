namespace Engine.Ecs;

internal sealed class ComponentArray<T> : IComponentArray where T : unmanaged
{
    private T[] _values = [];

    public ref T Get(int row) => ref _values[row];

    public void EnsureCapacity(int capacity)
    {
        if (_values.Length >= capacity) return;
        int newSize = _values.Length == 0 ? 16 : Math.Max(capacity, _values.Length * 2);
        Array.Resize(ref _values, newSize);
    }

    public void CopyRowFrom(IComponentArray source, int sourceRow, int targetRow)
        => _values[targetRow] = ((ComponentArray<T>)source)._values[sourceRow];

    public void SwapRemove(int row, int lastRow)
    {
        if (row != lastRow) _values[row] = _values[lastRow];
    }
}
