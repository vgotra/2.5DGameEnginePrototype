using System.Runtime.CompilerServices;
using Engine.Rendering;
using Vortice.Vulkan;

namespace Engine.Rendering.Vulkan;

public static class ShapePipelineDescription
{
    public static VkVertexInputBindingDescription Binding => new() { binding = 0, stride = (uint)Unsafe.SizeOf<ShapeVertex>(), inputRate = VkVertexInputRate.Vertex };

    public static VkVertexInputAttributeDescription[] Attributes => new[]
    {
        new VkVertexInputAttributeDescription { location = 0, binding = 0, format = VkFormat.R32G32Sfloat, offset = 0 },
        new VkVertexInputAttributeDescription { location = 1, binding = 0, format = VkFormat.R32G32B32A32Sfloat, offset = 8 },
        new VkVertexInputAttributeDescription { location = 2, binding = 0, format = VkFormat.R32G32Sfloat, offset = 24 }
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
