using System.Numerics;

namespace Engine.Rendering;

public readonly record struct ShapePacket(Vector2 Position, Vector2 Size, Vector4 Color, float SortKey, ShapeKind Shape = ShapeKind.Diamond);
