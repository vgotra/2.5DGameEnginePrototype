namespace Engine.Threading;

internal sealed class JobSlot
{
    internal int State;
    internal int Generation;
    internal Action? Work;
    internal Exception? Error;
    internal int DepCount;
    internal long D0;
    internal long D1;
    internal long D2;
    internal long D3;
    internal long D4;
    internal long D5;
    internal long D6;
    internal long D7;
    internal int ParentSlot = -1;
    internal int PendingChildren;
}
