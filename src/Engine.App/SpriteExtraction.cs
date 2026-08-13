using System.Numerics;
using System.Runtime.CompilerServices;
using Engine.Rendering;

namespace Engine.App;

public static class SpriteExtraction
{
    public const float BorderWidth = 2f;
    private static readonly Vector4 White = new(1, 1, 1, 1);
    private static readonly Vector4 Black = new(0, 0, 0, 1);

    public static int ExtractTiles(
        TileGrid grid,
        IsometricCamera camera,
        ITileTextureProvider? textures,
        Random? flicker,
        Span<SpritePacket> sprites)
    {
        ScreenTransform transform = camera.GetScreenTransform(grid);
        return ExtractTileRange(grid, in transform, camera.Viewport, camera.Projection.TileShape, grid.TileWidth,
            camera.Projection.GetTileHeight(grid), 0, grid.Height, sprites, textures, flicker);
    }

    public static int ExtractTileRange(
        TileGrid grid,
        in ScreenTransform transform,
        Vector2 viewport,
        ShapeKind shape,
        float tileWidth,
        float tileHeight,
        int yStart,
        int yEnd,
        Span<SpritePacket> sprites,
        ITileTextureProvider? textures,
        Random? flicker)
    {
        bool textured = textures is not null && flicker is not null;
        Vector2 tileSize = new(tileWidth, tileHeight);
        Vector2 borderSize = new(tileWidth + BorderWidth * 2, tileHeight + BorderWidth * 2);
        float halfWidth = tileWidth * 0.5f + BorderWidth;
        float halfHeight = tileHeight * 0.5f + BorderWidth;
        int written = 0;
        for (int y = yStart; y < yEnd && written + 2 <= sprites.Length; y++)
            for (int x = 0; x < grid.Width && written + 2 <= sprites.Length; x++)
            {
                Vector2 screen = transform.ToScreen(x + 0.5f, y + 0.5f);
                if (screen.X + halfWidth < 0 || screen.X - halfWidth > viewport.X ||
                    screen.Y + halfHeight < 0 || screen.Y - halfHeight > viewport.Y)
                    continue;
                int sortKey = y * grid.Width + x;
                sprites[written++] = new SpritePacket(screen, borderSize, Black, default, default, sortKey, shape);
                sprites[written++] = textured
                    ? WriteTileFill(screen, tileSize, (TileType)grid.Get(x, y), textures, flicker, sortKey, shape)
                    : new SpritePacket(screen, tileSize, White, default, default, sortKey, shape);
            }
        return written;
    }

    public static int WriteEntity(
        TileGrid grid,
        IsometricCamera camera,
        Span<SpritePacket> sprites,
        int written,
        Vector2 world,
        Vector2 size,
        TextureHandle texture,
        float jumpHeight,
        Vector4 color)
    {
        Vector2 groundScreen = camera.WorldToScreen(world, grid) - new Vector2(0, jumpHeight);
        float sortKey = SortKey(grid, world);
        Vector2 borderSize = size + new Vector2(BorderWidth * 2, BorderWidth * 2);
        Vector2 borderCenter = groundScreen - new Vector2(0, borderSize.Y * 0.5f);
        Vector2 center = groundScreen - new Vector2(0, size.Y * 0.5f);
        if (written + 2 > sprites.Length) return written;
        sprites[written++] = new SpritePacket(borderCenter, borderSize, Black, default, default, sortKey, ShapeKind.Box);
        sprites[written++] = new SpritePacket(center, size, color, texture, default, sortKey, ShapeKind.Box);
        return written;
    }

    public static int WriteEntity(
        TileGrid grid,
        IsometricCamera camera,
        Span<SpritePacket> sprites,
        int written,
        in RenderItem item,
        float jumpHeight = 0f)
    {
        int start = written;
        written = WriteEntity(grid, camera, sprites, written, item.WorldPosition, item.Size,
            item.Texture, jumpHeight, item.Color);
        for (int i = start; i < written; i++)
        {
            Vector4 color = sprites[i].Color;
            sprites[i] = sprites[i] with { Size = sprites[i].Size * item.Scale, Color = new Vector4(color.X, color.Y, color.Z, color.W * item.Opacity), Material = item.Material, Scale = item.Scale, AnimationFrame = item.AnimationFrame, Blend = item.Blend };
        }
        return written;
    }

    public static float SortKey(TileGrid grid, Vector2 world)
    {
        int x = (int)MathF.Floor(world.X);
        int y = (int)MathF.Floor(world.Y);
        x = Math.Clamp(x, 0, grid.Width - 1);
        y = Math.Clamp(y, 0, grid.Height - 1);
        return y * grid.Width + x;
    }

    private static SpritePacket WriteTileFill(
        Vector2 screen,
        Vector2 size,
        TileType type,
        ITileTextureProvider? textures,
        Random? flicker,
        float sortKey,
        ShapeKind shape)
    {
        if (textures is null || flicker is null)
            return new SpritePacket(screen, size, White, default, default, sortKey, shape);
        string? textureName = TileVisual.TextureName(type);
        TextureHandle? texture = textureName is null ? null : textures.TryGet(textureName);
        bool isBonfire = type == TileType.Bonfire;
        float brightness = isBonfire ? 0.7f + 0.3f * flicker.NextSingle() : 1f;
        if (texture.HasValue)
            return new SpritePacket(screen, size, White * brightness, texture.Value, default, sortKey, shape);
        return new SpritePacket(screen, size, TileVisual.Color(type) * brightness, default, default, sortKey, shape);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void StableSortByKey(Span<SpritePacket> buffer, int count, Span<int> keyCounts, Span<SpritePacket> scratch)
    {
        if (count < 2) return;
        keyCounts.Clear();
        for (int i = 0; i < count; i++) keyCounts[(int)buffer[i].SortKey]++;
        int running = 0;
        for (int k = 0; k < keyCounts.Length; k++)
        {
            int bucket = keyCounts[k];
            keyCounts[k] = running;
            running += bucket;
        }
        for (int i = 0; i < count; i++)
        {
            int key = (int)buffer[i].SortKey;
            scratch[keyCounts[key]++] = buffer[i];
        }
        scratch.Slice(0, count).CopyTo(buffer);
    }
}
