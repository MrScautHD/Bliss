using System.Runtime.CompilerServices;
using Bliss.CSharp.Geometry;
using Bliss.CSharp.Shaders;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using BlendFactor = Silk.NET.Vulkan.BlendFactor;
using FrontFace = Silk.NET.Vulkan.FrontFace;
using Pipeline = Silk.NET.Vulkan.Pipeline;
using PrimitiveTopology = Silk.NET.Vulkan.PrimitiveTopology;

namespace Bliss.CSharp.Vulkan;

public class BlissPipeline : Disposable {
    
    public readonly Vk Vk;
    public readonly BlissDevice Device;

    private Pipeline _graphicsPipeline;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BlissPipeline"/> class with the specified Vulkan instance, device, shader, and pipeline configuration.
    /// </summary>
    /// <param name="vk">The Vulkan instance.</param>
    /// <param name="device">The Bliss device.</param>
    /// <param name="shader">The shader program to be used for the pipeline.</param>
    /// <param name="configInfo">The configuration information for the pipeline.</param>
    public BlissPipeline(Vk vk, BlissDevice device, Shader shader, PipelineConfigInfo configInfo) {
        this.Vk = vk;
        this.Device = device;
        this.CreateGraphicsPipeline(shader, configInfo);
    }

    /// <summary>
    /// Retrieves the default pipeline configuration information.
    /// </summary>
    /// <param name="configInfo">The pipeline configuration information.</param>
    public static unsafe void GetDefaultPipelineConfigInfo(ref PipelineConfigInfo configInfo) {
        configInfo.InputAssemblyInfo.SType = StructureType.PipelineInputAssemblyStateCreateInfo;
        configInfo.InputAssemblyInfo.Topology = PrimitiveTopology.TriangleList;
        configInfo.InputAssemblyInfo.PrimitiveRestartEnable = Vk.False;

        configInfo.ViewportInfo.SType = StructureType.PipelineViewportStateCreateInfo;
        configInfo.ViewportInfo.ViewportCount = 1;
        configInfo.ViewportInfo.PViewports = default;
        configInfo.ViewportInfo.ScissorCount = 1;
        configInfo.ViewportInfo.PScissors = default;

        configInfo.RasterizationInfo.SType = StructureType.PipelineRasterizationStateCreateInfo;
        configInfo.RasterizationInfo.DepthClampEnable = Vk.False;
        configInfo.RasterizationInfo.RasterizerDiscardEnable = Vk.False;
        configInfo.RasterizationInfo.PolygonMode = PolygonMode.Fill;
        configInfo.RasterizationInfo.LineWidth = 1f;
        configInfo.RasterizationInfo.CullMode = CullModeFlags.None;
        configInfo.RasterizationInfo.FrontFace = FrontFace.CounterClockwise;
        configInfo.RasterizationInfo.DepthBiasEnable = Vk.False;
        configInfo.RasterizationInfo.DepthBiasConstantFactor = 0f;
        configInfo.RasterizationInfo.DepthBiasClamp = 0f;
        configInfo.RasterizationInfo.DepthBiasSlopeFactor = 0f;
        
        configInfo.MultisampleInfo.SType = StructureType.PipelineMultisampleStateCreateInfo;
        configInfo.MultisampleInfo.SampleShadingEnable = Vk.False;
        configInfo.MultisampleInfo.RasterizationSamples = SampleCountFlags.Count1Bit;
        configInfo.MultisampleInfo.MinSampleShading = 1.0f;
        configInfo.MultisampleInfo.PSampleMask = default;
        configInfo.MultisampleInfo.AlphaToCoverageEnable = Vk.False;
        configInfo.MultisampleInfo.AlphaToOneEnable = Vk.False;

        configInfo.ColorBlendAttachment.ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit;
        configInfo.ColorBlendAttachment.BlendEnable = Vk.False;
        configInfo.ColorBlendAttachment.SrcColorBlendFactor = BlendFactor.One;
        configInfo.ColorBlendAttachment.DstColorBlendFactor = BlendFactor.Zero;
        configInfo.ColorBlendAttachment.ColorBlendOp = BlendOp.Add;
        configInfo.ColorBlendAttachment.SrcAlphaBlendFactor = BlendFactor.One;
        configInfo.ColorBlendAttachment.DstAlphaBlendFactor = BlendFactor.Zero;
        configInfo.ColorBlendAttachment.AlphaBlendOp = BlendOp.Add;

        configInfo.ColorBlendInfo.SType = StructureType.PipelineColorBlendStateCreateInfo;
        configInfo.ColorBlendInfo.LogicOpEnable = Vk.False;
        configInfo.ColorBlendInfo.LogicOp = LogicOp.Copy;
        configInfo.ColorBlendInfo.AttachmentCount = 1;
        configInfo.ColorBlendInfo.PAttachments = (PipelineColorBlendAttachmentState*) Unsafe.AsPointer(ref configInfo.ColorBlendAttachment);

        configInfo.ColorBlendInfo.BlendConstants[0] = 0;
        configInfo.ColorBlendInfo.BlendConstants[1] = 0;
        configInfo.ColorBlendInfo.BlendConstants[2] = 0;
        configInfo.ColorBlendInfo.BlendConstants[3] = 0;
        
        configInfo.DepthStencilInfo.SType = StructureType.PipelineDepthStencilStateCreateInfo;
        configInfo.DepthStencilInfo.DepthTestEnable = Vk.True;
        configInfo.DepthStencilInfo.DepthWriteEnable = Vk.True;
        configInfo.DepthStencilInfo.DepthCompareOp = CompareOp.Less;
        configInfo.DepthStencilInfo.DepthBoundsTestEnable = Vk.False;
        configInfo.DepthStencilInfo.MinDepthBounds = 0.0f;
        configInfo.DepthStencilInfo.MaxDepthBounds = 1.0f;
        configInfo.DepthStencilInfo.StencilTestEnable = Vk.False;
        configInfo.DepthStencilInfo.Front = default;
        configInfo.DepthStencilInfo.Back = default;

        configInfo.BindingDescriptions = Vertex.GetBindingDescriptions();
        configInfo.AttributeDescriptions = Vertex.GetAttributeDescriptions();
    }

    /// <summary>
    /// Enables alpha blending for the pipeline.
    /// </summary>
    /// <param name="configInfo">The pipeline configuration information.</param>
    public static void EnableAlphaBlending(ref PipelineConfigInfo configInfo) {
        configInfo.ColorBlendAttachment.BlendEnable = Vk.True;
        configInfo.ColorBlendAttachment.ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit;
        configInfo.ColorBlendAttachment.SrcColorBlendFactor = BlendFactor.SrcAlpha;
        configInfo.ColorBlendAttachment.DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha;
    }

    /// <summary>
    /// Enables multi-sampling for the graphics pipeline.
    /// </summary>
    /// <param name="configInfo">The pipeline configuration information.</param>
    /// <param name="msaaSamples">The MSAA (Multi-Sample Anti-Aliasing) sample count.</param>
    public static void EnableMultiSampling(ref PipelineConfigInfo configInfo, SampleCountFlags msaaSamples) {
        configInfo.MultisampleInfo.RasterizationSamples = msaaSamples;
    }

    /// <summary>
    /// Binds a graphics pipeline to a command buffer.
    /// </summary>
    /// <param name="commandBuffer">The command buffer to bind the pipeline to.</param>
    public void Bind(CommandBuffer commandBuffer) {
        this.Vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, this._graphicsPipeline);
    }

    /// <summary>
    /// Creates a graphics pipeline using the provided shader and pipeline configuration information.
    /// </summary>
    /// <param name="shader">The shader to be used for the graphics pipeline.</param>
    /// <param name="configInfo">The pipeline configuration information.</param>
    private unsafe void CreateGraphicsPipeline(Shader shader, PipelineConfigInfo configInfo) {
        PipelineShaderStageCreateInfo vertShaderStageInfo = new() {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = shader.VertShaderModule,
            PName = (byte*) SilkMarshal.StringToPtr("main"),
            Flags = PipelineShaderStageCreateFlags.None,
            PNext = null,
            PSpecializationInfo = null
        };
        
        PipelineShaderStageCreateInfo fragShaderStageInfo = new() {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.FragmentBit,
            Module = shader.FragShaderModule,
            PName = (byte*) SilkMarshal.StringToPtr("main"),
            Flags = PipelineShaderStageCreateFlags.None,
            PNext = null,
            PSpecializationInfo = null
        };

        PipelineShaderStageCreateInfo* shaderStages = stackalloc[] {
            vertShaderStageInfo,
            fragShaderStageInfo
        };

        VertexInputBindingDescription[] bindingDescriptions = configInfo.BindingDescriptions;
        VertexInputAttributeDescription[] attributeDescriptions = configInfo.AttributeDescriptions;

        fixed (VertexInputBindingDescription* bindingDescriptionsPtr = bindingDescriptions) {
            fixed (VertexInputAttributeDescription* attributeDescriptionsPtr = attributeDescriptions) {

                PipelineVertexInputStateCreateInfo vertexInputInfo = new() {
                    SType = StructureType.PipelineVertexInputStateCreateInfo,
                    VertexAttributeDescriptionCount = (uint) attributeDescriptions.Length,
                    VertexBindingDescriptionCount = (uint) bindingDescriptions.Length,
                    PVertexAttributeDescriptions = attributeDescriptionsPtr,
                    PVertexBindingDescriptions = bindingDescriptionsPtr
                };

                Span<DynamicState> dynamicStates = stackalloc DynamicState[] {
                    DynamicState.Viewport,
                    DynamicState.Scissor
                };
                
                PipelineDynamicStateCreateInfo dynamicState = new() {
                    SType = StructureType.PipelineDynamicStateCreateInfo,
                    DynamicStateCount = (uint) dynamicStates.Length,
                    PDynamicStates = (DynamicState*) Unsafe.AsPointer(ref dynamicStates[0])
                };

                GraphicsPipelineCreateInfo pipelineInfo = new() {
                    SType = StructureType.GraphicsPipelineCreateInfo,
                    StageCount = 2,
                    PStages = shaderStages,
                    PVertexInputState = &vertexInputInfo,
                    PInputAssemblyState = &configInfo.InputAssemblyInfo,
                    PViewportState = &configInfo.ViewportInfo,
                    PRasterizationState = &configInfo.RasterizationInfo,
                    PMultisampleState = &configInfo.MultisampleInfo,
                    PColorBlendState = &configInfo.ColorBlendInfo,
                    PDepthStencilState = &configInfo.DepthStencilInfo,
                    PDynamicState = (PipelineDynamicStateCreateInfo*) Unsafe.AsPointer(ref dynamicState),
                    Layout = configInfo.PipelineLayout,
                    RenderPass = configInfo.RenderPass,
                    Subpass = configInfo.Subpass,
                    BasePipelineIndex = -1,
                    BasePipelineHandle = default
                };

                if (this.Vk.CreateGraphicsPipelines(this.Device.GetVkDevice(), default, 1, pipelineInfo, default, out this._graphicsPipeline) != Result.Success) {
                    throw new Exception("Failed to create graphics pipeline!");
                }
            }
        }

        SilkMarshal.Free((nint) shaderStages[0].PName);
        SilkMarshal.Free((nint) shaderStages[1].PName);
    }

    /// <summary>
    /// Retrieves the graphics pipeline associated with the BlissPipeline instance.
    /// </summary>
    /// <returns>The graphics pipeline.</returns>
    public Pipeline GetGraphicsPipeline() {
        return this._graphicsPipeline;
    }

    protected override unsafe void Dispose(bool disposing) {
        if (disposing) {
            this.Vk.DestroyPipeline(this.Device.GetVkDevice(), this._graphicsPipeline, null);
        }
    }
}