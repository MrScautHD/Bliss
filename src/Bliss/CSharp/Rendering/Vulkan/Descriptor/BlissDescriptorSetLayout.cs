using Silk.NET.Vulkan;

namespace Bliss.CSharp.Rendering.Vulkan.Descriptor;

public class BlissDescriptorSetLayout : Disposable {
    
    public readonly Vk Vk;
    public readonly BlissDevice Device;
    public readonly DescriptorSetLayout DescriptorSetLayout;

    public Dictionary<uint, DescriptorSetLayoutBinding> Bindings { get; private set; }
    
    public unsafe BlissDescriptorSetLayout(Vk vk, BlissDevice device, Dictionary<uint, DescriptorSetLayoutBinding> bindings) {
        this.Vk = vk;
        this.Device = device;
        this.Bindings = bindings;

        fixed (DescriptorSetLayoutBinding* setLayoutPtr = this.Bindings.Values.ToArray()) {
            DescriptorSetLayoutCreateInfo descriptorSetLayoutInfo = new() {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = (uint) this.Bindings.Count,
                PBindings = setLayoutPtr
            };

            if (vk.CreateDescriptorSetLayout(device.GetVkDevice(), &descriptorSetLayoutInfo, null, out this.DescriptorSetLayout) != Result.Success) {
                throw new ApplicationException("Failed to create descriptor set layout");
            }
        }
    }

    protected override unsafe void Dispose(bool disposing) {
        if (disposing) {
            this.Vk.DestroyDescriptorSetLayout(this.Device.GetVkDevice(), this.DescriptorSetLayout, null);
        }
    }
}