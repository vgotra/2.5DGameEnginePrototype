using System.Numerics;
using System.Runtime.CompilerServices;

namespace IsometricSandbox.Game;

public readonly struct ScreenTransform
{
    public readonly float OriginX;
    public readonly float OriginY;
    public readonly float ScaleX;
    public readonly float ScaleY;
    public readonly float ShearX;
    public readonly float ShearY;

    public ScreenTransform(float originX, float originY, float scaleX, float scaleY, float shearX, float shearY)
    {
        OriginX = originX;
        OriginY = originY;
        ScaleX = scaleX;
        ScaleY = scaleY;
        ShearX = shearX;
        ShearY = shearY;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 ToScreen(float worldX, float worldY)
        => new(OriginX + worldX * ScaleX + worldY * ShearX, OriginY + worldX * ShearY + worldY * ScaleY);
}
