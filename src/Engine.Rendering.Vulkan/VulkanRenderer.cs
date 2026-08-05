using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Platform;
using Engine.Rendering;
using Vortice.Vulkan;

namespace Engine.Rendering.Vulkan;

public sealed unsafe class VulkanRenderer : IRenderer
{
    private const int FramesInFlight = 3;

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
    private VkSemaphore[] _imageAvailable = Array.Empty<VkSemaphore>();
    private VkSemaphore[] _renderFinished = Array.Empty<VkSemaphore>();
    private VkFence[] _fences = Array.Empty<VkFence>();
    private VkFence[] _imagesInFlight = Array.Empty<VkFence>();
    private VkPhysicalDeviceMemoryProperties _memoryProperties;
    private uint _imageIndex;
    private int _currentFrame;
    private bool _inFrame;
    private bool _disposed;
    private bool _loaderInitialized;

    private ShaderModuleLoader _shaderLoader = null!;
    private VulkanPipeline _pipeline;
    private DescriptorSetAllocator _descriptorAllocator = null!;
    private TextureUploader _textureUploader = null!;
    private BatchRenderer _batchRenderer = null!;
#if DEBUG
    private VkDebugUtilsMessengerEXT _debugMessenger;
#endif

    public uint SwapchainImageCount => (uint)_swapchainImages.Length;
    public uint GraphicsQueueFamily { get; private set; }

    public VulkanRenderer(in NativeWindowSurface surface)
    {
        try
        {
            Initialize(surface);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private void Initialize(in NativeWindowSurface surface)
    {
        if (surface.Kind != PlatformKind.Win32)
            throw new PlatformNotSupportedException($"Vulkan surface kind '{surface.Kind}' is not implemented yet (see docs/LinuxSupportPlan.md).");

        VkResult loadResult = global::Vortice.Vulkan.Vulkan.vkInitialize(LoaderName());
        if (loadResult != VkResult.Success) throw new InvalidOperationException($"Vulkan loader initialization failed: {loadResult}");
        _loaderInitialized = true;
        ReadOnlySpan<byte> appName = "IsometricSandbox\0"u8;
        ReadOnlySpan<byte> engineName = "2D2.5D Game Engine\0"u8;
        ReadOnlySpan<byte> surfaceExtension = "VK_KHR_surface\0"u8;
        ReadOnlySpan<byte> win32Extension = "VK_KHR_win32_surface\0"u8;
        void* createInfoNext = null;
#if DEBUG
        VkDebugUtilsMessengerCreateInfoEXT messengerInfo = default;
        ReadOnlySpan<byte> debugUtilsExtension = "VK_EXT_debug_utils\0"u8;
        ReadOnlySpan<byte> validationLayer = "VK_LAYER_KHRONOS_validation\0"u8;
#endif
        fixed (byte* appPointer = appName, enginePointer = engineName, surfacePointer = surfaceExtension, win32Pointer = win32Extension)
        {
#if DEBUG
            fixed (byte* debugUtilsPointer = debugUtilsExtension, layerPointer = validationLayer)
#endif
            {
                nint* extensions = stackalloc nint[3];
                uint extensionCount = 2;
                extensions[0] = (nint)surfacePointer;
                extensions[1] = (nint)win32Pointer;
                byte** layerNames = stackalloc byte*[1];
                uint layerCount = 0;
#if DEBUG
                if (ValidationLayerAvailable())
                {
                    extensions[2] = (nint)debugUtilsPointer;
                    extensionCount = 3;
                    layerNames[0] = layerPointer;
                    layerCount = 1;
                    messengerInfo.sType = VkStructureType.DebugUtilsMessengerCreateInfoEXT;
                    messengerInfo.messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.Warning | VkDebugUtilsMessageSeverityFlagsEXT.Error;
                    messengerInfo.messageType = VkDebugUtilsMessageTypeFlagsEXT.General | VkDebugUtilsMessageTypeFlagsEXT.Validation | VkDebugUtilsMessageTypeFlagsEXT.Performance;
                    messengerInfo.pfnUserCallback = &ValidationCallback;
                    createInfoNext = &messengerInfo;
                }
#endif
                VkApplicationInfo application = new()
                {
                    pApplicationName = appPointer,
                    applicationVersion = global::Vortice.Vulkan.Vulkan.VK_API_VERSION_1_0,
                    pEngineName = enginePointer,
                    engineVersion = global::Vortice.Vulkan.Vulkan.VK_API_VERSION_1_0,
                    apiVersion = global::Vortice.Vulkan.Vulkan.VK_API_VERSION_1_0
                };
                VkInstanceCreateInfo createInfo = new()
                {
                    pApplicationInfo = &application,
                    enabledExtensionCount = extensionCount,
                    ppEnabledExtensionNames = (byte**)extensions,
                    enabledLayerCount = layerCount,
                    ppEnabledLayerNames = layerNames,
                    pNext = createInfoNext
                };
                VkResult instanceResult = global::Vortice.Vulkan.Vulkan.vkCreateInstance(&createInfo, out _instance);
                if (instanceResult != VkResult.Success) throw new InvalidOperationException($"Vulkan instance creation failed: {instanceResult}");
            }
        }
        _instanceApi = global::Vortice.Vulkan.Vulkan.GetApi(_instance);
#if DEBUG
        if (createInfoNext != null)
        {
            VkResult messengerResult = _instanceApi.vkCreateDebugUtilsMessengerEXT(&messengerInfo, out _debugMessenger);
            if (messengerResult != VkResult.Success) throw new InvalidOperationException($"Debug messenger creation failed: {messengerResult}");
        }
#endif
        VkWin32SurfaceCreateInfoKHR surfaceInfo = new() { hinstance = surface.ModuleHandle, hwnd = surface.WindowHandle };
        VkResult result = _instanceApi.vkCreateWin32SurfaceKHR(&surfaceInfo, out _surface);
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
        ReadOnlySpan<byte> swapchainExtension = "VK_KHR_swapchain\0"u8;
        fixed (byte* swapchainPointer = swapchainExtension)
        {
            nint* deviceExtensions = stackalloc nint[1] { (nint)swapchainPointer };
            VkDeviceCreateInfo deviceInfo = new() { queueCreateInfoCount = 1, pQueueCreateInfos = &queueInfo, enabledExtensionCount = 1, ppEnabledExtensionNames = (byte**)deviceExtensions };
            result = _instanceApi.vkCreateDevice(_physicalDevice, &deviceInfo, out _device);
            if (result != VkResult.Success) throw new InvalidOperationException($"Vulkan device creation failed: {result}");
        }
        _deviceApi = global::Vortice.Vulkan.Vulkan.GetApi(_instance, _device);
        _deviceApi.vkGetDeviceQueue(GraphicsQueueFamily, 0, out _graphicsQueue);
        CreateSwapchain(800, 600);
        _renderPass = CreateRenderPass();
        CreateFramebuffers();
        CreateCommandResources();
        _shaderLoader = new ShaderModuleLoader(_deviceApi);
        VkShaderModule vertexModule = _shaderLoader.Load(ShaderPath("shape.vert.spv"));
        VkShaderModule fragmentModule = _shaderLoader.Load(ShaderPath("shape.frag.spv"));
        _descriptorAllocator = new DescriptorSetAllocator(_device, _deviceApi);
        VkDescriptorSetLayout textureLayout = _descriptorAllocator.GetLayout(VkDescriptorType.CombinedImageSampler, VkShaderStageFlags.Fragment, 0);
        _pipeline = VulkanPipeline.Create(_device, _deviceApi, vertexModule, fragmentModule, _renderPass, textureLayout);
        _textureUploader = new TextureUploader(_device, _deviceApi, _physicalDevice, _memoryProperties, _graphicsQueue, _commandPool, _descriptorAllocator);
        _batchRenderer = new BatchRenderer(_device, _deviceApi, _physicalDevice, _memoryProperties, _pipeline, _descriptorAllocator, _textureUploader, _graphicsQueue, FramesInFlight);
        _batchRenderer.ResizeBuffers(16 * 1024, 16 * 1024);
    }

    public TextureHandle UploadTexture(ReadOnlySpan<byte> rgba, int width, int height)
        => _textureUploader.UploadTexture(rgba, width, height);

    public void BeginFrame(Vector2 viewport)
    {
        if (_swapchain.IsNull) throw new InvalidOperationException("Swapchain is not ready.");
        _currentFrame = (_currentFrame + 1) % FramesInFlight;

        VkFence slotFence = _fences[_currentFrame];
        VkResult result = _deviceApi.vkWaitForFences(slotFence, true, ulong.MaxValue);
        if (result != VkResult.Success) throw new InvalidOperationException($"Fence wait failed: {result}");
        _deviceApi.vkResetFences(slotFence);

        result = _deviceApi.vkAcquireNextImageKHR(_swapchain, ulong.MaxValue, _imageAvailable[_currentFrame], VkFence.Null, out _imageIndex);
        if (result == VkResult.ErrorOutOfDateKHR)
        {
            Resize((int)viewport.X, (int)viewport.Y);
            slotFence = _fences[_currentFrame];
            result = _deviceApi.vkWaitForFences(slotFence, true, ulong.MaxValue);
            if (result != VkResult.Success) throw new InvalidOperationException($"Fence wait failed: {result}");
            _deviceApi.vkResetFences(slotFence);
            result = _deviceApi.vkAcquireNextImageKHR(_swapchain, ulong.MaxValue, _imageAvailable[_currentFrame], VkFence.Null, out _imageIndex);
        }
        if (result is not (VkResult.Success or VkResult.SuboptimalKHR)) throw new InvalidOperationException($"Image acquire failed: {result}");

        VkFence imageFence = _imagesInFlight[_imageIndex];
        if (imageFence.IsNotNull && imageFence != slotFence)
        {
            result = _deviceApi.vkWaitForFences(imageFence, true, ulong.MaxValue);
            if (result != VkResult.Success) throw new InvalidOperationException($"Image fence wait failed: {result}");
        }
        _imagesInFlight[_imageIndex] = slotFence;

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
        _batchRenderer.BeginFrame(_currentFrame, commandBuffer, viewport);
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
        VkSemaphore imageAvailable = _imageAvailable[_currentFrame];
        VkSemaphore renderFinished = _renderFinished[_currentFrame];
        VkFence slotFence = _fences[_currentFrame];
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
        result = _deviceApi.vkQueueSubmit(_graphicsQueue, submit, slotFence);
        if (result != VkResult.Success) throw new InvalidOperationException($"Queue submit failed: {result}");
        result = _deviceApi.vkQueuePresentKHR(_graphicsQueue, renderFinished, _swapchain, _imageIndex);
        if (result is not (VkResult.Success or VkResult.SuboptimalKHR or VkResult.ErrorOutOfDateKHR)) throw new InvalidOperationException($"Present failed: {result}");
        _inFrame = false;
    }

    private static string ShaderPath(string name) => Path.Combine(AppContext.BaseDirectory, "shaders", name);

    private static string LoaderName() => OperatingSystem.IsWindows() ? "vulkan-1.dll" : "libvulkan.so.1";

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

    public void Resize(int width, int height)
    {
        if ((uint)width == _swapchainExtent.width && (uint)height == _swapchainExtent.height) return;
        _deviceApi.vkDeviceWaitIdle();
        for (int i = 0; i < _framebuffers.Length; i++) if (_framebuffers[i].IsNotNull) _deviceApi.vkDestroyFramebuffer(_framebuffers[i]);
        for (int i = 0; i < _swapchainViews.Length; i++) if (_swapchainViews[i].IsNotNull) _deviceApi.vkDestroyImageView(_swapchainViews[i]);
        if (_swapchain.IsNotNull) _deviceApi.vkDestroySwapchainKHR(_swapchain);
        DestroyCommandResources();
        CreateSwapchain((uint)width, (uint)height);
        CreateFramebuffers();
        CreateCommandResources();
        _imageIndex = uint.MaxValue;
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
        VkPresentModeKHR presentMode = VkPresentModeKHR.Fifo;
        for (int i = 0; i < modeCount; i++)
        {
            if (modes[i] == VkPresentModeKHR.Mailbox) { presentMode = VkPresentModeKHR.Mailbox; break; }
        }

        uint imageCount = Math.Max(capabilities.minImageCount, 3);
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
            presentMode = presentMode,
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
    }

    private void CreateCommandResources()
    {
        VkResult result;
        VkCommandPoolCreateInfo poolInfo = new() { queueFamilyIndex = GraphicsQueueFamily };
        result = _deviceApi.vkCreateCommandPool(&poolInfo, out _commandPool);
        if (result != VkResult.Success) throw new InvalidOperationException($"Command pool creation failed: {result}");
        _commandBuffers = new VkCommandBuffer[_swapchainImages.Length];
        VkCommandBufferAllocateInfo allocationInfo = new() { commandPool = _commandPool, level = VkCommandBufferLevel.Primary, commandBufferCount = (uint)_commandBuffers.Length };
        fixed (VkCommandBuffer* commandBufferPointer = _commandBuffers)
            result = _deviceApi.vkAllocateCommandBuffers(&allocationInfo, commandBufferPointer);
        if (result != VkResult.Success) throw new InvalidOperationException($"Command buffer allocation failed: {result}");
        _imageAvailable = new VkSemaphore[FramesInFlight];
        _renderFinished = new VkSemaphore[FramesInFlight];
        _fences = new VkFence[FramesInFlight];
        VkSemaphoreCreateInfo semaphoreInfo = new();
        VkFenceCreateInfo fenceInfo = new() { flags = VkFenceCreateFlags.Signaled };
        for (int i = 0; i < FramesInFlight; i++)
        {
            result = _deviceApi.vkCreateSemaphore(&semaphoreInfo, out _imageAvailable[i]);
            if (result != VkResult.Success) throw new InvalidOperationException($"Semaphore creation failed: {result}");
            result = _deviceApi.vkCreateSemaphore(&semaphoreInfo, out _renderFinished[i]);
            if (result != VkResult.Success) throw new InvalidOperationException($"Semaphore creation failed: {result}");
            result = _deviceApi.vkCreateFence(&fenceInfo, out _fences[i]);
            if (result != VkResult.Success) throw new InvalidOperationException($"Fence creation failed: {result}");
        }
        _imagesInFlight = new VkFence[_swapchainImages.Length];
        Array.Fill(_imagesInFlight, VkFence.Null);
    }

    private void DestroyCommandResources()
    {
        if (_commandPool.IsNull) return;
        if (_commandBuffers.Length > 0)
        {
            fixed (VkCommandBuffer* commandBufferPointer = _commandBuffers)
                _deviceApi.vkFreeCommandBuffers(_commandPool, (uint)_commandBuffers.Length, commandBufferPointer);
            _commandBuffers = Array.Empty<VkCommandBuffer>();
        }
        for (int i = 0; i < _fences.Length; i++) if (_fences[i].IsNotNull) _deviceApi.vkDestroyFence(_fences[i]);
        for (int i = 0; i < _renderFinished.Length; i++) if (_renderFinished[i].IsNotNull) _deviceApi.vkDestroySemaphore(_renderFinished[i]);
        for (int i = 0; i < _imageAvailable.Length; i++) if (_imageAvailable[i].IsNotNull) _deviceApi.vkDestroySemaphore(_imageAvailable[i]);
        _fences = Array.Empty<VkFence>();
        _renderFinished = Array.Empty<VkSemaphore>();
        _imageAvailable = Array.Empty<VkSemaphore>();
        _imagesInFlight = Array.Empty<VkFence>();
        _deviceApi.vkDestroyCommandPool(_commandPool);
        _commandPool = VkCommandPool.Null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_device.IsNotNull && _deviceApi != null) _deviceApi.vkDeviceWaitIdle();
        _batchRenderer?.Dispose();
        _textureUploader?.Dispose();
        _descriptorAllocator?.Dispose();
        if (_pipeline.Pipeline.IsNotNull) _pipeline.Dispose();
        _shaderLoader?.Dispose();
        if (_deviceApi != null)
        {
            for (int i = 0; i < _framebuffers.Length; i++) if (_framebuffers[i].IsNotNull) _deviceApi.vkDestroyFramebuffer(_framebuffers[i]);
            if (_renderPass.IsNotNull) _deviceApi.vkDestroyRenderPass(_renderPass);
            DestroyCommandResources();
            for (int i = 0; i < _swapchainViews.Length; i++) if (_swapchainViews[i].IsNotNull) _deviceApi.vkDestroyImageView(_swapchainViews[i]);
            if (_swapchain.IsNotNull && _device.IsNotNull) _deviceApi.vkDestroySwapchainKHR(_swapchain);
            if (_device.IsNotNull) _deviceApi.vkDestroyDevice();
        }
        if (_surface.IsNotNull) _instanceApi.vkDestroySurfaceKHR(_surface);
#if DEBUG
        if (_debugMessenger.IsNotNull) _instanceApi.vkDestroyDebugUtilsMessengerEXT(_debugMessenger);
#endif
        if (_instance.IsNotNull) _instanceApi.vkDestroyInstance();
        if (_loaderInitialized) global::Vortice.Vulkan.Vulkan.vkShutdown();
    }

#if DEBUG
    private static bool ValidationLayerAvailable()
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
#endif
}
