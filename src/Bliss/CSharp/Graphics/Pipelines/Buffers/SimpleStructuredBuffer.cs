using System.Runtime.InteropServices;
using Veldrid;

namespace Bliss.CSharp.Graphics.Pipelines.Buffers;

public class SimpleStructuredBuffer<THeader, TElement> : Disposable, ISimpleBuffer where THeader : unmanaged where TElement : unmanaged {
    
    /// <summary>
    /// Gets the graphics device associated with this buffer.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// Gets the number of header elements stored in this buffer.
    /// </summary>
    public uint HeaderDataSize { get; private set; }
    
    /// <summary>
    /// Gets the number of element entries stored in this buffer.
    /// </summary>
    public uint ElementDataSize { get; private set; }
    
    /// <summary>
    /// Gets a value indicating whether this buffer is read-only.
    /// </summary>
    public bool ReadOnly { get; private set; }
    
    /// <summary>
    /// Gets a value indicating whether this buffer is treated as a raw buffer.
    /// </summary>
    public bool RawBuffer { get; private set; }
    
    /// <summary>
    /// Gets the header data stored in this buffer.
    /// </summary>
    public THeader[] HeaderData { get; private set; }
    
    /// <summary>
    /// Gets the element data stored in this buffer.
    /// </summary>
    public TElement[] ElementData { get; private set; }
    
    /// <summary>
    /// Gets the shader stages where this buffer will be used.
    /// </summary>
    public ShaderStages ShaderStages { get; }
    
    /// <summary>
    /// Gets the GPU device buffer backing this structured buffer.
    /// </summary>
    public DeviceBuffer DeviceBuffer { get; }
    
    /// <summary>
    /// Caches resource sets keyed by their corresponding buffer layouts.
    /// </summary>
    private Dictionary<SimpleBufferLayout, ResourceSet> _cachedResourceSets;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleStructuredBuffer{THeader, TElement}"/> class
    /// with the specified device, sizes, shader stages, and usage options.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create the buffer.</param>
    /// <param name="headerDataSize">The number of header entries to allocate.</param>
    /// <param name="elementDataSize">The number of element entries to allocate.</param>
    /// <param name="stages">The shader stages where this buffer will be bound.</param>
    /// <param name="readOnly">Whether the buffer is read-only (default: true).</param>
    /// <param name="rawBuffer">Whether the buffer is treated as raw (default: false).</param>
    public SimpleStructuredBuffer(GraphicsDevice graphicsDevice, uint headerDataSize, uint elementDataSize, ShaderStages stages, bool readOnly = true, bool rawBuffer = false) {
        this.GraphicsDevice = graphicsDevice;
        this.HeaderDataSize = headerDataSize;
        this.ElementDataSize = elementDataSize;
        this.HeaderData = new THeader[headerDataSize];
        this.ElementData = new TElement[elementDataSize];
        this.ShaderStages = stages;
        this.ReadOnly = readOnly;
        this.RawBuffer = rawBuffer;
        
        uint alignment = graphicsDevice.StructuredBufferMinOffsetAlignment;
        
        long headerSize = headerDataSize * Marshal.SizeOf<THeader>();
        long elementSize = elementDataSize * Marshal.SizeOf<TElement>();
        
        long finalHeaderDataSize = (headerSize / alignment + (headerSize % alignment > 0 ? 1 : 0)) * alignment;
        long finalElementDataSize = (elementSize / alignment + (elementSize % alignment > 0 ? 1 : 0)) * alignment;
        
        long bufferSize = finalHeaderDataSize + finalElementDataSize;
        
        BufferUsage bufferUsage = readOnly ? BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic : BufferUsage.StructuredBufferReadWrite;
        
        this.DeviceBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint) bufferSize, bufferUsage) {
            StructureByteStride = (uint) Marshal.SizeOf<TElement>(),
            RawBuffer = rawBuffer
        });
        
        this._cachedResourceSets = new Dictionary<SimpleBufferLayout, ResourceSet>();
    }
    
    /// <summary>
    /// Retrieves a cached or newly created resource set for this buffer using the specified layout.
    /// </summary>
    /// <param name="layout">The buffer layout for resource binding.</param>
    /// <returns>A resource set containing this buffer bound to the given layout.</returns>
    public ResourceSet GetResourceSet(SimpleBufferLayout layout) {
        if (!this._cachedResourceSets.TryGetValue(layout, out ResourceSet? resourceSet)) {
            ResourceSet newResourceSet = this.GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(layout.Layout, this.DeviceBuffer));
            
            this._cachedResourceSets.Add(layout, newResourceSet);
            return newResourceSet;
        }
        
        return resourceSet;
    }
    
    /// <summary>
    /// Sets a header value in the CPU-side array without immediately updating the GPU buffer.
    /// </summary>
    /// <param name="index">The index of the header element.</param>
    /// <param name="value">The value to assign to the header element.</param>
    public void SetHeaderValue(int index, THeader value) {
        if (index < 0 || index >= this.HeaderDataSize) {
            throw new IndexOutOfRangeException($"Index {index} is outside the valid range of 0 to {this.HeaderDataSize - 1}.");
        }
        
        this.HeaderData[index] = value;
    }
    
    /// <summary>
    /// Sets a header value immediately in both the CPU-side array and GPU buffer.
    /// </summary>
    /// <param name="index">The index of the header element.</param>
    /// <param name="value">The value to set at the specified index.</param>
    public void SetHeaderValueImmediate(int index, THeader value) {
        this.SetHeaderValueImmediate(index, ref value);
    }
    
    /// <summary>
    /// Sets a header value immediately in both the CPU-side array and GPU buffer using a reference value.
    /// </summary>
    /// <param name="index">The index of the header element.</param>
    /// <param name="value">The reference to the value to set at the specified index.</param>
    public void SetHeaderValueImmediate(int index, ref THeader value) {
        if (index < 0 || index >= this.HeaderDataSize) {
            throw new IndexOutOfRangeException($"Index {index} is outside the valid range of 0 to {this.HeaderDataSize - 1}.");
        }
        
        this.HeaderData[index] = value;
        
        uint headerOffsetInBytes = (uint) (index * Marshal.SizeOf<THeader>());
        this.GraphicsDevice.UpdateBuffer(this.DeviceBuffer, headerOffsetInBytes, ref this.HeaderData[index]);
    }
    
    /// <summary>
    /// Sets a header value and schedules it to be uploaded to the GPU using a <see cref="CommandList"/>.
    /// </summary>
    /// <param name="commandList">The command list used to defer the buffer update.</param>
    /// <param name="index">The index of the header element.</param>
    /// <param name="value">The value to assign to the header element.</param>
    public void SetHeaderValueDeferred(CommandList commandList, int index, THeader value) {
        this.SetHeaderValueDeferred(commandList, index, ref value);
    }
    
    /// <summary>
    /// Sets a header value and schedules it to be uploaded to the GPU using a <see cref="CommandList"/> with a reference value.
    /// </summary>
    /// <param name="commandList">The command list used to defer the buffer update.</param>
    /// <param name="index">The index of the header element.</param>
    /// <param name="value">The reference to the value to assign to the header element.</param>
    public void SetHeaderValueDeferred(CommandList commandList, int index, ref THeader value) {
        if (index < 0 || index >= this.HeaderDataSize) {
            throw new IndexOutOfRangeException($"Index {index} is outside the valid range of 0 to {this.HeaderDataSize - 1}.");
        }
        
        this.HeaderData[index] = value;
        
        uint headerOffsetInBytes = (uint) (index * Marshal.SizeOf<THeader>());
        commandList.UpdateBuffer(this.DeviceBuffer, headerOffsetInBytes, ref this.HeaderData[index]);
    }
    
    /// <summary>
    /// Sets an element value in the CPU-side array without immediately updating the GPU buffer.
    /// </summary>
    /// <param name="index">The index of the element.</param>
    /// <param name="value">The value to assign to the element.</param>
    public void SetElementValue(int index, TElement value) {
        if (index < 0 || index >= this.ElementDataSize) {
            throw new IndexOutOfRangeException($"Index {index} is outside the valid range of 0 to {this.ElementDataSize - 1}.");
        }
        
        this.ElementData[index] = value;
    }
    
    /// <summary>
    /// Sets an element value immediately in both the CPU-side array and GPU buffer.
    /// </summary>
    /// <param name="index">The index of the element.</param>
    /// <param name="value">The value to assign to the element.</param>
    public void SetElementValueImmediate(int index, TElement value) {
        this.SetElementValueImmediate(index, ref value);
    }
    
    /// <summary>
    /// Sets an element value immediately in both the CPU-side array and GPU buffer using a reference value.
    /// </summary>
    /// <param name="index">The index of the element.</param>
    /// <param name="value">The reference to the value to assign to the element.</param>
    public void SetElementValueImmediate(int index, ref TElement value) {
        if (index < 0 || index >= this.ElementDataSize) {
            throw new IndexOutOfRangeException($"Index {index} is outside the valid range of 0 to {this.ElementDataSize - 1}.");
        }
        
        this.ElementData[index] = value;
        
        uint headerSizeInBytes = (uint) (this.ElementDataSize * Marshal.SizeOf<THeader>());
        uint elementOffsetInBytes = (uint) (index * Marshal.SizeOf<TElement>());
        uint totalBufferOffsetInBytes = headerSizeInBytes + elementOffsetInBytes;
        this.GraphicsDevice.UpdateBuffer(this.DeviceBuffer, totalBufferOffsetInBytes, ref this.ElementData[index]);
    }
    
    /// <summary>
    /// Sets an element value and schedules it to be uploaded to the GPU using a <see cref="CommandList"/>.
    /// </summary>
    /// <param name="commandList">The command list used to defer the buffer update.</param>
    /// <param name="index">The index of the element.</param>
    /// <param name="value">The value to assign to the element.</param>
    public void SetElementValueDeferred(CommandList commandList, int index, TElement value) {
        this.SetElementValueDeferred(commandList, index, ref value);
    }
    
    /// <summary>
    /// Sets an element value and schedules it to be uploaded to the GPU using a <see cref="CommandList"/> with a reference value.
    /// </summary>
    /// <param name="commandList">The command list used to defer the buffer update.</param>
    /// <param name="index">The index of the element.</param>
    /// <param name="value">The reference to the value to assign to the element.</param>
    public void SetElementValueDeferred(CommandList commandList, int index, ref TElement value) {
        if (index < 0 || index >= this.ElementDataSize) {
            throw new IndexOutOfRangeException($"Index {index} is outside the valid range of 0 to {this.ElementDataSize - 1}.");
        }
        
        this.ElementData[index] = value;
        
        uint headerSizeInBytes = (uint) (this.ElementDataSize * Marshal.SizeOf<THeader>());
        uint elementOffsetInBytes = (uint) (index * Marshal.SizeOf<TElement>());
        uint totalBufferOffsetInBytes = headerSizeInBytes + elementOffsetInBytes;
        commandList.UpdateBuffer(this.DeviceBuffer, totalBufferOffsetInBytes, ref this.ElementData[index]);
    }
    
    /// <summary>
    /// Updates the entire buffer immediately on the GPU with both header and element data.
    /// </summary>
    public void UpdateBufferImmediate() {
        
        // Update header data.
        this.GraphicsDevice.UpdateBuffer(this.DeviceBuffer, 0, this.HeaderData);
        
        // Update element data.
        uint headerSizeInBytes = (uint) (this.HeaderDataSize * Marshal.SizeOf<THeader>());
        this.GraphicsDevice.UpdateBuffer(this.DeviceBuffer, headerSizeInBytes, this.ElementData);
    }
    
    /// <summary>
    /// Updates the entire buffer deferred using a <see cref="CommandList"/> with both header and element data.
    /// </summary>
    /// <param name="commandList">The command list used to defer the buffer update.</param>
    public void UpdateBufferDeferred(CommandList commandList) {
        
        // Update header data.
        commandList.UpdateBuffer(this.DeviceBuffer, 0, this.HeaderData);
        
        // Update element data.
        uint headerSizeInBytes = (uint) (this.HeaderDataSize * Marshal.SizeOf<THeader>());
        commandList.UpdateBuffer(this.DeviceBuffer, headerSizeInBytes, this.ElementData);
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