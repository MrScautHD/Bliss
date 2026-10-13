using System.Runtime.InteropServices;
using Veldrith;

namespace Bliss.CSharp.Graphics.Pipelines.Buffers;

public class SimpleUniformBuffer<T> : Disposable, ISimpleBuffer where T : unmanaged {
    
    /// <summary>
    /// Gets the graphics device associated with this buffer.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// Gets the total number of elements in this buffer.
    /// </summary>
    public uint Size { get; private set; }
    
    /// <summary>
    /// Gets the CPU-side data storage backing this buffer.
    /// </summary>
    public T[] Data { get; private set; }
    
    /// <summary>
    /// Gets a value indicating whether this buffer was created with dynamic usage.
    /// </summary>
    public bool UseDynamicBuffer { get; private set; }
    
    /// <summary>
    /// Gets the shader stages where this buffer is bound and used.
    /// </summary>
    public ShaderStages ShaderStages { get; }
    
    /// <summary>
    /// Gets the GPU-side device buffer that stores this buffer’s data.
    /// </summary>
    public DeviceBuffer DeviceBuffer { get; }
    
    /// <summary>
    /// Caches resource sets keyed by their corresponding buffer layouts.
    /// </summary>
    private Dictionary<SimpleBufferLayout, ResourceSet> _cachedResourceSets;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleUniformBuffer{T}"/> class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create the buffer and related resources.</param>
    /// <param name="size">The total number of elements in the buffer.</param>
    /// <param name="stages">The shader stages where this buffer will be used.</param>
    /// <param name="useDynamicBuffer">Whether the GPU buffer should be created with dynamic usage.</param>
    public SimpleUniformBuffer(GraphicsDevice graphicsDevice, uint size, ShaderStages stages, bool useDynamicBuffer = true) {
        this.GraphicsDevice = graphicsDevice;
        this.Size = size;
        this.Data = new T[size];
        this.ShaderStages = stages;
        this.UseDynamicBuffer = useDynamicBuffer;
        
        uint alignment = graphicsDevice.UniformBufferMinOffsetAlignment;
        long dataSize = size * Marshal.SizeOf<T>();
        long bufferSize = (dataSize / alignment + (dataSize % alignment > 0 ? 1 : 0)) * alignment;
        
        BufferUsage usage = BufferUsage.UniformBuffer;
        
        if (useDynamicBuffer) {
            usage |= BufferUsage.Dynamic;
        }
        
        this.DeviceBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint) bufferSize, usage));
        this._cachedResourceSets = new Dictionary<SimpleBufferLayout, ResourceSet>();
    }
    
    /// <summary>
    /// Gets or creates a resource set for the specified buffer layout.
    /// </summary>
    /// <param name="layout">The layout describing how the buffer is bound.</param>
    /// <returns>The <see cref="ResourceSet"/> associated with the layout.</returns>
    public ResourceSet GetResourceSet(SimpleBufferLayout layout) {
        if (!this._cachedResourceSets.TryGetValue(layout, out ResourceSet? resourceSet)) {
            ResourceSet newResourceSet = this.GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(layout.Layout, this.DeviceBuffer));
            
            this._cachedResourceSets.Add(layout, newResourceSet);
            return newResourceSet;
        }
        
        return resourceSet;
    }
    
    /// <summary>
    /// Sets the value at the specified index in the buffer’s data.
    /// </summary>
    /// <param name="index">The zero-based index of the element to set.</param>
    /// <param name="value">The new value to assign.</param>
    public void SetValue(int index, T value) {
        if (index < 0 || index >= this.Size) {
            throw new IndexOutOfRangeException($"Index {index} is outside the valid range of 0 to {this.Size - 1}.");
        }
        
        this.Data[index] = value;
    }
    
    /// <summary>
    /// Sets the value at the specified index and immediately updates the GPU buffer.
    /// </summary>
    /// <param name="index">The zero-based index of the element to set.</param>
    /// <param name="value">The new value to assign.</param>
    public void SetValueImmediate(int index, T value) {
        this.SetValueImmediate(index, ref value);
    }
    
    /// <summary>
    /// Sets the value at the specified index by reference and immediately updates the GPU buffer.
    /// </summary>
    /// <param name="index">The zero-based index of the element to set.</param>
    /// <param name="value">The new value to assign by reference.</param>
    public void SetValueImmediate(int index, ref T value) {
        if (index < 0 || index >= this.Size) {
            throw new IndexOutOfRangeException($"Index {index} is outside the valid range of 0 to {this.Size - 1}.");
        }
        
        this.Data[index] = value;
        this.GraphicsDevice.UpdateBuffer(this.DeviceBuffer, (uint) (index * Marshal.SizeOf<T>()), ref this.Data[index]);
    }
    
    /// <summary>
    /// Updates a range of entries immediately on the GPU using the current CPU-side data.
    /// </summary>
    /// <param name="startIndex">The zero-based index of the first entry to upload.</param>
    /// <param name="count">The number of entries to upload.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="startIndex"/> or <paramref name="count"/> is negative.</exception>
    /// <exception cref="IndexOutOfRangeException">Thrown if the specified range is outside the valid data range.</exception>
    public void UpdateBufferRangeImmediate(int startIndex, int count) {
        if (startIndex < 0) {
            throw new ArgumentOutOfRangeException(nameof(startIndex), "Start index cannot be negative.");
        }
        
        if (count < 0) {
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
        }
        
        if ((uint) startIndex > this.Size || (uint) (startIndex + count) > this.Size) {
            throw new IndexOutOfRangeException($"Range [{startIndex}, {startIndex + count}] is outside the valid range of 0 to {this.Size}.");
        }
        
        if (count == 0) {
            return;
        }
        
        uint offsetInBytes = (uint) (startIndex * Marshal.SizeOf<T>());
        this.GraphicsDevice.UpdateBuffer(this.DeviceBuffer, offsetInBytes, this.Data.AsSpan(startIndex, count));
    }
    
    /// <summary>
    /// Updates the entire GPU buffer immediately with the current CPU data.
    /// </summary>
    public void UpdateBufferImmediate() {
        this.GraphicsDevice.UpdateBuffer(this.DeviceBuffer, 0, this.Data);
    }
    
    /// <summary>
    /// Sets the value at the specified index and defers the GPU update using a command list.
    /// </summary>
    /// <param name="commandList">The command list used to defer the update.</param>
    /// <param name="index">The zero-based index of the element to set.</param>
    /// <param name="value">The new value to assign.</param>
    public void SetValueDeferred(CommandList commandList, int index, T value) {
        this.SetValueDeferred(commandList, index, ref value);
    }
    
    /// <summary>
    /// Sets the value at the specified index in the buffer and defers the GPU update using the given command list.
    /// </summary>
    /// <param name="commandList">The command list used to schedule the deferred buffer update operation.</param>
    /// <param name="index">The zero-based index of the element to update within the buffer.</param>
    /// <param name="value">The value to set at the specified index within the buffer.</param>
    public void SetValueDeferred(CommandList commandList, int index, ref T value) {
        if (index < 0 || index >= this.Size) {
            throw new IndexOutOfRangeException($"Index {index} is outside the valid range of 0 to {this.Size - 1}.");
        }
        
        this.Data[index] = value;
        commandList.UpdateBuffer(this.DeviceBuffer, (uint) (index * Marshal.SizeOf<T>()), ref this.Data[index]);
    }
    
    /// <summary>
    /// Updates a range of entries on the GPU using the specified command list and the current CPU-side data.
    /// </summary>
    /// <param name="commandList">The command list used to defer the buffer update.</param>
    /// <param name="startIndex">The zero-based index of the first entry to upload.</param>
    /// <param name="count">The number of entries to upload.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="startIndex"/> or <paramref name="count"/> is negative.</exception>
    /// <exception cref="IndexOutOfRangeException">Thrown if the specified range is outside the valid data range.</exception>
    public void UpdateBufferRangeDeferred(CommandList commandList, int startIndex, int count) {
        if (startIndex < 0) {
            throw new ArgumentOutOfRangeException(nameof(startIndex), "Start index cannot be negative.");
        }
        
        if (count < 0) {
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
        }
        
        if ((uint) startIndex > this.Size || (uint) (startIndex + count) > this.Size) {
            throw new IndexOutOfRangeException($"Range [{startIndex}, {startIndex + count}] is outside the valid range of 0 to {this.Size}.");
        }
        
        if (count == 0) {
            return;
        }
        
        uint offsetInBytes = (uint) (startIndex * Marshal.SizeOf<T>());
        commandList.UpdateBuffer(this.DeviceBuffer, offsetInBytes, this.Data.AsSpan(startIndex, count));
    }
    
    /// <summary>
    /// Updates the entire GPU buffer using a deferred command list.
    /// </summary>
    /// <param name="commandList">The command list used to defer the update.</param>
    public void UpdateBufferDeferred(CommandList commandList) {
        commandList.UpdateBuffer(this.DeviceBuffer, 0, this.Data);
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            foreach (ResourceSet resourceSet in this._cachedResourceSets.Values) {
                resourceSet.Dispose();
            }
            
            this.DeviceBuffer.Dispose();
        }
    }
}