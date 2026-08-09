using System.Numerics;
using Engine.App;
using Engine.Mathematics;

namespace Engine.Benchmark.Benchmarks;

internal static class MathBenchmarks
{
    public static BenchmarkCase[] Create()
    {
        ScreenTransform iso = new(400f, 300f, 32f, 16f, -32f, 16f);
        ScreenTransform flat = new(400f, 300f, 64f, 64f, 0f, 0f);
        float sink = 0;

        return
        [
            new BenchmarkCase("Math_ScreenTransformIso", 500_000,
                () => { sink += iso.ToScreen(10.5f, 7.25f).X; }),
            new BenchmarkCase("Math_ScreenTransformFlat", 500_000,
                () => { sink += flat.ToScreen(10.5f, 7.25f).X; }),
            new BenchmarkCase("Math_IsometricWorldToScreen", 500_000,
                () => { sink += IsometricMath.WorldToScreen(new Vector2(10.5f, 7.25f), 64f, 32f).X; }),
        ];
    }
}
