using Engine.Rendering;

namespace Engine.Rendering.Vulkan;

internal readonly record struct TextureDrawRange(TextureHandle Texture, MaterialHandle Material, BlendMode Blend, uint FirstIndex, uint IndexCount);
