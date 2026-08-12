using System.Diagnostics;

namespace Engine.Core;

public sealed class FrameTimer(double targetFramesPerSecond = 0)
{
    private readonly double _targetFrameSeconds = targetFramesPerSecond > 0 ? 1.0 / targetFramesPerSecond : 0;
    private long _previousTick = Stopwatch.GetTimestamp();

    public double Advance()
    {
        long now = Stopwatch.GetTimestamp();
        double elapsed = (now - _previousTick) / (double)Stopwatch.Frequency;
        _previousTick = now;
        return elapsed;
    }

    public void WaitForNextFrame()
    {
        if (_targetFrameSeconds <= 0) return;
        long now = Stopwatch.GetTimestamp();
        double elapsed = (now - _previousTick) / (double)Stopwatch.Frequency;
        double remaining = _targetFrameSeconds - elapsed;
        if (remaining <= 0) return;
        int wholeMilliseconds = (int)(remaining * 1000.0);
        if (wholeMilliseconds > 0) Thread.Sleep(wholeMilliseconds);
        long deadline = _previousTick + (long)(_targetFrameSeconds * Stopwatch.Frequency);
        while (Stopwatch.GetTimestamp() < deadline)
        {
            Thread.Yield();
        }
    }
}
