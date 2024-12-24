using Veldrid;

namespace Bliss.CSharp.Graphics.Pipelines.Buffers;

public class SimpleBufferLayout : Disposable {

    /// <summary>
    /// The graphics device used to create the layout and associated resources.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// The name assigned to the buffer layout.
    /// </summary>
    public string Name { get; private set; }
    
    /// <summary>
    /// The type of the buffer, determining its usage and binding.
    /// </summary>
    public SimpleBufferType BufferType { get; private set; }
    
    /// <summary>
    /// The shader stages where this buffer layout is accessible.
    /// </summary>
    public ShaderStages ShaderStages { get; private set; }
    
    /// <summary>
    /// The resource layout representing the buffer's structure in the graphics pipeline.
    /// </summary>
    public ResourceLayout Layout { get; private set; }
    
    /// <summary>
    /// Creates a new instance of the <see cref="SimpleBufferLayout"/> class with the specified parameters.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for resource allocation.</param>
    /// <param name="name">The name of the buffer layout.</param>
    /// <param name="bufferType">The type of buffer (e.g., Uniform, Structured).</param>
    /// <param name="stages">The shader stages where this layout will be used.</param>
    public SimpleBufferLayout(GraphicsDevice graphicsDevice, string name, SimpleBufferType bufferType, ShaderStages stages) {
        this.GraphicsDevice = graphicsDevice;
        this.Name = name;
        this.BufferType = bufferType;
        this.ShaderStages = stages;
        
        this.Layout = this.GraphicsDevice.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription() {
            Elements = [
                new ResourceLayoutElementDescription(name, this.GetResourceKind(bufferType), stages)
            ]
        });
    }
    
    /// <summary>
    /// Determines the appropriate resource kind for the specified buffer type.
    /// </summary>
    /// <param name="bufferType">The buffer type to map to a resource kind.</param>
    /// <returns>The corresponding <see cref="ResourceKind"/>.</returns>
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
            this.Layout.Dispose();
        }
    }
}