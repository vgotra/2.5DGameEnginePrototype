namespace Engine.Threading;

internal sealed class JobSlot
{
    internal int State;
    internal int Generation;
    internal Action? Work;
    internal Exception? Error;
    internal int ParentSlot = -1;
    internal int PendingChildren;
}
