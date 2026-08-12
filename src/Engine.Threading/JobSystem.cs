using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Engine.Threading;

public interface IParallelForBody<TState, TBody>
    where TState : struct
    where TBody : struct, IParallelForBody<TState, TBody>
{
    static abstract void Execute(in TState state, int lo, int hi);
}

public sealed class JobSystem : IDisposable
{
    private const int SlotCount = 4096;
    private const int SlotMask = SlotCount - 1;

    private readonly JobSlot[] _slots = new JobSlot[SlotCount];
    private readonly Channel<int>[] _channels;
    private readonly SemaphoreSlim _workSignal = new(0);
    private readonly SemaphoreSlim _completion = new(0);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly ParallelForChunkPool _chunkPool = new(64);
    private readonly Task[] _workers;
    private int _slotCursor = -1;
    private int _outstanding;
    private int _enqueueCursor = -1;

    [ThreadStatic] private static int t_workerToken;

    public JobSystem(int? workerCount = null)
    {
        int count = Math.Max(1, workerCount ?? Environment.ProcessorCount - 1);
        for (int i = 0; i < SlotCount; i++) _slots[i] = new JobSlot();
        _channels = new Channel<int>[count];
        _workers = new Task[count];
        for (int i = 0; i < count; i++)
        {
            _channels[i] = Channel.CreateUnbounded<int>();
            int index = i;
            _workers[i] = Task.Run(() => WorkerLoop(index));
        }
    }

    public int WorkerCount => _workers.Length;

    public JobHandle Run(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        int slotIndex = ClaimSlot();
        JobSlot slot = _slots[slotIndex];
        slot.Work = action;
        slot.Error = null;
        slot.ParentSlot = -1;
        Volatile.Write(ref slot.State, (int)JobSlotState.Ready);
        EnqueueReady(slotIndex);
        return new JobHandle(slotIndex, slot.Generation);
    }

    public JobHandle ParallelFor(int count, int minChunkSize, Action<int, int> body)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (count <= 0) return JobHandle.None;
        int chunkMin = Math.Max(1, minChunkSize);
        int chunks = Math.Min((count + chunkMin - 1) / chunkMin, WorkerCount);
        int barrierIndex = ClaimSlot();
        JobSlot barrier = _slots[barrierIndex];
        barrier.Work = null;
        barrier.Error = null;
        barrier.ParentSlot = -1;
        Volatile.Write(ref barrier.PendingChildren, chunks);
        Volatile.Write(ref barrier.State, (int)JobSlotState.Waiting);
        JobHandle barrierHandle = new(barrierIndex, barrier.Generation);
        int baseSize = count / chunks;
        int remainder = count % chunks;
        int lo = 0;
        for (int i = 0; i < chunks; i++)
        {
            int hi = lo + baseSize + (i < remainder ? 1 : 0);
            ParallelForChunk chunk = _chunkPool.Rent();
            chunk.Pool = _chunkPool;
            chunk.Body = body;
            chunk.Lo = lo;
            chunk.Hi = hi;
            int chunkSlotIndex = ClaimSlot();
            JobSlot chunkSlot = _slots[chunkSlotIndex];
            chunkSlot.Work = chunk.Run;
            chunkSlot.Error = null;
            chunkSlot.ParentSlot = barrierIndex;
            Volatile.Write(ref chunkSlot.State, (int)JobSlotState.Ready);
            EnqueueReady(chunkSlotIndex);
            lo = hi;
        }
        return barrierHandle;
    }

    public JobHandle ParallelFor<TState, TBody>(int count, int minChunkSize, in TState state)
        where TState : struct
        where TBody : struct, IParallelForBody<TState, TBody>
    {
        if (count <= 0) return JobHandle.None;
        int chunkMin = Math.Max(1, minChunkSize);
        int chunks = Math.Min((count + chunkMin - 1) / chunkMin, WorkerCount);
        int barrierIndex = ClaimSlot();
        JobSlot barrier = _slots[barrierIndex];
        barrier.Work = null;
        barrier.Error = null;
        barrier.ParentSlot = -1;
        Volatile.Write(ref barrier.PendingChildren, chunks);
        Volatile.Write(ref barrier.State, (int)JobSlotState.Waiting);
        JobHandle barrierHandle = new(barrierIndex, barrier.Generation);
        int baseSize = count / chunks;
        int remainder = count % chunks;
        int lo = 0;
        for (int i = 0; i < chunks; i++)
        {
            int hi = lo + baseSize + (i < remainder ? 1 : 0);
            GenericParallelForChunk<TState, TBody> chunk = GenericParallelForChunk<TState, TBody>.Rent();
            chunk.Owner = this;
            chunk.State = state;
            chunk.Lo = lo;
            chunk.Hi = hi;
            chunk.ParentSlot = barrierIndex;
            int chunkSlotIndex = ClaimSlot();
            JobSlot chunkSlot = _slots[chunkSlotIndex];
            chunkSlot.Work = chunk.Run;
            chunkSlot.Error = null;
            chunkSlot.ParentSlot = barrierIndex;
            Volatile.Write(ref chunkSlot.State, (int)JobSlotState.Ready);
            EnqueueReady(chunkSlotIndex);
            lo = hi;
        }
        return barrierHandle;
    }

    public bool IsComplete(JobHandle handle)
    {
        if (!handle.IsValid) return true;
        JobSlot slot = _slots[handle.Slot];
        return Volatile.Read(ref slot.Generation) != handle.Generation
            || Volatile.Read(ref slot.State) == (int)JobSlotState.Done;
    }

    public void Wait(JobHandle handle)
    {
        if (!handle.IsValid) return;
        JobSlot slot = _slots[handle.Slot];
        while (Volatile.Read(ref slot.Generation) == handle.Generation
            && Volatile.Read(ref slot.State) != (int)JobSlotState.Done)
            _completion.Wait();
        if (Volatile.Read(ref slot.Generation) == handle.Generation && slot.Error != null)
            throw new AggregateException(slot.Error);
    }

    private int ClaimSlot()
    {
        int start = Interlocked.Increment(ref _slotCursor);
        for (int probe = 0; probe < SlotCount * 2; probe++)
        {
            int index = (start + probe) & SlotMask;
            JobSlot slot = _slots[index];
            int state = Volatile.Read(ref slot.State);
            if (state != (int)JobSlotState.Free && state != (int)JobSlotState.Done) continue;
            if (Interlocked.CompareExchange(ref slot.State, (int)JobSlotState.Claimed, state) != state) continue;
            slot.Generation++;
            Interlocked.Increment(ref _outstanding);
            return index;
        }
        throw new InvalidOperationException("Job slot table exhausted (4096 outstanding jobs).");
    }

    private void EnqueueReady(int slotIndex)
    {
        int target = t_workerToken > 0
            ? t_workerToken - 1
            : (int)((uint)Interlocked.Increment(ref _enqueueCursor) % (uint)_channels.Length);
        _channels[target].Writer.TryWrite(slotIndex);
        _workSignal.Release();
    }

    private void WorkerLoop(int index)
    {
        t_workerToken = index + 1;
        while (!_shutdown.IsCancellationRequested)
        {
            if (TryClaimAny(index, out int slotIndex))
            {
                Execute(slotIndex);
                continue;
            }
            try { _workSignal.Wait(_shutdown.Token); }
            catch (OperationCanceledException) { break; }
        }
    }

    private bool TryClaimAny(int index, out int slotIndex)
    {
        if (_channels[index].Reader.TryRead(out slotIndex)) return true;
        for (int step = 1; step < _channels.Length; step++)
        {
            if (_channels[(index + step) % _channels.Length].Reader.TryRead(out slotIndex)) return true;
        }
        return false;
    }

    private void Execute(int slotIndex)
    {
        JobSlot slot = _slots[slotIndex];
        int parent = slot.ParentSlot;
        try { slot.Work?.Invoke(); }
        catch (Exception failure) { slot.Error = failure; }
        Exception? error = slot.Error;
        slot.Work = null;
        Interlocked.Exchange(ref slot.State, (int)JobSlotState.Done);
        Interlocked.Decrement(ref _outstanding);
        _completion.Release();
        if (parent >= 0)
        {
            JobSlot barrier = _slots[parent];
            if (error != null) Interlocked.CompareExchange(ref barrier.Error, error, null);
            if (Interlocked.Decrement(ref barrier.PendingChildren) == 0) EnqueueReady(parent);
        }
    }

    public void Dispose()
    {
        _shutdown.Cancel();
        try { Task.WaitAll(_workers); } catch (AggregateException) { }
        for (int i = 0; i < _channels.Length; i++) _channels[i].Writer.TryComplete();
        _workSignal.Dispose();
        _completion.Dispose();
        _shutdown.Dispose();
    }

    private sealed class GenericParallelForChunk<TState, TBody>
        where TState : struct
        where TBody : struct, IParallelForBody<TState, TBody>
    {
        private static readonly ConcurrentStack<GenericParallelForChunk<TState, TBody>> Pool = new();
        internal readonly Action Run;
        internal JobSystem? Owner;
        internal TState State;
        internal int Lo;
        internal int Hi;
        internal int ParentSlot;

        private GenericParallelForChunk() => Run = RunCore;

        internal static GenericParallelForChunk<TState, TBody> Rent()
            => Pool.TryPop(out GenericParallelForChunk<TState, TBody>? chunk) ? chunk : new GenericParallelForChunk<TState, TBody>();

        private void RunCore()
        {
            try { TBody.Execute(in State, Lo, Hi); }
            finally
            {
                Owner = null;
                State = default;
                Lo = 0;
                Hi = 0;
                ParentSlot = -1;
                Pool.Push(this);
            }
        }
    }
}
