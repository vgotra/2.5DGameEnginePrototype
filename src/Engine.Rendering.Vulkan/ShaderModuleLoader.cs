using System.Runtime.InteropServices;
using Vortice.Vulkan;

namespace Engine.Rendering.Vulkan;

public sealed unsafe class ShaderModuleLoader(VkDeviceApi device)
{
    private readonly List<VkShaderModule> _modules = new();

    public VkShaderModule Load(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("SPIR-V shader was not found.", path);
        byte[] bytes = File.ReadAllBytes(path);
        if (bytes.Length == 0 || (bytes.Length & 3) != 0) throw new InvalidDataException("SPIR-V size must be a non-zero multiple of four bytes.");
        fixed (byte* data = bytes)
        {
            VkShaderModuleCreateInfo info = new() { codeSize = (nuint)bytes.Length, pCode = (uint*)data };
            VkResult result = device.vkCreateShaderModule(&info, out VkShaderModule module);
            if (result != VkResult.Success) throw new InvalidOperationException($"Shader module creation failed: {result}");
            _modules.Add(module);
            return module;
        }
    }

    public void Dispose()
    {
        for (int i = _modules.Count - 1; i >= 0; i--) device.vkDestroyShaderModule(_modules[i]);
        _modules.Clear();
    }
}
