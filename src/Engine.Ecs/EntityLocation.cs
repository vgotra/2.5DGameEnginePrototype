namespace Engine.Ecs;

internal readonly struct EntityLocation(int archetypeIndex, int row)
{
    public readonly int ArchetypeIndex = archetypeIndex;
    public readonly int Row = row;
}
