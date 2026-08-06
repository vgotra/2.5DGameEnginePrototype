using System.Runtime.InteropServices;
using Vortice.Vulkan;

namespace Engine.Rendering.Vulkan;

internal static unsafe class VulkanDebug
{
    public static bool ValidationLayerAvailable()
    {
        uint count = 0;
        if (global::Vortice.Vulkan.Vulkan.vkEnumerateInstanceLayerProperties(&count, null) != VkResult.Success || count == 0) return false;
        VkLayerProperties[] properties = new VkLayerProperties[count];
        fixed (VkLayerProperties* propertiesPointer = properties)
        {
            if (global::Vortice.Vulkan.Vulkan.vkEnumerateInstanceLayerProperties(&count, propertiesPointer) != VkResult.Success) return false;
            for (int i = 0; i < properties.Length; i++)
            {
                byte* name = propertiesPointer[i].layerName;
                if (NameEquals(name, "VK_LAYER_KHRONOS_validation")) return true;
            }
        }
        return false;
    }

    public static void ConfigureMessenger(ref VkDebugUtilsMessengerCreateInfoEXT messengerInfo)
    {
        messengerInfo.sType = VkStructureType.DebugUtilsMessengerCreateInfoEXT;
        messengerInfo.messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.Warning | VkDebugUtilsMessageSeverityFlagsEXT.Error;
        messengerInfo.messageType = VkDebugUtilsMessageTypeFlagsEXT.General | VkDebugUtilsMessageTypeFlagsEXT.Validation | VkDebugUtilsMessageTypeFlagsEXT.Performance;
        messengerInfo.pfnUserCallback = &ValidationCallback;
    }

    private static bool NameEquals(byte* name, string expected)
    {
        int i = 0;
        foreach (char c in expected)
        {
            if (name[i] != (byte)c) return false;
            i++;
        }
        return name[i] == 0;
    }

    [UnmanagedCallersOnly]
    private static uint ValidationCallback(
        VkDebugUtilsMessageSeverityFlagsEXT severity,
        VkDebugUtilsMessageTypeFlagsEXT type,
        VkDebugUtilsMessengerCallbackDataEXT* data,
        void* userData)
    {
        string message = data == null || data->pMessage == null
            ? "<null>"
            : Marshal.PtrToStringUTF8((nint)data->pMessage) ?? "<unreadable>";
        System.Diagnostics.Debug.WriteLine("[Vulkan validation] " + message);
        return 0;
    }
}
