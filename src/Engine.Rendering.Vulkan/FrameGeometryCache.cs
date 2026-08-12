using System.Runtime.CompilerServices;

namespace Engine.Rendering.Vulkan;

public sealed class FrameGeometryCache(int framesInFlight)
{
    private readonly byte[]?[] _vertexSnapshots = new byte[]?[framesInFlight];
    private readonly byte[]?[] _indexSnapshots = new byte[]?[framesInFlight];
    private readonly nuint[] _vertexSnapshotSizes = new nuint[framesInFlight];
    private readonly nuint[] _indexSnapshotSizes = new nuint[framesInFlight];

    public bool HasGeometryChanged(int frameIndex, nuint vertexBytes, nuint indexBytes, ReadOnlySpan<byte> vertexData, ReadOnlySpan<byte> indexData)
    {
        byte[]? vertexSnapshot = _vertexSnapshots[frameIndex];
        byte[]? indexSnapshot = _indexSnapshots[frameIndex];
        if (vertexSnapshot is null || indexSnapshot is null ||
            vertexBytes != _vertexSnapshotSizes[frameIndex] || indexBytes != _indexSnapshotSizes[frameIndex])
        {
            return true;
        }
        return !vertexData.SequenceEqual(vertexSnapshot.AsSpan(0, (int)vertexBytes)) ||
               !indexData.SequenceEqual(indexSnapshot.AsSpan(0, (int)indexBytes));
    }

    public void StoreGeometry(int frameIndex, nuint vertexBytes, nuint indexBytes, ReadOnlySpan<byte> vertexData, ReadOnlySpan<byte> indexData)
    {
        byte[]? vertexSnapshot = _vertexSnapshots[frameIndex];
        byte[]? indexSnapshot = _indexSnapshots[frameIndex];
        if (vertexSnapshot?.Length != (int)vertexBytes) _vertexSnapshots[frameIndex] = new byte[(int)vertexBytes];
        if (indexSnapshot?.Length != (int)indexBytes) _indexSnapshots[frameIndex] = new byte[(int)indexBytes];
        vertexData.CopyTo(_vertexSnapshots[frameIndex]!);
        indexData.CopyTo(_indexSnapshots[frameIndex]!);
        _vertexSnapshotSizes[frameIndex] = vertexBytes;
        _indexSnapshotSizes[frameIndex] = indexBytes;
    }
}
