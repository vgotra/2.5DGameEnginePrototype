namespace Engine.Threading;

internal enum JobSlotState
{
    Free = 0,
    Claimed = 1,
    Waiting = 2,
    Ready = 3,
    Done = 4
}
