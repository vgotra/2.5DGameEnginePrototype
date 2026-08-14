using Engine.Ecs.Sparse;
using Engine.Threading;

namespace Engine.Benchmark.Benchmarks;

internal static class RealisticEcsBenchmarks
{
    private static readonly int[] Counts = [1_000, 10_000];
    private static readonly JobSystem Jobs = new(4);

    public static void Dispose() => Jobs.Dispose();

    public static BenchmarkCase[] Create()
    {
        List<BenchmarkCase> cases = new(Counts.Length * 3);
        foreach (int count in Counts)
        {
            RealisticCase scenario = new(count);
            cases.Add(new($"RealisticEcs_Serial_{count}", 200, scenario.Serial));
            cases.Add(new($"RealisticEcs_Adaptive_{count}", 200, scenario.Adaptive));
            cases.Add(new($"RealisticEcs_Parallel_{count}", 200, scenario.Parallel));
        }
        return cases.ToArray();
    }

    private sealed class RealisticCase
    {
        private readonly World _world = new();
        private readonly JobSystem _jobs = Jobs;
        private readonly Query<Transform, Motion, Health> _query;
        private readonly RealisticSystem _serial;
        private readonly RealisticSystem _parallel;
        private readonly FrameScheduler _adaptive;

        public RealisticCase(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Entity entity = _world.Create();
                _world.Add(entity, new Transform(i * 0.01f));
                _world.Add(entity, new Motion(1f));
                _world.Add(entity, new Health(100));
                _world.Add(entity, new Combat(i & 3));
                _world.Add(entity, new Ai(i & 7));
                _world.Add(entity, new Animation(i & 7));
                _world.Add(entity, new Collider(0.5f));
                _world.Add(entity, new Target(i - 1));
                _world.Add(entity, new Status(0.25f));
            }
            _query = _world.Query<Transform, Motion, Health>();
            _serial = new RealisticSystem(_query, _jobs, false);
            _parallel = new RealisticSystem(_query, _jobs, true);
            _adaptive = new FrameScheduler(_jobs);
            _adaptive.Register(_serial, new("Realistic", ExecutionPolicy.Adaptive, 1_000, true, true, false));
        }

        public void Serial() { Reset(); _serial.Update(_world, 1f / 60f); }
        public void Adaptive() { Reset(); _adaptive.Run(_world, 1f / 60f); }
        public void Parallel() { Reset(); _parallel.Update(_world, 1f / 60f); }

        private void Reset()
        {
            ResetBody body = new();
            _query.ForEach(ref body);
        }
    }

    private sealed class RealisticSystem(Query<Transform, Motion, Health> query, JobSystem jobs, bool parallel) : ISystem
    {
        public void Update(World world, float deltaSeconds)
        {
            if (parallel)
            {
                UpdateBody body = new() { Delta = deltaSeconds };
                _query.ParallelForEach<UpdateBody>(_jobs, 128);
                return;
            }
            UpdateBody serial = new() { Delta = deltaSeconds };
            _query.ForEach(ref serial);
        }

        private readonly JobSystem _jobs = jobs;
        private readonly Query<Transform, Motion, Health> _query = query;
    }

    private struct ResetBody : IQueryAction<Transform, Motion, Health, ResetBody>
    {
        public static void Execute(ref ResetBody body, Entity entity, ref Transform transform, ref Motion motion, ref Health health)
        {
            transform.Value = entity.Id * 0.01f;
            motion.Value = 1f;
            health.Value = 100;
        }
    }

    private struct UpdateBody : IQueryAction<Transform, Motion, Health, UpdateBody>, IParallelQueryAction<Transform, Motion, Health, UpdateBody>
    {
        public float Delta;
        public static void Execute(ref UpdateBody body, Entity entity, ref Transform transform, ref Motion motion, ref Health health)
        {
            transform.Value += motion.Value * body.Delta;
            health.Value = Math.Max(1, health.Value - (entity.Id & 1));
        }

        public static void Execute(Entity entity, ref Transform transform, ref Motion motion, ref Health health)
        {
            transform.Value += motion.Value / 60f;
            health.Value = Math.Max(1, health.Value - (entity.Id & 1));
        }
    }

    private struct Transform(float value) { public float Value = value; }
    private struct Motion(float value) { public float Value = value; }
    private struct Health(int value) { public int Value = value; }
    private struct Combat(int value) { public int Value = value; }
    private struct Ai(int value) { public int Value = value; }
    private struct Animation(int value) { public int Value = value; }
    private struct Collider(float value) { public float Value = value; }
    private struct Target(int value) { public int Value = value; }
    private struct Status(float value) { public float Value = value; }
}
