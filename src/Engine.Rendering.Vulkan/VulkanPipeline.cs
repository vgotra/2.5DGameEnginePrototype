using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Vulkan;

namespace Engine.Rendering.Vulkan;

public unsafe struct VulkanPipeline : IDisposable
{
    public VkPipeline Pipeline;
    public VkPipelineLayout Layout;
    private VkDevice _device;
    private VkDeviceApi _deviceApi;

    public static VulkanPipeline Create(
        VkDevice device,
        VkDeviceApi deviceApi,
        VkShaderModule vertModule,
        VkShaderModule fragModule,
        VkRenderPass renderPass)
    {
        var pipeline = new VulkanPipeline { _device = device, _deviceApi = deviceApi };

        VkPipelineLayout* layoutPtr = &pipeline.Layout;
        VkPipeline* pipelinePtr = &pipeline.Pipeline;
        CreatePipelineLayout(device, deviceApi, layoutPtr);
        CreateGraphicsPipeline(device, deviceApi, vertModule, fragModule, renderPass, pipeline.Layout, pipelinePtr);

        return pipeline;
    }

    private static void CreatePipelineLayout(VkDevice device, VkDeviceApi api, VkPipelineLayout* layout)
    {
        VkPushConstantRange pushConstant = new()
        {
            stageFlags = VkShaderStageFlags.Vertex,
            offset = 0,
            size = (uint)sizeof(CameraPushConstants)
        };

        VkPipelineLayoutCreateInfo layoutInfo = new()
        {
            pushConstantRangeCount = 1,
            pPushConstantRanges = &pushConstant
        };

        VkResult result = api.vkCreatePipelineLayout(&layoutInfo, null, layout);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"Pipeline layout creation failed: {result}");
    }

    private static void CreateGraphicsPipeline(
        VkDevice device,
        VkDeviceApi api,
        VkShaderModule vertModule,
        VkShaderModule fragModule,
        VkRenderPass renderPass,
        VkPipelineLayout layout,
        VkPipeline* pipeline)
    {
        VkPipelineShaderStageCreateInfo vertStage = new()
        {
            stage = VkShaderStageFlags.Vertex,
            module = vertModule,
            pName = (byte*)Marshal.StringToCoTaskMemUTF8("main")
        };

        VkPipelineShaderStageCreateInfo fragStage = new()
        {
            stage = VkShaderStageFlags.Fragment,
            module = fragModule,
            pName = (byte*)Marshal.StringToCoTaskMemUTF8("main")
        };

        VkPipelineShaderStageCreateInfo[] stages = { vertStage, fragStage };

        VkVertexInputBindingDescription binding = ShapePipelineDescription.Binding;
        VkVertexInputAttributeDescription[] attributes = ShapePipelineDescription.Attributes;

        VkPipelineVertexInputStateCreateInfo vertexInput = new()
        {
            vertexBindingDescriptionCount = 1,
            pVertexBindingDescriptions = &binding,
            vertexAttributeDescriptionCount = (uint)attributes.Length,
            pVertexAttributeDescriptions = (VkVertexInputAttributeDescription*)Unsafe.AsPointer(ref attributes[0])
        };

        VkPipelineInputAssemblyStateCreateInfo inputAssembly = ShapePipelineDescription.InputAssembly;

        VkPipelineViewportStateCreateInfo viewport = new()
        {
            viewportCount = 1,
            scissorCount = 1
        };

        VkPipelineRasterizationStateCreateInfo rasterization = ShapePipelineDescription.Rasterization;

        VkPipelineMultisampleStateCreateInfo multisample = new()
        {
            rasterizationSamples = VkSampleCountFlags.Count1
        };

        VkPipelineColorBlendAttachmentState blendAttachment = PipelineConfiguration.AlphaBlendAttachment;

        VkPipelineColorBlendStateCreateInfo colorBlend = new()
        {
            logicOpEnable = false,
            attachmentCount = 1,
            pAttachments = &blendAttachment
        };

        VkDynamicState[] dynamicStates = { VkDynamicState.Viewport, VkDynamicState.Scissor };
        fixed (VkDynamicState* dynStates = dynamicStates)
        {
            VkPipelineDynamicStateCreateInfo dynamicState = new()
            {
                dynamicStateCount = (uint)dynamicStates.Length,
                pDynamicStates = dynStates
            };

            VkGraphicsPipelineCreateInfo pipelineInfo = new()
            {
                stageCount = 2,
                pStages = (VkPipelineShaderStageCreateInfo*)Unsafe.AsPointer(ref stages[0]),
                pVertexInputState = &vertexInput,
                pInputAssemblyState = &inputAssembly,
                pViewportState = &viewport,
                pRasterizationState = &rasterization,
                pMultisampleState = &multisample,
                pColorBlendState = &colorBlend,
                pDynamicState = &dynamicState,
                layout = layout,
                renderPass = renderPass,
                subpass = 0
            };

            VkResult result = api.vkCreateGraphicsPipelines(VkPipelineCache.Null, 1, &pipelineInfo, null, pipeline);
            if (result != VkResult.Success)
                throw new InvalidOperationException($"Graphics pipeline creation failed: {result}");
        }

        Marshal.FreeCoTaskMem((nint)vertStage.pName);
        Marshal.FreeCoTaskMem((nint)fragStage.pName);
    }

    public void Dispose()
    {
        if (Pipeline.IsNotNull && _device.IsNotNull)
            _deviceApi.vkDestroyPipeline(Pipeline);
        if (Layout.IsNotNull && _device.IsNotNull)
            _deviceApi.vkDestroyPipelineLayout(Layout);
        Pipeline = VkPipeline.Null;
        Layout = VkPipelineLayout.Null;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CameraPushConstants
    {
        public Vector2 Viewport;
    }
}