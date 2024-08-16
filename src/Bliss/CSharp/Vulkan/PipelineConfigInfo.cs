using Silk.NET.Vulkan;

namespace Bliss.CSharp.Vulkan;

public struct PipelineConfigInfo {
    
    public PipelineLayout PipelineLayout;
    public RenderPass RenderPass;
    public uint Subpass;
    
    public VertexInputBindingDescription[] BindingDescriptions;
    public VertexInputAttributeDescription[] AttributeDescriptions;

    public PipelineViewportStateCreateInfo ViewportInfo;
    public PipelineInputAssemblyStateCreateInfo InputAssemblyInfo;
    public PipelineRasterizationStateCreateInfo RasterizationInfo;
    public PipelineMultisampleStateCreateInfo MultisampleInfo;
    public PipelineColorBlendAttachmentState ColorBlendAttachment;
    public PipelineColorBlendStateCreateInfo ColorBlendInfo;
    public PipelineDepthStencilStateCreateInfo DepthStencilInfo;

    /// <summary>
    /// Represents the configuration information for a Vulkan pipeline.
    /// </summary>
    public PipelineConfigInfo() {
        this.Subpass = 0;    
        this.BindingDescriptions = [];
        this.AttributeDescriptions = [];
    }
}