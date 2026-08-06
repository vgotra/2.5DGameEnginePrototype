using System.Numerics;

namespace Engine.Rendering;

public readonly record struct SpritePacket(Vector2 Position, Vector2 Size, Vector4 Color, TextureHandle Texture, MaterialHandle Material, float SortKey, ShapeKind Shape = ShapeKind.Diamond);
