using System.Numerics;
using Vortice.Vulkan;

namespace Engine.Rendering.Vulkan;

internal readonly struct FrameRenderContext
{
    internal readonly int FrameSlot;
    internal readonly VkCommandBuffer Primary;
    internal readonly VkRenderPass RenderPass;
    internal readonly VkFramebuffer Framebuffer;
    internal readonly VkExtent2D Extent;
    internal readonly Vector2 Viewport;

    internal FrameRenderContext(
        int frameSlot,
        VkCommandBuffer primary,
        VkRenderPass renderPass,
        VkFramebuffer framebuffer,
        VkExtent2D extent,
        Vector2 viewport)
    {
        FrameSlot = frameSlot;
        Primary = primary;
        RenderPass = renderPass;
        Framebuffer = framebuffer;
        Extent = extent;
        Viewport = viewport;
    }
}
