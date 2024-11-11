using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Materials;
using Bliss.CSharp.Transformations;
using Veldrid;

namespace Bliss.CSharp.Geometry;

public class Mesh : Disposable {
    
    /// <summary>
    /// A dictionary that caches instances of SimplePipeline based on Material keys.
    /// This helps in reusing pipeline configurations for materials, enhancing rendering performance and reducing redundant pipeline creation.
    /// </summary>
    private static Dictionary<Material, SimplePipeline> _cachedPipelines = new();

    /// <summary>
    /// Represents the graphics device used for rendering operations.
    /// This property provides access to the underlying GraphicsDevice instance responsible for managing GPU resources and executing rendering commands.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }

    /// <summary>
    /// Represents the material properties used for rendering the mesh.
    /// This may include shaders (effects), texture mappings, blending states, and other rendering parameters.
    /// The Material controls how the mesh is rendered within the graphics pipeline.
    /// </summary>
    public Material Material { get; private set; }

    /// <summary>
    /// An array of Vertex3D structures that define the geometric points of a mesh.
    /// Each vertex contains attributes such as position, texture coordinates, normal, and optional color.
    /// Vertices are used to construct the shape and appearance of a 3D model.
    /// </summary>
    public Vertex3D[] Vertices { get; private set; }

    /// <summary>
    /// An array of indices that define the order in which vertices are drawn.
    /// Indices are used in conjunction with the vertex array to form geometric shapes
    /// such as triangles in a mesh. This allows for efficient reuse of vertex data.
    /// </summary>
    public uint[] Indices { get; private set; }

    /// <summary>
    /// The axis-aligned bounding box (AABB) for the mesh.
    /// This bounding box is calculated based on the vertices of the mesh and represents
    /// the minimum and maximum coordinates that encompass the entire mesh.
    /// </summary>
    public BoundingBox BoundingBox { get; private set; }

    /// <summary>
    /// The total count of vertices present in the mesh.
    /// This value determines the number of vertices available for rendering within the mesh.
    /// </summary>
    public uint VertexCount { get; private set; }

    /// <summary>
    /// The number of indices in the mesh used for rendering.
    /// </summary>
    public uint IndexCount { get; private set; }

    /// <summary>
    /// A buffer that stores vertex data used for rendering in the graphics pipeline.
    /// </summary>
    private DeviceBuffer _vertexBuffer;

    /// <summary>
    /// A buffer that stores index data used for indexed drawing in the graphics pipeline.
    /// </summary>
    private DeviceBuffer _indexBuffer;

    /// <summary>
    /// A buffer that stores model matrix data for shader usage in rendering.
    /// </summary>
    private SimpleBuffer<Matrix4x4> _modelMatrixBuffer;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Mesh"/> class with the specified graphics device, material, vertices, and indices.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create buffers and manage resources.</param>
    /// <param name="material">The material associated with the mesh, defining its visual properties.</param>
    /// <param name="vertices">The optional array of vertices defining the mesh geometry.</param>
    /// <param name="indices">The optional array of indices defining the order of vertex rendering.</param>
    public Mesh(GraphicsDevice graphicsDevice, Material material, Vertex3D[]? vertices = default, uint[]? indices = default) {
        this.GraphicsDevice = graphicsDevice;
        this.Material = material;
        this.Vertices = vertices ?? [];
        this.Indices = indices ?? [];
        this.BoundingBox = this.GenerateBoundingBox();

        this.VertexCount = (uint) this.Vertices.Length;
        this.IndexCount = (uint) this.Indices.Length;
        
        uint vertexBufferSize = this.VertexCount * (uint) Marshal.SizeOf<Vertex3D>();
        uint indexBufferSize = this.IndexCount * 4;
        
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        graphicsDevice.UpdateBuffer(this._vertexBuffer, 0, vertices);

        this._indexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(indexBufferSize, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        graphicsDevice.UpdateBuffer(this._indexBuffer, 0, indices);

        this._modelMatrixBuffer = new SimpleBuffer<Matrix4x4>(graphicsDevice, "MatrixBuffer", 3, SimpleBufferType.Uniform, ShaderStages.Vertex);
    }

    // TODO: Take care of color!!!!!
    /// <summary>
    /// Renders the mesh using the specified command list, output description, transformation, and optional color.
    /// </summary>
    /// <param name="commandList">The command list used for issuing rendering commands.</param>
    /// <param name="output">The output description for the rendering pipeline.</param>
    /// <param name="transform">The transformation to apply to the mesh.</param>
    /// <param name="color">Optional color parameter for coloring the mesh.</param>
    public void Draw(CommandList commandList, OutputDescription output, Transform transform, Color? color = default) {
        Cam3D? cam3D = Cam3D.ActiveCamera;

        if (cam3D == null) {
            return;
        }
        
        // Update matrix buffer.
        this._modelMatrixBuffer.SetValue(0, cam3D.GetProjection());
        this._modelMatrixBuffer.SetValue(1, cam3D.GetView());
        this._modelMatrixBuffer.SetValue(2, transform.GetTransform());
        this._modelMatrixBuffer.UpdateBuffer(commandList);
        
        if (this.IndexCount > 0) {
            
            // Set vertex and index buffer.
            commandList.SetVertexBuffer(0, this._vertexBuffer);
            commandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt32);
            
            // Set pipeline.
            commandList.SetPipeline(this.GetOrCreatePipeline(this.Material, output).Pipeline);
            
            // Set projection view buffer.
            commandList.SetGraphicsResourceSet(0, this._modelMatrixBuffer.ResourceSet);
            
            // Set material.
            for (int i = 0; i < 11; i++) {
                MaterialMapType mapType = (MaterialMapType) i;
                ResourceSet? resourceSet = this.Material.GetResourceSet(this.Material.TextureLayouts[i].Layout, mapType);

                if (resourceSet != null) {
                    commandList.SetGraphicsResourceSet((uint) i + 1, resourceSet);
                }
            }
            
            // Draw.
            commandList.DrawIndexed(this.IndexCount, 1, 0, 0, 0);
        }
        else {
            
            // Set vertex buffer.
            commandList.SetVertexBuffer(0, this._vertexBuffer);
            
            // Set pipeline.
            commandList.SetPipeline(this.GetOrCreatePipeline(this.Material, output).Pipeline);
            
            // Set projection view buffer.
            commandList.SetGraphicsResourceSet(0, this._modelMatrixBuffer.ResourceSet);
            
            // Draw.
            commandList.Draw(this.VertexCount);
        }
    }

    /// <summary>
    /// Retrieves an existing pipeline associated with the given material and output description, or creates a new one if it doesn't exist.
    /// </summary>
    /// <param name="material">The material associated with the pipeline.</param>
    /// <param name="output">The output description for the pipeline.</param>
    /// <returns>A pipeline that matches the specified material and output description.</returns>
    private SimplePipeline GetOrCreatePipeline(Material material, OutputDescription output) {
        if (!_cachedPipelines.TryGetValue(material, out SimplePipeline? pipeline)) {
            SimplePipeline newPipeline = new SimplePipeline(this.GraphicsDevice, new SimplePipelineDescription() {
                BlendState = material.BlendState.Description,
                DepthStencilState = new DepthStencilStateDescription(true, true, ComparisonKind.LessEqual),
                RasterizerState = new RasterizerStateDescription() {
                    CullMode = FaceCullMode.Back,
                    FillMode = PolygonFillMode.Solid,
                    FrontFace = FrontFace.Clockwise,
                    DepthClipEnabled = true,
                    ScissorTestEnabled = false
                },
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                Buffers = [
                    this._modelMatrixBuffer
                ],
                TextureLayouts = material.TextureLayouts,
                ShaderSet = new ShaderSetDescription() {
                    VertexLayouts = [
                        material.Effect.VertexLayout
                    ],
                    Shaders = [
                        material.Effect.Shader.Item1,
                        material.Effect.Shader.Item2
                    ]
                },
                Outputs = output
            });
            
            _cachedPipelines.Add(material, newPipeline);
            return newPipeline;
        }

        return pipeline;
    }

    /// <summary>
    /// Calculates the bounding box for the current mesh based on its vertices.
    /// </summary>
    /// <returns>A BoundingBox object that encompasses all vertices of the mesh.</returns>
    private BoundingBox GenerateBoundingBox() {
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (Vertex3D vertex in this.Vertices) {
            min = Vector3.Min(min, vertex.Position);
            max = Vector3.Max(max, vertex.Position);
        }

        return new BoundingBox(min, max);
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._vertexBuffer.Dispose();
            this._indexBuffer.Dispose();
            this._modelMatrixBuffer.Dispose();
        }
    }
}