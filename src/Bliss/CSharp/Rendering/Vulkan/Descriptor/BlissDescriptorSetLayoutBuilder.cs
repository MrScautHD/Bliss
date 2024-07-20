using Silk.NET.Vulkan;

namespace Bliss.CSharp.Rendering.Vulkan.Descriptor;

public class BlissDescriptorSetLayoutBuilder {
    
    public readonly Vk Vk;
    public readonly BlissDevice Device;
    
    private Dictionary<uint, DescriptorSetLayoutBinding> _bindings;

    public BlissDescriptorSetLayoutBuilder(Vk vk, BlissDevice device) {
        this.Vk = vk;
        this.Device = device;
        this._bindings = new Dictionary<uint, DescriptorSetLayoutBinding>();
    }

    public BlissDescriptorSetLayoutBuilder AddBinding(uint binding, DescriptorType type, ShaderStageFlags flags, uint count = 1) {
        if (this._bindings.ContainsKey(binding)) {
            throw new ApplicationException($"Binding {binding} is already in use, can't add");
        }
        
        this._bindings[binding] = new DescriptorSetLayoutBinding() {
            Binding = binding,
            DescriptorType = type,
            StageFlags = flags,
            DescriptorCount = count
        };
        
        return this;
    }

    public BlissDescriptorSetLayout Build() {
        return new BlissDescriptorSetLayout(this.Vk, this.Device, this._bindings);
    }
}