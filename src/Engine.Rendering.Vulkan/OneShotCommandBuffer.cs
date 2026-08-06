using Vortice.Vulkan;

namespace Engine.Rendering.Vulkan;

public unsafe class OneShotCommandBuffer : IDisposable
{
    private readonly VkDeviceApi _deviceApi;
    private readonly VkCommandPool _commandPool;
    private readonly VkQueue _graphicsQueue;
    private readonly List<VkCommandBuffer> _inFlight = new();

    public OneShotCommandBuffer(VkDeviceApi deviceApi, VkCommandPool commandPool, VkQueue graphicsQueue)
    {
        _deviceApi = deviceApi;
        _commandPool = commandPool;
        _graphicsQueue = graphicsQueue;
    }

    public VkCommandBuffer Begin()
    {
        VkCommandBufferAllocateInfo allocInfo = new()
        {
            commandPool = _commandPool,
            level = VkCommandBufferLevel.Primary,
            commandBufferCount = 1
        };
        VkCommandBuffer cmdBuffer;
        _deviceApi.vkAllocateCommandBuffers(&allocInfo, &cmdBuffer);

        VkCommandBufferBeginInfo beginInfo = new() { flags = VkCommandBufferUsageFlags.OneTimeSubmit };
        _deviceApi.vkBeginCommandBuffer(cmdBuffer, &beginInfo);

        _inFlight.Add(cmdBuffer);
        return cmdBuffer;
    }

    public void Submit(VkCommandBuffer cmdBuffer)
    {
        _deviceApi.vkEndCommandBuffer(cmdBuffer);

        VkSubmitInfo submitInfo = new()
        {
            commandBufferCount = 1,
            pCommandBuffers = &cmdBuffer
        };
        _deviceApi.vkQueueSubmit(_graphicsQueue, 1, &submitInfo, VkFence.Null);
        _deviceApi.vkQueueWaitIdle(_graphicsQueue);
        _deviceApi.vkFreeCommandBuffers(_commandPool, 1, &cmdBuffer);
        _inFlight.Remove(cmdBuffer);
    }

    public void Dispose()
    {
        for (int i = _inFlight.Count - 1; i >= 0; i--)
        {
            VkCommandBuffer cmdBuffer = _inFlight[i];
            _deviceApi.vkFreeCommandBuffers(_commandPool, 1, &cmdBuffer);
        }
        _inFlight.Clear();
    }
}
