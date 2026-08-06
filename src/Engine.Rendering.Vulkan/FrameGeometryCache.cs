using System.Runtime.CompilerServices;

namespace Engine.Rendering.Vulkan;

public sealed class FrameGeometryCache
{
    private readonly byte[]?[] _vertexSnapshots;
    private readonly byte[]?[] _indexSnapshots;
    private readonly nuint[] _vertexSnapshotSizes;
    private readonly nuint[] _indexSnapshotSizes;

    public FrameGeometryCache(int framesInFlight)
    {
        _vertexSnapshots = new byte[]?[framesInFlight];
        _indexSnapshots = new byte[]?[framesInFlight];
        _vertexSnapshotSizes = new nuint[framesInFlight];
        _indexSnapshotSizes = new nuint[framesInFlight];
    }

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
