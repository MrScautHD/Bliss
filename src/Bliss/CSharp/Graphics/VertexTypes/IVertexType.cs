using Bliss.CSharp.Graphics.Pipelines;

namespace Bliss.CSharp.Graphics.VertexTypes;

public interface IVertexType {
    
    /// <summary>
    /// Gets the vertex layout associated with the vertex type.
    /// </summary>
    VertexFormat VertexLayout { get; }
}