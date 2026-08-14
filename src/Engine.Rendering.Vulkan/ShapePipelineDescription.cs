using System.Runtime.CompilerServices;
using Vortice.Vulkan;

namespace Engine.Rendering.Vulkan;

public static class ShapePipelineDescription
{
    public static VkVertexInputBindingDescription[] Bindings =>
    [
        new VkVertexInputBindingDescription { binding = 0, stride = 0, inputRate = VkVertexInputRate.Vertex },
        new VkVertexInputBindingDescription { binding = 1, stride = (uint)Unsafe.SizeOf<SpriteInstance>(), inputRate = VkVertexInputRate.Instance }
    ];

    public static VkVertexInputAttributeDescription[] Attributes => new[]
    {
        new VkVertexInputAttributeDescription { location = 0, binding = 1, format = VkFormat.R32G32Sfloat, offset = 0 },
        new VkVertexInputAttributeDescription { location = 1, binding = 1, format = VkFormat.R32G32Sfloat, offset = 8 },
        new VkVertexInputAttributeDescription { location = 2, binding = 1, format = VkFormat.R32G32B32A32Sfloat, offset = 16 },
        new VkVertexInputAttributeDescription { location = 3, binding = 1, format = VkFormat.R32G32Sfloat, offset = 32 },
        new VkVertexInputAttributeDescription { location = 4, binding = 1, format = VkFormat.R32G32Sfloat, offset = 40 },
        new VkVertexInputAttributeDescription { location = 5, binding = 1, format = VkFormat.R32Uint, offset = 48 }
    };

    public static VkPipelineInputAssemblyStateCreateInfo InputAssembly => new() { topology = VkPrimitiveTopology.TriangleList, primitiveRestartEnable = false };

    public static VkPipelineRasterizationStateCreateInfo Rasterization => new()
    {
        polygonMode = VkPolygonMode.Fill,
        cullMode = VkCullModeFlags.None,
        frontFace = VkFrontFace.CounterClockwise,
        lineWidth = 1f
    };
}
