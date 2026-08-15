namespace Engine.App;

public readonly record struct WorldLocation(WorldMapLocationId Id, SceneId Scene, bool Unlocked = false);
public readonly record struct WorldMapLocationId(string Value);

public sealed class WorldMap
{
    private readonly Dictionary<WorldMapLocationId, WorldLocation> _locations = new();
    private readonly Dictionary<WorldMapLocationId, List<WorldMapLocationId>> _connections = new();

    public bool Register(WorldLocation location)
        => _locations.TryAdd(location.Id, location);

    public bool Connect(WorldMapLocationId from, WorldMapLocationId to)
    {
        if (!_locations.ContainsKey(from) || !_locations.ContainsKey(to)) return false;
        if (!_connections.TryGetValue(from, out List<WorldMapLocationId>? targets))
        {
            targets = new List<WorldMapLocationId>();
            _connections.Add(from, targets);
        }

        if (targets.Contains(to)) return true;
        targets.Add(to);
        return true;
    }

    public bool Unlock(WorldMapLocationId id)
    {
        if (!_locations.TryGetValue(id, out WorldLocation location)) return false;
        _locations[id] = location with { Unlocked = true };
        return true;
    }

    public bool CanTravel(WorldMapLocationId from, WorldMapLocationId to)
    {
        if (!_locations.TryGetValue(from, out WorldLocation source) || !source.Unlocked) return false;
        if (!_locations.TryGetValue(to, out WorldLocation target) || !target.Unlocked) return false;
        return _connections.TryGetValue(from, out List<WorldMapLocationId>? targets) && targets.Contains(to);
    }

    public bool TryGet(WorldMapLocationId id, out WorldLocation location)
        => _locations.TryGetValue(id, out location);
}
