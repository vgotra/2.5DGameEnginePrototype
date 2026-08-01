using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Rendering;
using Vortice.Vulkan;

namespace Engine.Rendering.Vulkan;

public sealed unsafe class VulkanRenderer : IRenderer
{
    private VkInstance _instance;
    private VkInstanceApi _instanceApi = null!;
    private VkSurfaceKHR _surface;
    private VkPhysicalDevice _physicalDevice;
    private VkDevice _device;
    private VkDeviceApi _deviceApi = null!;
    private VkQueue _graphicsQueue;
    private VkSwapchainKHR _swapchain;
    private VkFormat _swapchainFormat;
    private VkExtent2D _swapchainExtent;
    private VkImage[] _swapchainImages = Array.Empty<VkImage>();
    private VkImageView[] _swapchainViews = Array.Empty<VkImageView>();
    private VkFramebuffer[] _framebuffers = Array.Empty<VkFramebuffer>();
    private VkRenderPass _renderPass;
    private VkCommandPool _commandPool;
    private VkCommandBuffer[] _commandBuffers = Array.Empty<VkCommandBuffer>();
    private VkSemaphore _imageAvailable;
    private VkSemaphore _renderFinished;
    private VkFence _inFlight;
    private VkPhysicalDeviceMemoryProperties _memoryProperties;
    private uint _imageIndex;
    private bool _inFrame;

    private ShaderModuleLoader _shaderLoader = null!;
    private VulkanPipeline _pipeline;
    private DescriptorSetAllocator _descriptorAllocator = null!;
    private TextureUploader _textureUploader = null!;
    private BatchRenderer _batchRenderer = null!;

    public uint SwapchainImageCount => (uint)_swapchainImages.Length;
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
        VkPhysicalDeviceMemoryProperties memoryProperties;
        _instanceApi.vkGetPhysicalDeviceMemoryProperties(_physicalDevice, &memoryProperties);
        _memoryProperties = memoryProperties;
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
        _renderPass = CreateRenderPass();
        CreateFramebuffers();
        _shaderLoader = new ShaderModuleLoader(_deviceApi);
        VkShaderModule vertexModule = _shaderLoader.Load(ShaderPath("shape.vert.spv"));
        VkShaderModule fragmentModule = _shaderLoader.Load(ShaderPath("shape.frag.spv"));
        _pipeline = VulkanPipeline.Create(_device, _deviceApi, vertexModule, fragmentModule, _renderPass);
        _descriptorAllocator = new DescriptorSetAllocator(_device, _deviceApi);
        _textureUploader = new TextureUploader(_device, _deviceApi, _physicalDevice, _memoryProperties, _graphicsQueue, _commandPool, _descriptorAllocator);
        _batchRenderer = new BatchRenderer(_device, _deviceApi, _physicalDevice, _memoryProperties, _pipeline, _descriptorAllocator, _graphicsQueue, _commandPool, 1);
        _batchRenderer.ResizeBuffers(16 * 1024, 16 * 1024);
    }

    public void BeginFrame(Vector2 viewport)
    {
        if (_swapchain.IsNull) throw new InvalidOperationException("Swapchain is not ready.");
        VkResult result = _deviceApi.vkWaitForFences(_inFlight, true, ulong.MaxValue);
        if (result != VkResult.Success) throw new InvalidOperationException($"Fence wait failed: {result}");
        _deviceApi.vkResetFences(_inFlight);
        result = _deviceApi.vkAcquireNextImageKHR(_swapchain, ulong.MaxValue, _imageAvailable, VkFence.Null, out _imageIndex);
        if (result is not (VkResult.Success or VkResult.SuboptimalKHR)) throw new InvalidOperationException($"Image acquire failed: {result}");
        VkCommandBuffer commandBuffer = _commandBuffers[_imageIndex];
        _deviceApi.vkResetCommandBuffer(commandBuffer, VkCommandBufferResetFlags.None);
        VkCommandBufferBeginInfo beginInfo = new() { flags = VkCommandBufferUsageFlags.OneTimeSubmit };
        result = _deviceApi.vkBeginCommandBuffer(commandBuffer, &beginInfo);
        if (result != VkResult.Success) throw new InvalidOperationException($"Command buffer begin failed: {result}");
        VkClearValue clear = new(0.04f, 0.07f, 0.12f, 1f);
        VkRenderPassBeginInfo renderPassBegin = new()
        {
            renderPass = _renderPass,
            framebuffer = _framebuffers[_imageIndex],
            renderArea = new VkRect2D(0, 0, _swapchainExtent.width, _swapchainExtent.height),
            clearValueCount = 1,
            pClearValues = &clear
        };
        _deviceApi.vkCmdBeginRenderPass(commandBuffer, &renderPassBegin, VkSubpassContents.Inline);
        _batchRenderer.BeginFrame(commandBuffer, viewport);
        _inFrame = true;
    }

    public void Submit(ReadOnlySpan<SpritePacket> sprites)
    {
        if (!_inFrame) return;
        _batchRenderer.Submit(sprites);
    }

    public void EndFrame()
    {
        if (!_inFrame) return;
        VkCommandBuffer commandBuffer = _commandBuffers[_imageIndex];
        _batchRenderer.EndFrame(commandBuffer);
        _deviceApi.vkCmdEndRenderPass(commandBuffer);
        VkResult result = _deviceApi.vkEndCommandBuffer(commandBuffer);
        if (result != VkResult.Success) throw new InvalidOperationException($"Command buffer end failed: {result}");
        VkPipelineStageFlags waitStage = VkPipelineStageFlags.ColorAttachmentOutput;
        VkSemaphore imageAvailable = _imageAvailable;
        VkSemaphore renderFinished = _renderFinished;
        VkSubmitInfo submit = new()
        {
            commandBufferCount = 1,
            pCommandBuffers = &commandBuffer,
            waitSemaphoreCount = 1,
            pWaitSemaphores = &imageAvailable,
            pWaitDstStageMask = &waitStage,
            signalSemaphoreCount = 1,
            pSignalSemaphores = &renderFinished
        };
        result = _deviceApi.vkQueueSubmit(_graphicsQueue, submit, _inFlight);
        if (result != VkResult.Success) throw new InvalidOperationException($"Queue submit failed: {result}");
        result = _deviceApi.vkQueuePresentKHR(_graphicsQueue, _renderFinished, _swapchain, _imageIndex);
        if (result is not (VkResult.Success or VkResult.SuboptimalKHR)) throw new InvalidOperationException($"Present failed: {result}");
        _inFrame = false;
    }

    private static string ShaderPath(string name) => Path.Combine(AppContext.BaseDirectory, "shaders", name);

    private VkRenderPass CreateRenderPass()
    {
        VkAttachmentDescription colorAttachment = new()
        {
            format = _swapchainFormat,
            samples = VkSampleCountFlags.Count1,
            loadOp = VkAttachmentLoadOp.Clear,
            storeOp = VkAttachmentStoreOp.Store,
            stencilLoadOp = VkAttachmentLoadOp.DontCare,
            stencilStoreOp = VkAttachmentStoreOp.DontCare,
            initialLayout = VkImageLayout.Undefined,
            finalLayout = VkImageLayout.PresentSrcKHR
        };

        VkAttachmentReference colorReference = new(0, VkImageLayout.ColorAttachmentOptimal);

        VkSubpassDescription subpass = new()
        {
            pipelineBindPoint = VkPipelineBindPoint.Graphics,
            colorAttachmentCount = 1,
            pColorAttachments = &colorReference
        };

        VkRenderPassCreateInfo info = new()
        {
            attachmentCount = 1,
            pAttachments = &colorAttachment,
            subpassCount = 1,
            pSubpasses = &subpass
        };

        VkResult result = _deviceApi.vkCreateRenderPass(&info, out VkRenderPass renderPass);
        if (result != VkResult.Success) throw new InvalidOperationException($"Render pass creation failed: {result}");
        return renderPass;
    }

    private void CreateFramebuffers()
    {
        _framebuffers = new VkFramebuffer[_swapchainViews.Length];
        for (int i = 0; i < _swapchainViews.Length; i++)
        {
            VkImageView attachment = _swapchainViews[i];
            VkFramebufferCreateInfo info = new()
            {
                renderPass = _renderPass,
                attachmentCount = 1,
                pAttachments = &attachment,
                width = _swapchainExtent.width,
                height = _swapchainExtent.height,
                layers = 1
            };
            VkResult result = _deviceApi.vkCreateFramebuffer(&info, out _framebuffers[i]);
            if (result != VkResult.Success) throw new InvalidOperationException($"Framebuffer creation failed: {result}");
        }
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
        _swapchainFormat = format.format;
        _swapchainExtent = extent;
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
        _batchRenderer?.Dispose();
        _textureUploader?.Dispose();
        _descriptorAllocator?.Dispose();
        if (_pipeline.Pipeline.IsNotNull) _pipeline.Dispose();
        _shaderLoader?.Dispose();
        for (int i = 0; i < _framebuffers.Length; i++) if (_framebuffers[i].IsNotNull) _deviceApi.vkDestroyFramebuffer(_framebuffers[i]);
        if (_renderPass.IsNotNull) _deviceApi.vkDestroyRenderPass(_renderPass);
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
