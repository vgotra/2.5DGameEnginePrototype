using Vortice.Vulkan;

namespace Engine.Rendering.Vulkan;

public static class PipelineConfiguration
{
    public static VkPipelineColorBlendAttachmentState AlphaBlendAttachment => new()
    {
        blendEnable = true,
        srcColorBlendFactor = VkBlendFactor.SrcAlpha,
        dstColorBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
        colorBlendOp = VkBlendOp.Add,
        srcAlphaBlendFactor = VkBlendFactor.One,
        dstAlphaBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
        alphaBlendOp = VkBlendOp.Add,
        colorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A
    };
}
