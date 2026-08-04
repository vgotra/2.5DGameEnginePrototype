using System.Numerics;
using Engine.Rendering;
using IsometricSandbox.Game;

namespace Engine.Benchmark.Benchmarks;

internal static class ExtractionBenchmarks
{
    public static BenchmarkCase[] Create()
    {
        return
        [
            ExtractionCase("Extraction_Iso20x20", 20, isometric: true),
            ExtractionCase("Extraction_Flat20x20", 20, isometric: false),
            ExtractionCase("Extraction_Iso128x128", 128, isometric: true),
            ExtractionCase("Extraction_Flat128x128", 128, isometric: false),
        ];
    }

    private static BenchmarkCase ExtractionCase(string name, int size, bool isometric)
    {
        TileMap map = new(size, size);
        IsometricCamera camera = new(new Vector2(1920, 1080)) { Isometric = isometric };
        camera.Follow(new Vector2(size * 0.5f, size * 0.5f), map);
        SpritePacket[] sprites = new SpritePacket[size * size * 2];
        float sink = 0;
        return new BenchmarkCase(name, size >= 100 ? 5_000 : 50_000,
            () => { sink += RenderExtractionSystem.ExtractMapSprites(map, camera, sprites); });
    }
}
