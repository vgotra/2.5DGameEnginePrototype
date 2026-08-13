using System.Numerics;
using Engine.Ecs.Sparse;

namespace Engine.App;

public sealed class PresentationPositionHistory
{
    private Vector2[] _previous = Array.Empty<Vector2>();
    private Vector2[] _current = Array.Empty<Vector2>();
    private int[] _generations = Array.Empty<int>();
    private bool[] _valid = Array.Empty<bool>();
    private bool[] _initialized = Array.Empty<bool>();

    public void BeginStep(Engine.Ecs.Sparse.World world)
    {
        EnsureCapacity(world.Entities);
        Array.Copy(_current, _previous, _current.Length);
        Array.Clear(_valid);
        CaptureBody body = new(this);
        world.Query<Position>().ForEach(ref body);
    }

    public void EndStep(Engine.Ecs.Sparse.World world)
    {
        EnsureCapacity(world.Entities);
        CaptureBody body = new(this);
        world.Query<Position>().ForEach(ref body);
    }

    public void Reset()
    {
        Array.Clear(_valid);
        Array.Clear(_initialized);
    }

    public bool TryGetInterpolated(Entity entity, double alpha, out Vector2 position)
    {
        if ((uint)entity.Id >= (uint)_valid.Length || !_valid[entity.Id] || _generations[entity.Id] != entity.Generation)
        {
            position = default;
            return false;
        }
        position = Vector2.Lerp(_previous[entity.Id], _current[entity.Id], (float)Math.Clamp(alpha, 0d, 1d));
        return true;
    }

    private void EnsureCapacity(EntityRegistry entities)
    {
        int required = Math.Max(4, entities.AliveCount * 2);
        if (required <= _current.Length) return;
        Array.Resize(ref _previous, required);
        Array.Resize(ref _current, required);
        Array.Resize(ref _generations, required);
        Array.Resize(ref _valid, required);
        Array.Resize(ref _initialized, required);
    }

    private void Capture(Entity entity, Vector2 position)
    {
        if (entity.Id >= _current.Length) return;
        if (!_initialized[entity.Id] || _generations[entity.Id] != entity.Generation)
            _previous[entity.Id] = position;
        _current[entity.Id] = position;
        _generations[entity.Id] = entity.Generation;
        _valid[entity.Id] = true;
        _initialized[entity.Id] = true;
    }

    private struct CaptureBody : IQueryAction<Position, CaptureBody>
    {
        public PresentationPositionHistory History;
        public CaptureBody(PresentationPositionHistory history) => History = history;
        public static void Execute(ref CaptureBody body, Entity entity, ref Position position) => body.History.Capture(entity, position.Value);
    }
}
