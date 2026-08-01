using System.Numerics;

namespace IsometricSandbox.Game;

public readonly record struct WorldPosition(Vector2 Value);
public readonly record struct Movement(Vector2 Velocity, float Speed);
public readonly record struct TileCollider(float Radius);
public readonly record struct SpriteVisual(Vector4 Color, Vector2 Size, float SortKey);
public readonly record struct PlayerTag;
