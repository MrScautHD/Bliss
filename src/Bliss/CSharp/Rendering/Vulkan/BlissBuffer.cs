using System.Diagnostics;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Bliss.CSharp.Rendering.Vulkan;

public class BlissBuffer : Disposable {
    
    public readonly Vk Vk;
    public readonly BlissDevice Device;
    
    public readonly ulong InstanceSize;
    public readonly uint InstanceCount;
    
    public readonly BufferUsageFlags UsageFlags;
    public readonly MemoryPropertyFlags MemoryPropertyFlags;

    public readonly ulong AlignmentSize;
    public readonly ulong BufferSize;
    
    public readonly Buffer VkBuffer;
    
    private unsafe void* _mapped;
    private DeviceMemory _memory;
    
    /// <summary>
    /// Initializes a new instance of the BlissBuffer class.
    /// </summary>
    /// <param name="vk">The Vulkan instance.</param>
    /// <param name="device">The Bliss device.</param>
    /// <param name="instanceSize">The size of each instance.</param>
    /// <param name="instanceCount">The number of instances.</param>
    /// <param name="usageFlags">The buffer usage flags.</param>
    /// <param name="memoryPropertyFlags">The memory property flags.</param>
    /// <param name="minOffsetAlignment">The minimum offset alignment (optional).</param>
    public BlissBuffer(Vk vk, BlissDevice device, ulong instanceSize, uint instanceCount, BufferUsageFlags usageFlags, MemoryPropertyFlags memoryPropertyFlags, ulong minOffsetAlignment = 1) {
        this.Vk = vk;
        this.Device = device;
        this.InstanceSize = instanceSize;
        this.InstanceCount = instanceCount;
        this.UsageFlags = usageFlags;
        this.MemoryPropertyFlags = memoryPropertyFlags;
        
        this.AlignmentSize = this.GetAlignment(instanceSize, minOffsetAlignment);
        this.BufferSize = this.AlignmentSize * instanceCount;
        device.CreateBuffer(this.BufferSize, usageFlags, memoryPropertyFlags, ref this.VkBuffer, ref this._memory);
    }

    /// <summary>
    /// Map a memory range of this buffer. If successful, mapped points to the specified buffer range.
    /// </summary>
    /// <param name="size">Size of the memory range to map. Pass VK_WHOLE_SIZE to map the completebuffer range.</param>
    /// <param name="offset"> Byte offset from beginning.</param>
    /// <returns>VkResult of the buffer mapping call.</returns>
    public unsafe Result Map(ulong size = Vk.WholeSize, ulong offset = 0) {
        Debug.Assert(this.VkBuffer.Handle != 0 && this._memory.Handle != 0, "Called map on buffer before create");
        return this.Vk.MapMemory(this.Device.GetVkDevice(), this._memory, offset, size, 0, ref this._mapped);
    }

    /// <summary>
    /// Unmap a mapped memory range.
    /// </summary>
    public unsafe void UnMap() {
        if (this._mapped != null) {
            this.Vk.UnmapMemory(this.Device.GetVkDevice(), this._memory);
            this._mapped = null;
        }
    }

    /// <summary>
    /// Copies the specified data to the mapped buffer. Default value writes whole buffer range.
    /// </summary>
    /// <typeparam name="T">The type of data being written.</typeparam>
    /// <param name="data">The array of data.</param>
    /// <param name="size">Size of the data to copy. Pass VK_WHOLE_SIZE to flush the complete buffer (optional).</param>
    /// <param name="offset">Byte offset from beginning of mapped region (optional).</param>
    public unsafe void WriteToBuffer<T>(T[] data, ulong size = Vk.WholeSize, ulong offset = 0) where T : unmanaged {
        if (size == Vk.WholeSize) {
            fixed (void* dataPtr = data) {
                int dataSize = Marshal.SizeOf<T>() * data.Length;
                System.Buffer.MemoryCopy(dataPtr, this._mapped, dataSize, dataSize);
            }
        }
        else {
            // TODO: Implemented the offset.
            throw new NotImplementedException("don't have offset stuff working yet");
        }
    }

    /// <summary>
    /// Writes bytes to the buffer.
    /// </summary>
    /// <param name="data">The byte array to write.</param>
    public unsafe void WriteBytesToBuffer(byte[] data) {
        fixed (void* dataPtr = data) {
            int dataSize = Marshal.SizeOf<byte>() * data.Length;
            System.Buffer.MemoryCopy(dataPtr, this._mapped, dataSize, dataSize);
        }
    }

    /// <summary>
    /// Writes data to the buffer at the specified index.
    /// </summary>
    /// <typeparam name="T">The type of data to write.</typeparam>
    /// <param name="data">The array of data to write.</param>
    /// <param name="index">The index at which to start writing the data.</param>
    public unsafe void WriteToIndex<T>(T[] data, int index) {
        Span<T> tempSpan = new Span<T>(this._mapped, data.Length);
        data.AsSpan().CopyTo(tempSpan[index..]);
    }
    
    /// <summary>
    /// Flushes the mapped memory range of the buffer.
    /// </summary>
    /// <param name="size">The size of the mapped memory range to flush.</param>
    /// <param name="offset">The offset within the mapped memory range to flush (optional).</param>
    /// <returns>The result of the flush operation.</returns>
    public Result Flush(ulong size = Vk.WholeSize, ulong offset = 0) {
        MappedMemoryRange mappedRange = new() {
            SType = StructureType.MappedMemoryRange,
            Memory = this._memory,
            Offset = offset,
            Size = size
        };
        
        return this.Vk.FlushMappedMemoryRanges(this.Device.GetVkDevice(), 1, mappedRange);
    }

    /// <summary>
    /// Invalidates the mapped memory ranges of a Vulkan buffer.
    /// </summary>
    /// <param name="size">The size of the memory range to invalidate. If Vk.WholeSize is specified, the entire mapped range is invalidated. The default value is Vk.WholeSize.</param>
    /// <param name="offset">The offset from the start of the memory range to invalidate. The default value is 0.</param>
    /// <returns>The result of the invalidate operation.</returns>
    public Result Invalidate(ulong size = Vk.WholeSize, ulong offset = 0) {
        MappedMemoryRange mappedRange = new() {
            SType = StructureType.MappedMemoryRange,
            Memory = this._memory,
            Offset = offset,
            Size = size
        };
        
        return this.Vk.InvalidateMappedMemoryRanges(this.Device.GetVkDevice(), 1, mappedRange);
    }
    
    /// <summary>
    /// Retrieves a descriptor buffer info object for the buffer.
    /// </summary>
    /// <param name="size">The size of the buffer (optional). Uses the whole size by default.</param>
    /// <param name="offset">The offset into the buffer (optional). Zero by default.</param>
    /// <returns>A DescriptorBufferInfo object.</returns>
    public DescriptorBufferInfo DescriptorInfo(ulong size = Vk.WholeSize, ulong offset = 0) {
        return new() {
            Buffer = this.VkBuffer,
            Offset = offset,
            Range = size
        };
    }

    /// <summary>
    /// Flushes the mapped memory range of the buffer at the specified index.
    /// </summary>
    /// <param name="index">The index of the buffer.</param>
    /// <returns>The result of the flush operation.</returns>
    public Result FlushIndex(int index) { 
        return this.Flush(this.AlignmentSize, (ulong) index * this.AlignmentSize); 
    }

    /// <summary>
    /// Retrieves a descriptor buffer info object for the buffer at a specific index.
    /// </summary>
    /// <param name="index">The index of the buffer.</param>
    /// <returns>A DescriptorBufferInfo object for the buffer at the given index.</returns>
    public DescriptorBufferInfo DescriptorInfoForIndex(int index) {
        return this.DescriptorInfo(this.AlignmentSize, (ulong) index * this.AlignmentSize);
    }

    /// <summary>
    /// Invalidates the mapped memory range of the buffer at the specified index.
    /// </summary>
    /// <param name="index">The index of the buffer to invalidate.</param>
    /// <returns>The result of the invalidate operation.</returns>
    public Result InvalidateIndex(int index) {
        return this.Invalidate(this.AlignmentSize, (ulong) index * this.AlignmentSize);
    }

    /// <summary>
    /// Returns the alignment size for the instance size, based on the minimum offset alignment.
    /// </summary>
    /// <param name="instanceSize">The size of each instance.</param>
    /// <param name="minOffsetAlignment">The minimum offset alignment (optional).</param>
    /// <returns>The alignment size.</returns>
    private ulong GetAlignment(ulong instanceSize, ulong minOffsetAlignment) {
        if (minOffsetAlignment > 0) {
            return (instanceSize + minOffsetAlignment - 1) & ~(minOffsetAlignment - 1);
        }
        
        return instanceSize;
    }

    protected override unsafe void Dispose(bool disposing) {
        if (disposing) {
            this.UnMap();
            this.Vk.DestroyBuffer(this.Device.GetVkDevice(), this.VkBuffer, null);
            this.Vk.FreeMemory(this.Device.GetVkDevice(), this._memory, null);
        }
    }
}