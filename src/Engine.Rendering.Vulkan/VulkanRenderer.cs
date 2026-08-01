using System.Runtime.InteropServices;
using Vortice.Vulkan;

namespace Engine.Rendering.Vulkan;

public sealed unsafe class VulkanRenderer : IDisposable
{
    private VkInstance _instance;
    private VkInstanceApi _instanceApi = null!;
    private VkSurfaceKHR _surface;
    private VkPhysicalDevice _physicalDevice;
    private VkDevice _device;
    private VkDeviceApi _deviceApi = null!;
    private VkQueue _graphicsQueue;
    private VkSwapchainKHR _swapchain;
    private VkImage[] _swapchainImages = Array.Empty<VkImage>();
    private VkImageView[] _swapchainViews = Array.Empty<VkImageView>();
    private VkCommandPool _commandPool;
    private VkCommandBuffer[] _commandBuffers = Array.Empty<VkCommandBuffer>();
    private VkSemaphore _imageAvailable;
    private VkSemaphore _renderFinished;
    private VkFence _inFlight;
    public uint SwapchainImageCount => (uint)_swapchainImages.Length;

    public VkResult RenderFrame()
    {
        if (_swapchain.IsNull) return VkResult.ErrorInitializationFailed;
        VkResult result = _deviceApi.vkWaitForFences(_inFlight, true, ulong.MaxValue);
        if (result != VkResult.Success) return result;
        _deviceApi.vkResetFences(_inFlight);
        result = _deviceApi.vkAcquireNextImageKHR(_swapchain, ulong.MaxValue, _imageAvailable, VkFence.Null, out uint imageIndex);
        if (result is not (VkResult.Success or VkResult.SuboptimalKHR)) return result;
        VkCommandBuffer commandBuffer = _commandBuffers[imageIndex];
        _deviceApi.vkResetCommandBuffer(commandBuffer, VkCommandBufferResetFlags.None);
        result = _deviceApi.vkBeginCommandBuffer(commandBuffer, VkCommandBufferUsageFlags.OneTimeSubmit);
        if (result != VkResult.Success) return result;
        VkImageSubresourceRange range = new() { aspectMask = VkImageAspectFlags.Color, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1 };
        VkImageMemoryBarrier toTransfer = new() { oldLayout = VkImageLayout.Undefined, newLayout = VkImageLayout.TransferDstOptimal, srcQueueFamilyIndex = uint.MaxValue, dstQueueFamilyIndex = uint.MaxValue, image = _swapchainImages[imageIndex], subresourceRange = range };
        Span<VkMemoryBarrier> noMemory = stackalloc VkMemoryBarrier[0];
        Span<VkBufferMemoryBarrier> noBuffers = stackalloc VkBufferMemoryBarrier[0];
        Span<VkImageMemoryBarrier> transferBarrier = stackalloc VkImageMemoryBarrier[1]; transferBarrier[0] = toTransfer;
        _deviceApi.vkCmdPipelineBarrier(commandBuffer, VkPipelineStageFlags.TopOfPipe, VkPipelineStageFlags.Transfer, VkDependencyFlags.None, noMemory, noBuffers, transferBarrier);
        VkClearColorValue color = new(0.04f, 0.07f, 0.12f, 1f);
        _deviceApi.vkCmdClearColorImage(commandBuffer, _swapchainImages[imageIndex], VkImageLayout.TransferDstOptimal, &color, 1, &range);
        VkImageMemoryBarrier toPresent = new() { oldLayout = VkImageLayout.TransferDstOptimal, newLayout = VkImageLayout.PresentSrcKHR, srcQueueFamilyIndex = uint.MaxValue, dstQueueFamilyIndex = uint.MaxValue, image = _swapchainImages[imageIndex], subresourceRange = range };
        transferBarrier[0] = toPresent;
        _deviceApi.vkCmdPipelineBarrier(commandBuffer, VkPipelineStageFlags.Transfer, VkPipelineStageFlags.BottomOfPipe, VkDependencyFlags.None, noMemory, noBuffers, transferBarrier);
        result = _deviceApi.vkEndCommandBuffer(commandBuffer);
        if (result != VkResult.Success) return result;
        VkSubmitInfo submit = new() { commandBufferCount = 1, waitSemaphoreCount = 1, signalSemaphoreCount = 1 };
        VkPipelineStageFlags waitStage = VkPipelineStageFlags.Transfer;
        VkSemaphore available = _imageAvailable;
        VkSemaphore finished = _renderFinished;
        submit.pCommandBuffers = &commandBuffer;
        submit.pWaitSemaphores = &available;
        submit.pSignalSemaphores = &finished;
        submit.pWaitDstStageMask = &waitStage;
        result = _deviceApi.vkQueueSubmit(_graphicsQueue, submit, _inFlight);
        if (result != VkResult.Success) return result;
        return _deviceApi.vkQueuePresentKHR(_graphicsQueue, _renderFinished, _swapchain, imageIndex);
    }
    public uint GraphicsQueueFamily { get; private set; }
    private nint _appName;
    private nint _engineName;

    public VulkanRenderer(nint windowHandle, nint moduleHandle)
    {
        VkResult loadResult = global::Vortice.Vulkan.Vulkan.vkInitialize("vulkan-1.dll");
        if (loadResult != VkResult.Success) throw new InvalidOperationException($"Vulkan loader initialization failed: {loadResult}");
        _appName = Marshal.StringToCoTaskMemUTF8("IsometricSandbox");
        _engineName = Marshal.StringToCoTaskMemUTF8("2D2.5D Game Engine");
        VkApplicationInfo application = new()
        {
            pApplicationName = (byte*)_appName,
            applicationVersion = global::Vortice.Vulkan.Vulkan.VK_API_VERSION_1_0,
            pEngineName = (byte*)_engineName,
            engineVersion = global::Vortice.Vulkan.Vulkan.VK_API_VERSION_1_0,
            apiVersion = global::Vortice.Vulkan.Vulkan.VK_API_VERSION_1_0
        };
        nint surfaceExtension = Marshal.StringToCoTaskMemUTF8("VK_KHR_surface");
        nint win32Extension = Marshal.StringToCoTaskMemUTF8("VK_KHR_win32_surface");
        nint* extensions = stackalloc nint[2] { surfaceExtension, win32Extension };
        VkInstanceCreateInfo createInfo = new() { pApplicationInfo = &application, enabledExtensionCount = 2, ppEnabledExtensionNames = (byte**)extensions };
        VkResult result = global::Vortice.Vulkan.Vulkan.vkCreateInstance(&createInfo, out _instance);
        if (result != VkResult.Success) throw new InvalidOperationException($"Vulkan instance creation failed: {result}");
        Marshal.FreeCoTaskMem(surfaceExtension);
        Marshal.FreeCoTaskMem(win32Extension);
        _instanceApi = global::Vortice.Vulkan.Vulkan.GetApi(_instance);
        VkWin32SurfaceCreateInfoKHR surfaceInfo = new() { hinstance = moduleHandle, hwnd = windowHandle };
        result = _instanceApi.vkCreateWin32SurfaceKHR(&surfaceInfo, out _surface);
        if (result != VkResult.Success) throw new InvalidOperationException($"Vulkan surface creation failed: {result}");
        Span<VkPhysicalDevice> devices = stackalloc VkPhysicalDevice[8];
        uint deviceCount = (uint)devices.Length;
        result = _instanceApi.vkEnumeratePhysicalDevices(devices);
        if (result != VkResult.Success || deviceCount == 0) throw new InvalidOperationException("No Vulkan physical device found.");
        _physicalDevice = devices[0];
        Span<VkQueueFamilyProperties> families = stackalloc VkQueueFamilyProperties[16];
        uint familyCount = (uint)families.Length;
        _instanceApi.vkGetPhysicalDeviceQueueFamilyProperties(_physicalDevice, families);
        for (uint i = 0; i < familyCount; i++)
        {
            if ((families[(int)i].queueFlags & VkQueueFlags.Graphics) != 0) { GraphicsQueueFamily = i; break; }
        }
        float priority = 1f;
        VkDeviceQueueCreateInfo queueInfo = new() { queueFamilyIndex = GraphicsQueueFamily, queueCount = 1, pQueuePriorities = &priority };
        nint swapchainExtension = Marshal.StringToCoTaskMemUTF8("VK_KHR_swapchain");
        nint* deviceExtensions = stackalloc nint[1] { swapchainExtension };
        VkDeviceCreateInfo deviceInfo = new() { queueCreateInfoCount = 1, pQueueCreateInfos = &queueInfo, enabledExtensionCount = 1, ppEnabledExtensionNames = (byte**)deviceExtensions };
        result = _instanceApi.vkCreateDevice(_physicalDevice, &deviceInfo, out _device);
        Marshal.FreeCoTaskMem(swapchainExtension);
        if (result != VkResult.Success) throw new InvalidOperationException($"Vulkan device creation failed: {result}");
        _deviceApi = global::Vortice.Vulkan.Vulkan.GetApi(_instance, _device);
        _deviceApi.vkGetDeviceQueue(GraphicsQueueFamily, 0, out _graphicsQueue);
        CreateSwapchain(960, 640);
    }

    private void CreateSwapchain(uint width, uint height)
    {
        VkSurfaceCapabilitiesKHR capabilities = default;
        VkResult result = _instanceApi.vkGetPhysicalDeviceSurfaceCapabilitiesKHR(_physicalDevice, _surface, &capabilities);
        if (result != VkResult.Success) throw new InvalidOperationException($"Surface capabilities query failed: {result}");
        Span<VkSurfaceFormatKHR> formats = stackalloc VkSurfaceFormatKHR[16];
        uint formatCount = (uint)formats.Length;
        result = _instanceApi.vkGetPhysicalDeviceSurfaceFormatsKHR(_physicalDevice, _surface, formats);
        if (result != VkResult.Success || formatCount == 0) throw new InvalidOperationException("No Vulkan surface formats found.");
        VkSurfaceFormatKHR format = formats[0];
        Span<VkPresentModeKHR> modes = stackalloc VkPresentModeKHR[16];
        uint modeCount = (uint)modes.Length;
        result = _instanceApi.vkGetPhysicalDeviceSurfacePresentModesKHR(_physicalDevice, _surface, modes);
        if (result != VkResult.Success || modeCount == 0) throw new InvalidOperationException("No Vulkan present modes found.");
        VkExtent2D extent = new(Math.Clamp(width, capabilities.minImageExtent.width, capabilities.maxImageExtent.width), Math.Clamp(height, capabilities.minImageExtent.height, capabilities.maxImageExtent.height));
        uint imageCount = Math.Max(capabilities.minImageCount, 2);
        if (capabilities.maxImageCount > 0) imageCount = Math.Min(imageCount, capabilities.maxImageCount);
        VkSwapchainCreateInfoKHR info = new()
        {
            surface = _surface,
            minImageCount = imageCount,
            imageFormat = format.format,
            imageColorSpace = format.colorSpace,
            imageExtent = extent,
            imageArrayLayers = 1,
            imageUsage = VkImageUsageFlags.ColorAttachment,
            imageSharingMode = VkSharingMode.Exclusive,
            preTransform = capabilities.currentTransform,
            compositeAlpha = VkCompositeAlphaFlagsKHR.Opaque,
            presentMode = modes[0],
            clipped = true
        };
        result = _deviceApi.vkCreateSwapchainKHR(&info, out _swapchain);
        if (result != VkResult.Success) throw new InvalidOperationException($"Swapchain creation failed: {result}");
        uint swapchainImageCount = 0;
        result = _deviceApi.vkGetSwapchainImagesKHR(_swapchain, out swapchainImageCount);
        if (result != VkResult.Success || swapchainImageCount == 0) throw new InvalidOperationException("Swapchain returned no images.");
        VkImage[] images = new VkImage[swapchainImageCount];
        result = _deviceApi.vkGetSwapchainImagesKHR(_swapchain, images);
        if (result != VkResult.Success) throw new InvalidOperationException($"Swapchain image query failed: {result}");
        _swapchainImages = images;
        _swapchainViews = new VkImageView[_swapchainImages.Length];
        for (int i = 0; i < _swapchainImages.Length; i++)
        {
            VkImageViewCreateInfo viewInfo = new()
            {
                image = _swapchainImages[i],
                viewType = VkImageViewType.Image2D,
                format = format.format,
                components = VkComponentMapping.Identity,
                subresourceRange = new VkImageSubresourceRange
                {
                    aspectMask = VkImageAspectFlags.Color,
                    baseMipLevel = 0,
                    levelCount = 1,
                    baseArrayLayer = 0,
                    layerCount = 1
                }
            };
            result = _deviceApi.vkCreateImageView(&viewInfo, out _swapchainViews[i]);
            if (result != VkResult.Success) throw new InvalidOperationException($"Swapchain image view creation failed: {result}");
        }
        VkCommandPoolCreateInfo poolInfo = new() { queueFamilyIndex = GraphicsQueueFamily };
        result = _deviceApi.vkCreateCommandPool(&poolInfo, out _commandPool);
        if (result != VkResult.Success) throw new InvalidOperationException($"Command pool creation failed: {result}");
        _commandBuffers = new VkCommandBuffer[_swapchainImages.Length];
        VkCommandBufferAllocateInfo allocationInfo = new() { commandPool = _commandPool, level = VkCommandBufferLevel.Primary, commandBufferCount = (uint)_commandBuffers.Length };
        fixed (VkCommandBuffer* commandBufferPointer = _commandBuffers)
            result = _deviceApi.vkAllocateCommandBuffers(&allocationInfo, commandBufferPointer);
        if (result != VkResult.Success) throw new InvalidOperationException($"Command buffer allocation failed: {result}");
        VkSemaphoreCreateInfo semaphoreInfo = new();
        result = _deviceApi.vkCreateSemaphore(&semaphoreInfo, out _imageAvailable);
        if (result != VkResult.Success) throw new InvalidOperationException($"Semaphore creation failed: {result}");
        result = _deviceApi.vkCreateSemaphore(&semaphoreInfo, out _renderFinished);
        if (result != VkResult.Success) throw new InvalidOperationException($"Semaphore creation failed: {result}");
        VkFenceCreateInfo fenceInfo = new() { flags = VkFenceCreateFlags.Signaled };
        result = _deviceApi.vkCreateFence(&fenceInfo, out _inFlight);
        if (result != VkResult.Success) throw new InvalidOperationException($"Fence creation failed: {result}");
    }

    public void Dispose()
    {
        if (_device.IsNotNull) _deviceApi.vkDeviceWaitIdle();
        if (_inFlight.IsNotNull) _deviceApi.vkDestroyFence(_inFlight);
        if (_renderFinished.IsNotNull) _deviceApi.vkDestroySemaphore(_renderFinished);
        if (_imageAvailable.IsNotNull) _deviceApi.vkDestroySemaphore(_imageAvailable);
        if (_commandPool.IsNotNull) _deviceApi.vkDestroyCommandPool(_commandPool);
        for (int i = 0; i < _swapchainViews.Length; i++) if (_swapchainViews[i].IsNotNull) _deviceApi.vkDestroyImageView(_swapchainViews[i]);
        if (_swapchain.IsNotNull && _device.IsNotNull) _deviceApi.vkDestroySwapchainKHR(_swapchain);
        if (_device.IsNotNull) _deviceApi.vkDestroyDevice();
        if (_surface.IsNotNull) _instanceApi.vkDestroySurfaceKHR(_surface);
        if (_instance.IsNotNull) _instanceApi.vkDestroyInstance();
        Marshal.FreeCoTaskMem(_appName);
        Marshal.FreeCoTaskMem(_engineName);
        global::Vortice.Vulkan.Vulkan.vkShutdown();
    }
}
