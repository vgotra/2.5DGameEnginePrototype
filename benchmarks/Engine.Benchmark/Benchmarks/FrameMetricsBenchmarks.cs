using Engine.App;

namespace Engine.Benchmark.Benchmarks;

internal static class FrameMetricsBenchmarks
{
    private static FrameMetrics _metrics = new(0, "Mailbox", false);
    private static readonly double[] Uncapped = CreateSamples(0);
    private static readonly double[] Fps60 = CreateSamples(60);
    private static readonly double[] Fps120 = CreateSamples(120);
    private static readonly double[] Fps144 = CreateSamples(144);
    private static readonly double[] Fps240 = CreateSamples(240);
    private static readonly double[] Jitter = CreateJitterSamples();

    public static BenchmarkCase[] Create() =>
    [
        new("FrameMetrics_Uncapped", 5_000, () => Run(Uncapped)),
        new("FrameMetrics_60Fps", 5_000, () => Run(Fps60)),
        new("FrameMetrics_120Fps", 5_000, () => Run(Fps120)),
        new("FrameMetrics_144Fps", 5_000, () => Run(Fps144)),
        new("FrameMetrics_240Fps", 5_000, () => Run(Fps240)),
        new("FrameMetrics_Jitter", 5_000, () => Run(Jitter)),
    ];

    private static void Run(double[] samples)
    {
        for (int i = 0; i < samples.Length; i++)
            _metrics.Add(samples[i], 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0);
    }

    private static double[] CreateSamples(double fps)
    {
        double frameMs = fps <= 0 ? 7.5 : 1000.0 / fps;
        double[] samples = new double[120];
        Array.Fill(samples, frameMs);
        return samples;
    }

    private static double[] CreateJitterSamples()
    {
        double[] samples = CreateSamples(60);
        samples[30] = 24;
        samples[90] = 100;
        return samples;
    }
}
