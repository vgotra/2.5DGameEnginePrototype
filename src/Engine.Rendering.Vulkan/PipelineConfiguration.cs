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

    public static VkPipelineColorBlendAttachmentState AdditiveBlendAttachment => new()
    {
        blendEnable = true,
        srcColorBlendFactor = VkBlendFactor.One,
        dstColorBlendFactor = VkBlendFactor.One,
        colorBlendOp = VkBlendOp.Add,
        srcAlphaBlendFactor = VkBlendFactor.One,
        dstAlphaBlendFactor = VkBlendFactor.One,
        alphaBlendOp = VkBlendOp.Add,
        colorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A
    };
}
