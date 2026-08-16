using Engine.Threading;
using IsometricSandbox.Game.Workloads;

int frames = ReadPositiveInt(args, "--frames", 10);
bool parallel = args.Contains("--parallel");
using JobSystem jobs = new();
ArpgWorkload workload = new(1337, jobs);
ArpgExecutionMode mode = parallel ? ArpgExecutionMode.AdaptiveParallel : ArpgExecutionMode.Serial;
ArpgWorkloadSnapshot snapshot = default;
for (int frame = 0; frame < frames; frame++) snapshot = workload.Tick(mode);
Console.WriteLine($"simulation  frames={frames}  monsters={snapshot.Monsters}  projectiles={snapshot.Projectiles}  effects={snapshot.Effects}  checksum={snapshot.Checksum}  parallel={parallel}");

static int ReadPositiveInt(string[] arguments, string name, int fallback)
{
    for (int i = 0; i < arguments.Length - 1; i++)
        if (arguments[i] == name && int.TryParse(arguments[i + 1], out int value) && value > 0) return value;
    return fallback;
}
