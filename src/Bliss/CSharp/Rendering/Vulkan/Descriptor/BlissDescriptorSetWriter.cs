using Silk.NET.Vulkan;

namespace Bliss.CSharp.Rendering.Vulkan.Descriptor;

public class BlissDescriptorSetWriter {
    
    public readonly Vk Vk;
    public readonly BlissDevice Device;
    
    private BlissDescriptorSetLayout _setLayout;
    private WriteDescriptorSet[] _writes;

    public BlissDescriptorSetWriter(Vk vk, BlissDevice device, BlissDescriptorSetLayout setLayout) {
        this.Vk = vk;
        this.Device = device;
        this._setLayout = setLayout;
        this._writes = [];
    }

    public unsafe BlissDescriptorSetWriter WriteBuffer(uint binding, DescriptorBufferInfo bufferInfo) {
        if (!this._setLayout.Bindings.TryGetValue(binding, out var bindingDescription)) {
            throw new ApplicationException($"Layout does not contain the specified binding at {binding}");
        }

        if (bindingDescription.DescriptorCount > 1) {
            throw new ApplicationException("Binding single descriptor info, but binding expects multiple");
        }

        WriteDescriptorSet write = new() {
            SType = StructureType.WriteDescriptorSet,
            DescriptorType = bindingDescription.DescriptorType,
            DstBinding = binding,
            PBufferInfo = &bufferInfo,
            DescriptorCount = 1
        };

        int writesLength = this._writes.Length;
        Array.Resize(ref this._writes, writesLength + 1);
        this._writes[writesLength] = write;
        return this;
    }

    public unsafe BlissDescriptorSetWriter WriteImage(uint binding, DescriptorImageInfo imageInfo) {
        if (!this._setLayout.Bindings.TryGetValue(binding, out var bindingDescription)) {
            throw new ApplicationException($"Layout does not contain the specified binding at {binding}");
        }

        if (bindingDescription.DescriptorCount > 1) {
            throw new ApplicationException("Binding single descriptor info, but binding expects multiple");
        }

        WriteDescriptorSet write = new() {
            SType = StructureType.WriteDescriptorSet,
            DescriptorType = bindingDescription.DescriptorType,
            DstBinding = binding,
            PImageInfo = &imageInfo,
            DescriptorCount = 1,
        };

        int writesLength = this._writes.Length;
        Array.Resize(ref this._writes, writesLength + 1);
        this._writes[writesLength] = write;
        return this;
    }

    public bool Build(BlissDescriptorPool pool, ref DescriptorSet set) {
        if (!pool.AllocateDescriptorSet(this._setLayout.DescriptorSetLayout, ref set)) {
            return false;
        }
        
        this.Overwrite(ref set);
        return true;
    }

    private unsafe void Overwrite(ref DescriptorSet set) {
        for (var i = 0; i < this._writes.Length; i++) {
            this._writes[i].DstSet = set;
        }
        
        fixed (WriteDescriptorSet* writesPtr = this._writes) {
            this.Vk.UpdateDescriptorSets(this.Device.GetVkDevice(), (uint) this._writes.Length, writesPtr, 0, null);
        }
    }
}