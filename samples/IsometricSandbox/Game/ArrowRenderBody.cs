using System.Numerics;
using Engine.App;
using Engine.Ecs.Sparse;
using Engine.Rendering;

namespace IsometricSandbox.Game;

// Draws each arrow as a small white diamond, culled when fully off-screen.
public struct ArrowRenderBody : IQueryAction<Position, ArrowProjectile, ArrowRenderBody>
{
    private static readonly Vector4 ArrowColor = new(1, 1, 1, 1);
    private static readonly Vector2 ArrowSize = new(10, 10);

    public TerrainSurface Grid;
    public IsometricCamera Camera;
    public SpritePacket[] Sprites;
    public int Written;
    public PresentationPositionHistory? History;
    public double InterpolationAlpha;

    public static void Execute(ref ArrowRenderBody body, Entity entity, ref Position position, ref ArrowProjectile arrow)
    {
        Vector2 world = body.History is not null && body.History.TryGetInterpolated(entity, body.InterpolationAlpha, out Vector2 interpolated) ? interpolated : position.Value;
        Vector2 screen = body.Camera.WorldToScreen(world, body.Grid);
        if (screen.X < -8 || screen.X > body.Camera.Viewport.X + 8 || screen.Y < -8 || screen.Y > body.Camera.Viewport.Y + 8)
            return;
        float sortKey = SpriteExtraction.SortKey(body.Grid, world);
        body.Sprites[body.Written++] = new SpritePacket(screen, ArrowSize, ArrowColor, default, default, sortKey);
    }
}
