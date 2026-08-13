using Engine.App;
using Engine.Ecs.Sparse;
using Engine.Rendering;

namespace IsometricSandbox.Game;

// Writes the border+fill quads for every rendered entity (critters and the
// simulation herd). The player is drawn manually so the jump lift applies.
public struct EntityRenderBody : IQueryAction<Position, Renderable, EntityRenderBody>
{
    public TileGrid Grid;
    public IsometricCamera Camera;
    public SpritePacket[] Sprites;
    public int Written;
    public Entity ExcludedEntity;

    public static void Execute(ref EntityRenderBody body, Entity entity, ref Position position, ref Renderable renderable)
    {
        if (entity == body.ExcludedEntity) return;
        RenderItem item = renderable.ToRenderItem(position.Value);
        body.Written = SpriteExtraction.WriteEntity(
            body.Grid, body.Camera, body.Sprites.AsSpan(), body.Written, in item);
    }
}
