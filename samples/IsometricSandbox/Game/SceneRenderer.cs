using System.Numerics;
using Engine.Platform;
using Engine.Rendering;
using Engine.Rendering.Vulkan;

namespace IsometricSandbox.Game;

// Owns the Vulkan renderer, the texture library, and the reusable sprite
// buffers used by extraction. Submitting a frame extracts the whole scene
// (tiles + animals + player + arrows), depth-sorts it, and presents.
public sealed class SceneRenderer : IDisposable
{
    private readonly VulkanRenderer _renderer;
    private readonly TextureLibrary _textures;
    private readonly SpritePacket[] _sprites;
    private readonly SpritePacket[] _spriteScratch;
    private readonly int[] _sortKeyCounts;
    private readonly Random _flicker = new(7);
    private Vector2 _viewport;

    public SceneRenderer(in NativeWindowSurface surface, TileMap map, Vector2 viewport)
    {
        _renderer = new VulkanRenderer(surface);
        _textures = new TextureLibrary(_renderer);
        int tileCapacity = map.Width * map.Height;
        _sprites = new SpritePacket[tileCapacity * 2 + 128];
        _spriteScratch = new SpritePacket[tileCapacity * 2 + 128];
        _sortKeyCounts = new int[tileCapacity];
        _viewport = viewport;
    }

    // Keeps the swapchain and the viewport used for extraction in sync with
    // the window size.
    public void Resize(Vector2 viewport)
    {
        _viewport = viewport;
        _renderer.Resize((int)viewport.X, (int)viewport.Y);
    }

    // Extracts and presents one frame; returns the number of sprites drawn.
    public int SubmitFrame(
        TileMap map,
        IsometricCamera camera,
        ReadOnlySpan<Animal> animals,
        ReadOnlySpan<Arrow> arrows,
        Vector2 playerWorld,
        float jumpHeight)
    {
        _renderer.BeginFrame(_viewport);
        int spriteCount = RenderExtractionSystem.ExtractScene(
            map, camera, _sprites, animals, arrows, playerWorld, jumpHeight,
            _textures, _flicker, _sortKeyCounts, _spriteScratch);
        _renderer.Submit(_sprites.AsSpan(0, spriteCount));
        _renderer.EndFrame();
        return spriteCount;
    }

    public void Dispose() => _renderer.Dispose();
}
