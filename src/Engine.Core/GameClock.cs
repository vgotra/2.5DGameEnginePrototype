namespace Engine.Core;

public struct GameClock
{
    public const double FixedStep = 1.0 / 60.0;
    public double TotalSeconds;
    public double DeltaSeconds;
    public double Accumulator;

    public void Advance(double elapsedSeconds)
    {
        DeltaSeconds = Math.Min(elapsedSeconds, 0.25);
        TotalSeconds += DeltaSeconds;
        Accumulator += DeltaSeconds;
    }

    public bool TryConsumeFixedStep()
    {
        if (Accumulator < FixedStep) return false;
        Accumulator -= FixedStep;
        return true;
    }
}
