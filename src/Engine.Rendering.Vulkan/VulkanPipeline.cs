using System.Numerics;
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
        // "main\0" pinned in-place: no per-call CoTaskMem allocation, so nothing leaks if
        // vkCreateGraphicsPipelines throws mid-construction.
        ReadOnlySpan<byte> entryPoint = "main\0"u8;
        fixed (byte* entryPointPointer = entryPoint)
        {
            VkPipelineShaderStageCreateInfo vertStage = new()
            {
                stage = VkShaderStageFlags.Vertex,
                module = vertModule,
                pName = entryPointPointer
            };

            VkPipelineShaderStageCreateInfo fragStage = new()
            {
                stage = VkShaderStageFlags.Fragment,
                module = fragModule,
                pName = entryPointPointer
            };

            VkPipelineShaderStageCreateInfo[] stages = { vertStage, fragStage };

            VkVertexInputBindingDescription binding = ShapePipelineDescription.Binding;
            VkVertexInputAttributeDescription[] attributes = ShapePipelineDescription.Attributes;

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

            // All managed arrays referenced by the create-info graph are pinned for the duration
            // of vkCreateGraphicsPipelines. Taking Unsafe.AsPointer on an unpinned managed array
            // is a GC-relocation hazard: a background GC can move the array between pointer capture
            // and the native call reading it, which would silently corrupt the pipeline description.
            fixed (VkPipelineShaderStageCreateInfo* stagePtr = stages)
            fixed (VkVertexInputAttributeDescription* attributePtr = attributes)
            fixed (VkDynamicState* dynStates = dynamicStates)
            {
                VkPipelineVertexInputStateCreateInfo vertexInput = new()
                {
                    vertexBindingDescriptionCount = 1,
                    pVertexBindingDescriptions = &binding,
                    vertexAttributeDescriptionCount = (uint)attributes.Length,
                    pVertexAttributeDescriptions = attributePtr
                };

                VkPipelineDynamicStateCreateInfo dynamicState = new()
                {
                    dynamicStateCount = (uint)dynamicStates.Length,
                    pDynamicStates = dynStates
                };

                VkGraphicsPipelineCreateInfo pipelineInfo = new()
                {
                    stageCount = 2,
                    pStages = stagePtr,
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
        }
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