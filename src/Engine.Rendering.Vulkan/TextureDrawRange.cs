using Engine.Rendering;

namespace Engine.Rendering.Vulkan;

internal readonly record struct TextureDrawRange(TextureHandle Texture, uint FirstIndex, uint IndexCount);
