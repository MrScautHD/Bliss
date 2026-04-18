using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.VertexTypes;
using Veldrid;

namespace Bliss.CSharp.Geometry.Meshes.Data;

public interface IMeshData<T> where T : unmanaged, IVertexType {
    
    /// <summary>
    /// Gets the number of vertices in the mesh data.
    /// </summary>
    uint VertexCount { get; }
    
    /// <summary>
    /// Gets the number of indices in the mesh data.
    /// </summary>
    uint IndexCount { get; }
    
    /// <summary>
    /// Gets the vertex array used by the mesh data.
    /// </summary>
    T[] Vertices { get; }
    
    /// <summary>
    /// Gets the index array used by the mesh data.
    /// </summary>
    uint[] Indices { get; }
    
    /// <summary>
    /// Gets or sets the vertex format describing the layout of the mesh vertices.
    /// </summary>
    VertexFormat VertexFormat { get; set; }
    
    /// <summary>
    /// Creates a GPU vertex buffer from the mesh data.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create the buffer.</param>
    /// <returns>A GPU buffer containing the vertex data.</returns>
    DeviceBuffer CreateVertexBuffer(GraphicsDevice graphicsDevice);
    
    /// <summary>
    /// Creates a GPU index buffer from the mesh data.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create the buffer.</param>
    /// <returns>A GPU buffer containing the index data.</returns>
    DeviceBuffer CreateIndexBuffer(GraphicsDevice graphicsDevice);
    
    /// <summary>
    /// Generates a bounding box that encloses the mesh data.
    /// </summary>
    /// <returns>A bounding box covering the mesh vertices.</returns>
    BoundingBox GenBoundingBox();
    
    /// <summary>
    /// Generates tangent vectors for the mesh vertices using the vertex positions and texture coordinates.
    /// </summary>
    void GenTangents();
}