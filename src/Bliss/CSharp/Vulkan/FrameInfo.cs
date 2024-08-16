using Bliss.CSharp.Camera;
using Bliss.CSharp.Rendering;
using Silk.NET.Vulkan;

namespace Bliss.CSharp.Vulkan;

public struct FrameInfo {

    public int FrameIndex;
    public float FrameTime;
    public CommandBuffer CommandBuffer;
    public ICam Camera;
    public DescriptorSet GlobalDescriptorSet;
    public IEnumerable<Renderable> RenderableObjects;
}