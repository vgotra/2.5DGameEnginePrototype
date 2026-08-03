namespace Engine.Core;

public readonly record struct EntityId(uint Index, uint Generation)
{
    public static readonly EntityId Invalid = new(uint.MaxValue, 0);
    public bool IsValid => Index != uint.MaxValue;
}
