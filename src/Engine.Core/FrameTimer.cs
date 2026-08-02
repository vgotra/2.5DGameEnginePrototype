using System.Diagnostics;

namespace Engine.Core;

/// <summary>
/// High-resolution frame timer for the game loop. Measures elapsed time with
/// <see cref="Stopwatch"/> (replacing coarse <c>Environment.TickCount64</c> dt) and can
/// optionally pace the loop to a target frame rate. A target of 0 leaves the loop unpaced.
/// </summary>
public sealed class FrameTimer
{
    private readonly double _targetFrameSeconds;
    private long _previousTick;

    /// <param name="targetFramesPerSecond">Frame cap; 0 or negative means unpaced.</param>
    public FrameTimer(double targetFramesPerSecond = 0)
    {
        _targetFrameSeconds = targetFramesPerSecond > 0 ? 1.0 / targetFramesPerSecond : 0;
        _previousTick = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Call once per frame at the start of the loop. Returns the elapsed time (seconds) since
    /// the previous call, measured from the high-resolution stopwatch.
    /// </summary>
    public double Advance()
    {
        long now = Stopwatch.GetTimestamp();
        double elapsed = (now - _previousTick) / (double)Stopwatch.Frequency;
        _previousTick = now;
        return elapsed;
    }

    /// <summary>
    /// Call once per frame at the end of the loop. When a target frame rate is configured, sleeps
    /// until the frame deadline so presentations land on a steady cadence; otherwise returns
    /// immediately so the loop runs as fast as the CPU/GPU allow (with MAILBOX present this still
    /// delivers exactly one non-blocking present per iteration).
    /// </summary>
    public void WaitForNextFrame()
    {
        if (_targetFrameSeconds <= 0) return;
        long now = Stopwatch.GetTimestamp();
        double elapsed = (now - _previousTick) / (double)Stopwatch.Frequency;
        double remaining = _targetFrameSeconds - elapsed;
        if (remaining > 0.001) Thread.Sleep((int)(remaining * 1000.0));
    }
}
