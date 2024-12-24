using Veldrid;

namespace Bliss.CSharp.Graphics.Pipelines.Buffers;

public interface ISimpleBuffer : IDisposable {
    
    /// <summary>
    /// The device buffer associated with this instance.
    /// </summary>
    public DeviceBuffer DeviceBuffer  { get; }
}