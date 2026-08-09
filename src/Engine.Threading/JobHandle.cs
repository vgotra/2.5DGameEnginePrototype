namespace Engine.Threading;

public readonly struct JobHandle : IEquatable<JobHandle>
{
    internal readonly int Token;
    internal readonly int Generation;

    internal JobHandle(int slot, int generation)
    {
        Token = slot + 1;
        Generation = generation;
    }

    internal int Slot => Token - 1;
    public bool IsValid => Token != 0;
    public static JobHandle None => default;

    public bool Equals(JobHandle other) => Token == other.Token && Generation == other.Generation;
    public override bool Equals(object? obj) => obj is JobHandle other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Token, Generation);
    public static bool operator ==(JobHandle left, JobHandle right) => left.Equals(right);
    public static bool operator !=(JobHandle left, JobHandle right) => !left.Equals(right);
}
