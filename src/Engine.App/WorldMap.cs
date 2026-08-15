namespace Engine.App;

public readonly record struct WorldLocation(WorldMapLocationId Id, SceneId Scene, bool Unlocked = false);
public readonly record struct WorldMapLocationId(string Value);

public sealed class WorldMap
{
    private readonly Dictionary<WorldMapLocationId, WorldLocation> _locations = new();
    private readonly Dictionary<WorldMapLocationId, List<WorldMapLocationId>> _connections = new();
    public WorldMapLocationId CurrentLocation { get; private set; }
    public bool HasCurrentLocation { get; private set; }

    public bool Register(WorldLocation location)
    {
        if (!_locations.TryAdd(location.Id, location)) return false;
        if (location.Unlocked && !HasCurrentLocation)
        {
            CurrentLocation = location.Id;
            HasCurrentLocation = true;
        }
        return true;
    }

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
        if (!HasCurrentLocation)
        {
            CurrentLocation = id;
            HasCurrentLocation = true;
        }
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

    public bool TravelTo(WorldMapLocationId destination)
    {
        return HasCurrentLocation && TravelTo(CurrentLocation, destination);
    }

    public bool TravelTo(WorldMapLocationId source, WorldMapLocationId destination)
    {
        if (!CanTravel(source, destination)) return false;
        CurrentLocation = destination;
        HasCurrentLocation = true;
        return true;
    }

    public bool TryGetScene(WorldMapLocationId id, out SceneId scene)
    {
        if (_locations.TryGetValue(id, out WorldLocation location))
        {
            scene = location.Scene;
            return true;
        }
        scene = default;
        return false;
    }
}
