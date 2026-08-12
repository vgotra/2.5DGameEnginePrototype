using System.Diagnostics;
using System.Text.Json;
using Engine.Core;
using Engine.Ecs;
using Engine.Threading;

namespace Engine.Benchmark;

internal static class Milestone0Benchmark
{
    private static readonly int[] Counts = [100, 250, 500, 1_000, 5_000, 100_000];

    public static int Run(int? iterationsOverride, int? workerOverride, int? chunkOverride)
    {
        int iterations = iterationsOverride ?? 25;
        int chunkSize = chunkOverride ?? 256;
        List<Milestone0Result> results = new(Counts.Length * 2);
        foreach (int count in Counts)
        {
            results.Add(RunCase(count, false, iterations, workerOverride, chunkSize));
            results.Add(RunCase(count, true, iterations, workerOverride, chunkSize));
        }

        Print(results);
        string path = Path.Combine(Directory.GetCurrentDirectory(), "benchmarks", "results", "milestone0.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Milestone 0 results: {path}");
        return 0;
    }

    private static Milestone0Result RunCase(int count, bool parallel, int iterations, int? workerOverride, int chunkSize)
    {
        using JobSystem jobs = new(workerOverride);
        Engine.Ecs.World world = CreateWorld(count);
        Query<Position, Velocity, Critter> query = world.Query<Position, Velocity, Critter>();
        SimulationBody body = new(parallel ? new WorkerMetrics(jobs.WorkerCount) : null);
        UpdateSystem system = new(query, jobs, parallel, chunkSize, body);
        SystemScheduler scheduler = new();
        scheduler.Register(system);
        scheduler.Run(world, 1f / 60f);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long allocStart = GC.GetAllocatedBytesForCurrentThread();
        int gen0Start = GC.CollectionCount(0);
        int gen1Start = GC.CollectionCount(1);
        int gen2Start = GC.CollectionCount(2);
        Stopwatch simulationTimer = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++) system.Update(world, 1f / 60f);
        simulationTimer.Stop();
        double simulationMs = simulationTimer.Elapsed.TotalMilliseconds / iterations;
        long alloc = GC.GetAllocatedBytesForCurrentThread() - allocStart;

        WorkerMetrics? metrics = body.Metrics;
        metrics?.Reset();
        Stopwatch scheduledTimer = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++) scheduler.Run(world, 1f / 60f);
        scheduledTimer.Stop();
        int jobsPerFrame = parallel ? Math.Min((count + chunkSize - 1) / chunkSize, jobs.WorkerCount) : 0;
        long checksum = body.Checksum;
        int activeWorkers = metrics?.ActiveWorkers ?? 1;
        double utilization = metrics?.Utilization(scheduledTimer.Elapsed.TotalSeconds * Stopwatch.Frequency) ?? 1.0;
        return new Milestone0Result(count, parallel ? "parallel" : "serial", iterations, simulationMs,
            scheduledTimer.Elapsed.TotalMilliseconds / iterations,
            Math.Max(0, scheduledTimer.Elapsed.TotalMilliseconds - simulationTimer.Elapsed.TotalMilliseconds) * 1000 / iterations,
            (double)alloc / iterations,
            GC.CollectionCount(0) - gen0Start, GC.CollectionCount(1) - gen1Start, GC.CollectionCount(2) - gen2Start,
            jobsPerFrame, jobs.WorkerCount, activeWorkers, utilization, checksum);
    }

    private static Engine.Ecs.World CreateWorld(int count)
    {
        Engine.Ecs.World world = new();
        for (int i = 0; i < count; i++)
        {
            EntityId entity = world.Create();
            world.AddComponent(entity, new Position(i * 0.01f));
            world.AddComponent(entity, new Velocity(1f));
            world.AddComponent(entity, new Critter(i));
        }
        return world;
    }

    private static void Print(List<Milestone0Result> results)
    {
        Console.WriteLine("Count   Mode       Sim ms   Frame ms Sched us  Alloc B  Jobs  Workers Active Utilization");
        foreach (Milestone0Result result in results)
            Console.WriteLine($"{result.EntityCount,6} {result.Mode,-9} {result.SimulationMs,8:F3} {result.FrameMs,9:F3} {result.SchedulerOverheadUs,8:F2} {result.AllocBytesPerFrame,8:F1} {result.JobsPerFrame,5} {result.WorkerCount,7} {result.ActiveWorkers,6} {result.WorkerUtilization,10:P1}");
        foreach (int count in Counts)
        {
            Milestone0Result serial = results.First(r => r.EntityCount == count && r.Mode == "serial");
            Milestone0Result parallel = results.First(r => r.EntityCount == count && r.Mode == "parallel");
            double speedup = serial.FrameMs / parallel.FrameMs;
            Console.WriteLine($"{count}: parallel speedup {speedup:F2}x; faster={(speedup > 1 ? "parallel" : "serial")}; checksum={(serial.Checksum == parallel.Checksum ? "match" : "MISMATCH")}");
        }
    }

    private readonly record struct Position(float Value);
    private readonly record struct Velocity(float Value);
    private readonly record struct Critter(int Seed);

    private sealed class UpdateSystem(Query<Position, Velocity, Critter> query, JobSystem jobs, bool parallel, int chunkSize, SimulationBody body) : ISystem
    {
        public ComponentAccess Access => ComponentAccess.ReadWrite<Position, Velocity, Critter>();
        public void Update(Engine.Ecs.World world, float deltaSeconds)
        {
            if (parallel) query.ForEachParallel(jobs, ref body, chunkSize);
            else query.ForEach(ref body);
        }
    }

    private struct SimulationBody(WorkerMetrics? metrics) : IForEach<Position, Velocity, Critter, SimulationBody>
    {
        public WorkerMetrics? Metrics = metrics;
        public long Checksum;
        public static void Execute(ref SimulationBody body, EntityId entity, ref Position position, ref Velocity velocity, ref Critter critter)
        {
            long start = Stopwatch.GetTimestamp();
            position = new(position.Value + velocity.Value * (1f / 60f));
            Interlocked.Add(ref body.Checksum, (long)(position.Value * 1_000_000f) + critter.Seed);
            body.Metrics?.Record(start);
        }
    }

    private sealed class WorkerMetrics(int workerCount)
    {
        private readonly int[] _workers = new int[workerCount];
        private long _activeTicks;
        public int ActiveWorkers
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _workers.Length; i++) if (Volatile.Read(ref _workers[i]) != 0) count++;
                return count;
            }
        }
        public void Reset()
        {
            Array.Clear(_workers);
            Interlocked.Exchange(ref _activeTicks, 0);
        }
        public void Record(long start)
        {
            int threadId = Environment.CurrentManagedThreadId + 1;
            int startSlot = threadId % _workers.Length;
            for (int i = 0; i < _workers.Length; i++)
            {
                int slot = (startSlot + i) % _workers.Length;
                int owner = Volatile.Read(ref _workers[slot]);
                if (owner == threadId || (owner == 0 && Interlocked.CompareExchange(ref _workers[slot], threadId, 0) == 0)) break;
            }
            Interlocked.Add(ref _activeTicks, Stopwatch.GetTimestamp() - start);
        }
        public double Utilization(double frameTicks)
            => frameTicks <= 0 ? 0 : Math.Clamp(_activeTicks / (frameTicks * workerCount), 0, 1);
    }
}
