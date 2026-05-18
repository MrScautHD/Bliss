using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.VertexTypes;
using Veldrid;

namespace Bliss.CSharp.Geometry.Meshes.Data;

public class BasicMeshData : IMeshData<Vertex3D> {
    
    /// <summary>
    /// Gets the number of vertices stored in this mesh data.
    /// </summary>
    public uint VertexCount => (uint) this.Vertices.Length;
    
    /// <summary>
    /// Gets the number of indices stored in this mesh data.
    /// </summary>
    public uint IndexCount => (uint) this.Indices.Length;
    
    /// <summary>
    /// Gets or sets the vertex array used by this mesh data.
    /// </summary>
    public Vertex3D[] Vertices { get; private set; }
    
    /// <summary>
    /// Gets or sets the index array used by this mesh data.
    /// </summary>
    public uint[] Indices { get; private set; }
    
    /// <summary>
    /// Gets the number of bones used in this mesh data.
    /// </summary>
    public uint BoneCount => 0;

    /// <summary>
    /// Gets or sets the vertex format describing the layout of the mesh vertices.
    /// </summary>
    public VertexFormat VertexFormat { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BasicMeshData"/> class with the specified vertices, indices, and optional vertex format.
    /// </summary>
    /// <param name="vertices">The vertex array that defines the mesh geometry.</param>
    /// <param name="indices">The index array that defines how vertices are connected into primitives.</param>
    /// <param name="vertexFormat">The vertex format describing the layout of the vertex data. If not specified, <see cref="Vertex3D.VertexLayout"/> is used.</param>
    public BasicMeshData(Vertex3D[] vertices, uint[] indices, VertexFormat? vertexFormat = null) {
        this.Vertices = vertices;
        this.Indices = indices;
        this.VertexFormat = vertexFormat ?? Vertex3D.VertexLayout;
    }
    
    /// <summary>
    /// Creates and uploads a vertex buffer containing this mesh's vertex data.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create and upload the buffer.</param>
    /// <returns>A GPU buffer containing the mesh vertices.</returns>
    public DeviceBuffer CreateVertexBuffer(GraphicsDevice graphicsDevice) {
        uint bufferSize = this.VertexCount * (uint) Marshal.SizeOf<Vertex3D>();
        DeviceBuffer buffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(bufferSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        graphicsDevice.UpdateBuffer(buffer, 0, this.Vertices);
        
        return buffer;
    }
    
    /// <summary>
    /// Creates and uploads an index buffer containing this mesh's index data.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create and upload the buffer.</param>
    /// <returns>A GPU buffer containing the mesh indices.</returns>
    public DeviceBuffer CreateIndexBuffer(GraphicsDevice graphicsDevice) {
        uint bufferSize = this.IndexCount * sizeof(uint);
        DeviceBuffer buffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(bufferSize, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        graphicsDevice.UpdateBuffer(buffer, 0, this.Indices);
        
        return buffer;
    }
    
    /// <summary>
    /// Generates an axis-aligned bounding box that encloses all vertices in the mesh.
    /// </summary>
    /// <returns>A bounding box covering the full mesh extent.</returns>
    public BoundingBox GenBoundingBox() {
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        
        foreach (Vertex3D vertex in this.Vertices) {
            min = Vector3.Min(min, vertex.Position);
            max = Vector3.Max(max, vertex.Position);
        }
        
        return new BoundingBox(min, max);
    }
    
    /// <summary>
    /// Generates tangent vectors for the mesh vertices using the vertex positions and texture coordinates.
    /// </summary>
    public void GenTangents() {
        if (this.Vertices.Length < 3 || this.Indices.Length < 3) {
            return;
        }
        
        Vector3[] tan1 = new Vector3[this.Vertices.Length];
        Vector3[] tan2 = new Vector3[this.Vertices.Length];
        
        for (int i = 0; i < this.Indices.Length; i += 3) {
            int i1 = (int) this.Indices[i];
            int i2 = (int) this.Indices[i + 1];
            int i3 = (int) this.Indices[i + 2];
            
            Vertex3D v1 = this.Vertices[i1];
            Vertex3D v2 = this.Vertices[i2];
            Vertex3D v3 = this.Vertices[i3];
            
            Vector3 p1 = v1.Position;
            Vector3 p2 = v2.Position;
            Vector3 p3 = v3.Position;
            
            Vector3 w1 = new Vector3(v1.TexCoords.X, v1.TexCoords.Y, 1.0F);
            Vector3 w2 = new Vector3(v2.TexCoords.X, v2.TexCoords.Y, 1.0F);
            
            Vector3 q1 = p2 - p1;
            Vector3 q2 = p3 - p1;
            
            Vector3 sdir = new Vector3(
                w2.Y * q1.X - w1.Y * q2.X,
                w2.Y * q1.Y - w1.Y * q2.Y,
                w2.Y * q1.Z - w1.Y * q2.Z
            );
            
            Vector3 tdir = new Vector3(
                w1.X * q2.X - w2.X * q1.X,
                w1.X * q2.Y - w2.X * q1.Y,
                w1.X * q2.Z - w2.X * q1.Z
            );
            
            tan1[i1] += sdir;
            tan1[i2] += sdir;
            tan1[i3] += sdir;
            
            tan2[i1] += tdir;
            tan2[i2] += tdir;
            tan2[i3] += tdir;
        }
        
        for (int i = 0; i < this.Vertices.Length; i++) {
            Vertex3D vertex = this.Vertices[i];
            Vector3 n = vertex.Normal;
            Vector3 t = tan1[i];
            Vector3 b = tan2[i];
            
            float sign = Vector3.Dot(Vector3.Cross(n, t), b) > 0.0F ? 1.0F : -1.0F;
            
            Vector4 tangent = new Vector4(Vector3.Normalize(t - n * Vector3.Dot(n, t)), sign);
            this.Vertices[i].Tangent = tangent;
        }
    }
}