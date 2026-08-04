using System.Numerics;
using Engine.Rendering;
using Engine.Rendering.Vulkan;

namespace Engine.Benchmark.Benchmarks;

internal static class BufferBenchmarks
{
    public static BenchmarkCase[] Create()
    {
        GrowableBuffer<ShapeVertex> buffer = new();
        buffer.EnsureCapacity(4096);
        ShapeVertex vertex = new(new Vector2(1f, 2f), new Vector4(1f, 1f, 1f, 1f));

        return
        [
            new BenchmarkCase("Buffer_AddClear", 200_000,
                () => { buffer.Add(vertex); buffer.Clear(); }),
            new BenchmarkCase("Buffer_Add64Clear", 100_000,
                () =>
                {
                    for (int i = 0; i < 64; i++) buffer.Add(vertex);
                    buffer.Clear();
                }),
        ];
    }
}
