using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Geometry.Animations;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Materials;
using Bliss.CSharp.Transformations;
using Veldrid;
using Material = Bliss.CSharp.Materials.Material;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace Bliss.CSharp.Geometry;

public class Mesh : Disposable {
    
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
    /// An array containing information about each bone in a mesh, used for skeletal animation.
    /// Each element provides details such as the bone's name, its identifier, and its transformation matrix.
    /// </summary>
    public Dictionary<string, Dictionary<int, BoneInfo[]>> BoneInfos { get; private set; }

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
    /// A buffer that stores bone transformation data used for skeletal animation.
    /// </summary>
    private SimpleBuffer<Matrix4x4> _boneBuffer;

    /// <summary>
    /// A buffer that stores color data as a collection of 4D vectors (Vector4) for rendering purposes.
    /// </summary>
    private SimpleBuffer<Vector4> _colorBuffer;

    /// <summary>
    /// Represents a GPU buffer that stores floating-point values for use in rendering or animation systems.
    /// </summary>
    private SimpleBuffer<float> _valueBuffer;
    
    /// <summary>
    /// Defines the characteristics of the rendering pipeline used by the mesh.
    /// This field specifies the pipeline configurations such as blending, depth stencil, rasterizer state,
    /// primitive topology, associated buffers, texture layouts, shader set, and output descriptions.
    /// </summary>
    private SimplePipelineDescription _pipelineDescription;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Mesh"/> class with the specified properties.
    /// </summary>
    /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/> used for managing GPU resources.</param>
    /// <param name="material">The material applied to the mesh, which defines its appearance (textures, shaders, etc.).</param>
    /// <param name="vertices">An optional array of <see cref="Vertex3D"/> instances representing the mesh's vertices.</param>
    /// <param name="indices">An optional array of indices used for defining the mesh's primitive topology.</param>
    /// <param name="boneInfos">An optional dictionary containing bone information used for skeletal animation, where the key is a bone name and the value is a dictionary of bone indices mapped to bone data.</param>
    public Mesh(GraphicsDevice graphicsDevice, Material material, Vertex3D[]? vertices = default, uint[]? indices = default, Dictionary<string, Dictionary<int, BoneInfo[]>>? boneInfos = default) {
        this.GraphicsDevice = graphicsDevice;
        this.Material = material;
        this.Vertices = vertices ?? [];
        this.Indices = indices ?? [];
        this.BoneInfos = boneInfos ?? new Dictionary<string, Dictionary<int, BoneInfo[]>>();
        this.BoundingBox = this.GenerateBoundingBox();

        this.VertexCount = (uint) this.Vertices.Length;
        this.IndexCount = (uint) this.Indices.Length;
        
        uint vertexBufferSize = this.VertexCount * (uint) Marshal.SizeOf<Vertex3D>();
        uint indexBufferSize = this.IndexCount * 4;
        
        // Create vertex buffer.
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        graphicsDevice.UpdateBuffer(this._vertexBuffer, 0, this.Vertices);

        // Create index buffer.
        this._indexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(indexBufferSize, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        graphicsDevice.UpdateBuffer(this._indexBuffer, 0, this.Indices);
        
        // Create model matrix buffer.
        this._modelMatrixBuffer = new SimpleBuffer<Matrix4x4>(graphicsDevice, 3, SimpleBufferType.Uniform, ShaderStages.Vertex);
        
        // Create bone buffer.
        this._boneBuffer = new SimpleBuffer<Matrix4x4>(graphicsDevice, 128, SimpleBufferType.Uniform, ShaderStages.Vertex);

        for (int i = 0; i < 128; i++) {
            this._boneBuffer.SetValue(i, Matrix4x4.Identity);
        }
        
        this._boneBuffer.UpdateBufferImmediate();
        
        // Create color buffer.
        this._colorBuffer = new SimpleBuffer<Vector4>(graphicsDevice, 7, SimpleBufferType.Uniform, ShaderStages.Fragment);

        for (int i = 0; i < 7; i++) {
            this._colorBuffer.SetValue(i, Color.White.ToRgbaFloatVec4());
        }
        
        this._colorBuffer.UpdateBufferImmediate();
        
        // Create value buffer.
        this._valueBuffer = new SimpleBuffer<float>(graphicsDevice, 7, SimpleBufferType.Uniform, ShaderStages.Fragment);

        // Create pipeline description.
        this._pipelineDescription = this.CreatePipelineDescription();
    }

    /// <summary>
    /// Updates the transformation matrices of the animation bones for a specific frame using the provided command list and animation data.
    /// </summary>
    /// <param name="commandList">The command list used to issue rendering commands.</param>
    /// <param name="animation">The animation data containing the bone transformations.</param>
    /// <param name="frame">The specific frame of the animation to update the bone transformations.</param>
    public void UpdateAnimationBones(CommandList commandList, ModelAnimation animation, int frame) {
        if (this.BoneInfos.Count > 0) {
            for (int boneId = 0; boneId < this.BoneInfos[animation.Name][frame].Length; boneId++) {
                this._boneBuffer.SetValue(boneId, this.BoneInfos[animation.Name][frame][boneId].Transformation);
            }
            
            this._boneBuffer.UpdateBuffer(commandList);
        }
    }

    /// <summary>
    /// Resets the bone transformation matrices to their identity state and updates the buffer on the GPU using the provided command list.
    /// </summary>
    /// <param name="commandList">The command list used to record the buffer update command, ensuring the changes are applied to the GPU.</param>
    public void ResetAnimationBones(CommandList commandList) {
        for (int i = 0; i < 128; i++) {
            this._boneBuffer.SetValue(i, Matrix4x4.Identity);
        }
        
        this._boneBuffer.UpdateBuffer(commandList);
    }
    
    /// <summary>
    /// Renders the mesh using the specified command list, output description, transformation, and optional color.
    /// </summary>
    /// <param name="commandList">The command list used for issuing rendering commands.</param>
    /// <param name="output">The output description for the rendering pipeline.</param>
    /// <param name="transform">The transformation to apply to the mesh.</param>
    /// <param name="color">Optional color parameter for coloring the mesh.</param>
    public void Draw(CommandList commandList, OutputDescription output, Transform transform, Color? color = null) {
        Cam3D? cam3D = Cam3D.ActiveCamera;

        if (cam3D == null) {
            return;
        }
        
        // Set optional color.
        Color cachedColor = this.Material.GetMapColor(MaterialMapType.Albedo.GetName()) ?? Color.White;
        this.Material.SetMapColor(MaterialMapType.Albedo.GetName(), color ?? cachedColor);
        
        // Update matrix buffer.
        this._modelMatrixBuffer.SetValue(0, cam3D.GetProjection());
        this._modelMatrixBuffer.SetValue(1, cam3D.GetView());
        this._modelMatrixBuffer.SetValue(2, transform.GetTransform());
        this._modelMatrixBuffer.UpdateBuffer(commandList);
        
        // Update color buffer.
        for (int i = 0; i < 7; i++) {
            Color? mapColor = this.Material.GetMapColor(((MaterialMapType) i).GetName());

            if (mapColor.HasValue) {
                this._colorBuffer.SetValue(i, mapColor.Value.ToRgbaFloatVec4());
            }
        }

        this._colorBuffer.UpdateBuffer(commandList);
        
        // Update value buffer.
        for (int i = 0; i < 7; i++) {
            this._valueBuffer.SetValue(i, this.Material.GetMapValue(((MaterialMapType) i).GetName()));
        }
        
        this._valueBuffer.UpdateBuffer(commandList);

        // Update pipeline description.
        this._pipelineDescription.BlendState = this.Material.BlendState.Description;
        this._pipelineDescription.Outputs = output;
        
        if (this.IndexCount > 0) {
            
            // Set vertex and index buffer.
            commandList.SetVertexBuffer(0, this._vertexBuffer);
            commandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt32);
            
            // Set pipeline.
            commandList.SetPipeline(this.Material.Effect.GetPipeline(this._pipelineDescription).Pipeline);
            
            // Set projection view buffer.
            commandList.SetGraphicsResourceSet(0, this._modelMatrixBuffer.GetResourceSet(this.Material.Effect.GetBufferLayout("MatrixBuffer")));
            
            // Set bone buffer.
            commandList.SetGraphicsResourceSet(1, this._boneBuffer.GetResourceSet(this.Material.Effect.GetBufferLayout("BoneBuffer")));
            
            // Set color buffer.
            commandList.SetGraphicsResourceSet(2, this._colorBuffer.GetResourceSet(this.Material.Effect.GetBufferLayout("ColorBuffer")));
            
            // Set value buffer.
            commandList.SetGraphicsResourceSet(3, this._valueBuffer.GetResourceSet(this.Material.Effect.GetBufferLayout("ValueBuffer")));
            
            // Set material texture.
            for (int i = 0; i < this.Material.Effect.GetTextureLayoutKeys().Length; i++) {
                string key = this.Material.Effect.GetTextureLayoutKeys()[i];
                ResourceSet? resourceSet = this.Material.GetResourceSet(this.Material.Effect.GetTextureLayout(key), key);

                if (resourceSet != null) {
                    commandList.SetGraphicsResourceSet((uint) i + 4, resourceSet);
                }
            }
            
            // Draw.
            commandList.DrawIndexed(this.IndexCount);
        }
        else {
            
            // Set vertex buffer.
            commandList.SetVertexBuffer(0, this._vertexBuffer);
            
            // Set pipeline.
            commandList.SetPipeline(this.Material.Effect.GetPipeline(this._pipelineDescription).Pipeline);
            
            // Set projection view buffer.
            commandList.SetGraphicsResourceSet(0, this._modelMatrixBuffer.GetResourceSet(this.Material.Effect.GetBufferLayout("MatrixBuffer")));
            
            // Set bone buffer.
            commandList.SetGraphicsResourceSet(1, this._boneBuffer.GetResourceSet(this.Material.Effect.GetBufferLayout("BoneBuffer")));
            
            // Set color buffer.
            commandList.SetGraphicsResourceSet(2, this._colorBuffer.GetResourceSet(this.Material.Effect.GetBufferLayout("ColorBuffer")));
            
            // Set value buffer.
            commandList.SetGraphicsResourceSet(3, this._valueBuffer.GetResourceSet(this.Material.Effect.GetBufferLayout("ValueBuffer")));
            
            // Set material texture.
            for (int i = 0; i < this.Material.Effect.GetTextureLayoutKeys().Length; i++) {
                string key = this.Material.Effect.GetTextureLayoutKeys()[i];
                ResourceSet? resourceSet = this.Material.GetResourceSet(this.Material.Effect.GetTextureLayout(key), key);

                if (resourceSet != null) {
                    commandList.SetGraphicsResourceSet((uint) i + 4, resourceSet);
                }
            }
            
            // Draw.
            commandList.Draw(this.VertexCount);
        }
        
        // Reset albedo material color.
        this.Material.SetMapColor(MaterialMapType.Albedo.GetName(), cachedColor);
    }

    /// <summary>
    /// Creates and returns a configured instance of <see cref="SimplePipelineDescription"/> for rendering the mesh.
    /// </summary>
    /// <returns>A <see cref="SimplePipelineDescription"/> containing settings for blend state, depth-stencil state, rasterizer state,
    /// primitive topology, buffer bindings, texture layouts, and shader set configuration.</returns>
    private SimplePipelineDescription CreatePipelineDescription() {
        return new SimplePipelineDescription() {
            BlendState = this.Material.BlendState.Description,
            DepthStencilState = new DepthStencilStateDescription(true, true, ComparisonKind.LessEqual),
            RasterizerState = new RasterizerStateDescription() {
                CullMode = FaceCullMode.Back,
                FillMode = PolygonFillMode.Solid,
                FrontFace = FrontFace.Clockwise,
                DepthClipEnabled = true,
                ScissorTestEnabled = false
            },
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            BufferLayouts = this.Material.Effect.GetBufferLayouts(),
            TextureLayouts = this.Material.Effect.GetTextureLayouts(),
            ShaderSet = new ShaderSetDescription() {
                VertexLayouts = [
                    this.Material.Effect.VertexLayout
                ],
                Shaders = [
                    this.Material.Effect.Shader.Item1,
                    this.Material.Effect.Shader.Item2
                ]
            }
        };
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