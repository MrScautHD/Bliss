using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Materials;
using Veldrid;

namespace Bliss.CSharp.Geometry.Meshes;

public interface IMesh : IDisposable {
    
    /// <summary>
    /// The maximum number of bones supported by a skinned mesh.
    /// </summary>
    public const int MaxBoneCount = 256;
    
    /// <summary>
    /// Gets the graphics device used by this mesh for creating and updating GPU resources.
    /// </summary>
    GraphicsDevice GraphicsDevice { get; }
    
    /// <summary>
    /// Gets or sets the material used to render this mesh.
    /// </summary>
    Material Material { get; set; }
    
    /// <summary>
    /// Gets the vertex format describing the layout of this mesh's vertex data.
    /// </summary>
    VertexFormat VertexFormat { get; }
    
    /// <summary>
    /// Gets the number of vertices contained in this mesh.
    /// </summary>
    uint VertexCount { get; }
    
    /// <summary>
    /// Gets the number of indices contained in this mesh.
    /// </summary>
    uint IndexCount { get; }
    
    /// <summary>
    /// The number of bones influencing the skinned mesh.
    /// </summary>
    uint BoneCount { get; }
    
    /// <summary>
    /// Gets a value indicating whether this mesh uses skinning data.
    /// </summary>
    bool IsSkinned { get; }
    
    /// <summary>
    /// Gets the GPU vertex buffer associated with this mesh.
    /// </summary>
    DeviceBuffer VertexBuffer { get; }
    
    /// <summary>
    /// Gets the GPU index buffer associated with this mesh, if one exists.
    /// </summary>
    DeviceBuffer? IndexBuffer { get; }
    
    /// <summary>
    /// Generates a bounding box that encloses the mesh geometry.
    /// </summary>
    /// <returns>A bounding box covering the mesh's vertices.</returns>
    BoundingBox GenBoundingBox();
    
    /// <summary>
    /// Generates tangent vectors for the mesh vertices.
    /// </summary>
    void GenTangents();
}