namespace Engine.App;

public enum SceneSpawnKind : byte { None, Hero, Enemy, Npc, Item, Effect }

public readonly record struct SceneEntryPoint(string Name, string Marker);
public readonly record struct SceneMarkerDefinition(string Name, System.Numerics.Vector2 Position, float Elevation = 0f);

public readonly record struct SceneSpawnDefinition(
    SceneSpawnKind Kind,
    string Marker,
    HeroId Hero,
    EnemyId Enemy,
    NpcId Npc,
    ItemDefinition Item,
    EffectDefinition Effect);

public readonly record struct SceneDefinition(
    SceneId Id,
    MapId Map,
    SceneMarkerDefinition Marker1,
    SceneMarkerDefinition Marker2,
    SceneMarkerDefinition Marker3,
    SceneMarkerDefinition Marker4,
    int MarkerCount,
    SceneEntryPoint EntryPoint1,
    SceneEntryPoint EntryPoint2,
    SceneSpawnDefinition Spawn1,
    SceneSpawnDefinition Spawn2,
    SceneSpawnDefinition Spawn3,
    SceneSpawnDefinition Spawn4,
    int SpawnCount = 0);
