using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Engine.Threading;

public sealed class JobSystem : IDisposable
{
    private const int SlotCount = 4096;
    private const int SlotMask = SlotCount - 1;
    internal const int MaxDependencies = 8;

    private readonly JobSlot[] _slots = new JobSlot[SlotCount];
    private readonly Channel<int>[] _channels;
    private readonly ConcurrentQueue<int> _waiting = new();
    private readonly SemaphoreSlim _workSignal = new(0);
    private readonly SemaphoreSlim _completion = new(0);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly ParallelForChunkPool _chunkPool = new();
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

    public JobHandle Schedule(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        int slotIndex = ClaimSlot();
        JobSlot slot = _slots[slotIndex];
        slot.Work = action;
        slot.Error = null;
        slot.DepCount = 0;
        slot.ParentSlot = -1;
        Volatile.Write(ref slot.State, (int)JobSlotState.Ready);
        EnqueueReady(slotIndex);
        return new JobHandle(slotIndex, slot.Generation);
    }

    public JobHandle Schedule(Action action, JobHandle dependency)
    {
        Span<JobHandle> dependencies = stackalloc JobHandle[1] { dependency };
        return Schedule(action, dependencies);
    }

    public JobHandle Schedule(Action action, ReadOnlySpan<JobHandle> dependencies)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (dependencies.Length > MaxDependencies)
            throw new ArgumentException($"A job accepts at most {MaxDependencies} dependencies.", nameof(dependencies));
        int slotIndex = ClaimSlot();
        JobSlot slot = _slots[slotIndex];
        slot.Work = action;
        slot.Error = null;
        slot.ParentSlot = -1;
        int pending = 0;
        for (int i = 0; i < dependencies.Length; i++)
        {
            JobHandle dependency = dependencies[i];
            if (!dependency.IsValid || IsComplete(dependency)) continue;
            WriteDep(slot, pending++, dependency);
        }
        slot.DepCount = pending;
        if (pending == 0)
        {
            Volatile.Write(ref slot.State, (int)JobSlotState.Ready);
            EnqueueReady(slotIndex);
        }
        else
        {
            Volatile.Write(ref slot.State, (int)JobSlotState.Waiting);
            _waiting.Enqueue(slotIndex);
            if (AllDepsComplete(slot)) TryPromote(slotIndex, slot);
        }
        return new JobHandle(slotIndex, slot.Generation);
    }

    public JobHandle ScheduleFor(int count, int minChunkSize, Action<int, int> body)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (count <= 0) return JobHandle.None;
        int chunkMin = Math.Max(1, minChunkSize);
        int chunks = Math.Min((count + chunkMin - 1) / chunkMin, WorkerCount);
        int barrierIndex = ClaimSlot();
        JobSlot barrier = _slots[barrierIndex];
        barrier.Work = null;
        barrier.Error = null;
        barrier.DepCount = 0;
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
            chunkSlot.DepCount = 0;
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

    public void Complete(JobHandle handle)
    {
        if (!handle.IsValid) return;
        JobSlot slot = _slots[handle.Slot];
        while (Volatile.Read(ref slot.Generation) == handle.Generation
            && Volatile.Read(ref slot.State) != (int)JobSlotState.Done)
            _completion.Wait();
        if (Volatile.Read(ref slot.Generation) == handle.Generation && slot.Error != null)
            throw new AggregateException(slot.Error);
    }

    public async ValueTask DrainAsync()
    {
        while (Volatile.Read(ref _outstanding) > 0)
            await _completion.WaitAsync().ConfigureAwait(false);
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
            if (Interlocked.Decrement(ref barrier.PendingChildren) == 0) TryPromote(parent, barrier);
        }
        SweepWaiting();
    }

    private void SweepWaiting()
    {
        int budget = _waiting.Count;
        while (budget-- > 0 && _waiting.TryDequeue(out int slotIndex))
        {
            JobSlot slot = _slots[slotIndex];
            if (Volatile.Read(ref slot.State) != (int)JobSlotState.Waiting) continue;
            if (AllDepsComplete(slot)) TryPromote(slotIndex, slot);
            else _waiting.Enqueue(slotIndex);
        }
    }

    private bool TryPromote(int slotIndex, JobSlot slot)
    {
        if (Interlocked.CompareExchange(ref slot.State, (int)JobSlotState.Ready, (int)JobSlotState.Waiting) != (int)JobSlotState.Waiting)
            return false;
        EnqueueReady(slotIndex);
        return true;
    }

    private bool AllDepsComplete(JobSlot slot)
    {
        int count = Volatile.Read(ref slot.DepCount);
        for (int i = 0; i < count; i++)
        {
            long packed = ReadDep(slot, i);
            int depSlot = (int)(packed & 0xFFFFFFFFL);
            int depGeneration = (int)((ulong)packed >> 32);
            JobSlot dependency = _slots[depSlot];
            if (Volatile.Read(ref dependency.Generation) != depGeneration) continue;
            if (Volatile.Read(ref dependency.State) != (int)JobSlotState.Done) return false;
        }
        return true;
    }

    private static long PackDep(JobHandle handle) => ((long)(uint)handle.Generation << 32) | (uint)handle.Slot;

    private static void WriteDep(JobSlot slot, int index, JobHandle dependency)
    {
        long packed = PackDep(dependency);
        switch (index)
        {
            case 0: Volatile.Write(ref slot.D0, packed); break;
            case 1: Volatile.Write(ref slot.D1, packed); break;
            case 2: Volatile.Write(ref slot.D2, packed); break;
            case 3: Volatile.Write(ref slot.D3, packed); break;
            case 4: Volatile.Write(ref slot.D4, packed); break;
            case 5: Volatile.Write(ref slot.D5, packed); break;
            case 6: Volatile.Write(ref slot.D6, packed); break;
            default: Volatile.Write(ref slot.D7, packed); break;
        }
    }

    private static long ReadDep(JobSlot slot, int index) => index switch
    {
        0 => Volatile.Read(ref slot.D0),
        1 => Volatile.Read(ref slot.D1),
        2 => Volatile.Read(ref slot.D2),
        3 => Volatile.Read(ref slot.D3),
        4 => Volatile.Read(ref slot.D4),
        5 => Volatile.Read(ref slot.D5),
        6 => Volatile.Read(ref slot.D6),
        _ => Volatile.Read(ref slot.D7),
    };

    public void Dispose()
    {
        _shutdown.Cancel();
        try { Task.WaitAll(_workers); } catch (AggregateException) { }
        for (int i = 0; i < _channels.Length; i++) _channels[i].Writer.TryComplete();
        _workSignal.Dispose();
        _completion.Dispose();
        _shutdown.Dispose();
    }
}
