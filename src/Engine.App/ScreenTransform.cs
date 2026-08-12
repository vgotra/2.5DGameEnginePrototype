using System.Numerics;
using System.Runtime.CompilerServices;

namespace Engine.App;

public readonly struct ScreenTransform(float originX, float originY, float scaleX, float scaleY, float shearX, float shearY)
{
    public readonly float OriginX = originX;
    public readonly float OriginY = originY;
    public readonly float ScaleX = scaleX;
    public readonly float ScaleY = scaleY;
    public readonly float ShearX = shearX;
    public readonly float ShearY = shearY;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 ToScreen(float worldX, float worldY)
        => new(OriginX + worldX * ScaleX + worldY * ShearX, OriginY + worldX * ShearY + worldY * ScaleY);
}
