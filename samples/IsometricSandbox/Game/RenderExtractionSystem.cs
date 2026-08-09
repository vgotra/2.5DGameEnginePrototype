using System.Numerics;
using Engine.Rendering;

namespace IsometricSandbox.Game;

public static class RenderExtractionSystem
{
    public const float BorderWidth = 2f;
    private static readonly Vector4 White = new(1, 1, 1, 1);
    private static readonly Vector4 Black = new(0, 0, 0, 1);
    private static readonly Vector4 ArrowColor = new(1, 1, 1, 1);
    private static readonly Vector4 DeerColor = new(0.55f, 0.85f, 0.55f, 1);
    private static readonly Vector4 RabbitColor = new(0.95f, 0.65f, 0.75f, 1);

    public static int ExtractMapSprites(TileMap map, IsometricCamera camera, Span<SpritePacket> sprites)
        => ExtractTiles(map, camera, sprites, null, null);

    public static int ExtractScene(
        TileMap map,
        IsometricCamera camera,
        Span<SpritePacket> sprites,
        ReadOnlySpan<Animal> animals,
        ReadOnlySpan<Arrow> arrows,
        Vector2 playerWorld,
        float jumpHeight,
        TextureLibrary textures,
        Random flicker,
        Span<int> keyCounts,
        Span<SpritePacket> scratch)
    {
        int written = ExtractTiles(map, camera, sprites, textures, flicker);
        written = ExtractEntities(map, camera, sprites, written, animals, arrows, playerWorld, jumpHeight, textures);
        StableSortByKey(sprites, written, keyCounts, scratch);
        return written;
    }

    public static int ExtractEntities(
        TileMap map,
        IsometricCamera camera,
        Span<SpritePacket> sprites,
        int written,
        ReadOnlySpan<Animal> animals,
        ReadOnlySpan<Arrow> arrows,
        Vector2 playerWorld,
        float jumpHeight,
        TextureLibrary textures)
    {
        written = ExtractEntity(map, camera, sprites, written, playerWorld, new(44, 56), textures.Player, jumpHeight, White);
        for (int i = 0; i < animals.Length; i++)
        {
            Animal animal = animals[i];
            if (!animal.Alive) continue;
            bool deer = animal.Species == AnimalSpecies.Deer;
            Vector2 size = deer ? new(36, 44) : new(28, 36);
            TextureHandle texture = deer ? textures.Deer : textures.Rabbit;
            Vector4 color = deer ? DeerColor : RabbitColor;
            written = ExtractEntity(map, camera, sprites, written, animal.Position, size, texture, 0f, color);
        }
        for (int i = 0; i < arrows.Length; i++)
        {
            Arrow arrow = arrows[i];
            Vector2 screen = camera.WorldToScreen(arrow.Position, map);
            if (screen.X < -8 || screen.X > camera.Viewport.X + 8 || screen.Y < -8 || screen.Y > camera.Viewport.Y + 8)
                continue;
            float sortKey = SortKey(map, arrow.Position);
            sprites[written++] = new SpritePacket(screen, new(10, 10), ArrowColor, default, default, sortKey, ShapeKind.Diamond);
        }
        return written;
    }

    private static int ExtractTiles(TileMap map, IsometricCamera camera, Span<SpritePacket> sprites, TextureLibrary? textures, Random? flicker)
    {
        ScreenTransform transform = camera.GetScreenTransform(map);
        return ExtractTileRange(map, in transform, camera.Viewport, camera.Projection.TileShape, map.TileWidth, camera.Projection.GetTileHeight(map), 0, map.Height, sprites, textures, flicker);
    }

    internal static int ExtractTileRange(
        TileMap map,
        in ScreenTransform transform,
        Vector2 viewport,
        ShapeKind shape,
        float tileWidth,
        float tileHeight,
        int yStart,
        int yEnd,
        Span<SpritePacket> sprites,
        TextureLibrary? textures,
        Random? flicker)
    {
        bool textured = textures is not null && flicker is not null;
        Vector2 tileSize = new(tileWidth, tileHeight);
        Vector2 borderSize = new(tileWidth + BorderWidth * 2, tileHeight + BorderWidth * 2);
        float halfWidth = tileWidth * 0.5f + BorderWidth;
        float halfHeight = tileHeight * 0.5f + BorderWidth;
        int written = 0;
        for (int y = yStart; y < yEnd && written + 2 <= sprites.Length; y++)
            for (int x = 0; x < map.Width && written + 2 <= sprites.Length; x++)
            {
                Vector2 screen = transform.ToScreen(x + 0.5f, y + 0.5f);
                if (screen.X + halfWidth < 0 || screen.X - halfWidth > viewport.X ||
                    screen.Y + halfHeight < 0 || screen.Y - halfHeight > viewport.Y)
                    continue;
                int sortKey = y * map.Width + x;
                sprites[written++] = new SpritePacket(screen, borderSize, Black, default, default, sortKey, shape);
                sprites[written++] = textured
                    ? WriteTileFill(screen, tileSize, map.Get(x, y), textures, flicker, sortKey, shape)
                    : new SpritePacket(screen, tileSize, White, default, default, sortKey, shape);
            }
        return written;
    }

    // With no texture library this writes the plain white fill used by
    // ExtractMapSprites; otherwise it picks the per-tile texture/color and
    // applies the bonfire flicker.
    private static SpritePacket WriteTileFill(Vector2 screen, Vector2 size, TileType type, TextureLibrary? textures, Random? flicker, float sortKey, ShapeKind shape)
    {
        if (textures is null || flicker is null)
            return new SpritePacket(screen, size, White, default, default, sortKey, shape);
        string? textureName = TileVisual.TextureName(type);
        TextureHandle? texture = textureName is null ? null : textures.TryGetTile(textureName);
        bool isBonfire = type == TileType.Bonfire;
        float brightness = isBonfire ? 0.7f + 0.3f * flicker.NextSingle() : 1f;
        if (texture.HasValue)
            return new SpritePacket(screen, size, White * brightness, texture.Value, default, sortKey, shape);
        return new SpritePacket(screen, size, TileVisual.Color(type) * brightness, default, default, sortKey, shape);
    }

    private static int ExtractEntity(TileMap map, IsometricCamera camera, Span<SpritePacket> sprites, int written, Vector2 world, Vector2 size, TextureHandle texture, float jumpHeight, Vector4 color)
    {
        Vector2 groundScreen = camera.WorldToScreen(world, map) - new Vector2(0, jumpHeight);
        float sortKey = SortKey(map, world);
        Vector2 borderSize = size + new Vector2(BorderWidth * 2, BorderWidth * 2);
        Vector2 borderCenter = groundScreen - new Vector2(0, borderSize.Y * 0.5f);
        Vector2 center = groundScreen - new Vector2(0, size.Y * 0.5f);
        if (written + 2 > sprites.Length) return written;
        sprites[written++] = new SpritePacket(borderCenter, borderSize, Black, default, default, sortKey, ShapeKind.Box);
        sprites[written++] = new SpritePacket(center, size, color, texture, default, sortKey, ShapeKind.Box);
        return written;
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static float SortKey(TileMap map, Vector2 world)
    {
        int x = (int)MathF.Floor(world.X);
        int y = (int)MathF.Floor(world.Y);
        x = Math.Clamp(x, 0, map.Width - 1);
        y = Math.Clamp(y, 0, map.Height - 1);
        return y * map.Width + x;
    }

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
