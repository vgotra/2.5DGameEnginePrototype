using System.Numerics;
using Engine.App;
using Engine.Rendering;
using Engine.Threading;

namespace Engine.Benchmark.Benchmarks;

internal static class ExtractionBenchmarks
{
    public static BenchmarkCase[] CreateQuick()
    {
        TerrainSurface grid = OpenGrid(4);
        IsometricCamera camera = new(new Vector2(320, 240));
        camera.Follow(new Vector2(2, 2), grid);
        SpritePacket[] sprites = new SpritePacket[32];
        return [new BenchmarkCase("Extraction_QuickSmoke", 1, () => SpriteExtraction.ExtractTiles(grid, camera, null, null, sprites))];
    }

    public static BenchmarkCase[] Create()
    {
        return
        [
            ExtractionCase("Extraction_Iso20x20", 20),
            ExtractionCase("Extraction_Iso128x128", 128),
            ParallelExtractionCase("Extraction_Iso128x128_Parallel", 128),
        ];
    }

    private static TerrainSurface OpenGrid(int size) => new(size, size, 1f, 64f, 32f, 7);

    private static BenchmarkCase ExtractionCase(string name, int size)
    {
        TerrainSurface grid = OpenGrid(size);
        IsometricCamera camera = new(new Vector2(1920, 1080));
        camera.Follow(new Vector2(size * 0.5f, size * 0.5f), grid);
        SpritePacket[] sprites = new SpritePacket[size * size * 2];
        float sink = 0;
        return new BenchmarkCase(name, size >= 100 ? 5_000 : 50_000,
            () => { sink += SpriteExtraction.ExtractTiles(grid, camera, null, null, sprites); });
    }

    private static BenchmarkCase ParallelExtractionCase(string name, int size)
    {
        TerrainSurface grid = OpenGrid(size);
        IsometricCamera camera = new(new Vector2(1920, 1080));
        camera.Follow(new Vector2(size * 0.5f, size * 0.5f), grid);
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
            JobHandle barrier = dispatch.Schedule(jobs, grid, camera, null, bands, counts, flickers, bandCount, rowsPerBand);
            jobs.Wait(barrier);
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
