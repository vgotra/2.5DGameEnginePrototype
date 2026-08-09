using System.Numerics;
using Engine.Rendering;
using Engine.Threading;
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
            ParallelExtractionCase("Extraction_Iso128x128_Parallel", 128, isometric: true),
        ];
    }

    private static BenchmarkCase ExtractionCase(string name, int size, bool isometric)
    {
        TileMap map = new(size, size);
        IsometricCamera camera = new(new Vector2(1920, 1080)) { Mode = isometric ? GameMode.Isometric : GameMode.TopDown };
        camera.Follow(new Vector2(size * 0.5f, size * 0.5f), map);
        SpritePacket[] sprites = new SpritePacket[size * size * 2];
        float sink = 0;
        return new BenchmarkCase(name, size >= 100 ? 5_000 : 50_000,
            () => { sink += RenderExtractionSystem.ExtractMapSprites(map, camera, sprites); });
    }

    private static BenchmarkCase ParallelExtractionCase(string name, int size, bool isometric)
    {
        TileMap map = new(size, size);
        IsometricCamera camera = new(new Vector2(1920, 1080)) { Mode = isometric ? GameMode.Isometric : GameMode.TopDown };
        camera.Follow(new Vector2(size * 0.5f, size * 0.5f), map);
        JobSystem jobs = new();
        int bandCount = Math.Min(jobs.WorkerCount, size);
        int rowsPerBand = (size + bandCount - 1) / bandCount;
        SpritePacket[][] bands = new SpritePacket[bandCount][];
        for (int i = 0; i < bandCount; i++) bands[i] = new SpritePacket[rowsPerBand * size * 2];
        int[] counts = new int[bandCount];
        Random[] flickers = new Random[bandCount];
        for (int i = 0; i < bandCount; i++) flickers[i] = new Random(7 + i);
        TileExtractionDispatch dispatch = new();
        SpritePacket[] merged = new SpritePacket[size * size * 2];
        float sink = 0;
        return new BenchmarkCase(name, 5_000, () =>
        {
            JobHandle barrier = dispatch.Schedule(jobs, map, camera, null, bands, counts, flickers, bandCount, rowsPerBand);
            jobs.Complete(barrier);
            int written = 0;
            for (int band = 0; band < bandCount; band++)
            {
                bands[band].AsSpan(0, counts[band]).CopyTo(merged.AsSpan(written));
                written += counts[band];
            }
            sink += written;
        });
    }
}
