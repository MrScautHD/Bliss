using Veldrid;

namespace Bliss.CSharp.Graphics.Pipelines.Buffers;

public interface ISimpleBuffer : IDisposable {
    
    /// <summary>
    /// The name of the buffer.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The device buffer associated with this instance.
    /// </summary>
    public DeviceBuffer DeviceBuffer  { get; }

    /// <summary>
    /// The layout of the resource in the graphics pipeline.
    /// </summary>
    public ResourceLayout ResourceLayout { get; }

    /// <summary>
    /// The set of resources associated with this buffer, typically used for binding resources to shaders.
    /// </summary>
    public ResourceSet ResourceSet { get; }
}