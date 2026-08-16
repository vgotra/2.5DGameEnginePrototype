using System.Numerics;
using Engine.App;
using Engine.Ecs.Sparse;
using Engine.Rendering;

namespace IsometricSandbox.Game.Gameplay.Systems;

public struct EntityRenderBody : IQueryAction<Position, Renderable, EntityRenderBody>
{
    public TerrainSurface Grid;
    public IsometricCamera Camera;
    public SpritePacket[] Sprites;
    public int Written;
    public Entity ExcludedEntity;
    public PresentationPositionHistory? History;
    public double InterpolationAlpha;

    public static void Execute(ref EntityRenderBody body, Entity entity, ref Position position, ref Renderable renderable)
    {
        if (entity == body.ExcludedEntity) return;
        Vector2 world = body.History is not null && body.History.TryGetInterpolated(entity, body.InterpolationAlpha, out Vector2 interpolated) ? interpolated : position.Value;
        RenderItem item = renderable.ToRenderItem(world);
        body.Written = SpriteExtraction.WriteEntity(
            body.Grid, body.Camera, body.Sprites.AsSpan(), body.Written, in item);
    }
}
