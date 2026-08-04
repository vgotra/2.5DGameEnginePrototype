using System.Collections.Concurrent;

namespace Engine.Threading;

public sealed class JobSystem : IDisposable
{
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Task[] _workers;
    private readonly ConcurrentQueue<Action> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly SemaphoreSlim _completed = new(0);
    private int _pending;

    public JobSystem(int? workerCount = null)
    {
        int count = Math.Max(1, workerCount ?? Environment.ProcessorCount - 1);
        _workers = new Task[count];
        for (int i = 0; i < count; i++) _workers[i] = Task.Run(WorkerAsync);
    }

    public ValueTask Schedule(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _queue.Enqueue(action);
        Interlocked.Increment(ref _pending);
        _signal.Release();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DrainAsync()
    {
        while (Volatile.Read(ref _pending) > 0) await _completed.WaitAsync();
    }

    private async Task WorkerAsync()
    {
        while (!_shutdown.IsCancellationRequested)
        {
            await _signal.WaitAsync(_shutdown.Token).ConfigureAwait(false);
            if (!_queue.TryDequeue(out Action? action)) continue;
            action();
            if (Interlocked.Decrement(ref _pending) == 0) _completed.Release();
        }
    }

    public void Dispose()
    {
        _shutdown.Cancel();
        _signal.Release(_workers.Length);
        try { Task.WaitAll(_workers); } catch (AggregateException) { }
        _signal.Dispose();
        _completed.Dispose();
        _shutdown.Dispose();
    }
}
