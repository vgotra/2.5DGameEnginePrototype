using System.Numerics;
using Engine.App;
using Engine.Ecs.Sparse;
using SparseWorld = Engine.Ecs.Sparse.World;

namespace Engine.Tests;

internal static class PresentationHistoryTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(StableEntity_InterpolatesPreviousAndCurrent), StableEntity_InterpolatesPreviousAndCurrent),
        new(nameof(NewEntity_UsesCurrentPosition), NewEntity_UsesCurrentPosition),
        new(nameof(GenerationReuse_DoesNotReuseHistory), GenerationReuse_DoesNotReuseHistory),
    ];

    private static void StableEntity_InterpolatesPreviousAndCurrent()
    {
        SparseWorld world = new();
        Entity entity = world.Create();
        world.Add(entity, new Position(new Vector2(0, 0)));
        PresentationPositionHistory history = new();
        history.BeginStep(world);
        world.Get<Position>(entity).Value = new Vector2(10, 0);
        history.EndStep(world);
        TestAssert.True(history.TryGetInterpolated(entity, 0.5, out Vector2 position) && position == new Vector2(5, 0), "presentation history interpolates stable entities");
    }

    private static void NewEntity_UsesCurrentPosition()
    {
        SparseWorld world = new();
        Entity entity = world.Create();
        world.Add(entity, new Position(new Vector2(3, 4)));
        PresentationPositionHistory history = new();
        history.BeginStep(world);
        history.EndStep(world);
        TestAssert.True(history.TryGetInterpolated(entity, 0.5, out Vector2 position) && position == new Vector2(3, 4), "new entities render at current position");
    }

    private static void GenerationReuse_DoesNotReuseHistory()
    {
        SparseWorld world = new();
        Entity first = world.Create();
        world.Add(first, new Position(Vector2.One));
        PresentationPositionHistory history = new();
        history.BeginStep(world);
        history.EndStep(world);
        world.Destroy(first);
        Entity reused = world.Create();
        world.Add(reused, new Position(new Vector2(8, 9)));
        history.BeginStep(world);
        history.EndStep(world);
        TestAssert.True(history.TryGetInterpolated(reused, 0.5, out Vector2 position) && position == new Vector2(8, 9), "generation reuse does not inherit stale presentation state");
    }
}
