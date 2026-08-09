using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Engine.Mathematics;

public readonly struct SimdVector2
{
    private readonly Vector128<float> _v;

    public SimdVector2(float x, float y) => _v = Vector64.Create(x, y).ToVector128();

    private SimdVector2(Vector128<float> v) => _v = v;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SimdVector2(Vector2 v) => new(v.X, v.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector2(SimdVector2 v) => new(v._v.GetElement(0), v._v.GetElement(1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SimdVector2 operator +(SimdVector2 a, SimdVector2 b) => new(a._v + b._v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SimdVector2 operator -(SimdVector2 a, SimdVector2 b) => new(a._v - b._v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SimdVector2 operator *(SimdVector2 a, SimdVector2 b) => new(a._v * b._v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SimdVector2 operator *(SimdVector2 v, float s) => new(v._v * s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SimdVector2 operator *(float s, SimdVector2 v) => new(s * v._v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float LengthSquared() => Vector128.Dot(_v, _v);
}
