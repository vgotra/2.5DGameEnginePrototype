namespace Engine.Threading;

public sealed class JobSystem : IDisposable
{
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Task[] _workers;
    private readonly System.Collections.Concurrent.ConcurrentQueue<Action> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);

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
        _signal.Release();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DrainAsync()
    {
        while (!_queue.IsEmpty) await Task.Yield();
    }

    private async Task WorkerAsync()
    {
        while (!_shutdown.IsCancellationRequested)
        {
            await _signal.WaitAsync(_shutdown.Token).ConfigureAwait(false);
            if (_queue.TryDequeue(out Action? action)) action();
        }
    }

    public void Dispose()
    {
        _shutdown.Cancel();
        _signal.Release(_workers.Length);
        try { Task.WaitAll(_workers); } catch (AggregateException) { }
        _signal.Dispose();
        _shutdown.Dispose();
    }
}
