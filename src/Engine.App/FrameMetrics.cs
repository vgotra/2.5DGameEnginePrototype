namespace Engine.App;

public struct FrameMetrics
{
    private const int PrintInterval = 120;
    private double _totalFrameMs;
    private double _maxFrameMs;
    private long _totalFixedSteps;
    private long _totalSprites;
    private long _totalAllocatedBytes;
    private long _totalFixedBytes;
    private long _totalRenderBytes;
    private long _gen0;
    private long _gen1;
    private long _gen2;
    private int _frames;
    private readonly double[] _frameSamples = new double[PrintInterval];
    private readonly double[][] _phaseSamples =
    [
        new double[PrintInterval], new double[PrintInterval], new double[PrintInterval],
        new double[PrintInterval], new double[PrintInterval], new double[PrintInterval]
    ];

    public FrameMetrics()
    {
        _frameSamples = new double[PrintInterval];
        _phaseSamples =
        [
            new double[PrintInterval], new double[PrintInterval], new double[PrintInterval],
            new double[PrintInterval], new double[PrintInterval], new double[PrintInterval]
        ];
    }

    public void Add(double frameMs, double simulationMs, double ecsMs, double schedulerMs, double renderMs, double presentMs, int fixedSteps, int sprites, long allocatedBytes, long fixedBytes, long renderBytes, long gen0, long gen1, long gen2)
    {
        _totalFrameMs += frameMs;
        if (frameMs > _maxFrameMs) _maxFrameMs = frameMs;
        _totalFixedSteps += fixedSteps;
        _totalSprites += sprites;
        _totalAllocatedBytes += allocatedBytes;
        _totalFixedBytes += fixedBytes;
        _totalRenderBytes += renderBytes;
        _gen0 += gen0;
        _gen1 += gen1;
        _gen2 += gen2;
        _frames++;
        _frameSamples[_frames - 1] = frameMs;
        _phaseSamples[0][_frames - 1] = simulationMs;
        _phaseSamples[1][_frames - 1] = ecsMs;
        _phaseSamples[2][_frames - 1] = schedulerMs;
        _phaseSamples[3][_frames - 1] = renderMs;
        _phaseSamples[4][_frames - 1] = presentMs;
        _phaseSamples[5][_frames - 1] = frameMs;
        if (_frames >= PrintInterval) PrintAndReset();
    }

    private void PrintAndReset()
    {
        double avgFrameMs = _totalFrameMs / _frames;
        double avgAlloc = (double)_totalAllocatedBytes / _frames;
        double avgFixed = (double)_totalFixedBytes / _frames;
        double avgRender = (double)_totalRenderBytes / _frames;
        Span<double> ordered = stackalloc double[PrintInterval];
        _frameSamples.AsSpan(0, _frames).CopyTo(ordered);
        ordered[.._frames].Sort();
        double median = Percentile(ordered[.._frames], 0.50);
        double p95 = Percentile(ordered[.._frames], 0.95);
        double p99 = Percentile(ordered[.._frames], 0.99);
        double simulation = Average(_phaseSamples[0]);
        double ecs = Average(_phaseSamples[1]);
        double scheduler = Average(_phaseSamples[2]);
        double render = Average(_phaseSamples[3]);
        double present = Average(_phaseSamples[4]);
        Console.WriteLine(
            $"metrics  frames={_frames,4}  avg={avgFrameMs,7:F2}ms  median={median,7:F2}ms  p95={p95,7:F2}ms  p99={p99,7:F2}ms  max={_maxFrameMs,7:F2}ms  " +
            $"sim={_totalFixedSteps,4}  sprites={_totalSprites,6}  alloc={avgAlloc,6:F1} B/frame  " +
            $"simMs={simulation,6:F2} ecsMs={ecs,6:F2} schedMs={scheduler,6:F2} renderMs={render,6:F2} presentMs={present,6:F2}  " +
            $"fixedAlloc={avgFixed,6:F1} renderAlloc={avgRender,6:F1} gen0={_gen0} gen1={_gen1} gen2={_gen2}");
        _totalFrameMs = 0;
        _maxFrameMs = 0;
        _totalFixedSteps = 0;
        _totalSprites = 0;
        _totalAllocatedBytes = 0;
        _totalFixedBytes = 0;
        _totalRenderBytes = 0;
        _gen0 = 0;
        _gen1 = 0;
        _gen2 = 0;
        _frames = 0;
    }

    private static double Percentile(ReadOnlySpan<double> sorted, double percentile)
    {
        int index = Math.Clamp((int)Math.Ceiling(sorted.Length * percentile) - 1, 0, sorted.Length - 1);
        return sorted[index];
    }

    private double Average(double[] values)
    {
        double total = 0;
        for (int i = 0; i < _frames; i++) total += values[i];
        return total / _frames;
    }
}

