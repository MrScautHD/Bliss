using Silk.NET.Vulkan;

namespace Bliss.CSharp.Rendering.Vulkan;

public struct FrameInfo {

    public int FrameIndex;
    public float FrameTime;
    public CommandBuffer CommandBuffer;
    public ICamera Camera;
    public DescriptorSet DescriptorSet;
    public Dictionary<uint, Renderable> RenderObjects;
}