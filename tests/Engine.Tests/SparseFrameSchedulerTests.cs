using Engine.Ecs.Sparse;
using Engine.Threading;

namespace Engine.Tests;

internal static class SparseFrameSchedulerTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(SerialSystems_RunInRegistrationOrder), SerialSystems_RunInRegistrationOrder),
        new(nameof(CommandBuffer_DeferredDestroyRemovesComponents), CommandBuffer_DeferredDestroyRemovesComponents),
    ];

    private static void SerialSystems_RunInRegistrationOrder()
    {
        using JobSystem jobs = new(1);
        FrameScheduler scheduler = new(jobs);
        List<int> order = new();
        scheduler.Register(new RecordingSystem(order, 1));
        scheduler.Register(new RecordingSystem(order, 2));
        scheduler.Run(new World(), 0.016f);
        TestAssert.True(order.SequenceEqual([1, 2]), "sparse frame systems run deterministically");
    }

    private static void CommandBuffer_DeferredDestroyRemovesComponents()
    {
        World world = new();
        Entity entity = world.Create();
        world.Add(entity, new ValueComponent(7));
        WorldCommandBuffer buffer = new();
        buffer.Destroy(entity);
        TestAssert.True(world.IsAlive(entity), "destroy remains deferred before apply");
        buffer.Apply(world);
        TestAssert.True(!world.IsAlive(entity) && !world.Has<ValueComponent>(entity), "deferred destroy removes the entity and components");
    }

    private struct ValueComponent(int value)
    {
        public int Value = value;
    }

    private sealed class RecordingSystem(List<int> order, int value) : ISystem
    {
        public void Update(World world, float deltaSeconds) => order.Add(value);
    }
}
