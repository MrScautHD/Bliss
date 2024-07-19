using Bliss.CSharp.Rendering.Vulkan;
using Silk.NET.Vulkan;

namespace Bliss.CSharp.Descriptor;

public class BlissDescriptorPoolBuilder {
    
    public readonly Vk Vk;
    public readonly BlissDevice Device;
    
    private uint _maxSets;
    private DescriptorPoolCreateFlags _flags;

    private readonly List<DescriptorPoolSize> _size = new();

    public BlissDescriptorPoolBuilder(Vk vk, BlissDevice device) {
        this.Vk = vk;
        this.Device = device;
    }
    
    public BlissDescriptorPoolBuilder AddSize(DescriptorType descriptorType, uint count) {
        this._size.Add(new DescriptorPoolSize(descriptorType, count));
        return this;
    }

    public BlissDescriptorPoolBuilder SetMaxSets(uint count) {
        this._maxSets = count;
        return this;
    }
    
    public BlissDescriptorPoolBuilder SetFlags(DescriptorPoolCreateFlags flags) {
        this._flags = flags;
        return this;
    }

    public BlissDescriptorPool Build() {
        return new BlissDescriptorPool(this.Vk, this.Device, this._maxSets, this._flags, this._size.ToArray());
    }
}