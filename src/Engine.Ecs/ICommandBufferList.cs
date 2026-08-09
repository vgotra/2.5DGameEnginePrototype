using Engine.Core;

namespace Engine.Ecs;

internal interface ICommandBufferList
{
    void Apply(World world);
    void Clear();
}
