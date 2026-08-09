namespace Engine.Ecs;

internal interface IComponentArray
{
    void EnsureCapacity(int capacity);
    void CopyRowFrom(IComponentArray source, int sourceRow, int targetRow);
    void SwapRemove(int row, int lastRow);
}
