using System.Globalization;

namespace Engine.Benchmark;

internal sealed record CompareEntry(
    string Name,
    string Verdict,
    double BaselineNs,
    double CurrentNs,
    double TimeDeltaPct,
    double CurrentAllocBytes);

internal sealed record CompareReport(bool SameMachine, int Failures, List<CompareEntry> Entries);

/// <summary>
/// Session-to-session comparison. Same-machine only: absolute numbers are noisy across machines
/// (CPU frequency scaling, background load), so verdicts below are trustworthy only when
/// <see cref="CompareReport.SameMachine"/> is true. Time thresholds: warn at +15%, fail at +30%.
/// Allocations fail whenever average bytes/op exceeds the tolerance or any gen0 collection occurred
/// (steady-state target is zero allocation).
/// </summary>
internal static class BenchmarkComparer
{
    private const double TimeWarnPct = 15.0;
    private const double TimeFailPct = 30.0;

    public static CompareReport Compare(BenchRunResult baseline, BenchRunResult current, double allocTolerance)
    {
        var entries = new List<CompareEntry>(current.Benchmarks.Count);
        int failures = 0;
        foreach (BenchmarkResult currentResult in current.Benchmarks)
        {
            BenchmarkResult? baselineResult = baseline.Benchmarks.Find(b => b.Name == currentResult.Name);
            string verdict;
            double timeDeltaPct;
            if (baselineResult is null)
            {
                timeDeltaPct = double.NaN;
                verdict = "NEW";
            }
            else
            {
                double baseNs = baselineResult.MedianNsPerOp;
                timeDeltaPct = baseNs == 0 ? (currentResult.MedianNsPerOp > 0 ? 100.0 : 0.0)
                    : (currentResult.MedianNsPerOp - baseNs) / baseNs * 100.0;
                bool allocRegressed = currentResult.AllocBytesPerOp > allocTolerance || currentResult.Gen0Collections > 0;
                string timeVerdict = timeDeltaPct >= TimeFailPct ? "FAIL" : timeDeltaPct >= TimeWarnPct ? "WARN" : "PASS";
                verdict = allocRegressed ? "FAIL(alloc)" : timeVerdict;
            }
            if (verdict.StartsWith("FAIL", StringComparison.Ordinal)) failures++;
            entries.Add(new CompareEntry(
                currentResult.Name,
                verdict,
                baselineResult?.MedianNsPerOp ?? double.NaN,
                currentResult.MedianNsPerOp,
                timeDeltaPct,
                currentResult.AllocBytesPerOp));
        }
        bool sameMachine = string.Equals(baseline.Machine, current.Machine, StringComparison.Ordinal);
        return new CompareReport(sameMachine, failures, entries);
    }

    public static void Print(CompareReport report)
    {
        Console.WriteLine();
        Console.WriteLine($"Comparing against previous run ({report.Entries.Count} benchmarks)");
        if (!report.SameMachine) Console.WriteLine("WARNING: different machine than the reference — deltas are advisory only.");
        Console.WriteLine();
        Console.WriteLine($"{"Name",-28} {"Baseline",10} {"Current",10} {"Δtime",8} {"Alloc/op",10}  Verdict");
        int pass = 0, warn = 0, fail = 0;
        foreach (CompareEntry entry in report.Entries)
        {
            Console.WriteLine(
                $"{entry.Name,-28} {FormatNs(entry.BaselineNs),10} {FormatNs(entry.CurrentNs),10} " +
                $"{FormatPct(entry.TimeDeltaPct),8} {entry.CurrentAllocBytes.ToString("0.00", CultureInfo.InvariantCulture) + " B",10}  {entry.Verdict}");
            if (entry.Verdict == "PASS") pass++;
            else if (entry.Verdict == "WARN") warn++;
            else if (entry.Verdict != "NEW") fail++;
        }
        Console.WriteLine();
        string suffix = report.SameMachine ? string.Empty : " (advisory)";
        Console.WriteLine($"PASS: {pass}  WARN: {warn}  FAIL: {fail}{suffix}");
    }

    private static string FormatNs(double ns)
    {
        if (double.IsNaN(ns)) return "-";
        if (ns >= 1_000_000) return (ns / 1_000_000.0).ToString("0.000", CultureInfo.InvariantCulture) + " ms";
        if (ns >= 1_000) return (ns / 1_000.0).ToString("0.00", CultureInfo.InvariantCulture) + " µs";
        return ns.ToString("0.0", CultureInfo.InvariantCulture) + " ns";
    }

    private static string FormatPct(double pct)
    {
        if (double.IsNaN(pct)) return "-";
        return (pct > 0 ? "+" : "") + pct.ToString("0.0", CultureInfo.InvariantCulture) + "%";
    }
}
