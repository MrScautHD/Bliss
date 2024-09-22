using System.Runtime.InteropServices;
using Bliss.CSharp.Logging;
using Veldrid;

namespace Bliss.CSharp.Graphics.Pipelines.Buffers;

public class SimpleBuffer<T> : Disposable, ISimpleBuffer where T : unmanaged {
    
    /// <summary>
    /// The graphics device used to create the buffer and related resources.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }

    /// <summary>
    /// The name of the buffer.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// The size of the buffer in elements.
    /// </summary>
    public uint Size { get; private set; }

    /// <summary>
    /// The type of the buffer, which defines its usage.
    /// </summary>
    public SimpleBufferType BufferType { get; private set; }

    /// <summary>
    /// The shader stages where this buffer will be used.
    /// </summary>
    public ShaderStages ShaderStages { get; private set; }

    /// <summary>
    /// Represents the buffer resource allocated on the graphics device.
    /// </summary>
    public DeviceBuffer DeviceBuffer { get; private set; }

    /// <summary>
    /// The resource layout associated with the buffer.
    /// </summary>
    public ResourceLayout ResourceLayout { get; private set; }

    /// <summary>
    /// Represents a collection of GPU resources such as buffers and textures, bound together for use in a rendering pipeline.
    /// </summary>
    public ResourceSet ResourceSet { get; private set; }

    /// <summary>
    /// An array containing the data stored in the buffer.
    /// </summary>
    public T[] Data { get; private set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleBuffer{T}"/> class with the specified graphics device, buffer name, size, buffer type, and shader stages.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create the buffer and related resources.</param>
    /// <param name="name">The name to be assigned to the buffer and resources.</param>
    /// <param name="size">The size of the buffer in elements.</param>
    /// <param name="bufferType">The type of the buffer, which defines its usage.</param>
    /// <param name="stages">The shader stages where this buffer will be used.</param>
    public SimpleBuffer(GraphicsDevice graphicsDevice, string name, uint size, SimpleBufferType bufferType, ShaderStages stages) {
        this.GraphicsDevice = graphicsDevice;
        this.Name = name;
        this.Size = size;
        this.BufferType = bufferType;
        this.ShaderStages = stages;
        this.Data = new T[size];
        
        long dataSize = size * Marshal.SizeOf<T>();
        long bufferSize = (dataSize / 16 + (dataSize % 16 > 0 ? 1 : 0)) * 16;
        
        //uint alignment = graphicsDevice.UniformBufferMinOffsetAlignment; // More common requirement in Metal
        //long dataSize = size * Marshal.SizeOf<T>();
        //long bufferSize = (dataSize / alignment + (dataSize % alignment > 0 ? 1 : 0)) * alignment;
        
        Logger.Error(bufferSize + "");
        
        this.DeviceBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint) (bufferSize), this.GetBufferUsage(bufferType)));
        this.DeviceBuffer.Name = name;
        
        this.ResourceLayout = graphicsDevice.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription(name, this.GetResourceKind(bufferType), stages)));
        this.ResourceSet = graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(this.ResourceLayout, this.DeviceBuffer));
    }

    /// <summary>
    /// Sets the value at the specified index of the buffer and optionally updates the GPU buffer.
    /// </summary>
    /// <param name="index">The index at which the value will be set.</param>
    /// <param name="value">The value to set at the specified index.</param>
    /// <param name="updateBuffer">Indicates whether the GPU buffer should be updated with the new value.</param>
    public void SetValue(int index, T value, bool updateBuffer = false) {
        this.Data[index] = value;
        
        if (updateBuffer) {
            this.GraphicsDevice.UpdateBuffer(this.DeviceBuffer, (uint) (index * Marshal.SizeOf<T>()), this.Data[index]);
        }
    }
    
    /// <summary>
    /// Updates the GPU buffer with the current data stored in the <see cref="Data"/> array.
    /// This method synchronizes the CPU-side data with the GPU-side buffer by copying the
    /// contents of the <see cref="Data"/> array into the <see cref="DeviceBuffer"/>.
    /// </summary>
    public void UpdateBuffer() {
        this.GraphicsDevice.UpdateBuffer(this.DeviceBuffer, 0, this.Data);
    }

    /// <summary>
    /// Determines the buffer usage based on the provided buffer type.
    /// </summary>
    /// <param name="bufferType">The type of buffer to get the usage for.</param>
    /// <return>The corresponding buffer usage for the specified buffer type.</return>
    private BufferUsage GetBufferUsage(SimpleBufferType bufferType) {
        return bufferType switch {
            SimpleBufferType.Uniform => BufferUsage.UniformBuffer | BufferUsage.Dynamic,
            SimpleBufferType.StructuredReadOnly => BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic,
            SimpleBufferType.StructuredReadWrite => BufferUsage.StructuredBufferReadWrite,
            _ => throw new ArgumentException($"Unsupported buffer type: {bufferType}", nameof(bufferType))
        };
    }

    /// <summary>
    /// Determines the appropriate resource kind based on the buffer type.
    /// </summary>
    /// <param name="bufferType">The type of the buffer for which the resource kind is to be determined.</param>
    /// <returns>The resource kind that corresponds to the specified buffer type.</returns>
    private ResourceKind GetResourceKind(SimpleBufferType bufferType) {
        return bufferType switch {
            SimpleBufferType.Uniform => ResourceKind.UniformBuffer,
            SimpleBufferType.StructuredReadOnly => ResourceKind.StructuredBufferReadOnly,
            SimpleBufferType.StructuredReadWrite => ResourceKind.StructuredBufferReadWrite,
            _ => throw new ArgumentException($"Unsupported buffer type: {bufferType}", nameof(bufferType))
        };
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.DeviceBuffer.Dispose();
            this.ResourceLayout.Dispose();
            this.ResourceSet.Dispose();
        }
    }
}