using Vortice.Vulkan;

namespace Engine.Rendering.Vulkan;

public unsafe class OneShotCommandBuffer(VkDeviceApi deviceApi, VkCommandPool commandPool, VkQueue graphicsQueue) : IDisposable
{
    private readonly List<VkCommandBuffer> _inFlight = new();

    public VkCommandBuffer Begin()
    {
        VkCommandBufferAllocateInfo allocInfo = new()
        {
            commandPool = commandPool,
            level = VkCommandBufferLevel.Primary,
            commandBufferCount = 1
        };
        VkCommandBuffer cmdBuffer;
        deviceApi.vkAllocateCommandBuffers(&allocInfo, &cmdBuffer);

        VkCommandBufferBeginInfo beginInfo = new() { flags = VkCommandBufferUsageFlags.OneTimeSubmit };
        deviceApi.vkBeginCommandBuffer(cmdBuffer, &beginInfo);

        _inFlight.Add(cmdBuffer);
        return cmdBuffer;
    }

    public void Submit(VkCommandBuffer cmdBuffer)
    {
        deviceApi.vkEndCommandBuffer(cmdBuffer);

        VkSubmitInfo submitInfo = new()
        {
            commandBufferCount = 1,
            pCommandBuffers = &cmdBuffer
        };
        deviceApi.vkQueueSubmit(graphicsQueue, 1, &submitInfo, VkFence.Null);
        deviceApi.vkQueueWaitIdle(graphicsQueue);
        deviceApi.vkFreeCommandBuffers(commandPool, 1, &cmdBuffer);
        _inFlight.Remove(cmdBuffer);
    }

    public void Dispose()
    {
        for (int i = _inFlight.Count - 1; i >= 0; i--)
        {
            VkCommandBuffer cmdBuffer = _inFlight[i];
            deviceApi.vkFreeCommandBuffers(commandPool, 1, &cmdBuffer);
        }
        _inFlight.Clear();
    }
}
