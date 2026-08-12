using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Engine.Benchmark.Benchmarks;

namespace Engine.Benchmark;

internal static class Program
{
    private const int SchemaVersion = 1;
    private const double DefaultAllocTolerance = 0.5;

    private static int Main(string[] args)
    {
        string? machine = null;
        int? iterations = null;
        bool save = false;
        string compare = "last";
        double allocTolerance = DefaultAllocTolerance;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--save": save = true; break;
                case "--machine": machine = NextValue(args, ref i); break;
                case "--iterations": iterations = int.Parse(NextValue(args, ref i), CultureInfo.InvariantCulture); break;
                case "--alloc-tolerance": allocTolerance = double.Parse(NextValue(args, ref i), CultureInfo.InvariantCulture); break;
                case "--compare": compare = NextValue(args, ref i); break;
                case "--help": PrintUsage(); return 0;
                default:
                    Console.WriteLine($"Unknown argument '{args[i]}'");
                    PrintUsage();
                    return 2;
            }
        }

        string resultsDir = Path.Combine(Directory.GetCurrentDirectory(), "benchmarks", "results");
        Directory.CreateDirectory(resultsDir);
        string lastPath = Path.Combine(resultsDir, "last.json");
        string baselinePath = Path.Combine(resultsDir, "baseline.json");

        BenchRunResult? previous = compare == "last" ? ReadResults(lastPath) : null;
        BenchRunResult? baseline = compare == "baseline" ? ReadResults(baselinePath) : null;
        if (compare is not ("last" or "baseline" or "none"))
        {
            Console.WriteLine($"Unknown --compare target '{compare}'");
            return 2;
        }

        BenchRunResult run;
        try
        {
            run = new(
                SchemaVersion,
                machine ?? Environment.MachineName,
                TryGetCommit(),
                DateTime.UtcNow,
                BenchmarkCatalog.Create(iterations).Select(BenchRunner.Run).ToList());
        }
        finally
        {
            ArpgBenchmarks.Dispose();
            PolicyBenchmarks.Dispose();
        }

        PrintResults(run);
        WriteResults(lastPath, run);
        if (save) WriteResults(baselinePath, run);

        BenchRunResult? reference = compare switch { "last" => previous, "baseline" => baseline, _ => null };
        if (reference is null)
        {
            if (compare != "none")
                Console.WriteLine($"\nNo {compare} results to compare against (run with --save to create a baseline).");
            return 0;
        }

        CompareReport report = BenchmarkComparer.Compare(reference, run, allocTolerance);
        BenchmarkComparer.Print(report);
        return report.SameMachine && report.Failures > 0 ? 1 : 0;
    }

    private static void PrintResults(BenchRunResult run)
    {
        Console.WriteLine($"Machine: {run.Machine}   Commit: {run.Commit}   Run: {run.TimestampUtc:u}");
        Console.WriteLine();
        Console.WriteLine($"{"Name",-28} {"Iterations",10} {"Median",10} {"Min",10} {"Max",10} {"Alloc/op",10} {"gen0",4} {"gen1",4} {"gen2",4}");
        foreach (BenchmarkResult result in run.Benchmarks)
        {
            Console.WriteLine(
                $"{result.Name,-28} {result.Iterations,10} {FormatNs(result.MedianNsPerOp),10} {FormatNs(result.MinNsPerOp),10} " +
                $"{FormatNs(result.MaxNsPerOp),10} {result.AllocBytesPerOp.ToString("0.00", CultureInfo.InvariantCulture) + " B",10} " +
                $"{result.Gen0Collections,4} {result.Gen1Collections,4} {result.Gen2Collections,4}");
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
            Usage: dotnet run -c Release --project benchmarks\Engine.Benchmark -- [options]

            Options:
              --save                  Write results as the committed baseline (benchmarks/results/baseline.json).
              --compare <target>      Compare against 'last' (default), 'baseline', or 'none'.
              --iterations <count>    Override per-benchmark iteration counts.
              --machine <name>        Machine tag recorded in results (default: machine name).
              --alloc-tolerance <B>   Average bytes/op above which allocations FAIL (default: 0.5).
              --help                  Show this help.

            Results: benchmarks/results/last.json every run (gitignored); baseline.json on --save.
            Verdicts: time WARN at +15%, FAIL at +30%; allocations FAIL above tolerance or on any gen0.
            Only same-machine comparisons are authoritative.
            """);
    }

    private static string NextValue(string[] args, ref int i)
    {
        if (++i >= args.Length) throw new ArgumentException($"Missing value for '{args[i - 1]}'");
        return args[i];
    }

    private static string FormatNs(double ns)
    {
        if (ns >= 1_000_000) return (ns / 1_000_000.0).ToString("0.000", CultureInfo.InvariantCulture) + " ms";
        if (ns >= 1_000) return (ns / 1_000.0).ToString("0.00", CultureInfo.InvariantCulture) + " µs";
        return ns.ToString("0.0", CultureInfo.InvariantCulture) + " ns";
    }

    private static string TryGetCommit()
    {
        try
        {
            ProcessStartInfo info = new("git", "rev-parse --short HEAD")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using Process process = Process.Start(info)!;
            string commit = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return string.IsNullOrEmpty(commit) ? "unknown" : commit;
        }
        catch
        {
            return "unknown";
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static void WriteResults(string path, BenchRunResult run)
        => File.WriteAllText(path, JsonSerializer.Serialize(run, JsonOptions));

    private static BenchRunResult? ReadResults(string path)
    {
        if (!File.Exists(path)) return null;
        return JsonSerializer.Deserialize<BenchRunResult>(File.ReadAllText(path));
    }
}
