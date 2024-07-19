using Bliss.CSharp.Rendering.Vulkan;
using Silk.NET.Vulkan;

namespace Bliss.CSharp.Descriptor;

public class BlissDescriptorPool : Disposable {

    public readonly Vk Vk;
    public readonly BlissDevice Device;
    public readonly DescriptorPool DescriptorPool;
    
    private uint _maxSets;
    private DescriptorPoolCreateFlags _flags;
    private DescriptorPoolSize[] _size;
    
    public unsafe BlissDescriptorPool(Vk vk, BlissDevice device, uint maxSets, DescriptorPoolCreateFlags flags, DescriptorPoolSize[] size) {
        this.Vk = vk;
        this.Device = device;
        this._maxSets = maxSets;
        this._flags = flags;
        this._size = size;

        fixed (DescriptorPool* descriptorPoolPtr = &this.DescriptorPool) {
            fixed (DescriptorPoolSize* poolSizesPtr = size) {
                DescriptorPoolCreateInfo descriptorPoolInfo = new() {
                    SType = StructureType.DescriptorPoolCreateInfo,
                    PoolSizeCount = (uint) size.Length,
                    PPoolSizes = poolSizesPtr,
                    MaxSets = maxSets,
                    Flags = flags
                };

                if (vk.CreateDescriptorPool(device.GetDevice(), &descriptorPoolInfo, null, descriptorPoolPtr) != Result.Success) {
                    throw new ApplicationException("Failed to create descriptor pool");
                }
            }
        }
    }

    public unsafe bool AllocateDescriptorSet(DescriptorSetLayout descriptorSetLayout, ref DescriptorSet descriptorSet) {
        DescriptorSetAllocateInfo allocInfo = new DescriptorSetAllocateInfo {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = this.DescriptorPool,
            PSetLayouts = &descriptorSetLayout,
            DescriptorSetCount = 1
        };
        
        return this.Vk.AllocateDescriptorSets(this.Device.GetDevice(), allocInfo, out descriptorSet) == Result.Success;
    }

    private void FreeDescriptors(ref DescriptorSet[] descriptors) {
        this.Vk.FreeDescriptorSets(this.Device.GetDevice(), this.DescriptorPool, descriptors);
    }

    private void ResetPool() {
        this.Vk.ResetDescriptorPool(this.Device.GetDevice(), this.DescriptorPool, 0);
    }

    protected override unsafe void Dispose(bool disposing) {
        if (disposing) {
            this.Vk.DestroyDescriptorPool(this.Device.GetDevice(), this.DescriptorPool, null);
        }
    }
}