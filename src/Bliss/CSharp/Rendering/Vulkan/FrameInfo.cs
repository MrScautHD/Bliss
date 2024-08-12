using Bliss.CSharp.Camera;
using Silk.NET.Vulkan;

namespace Bliss.CSharp.Rendering.Vulkan;

public struct FrameInfo {

    public int FrameIndex;
    public float FrameTime;
    public CommandBuffer CommandBuffer;
    public ICam Camera;
    public DescriptorSet GlobalDescriptorSet;
    public IEnumerable<Renderable> RenderableObjects;
}