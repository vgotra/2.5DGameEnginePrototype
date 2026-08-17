using System.Numerics;

namespace Engine.App;

public readonly record struct MapLocation(MapId Map, Vector2 Position, float Elevation = 0f, string? Marker = null)
{
    public static MapLocation At(MapId map, Vector2 position, float elevation = 0f)
        => new(map, position, elevation);

    public static MapLocation At(string marker)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(marker);
        return new MapLocation(default, default, 0f, marker);
    }
}

public sealed class SceneMap
{
    private readonly Dictionary<string, MapLocation> _markers = new(StringComparer.Ordinal);

    public SceneMap(MapId id) => Id = id;

    public MapId Id { get; }

    public void AddMarker(string name, Vector2 position, float elevation = 0f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _markers[name] = new MapLocation(Id, position, elevation);
    }

    public bool TryResolve(string name, out MapLocation location)
        => _markers.TryGetValue(name, out location);

    public MapLocation Resolve(string name)
        => _markers.TryGetValue(name, out MapLocation location)
            ? location
            : throw new KeyNotFoundException($"Map marker '{name}' was not found in '{Id.Value}'.");
}
