/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

using System.Runtime.InteropServices;
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
        
        uint alignment = graphicsDevice.UniformBufferMinOffsetAlignment;
        long dataSize = size * Marshal.SizeOf<T>();
        long bufferSize = (dataSize / alignment + (dataSize % alignment > 0 ? 1 : 0)) * alignment;
        
        this.DeviceBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint) bufferSize, this.GetBufferUsage(bufferType)));
        this.DeviceBuffer.Name = name;
        
        this.ResourceLayout = graphicsDevice.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription(name, this.GetResourceKind(bufferType), stages)));
        this.ResourceSet = graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(this.ResourceLayout, this.DeviceBuffer));
    }

    /// <summary>
    /// Sets the value at the specified index in the buffer.
    /// </summary>
    /// <param name="index">The index in the buffer where the value should be set.</param>
    /// <param name="value">The value to set in the buffer.</param>
    public void SetValue(int index, T value) {
        this.Data[index] = value;
    }

    /// <summary>
    /// Sets the value of the buffer at the specified index and updates the buffer on the graphics device immediately.
    /// </summary>
    /// <param name="index">The index at which the value should be set.</param>
    /// <param name="value">The value to set at the specified index.</param>
    public void SetValueImmediate(int index, T value) {
        this.Data[index] = value;
        this.GraphicsDevice.UpdateBuffer(this.DeviceBuffer, (uint) (index * Marshal.SizeOf<T>()), this.Data[index]);
    }

    /// <summary>
    /// Sets the value at the specified index in the buffer and schedules a command to update the buffer on the command list.
    /// </summary>
    /// <param name="commandList">The command list to which the buffer update command will be added.</param>
    /// <param name="index">The index of the buffer element to set.</param>
    /// <param name="value">The value to set at the specified index.</param>
    public void SetValueDeferred(CommandList commandList, int index, T value) {
        this.Data[index] = value;
        commandList.UpdateBuffer(this.DeviceBuffer, (uint) (index * Marshal.SizeOf<T>()), this.Data[index]);
    }

    /// <summary>
    /// Immediately updates the GPU buffer with the current data held in the CPU buffer.
    /// </summary>
    public void UpdateBufferImmediate() {
        this.GraphicsDevice.UpdateBuffer(this.DeviceBuffer, 0, this.Data);
    }

    /// <summary>
    /// Updates the contents of the device buffer with the current data.
    /// </summary>
    /// <param name="commandList">The command list used to record the buffer update command.</param>
    public void UpdateBuffer(CommandList commandList) {
        commandList.UpdateBuffer(this.DeviceBuffer, 0, this.Data);
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