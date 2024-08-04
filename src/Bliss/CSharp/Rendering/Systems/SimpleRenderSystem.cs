using System.Diagnostics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Rendering.Vulkan;
using Bliss.CSharp.Shaders;
using Silk.NET.Vulkan;

namespace Bliss.CSharp.Rendering.Systems;

public class SimpleRenderSystem : Disposable {
    
    public readonly Vk Vk;
    public readonly BlissDevice Device;

    private BlissPipeline _pipeline;
    private PipelineLayout _pipelineLayout;

    public SimpleRenderSystem(Vk vk, BlissDevice device, RenderPass renderPass, DescriptorSetLayout globalSetLayout) {
		this.Vk = vk;
		this.Device = device;
        
        this.CreatePipelineLayout(globalSetLayout);
        this.CreatePipeline(renderPass);
	}
    
    public unsafe void Render(FrameInfo frameInfo) {
        this._pipeline.Bind(frameInfo.CommandBuffer);
        this.Vk.CmdBindDescriptorSets(frameInfo.CommandBuffer, PipelineBindPoint.Graphics, this._pipelineLayout, 0, 1, frameInfo.GlobalDescriptorSet, 0, null);

        foreach (Renderable renderable in frameInfo.RenderableObjects) {
            SimplePushConstantData push = new() {
                ModelMatrix = renderable.Transform.GetMatrix(),
                NormalMatrix = renderable.Transform.GetNormalMatrix()
            };
            
            this.Vk.CmdPushConstants(frameInfo.CommandBuffer, this._pipelineLayout, ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit, 0, (uint) Marshal.SizeOf<SimplePushConstantData>(), ref push);
            
            renderable.Model.Bind(frameInfo.CommandBuffer);
            renderable.Model.Draw(frameInfo.CommandBuffer);
        }
    }
    
    private unsafe void CreatePipelineLayout(DescriptorSetLayout globalSetLayout) {
        DescriptorSetLayout[] descriptorSetLayouts = [
            globalSetLayout
        ];
        
        PushConstantRange pushConstantRange = new() {
            StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
            Offset = 0,
            Size = (uint) Marshal.SizeOf<SimplePushConstantData>()
        };

        fixed (DescriptorSetLayout* descriptorSetLayoutPtr = descriptorSetLayouts) {
            PipelineLayoutCreateInfo pipelineLayoutInfo = new() {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = (uint) descriptorSetLayouts.Length,
                PSetLayouts = descriptorSetLayoutPtr,
                PushConstantRangeCount = 1,
                PPushConstantRanges = &pushConstantRange,
            };

            if (this.Vk.CreatePipelineLayout(this.Device.GetVkDevice(), pipelineLayoutInfo, null, out this._pipelineLayout) != Result.Success) {
                throw new Exception("Failed to create pipeline layout!");
            }
        }
    }

    private void CreatePipeline(RenderPass renderPass) {
        Debug.Assert(this._pipelineLayout.Handle != 0, "Cannot create pipeline before pipeline layout");

        PipelineConfigInfo pipelineConfig = new();
        BlissPipeline.GetDefaultPipelineConfigInfo(ref pipelineConfig);
        BlissPipeline.EnableMultiSampling(ref pipelineConfig, this.Device.MsaaSamples);

        pipelineConfig.RenderPass = renderPass;
        pipelineConfig.PipelineLayout = this._pipelineLayout;
        
        this._pipeline = new BlissPipeline(this.Vk, this.Device, new Shader(this.Vk, this.Device, "content/shaders/default_shader.frag", "content/shaders/default_shader.vert"), pipelineConfig);
    }

    protected override unsafe void Dispose(bool disposing) {
        if (disposing) {
            this._pipeline.Dispose();
            this.Vk.DestroyPipelineLayout(this.Device.GetVkDevice(), this._pipelineLayout, null);
        }
    }
}