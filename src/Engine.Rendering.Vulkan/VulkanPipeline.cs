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
        VkRenderPass renderPass,
        VkDescriptorSetLayout textureLayout)
    {
        var pipeline = new VulkanPipeline { _device = device, _deviceApi = deviceApi };

        VkPipelineLayout* layoutPtr = &pipeline.Layout;
        VkPipeline* pipelinePtr = &pipeline.Pipeline;
        CreatePipelineLayout(device, deviceApi, textureLayout, layoutPtr);
        CreateGraphicsPipeline(device, deviceApi, vertModule, fragModule, renderPass, pipeline.Layout, pipelinePtr);

        return pipeline;
    }

    private static void CreatePipelineLayout(VkDevice device, VkDeviceApi api, VkDescriptorSetLayout textureLayout, VkPipelineLayout* layout)
    {
        VkPushConstantRange pushConstant = new()
        {
            stageFlags = VkShaderStageFlags.Vertex,
            offset = 0,
            size = (uint)sizeof(CameraPushConstants)
        };

        VkPipelineLayoutCreateInfo layoutInfo = new()
        {
            setLayoutCount = 1,
            pSetLayouts = &textureLayout,
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
        ReadOnlySpan<byte> entryPoint = "main\0"u8;
        fixed (byte* entryPointPointer = entryPoint)
        {
            VkPipelineShaderStageCreateInfo[] stages = ShaderStages(vertModule, fragModule, entryPointPointer);

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

            VkDynamicState[] dynamicStates = DynamicStates();

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

    private static VkPipelineShaderStageCreateInfo[] ShaderStages(VkShaderModule vertModule, VkShaderModule fragModule, byte* entryPoint)
    {
        VkPipelineShaderStageCreateInfo vertStage = new()
        {
            stage = VkShaderStageFlags.Vertex,
            module = vertModule,
            pName = entryPoint
        };

        VkPipelineShaderStageCreateInfo fragStage = new()
        {
            stage = VkShaderStageFlags.Fragment,
            module = fragModule,
            pName = entryPoint
        };

        return [vertStage, fragStage];
    }

    private static VkDynamicState[] DynamicStates() => [VkDynamicState.Viewport, VkDynamicState.Scissor];

    public void Dispose()
    {
        if (Pipeline.IsNotNull && _device.IsNotNull)
            _deviceApi.vkDestroyPipeline(Pipeline);
        if (Layout.IsNotNull && _device.IsNotNull)
            _deviceApi.vkDestroyPipelineLayout(Layout);
        Pipeline = VkPipeline.Null;
        Layout = VkPipelineLayout.Null;
    }
}
