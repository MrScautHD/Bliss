using Silk.NET.Vulkan;

namespace Bliss.CSharp;

public class Bliss {

    public Vk Vk { get; private set; }
    
    public Bliss() {
        this.Vk = Vk.GetApi();
    }
}