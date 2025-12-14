using Veldrid;

namespace Bliss.CSharp.Graphics.Pipelines.Buffers;

public interface ISimpleBuffer : IDisposable {
    
    /// <summary>
    /// Gets the shader stages where this buffer is used.
    /// </summary>
    ShaderStages ShaderStages { get; }
    
    /// <summary>
    /// Gets the GPU device buffer associated with this simple buffer.
    /// </summary>
    DeviceBuffer DeviceBuffer { get; }
    
    /// <summary>
    /// Retrieves a resource set for this buffer using the specified layout.
    /// </summary>
    /// <param name="layout">The layout defining how the buffer is bound to the shader.</param>
    /// <returns>A <see cref="ResourceSet"/> containing this buffer bound according to the given layout.</returns>
    ResourceSet GetResourceSet(SimpleBufferLayout layout);
}