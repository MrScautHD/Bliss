using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
using Veldrid;

namespace Bliss.CSharp.Graphics.Rendering.Renderers;

public class ImmediateRenderer : Disposable {
    
    /// <summary>
    /// Gets the graphics device used for rendering.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// Gets the output description used for rendering.
    /// </summary>
    public OutputDescription Output { get; private set; }
    
    /// <summary>
    /// Gets the effect (shader) used for rendering.
    /// </summary>
    public Effect Effect { get; private set; }
    
    /// <summary>
    /// Gets the maximum number of vertices that can be batched.
    /// </summary>
    public uint Capacity { get; private set; }

    /// <summary>
    /// The array of vertices used for batching immediate mode geometry.
    /// </summary>
    private ImmediateVertex3D[] _vertices;
    
    /// <summary>
    /// The array of indices used for batching immediate mode geometry.
    /// </summary>
    private uint[] _indices;

    /// <summary>
    /// A temporary storage of vertices used for rendering operations.
    /// </summary>
    private List<ImmediateVertex3D> _tempVertices;

    /// <summary>
    /// A Temporary storage of indices used for rendering operations.
    /// </summary>
    private List<uint> _tempIndices;
    
    /// <summary>
    /// The current count of batched vertices.
    /// </summary>
    private int _vertexCount;
    
    /// <summary>
    /// The current count of batched indices.
    /// </summary>
    private int _indexCount;

    /// <summary>
    /// The GPU buffer that stores vertex data.
    /// </summary>
    private DeviceBuffer _vertexBuffer;
    
    /// <summary>
    /// The GPU buffer that stores index data.
    /// </summary>
    private DeviceBuffer _indexBuffer;

    /// <summary>
    /// The uniform buffer that holds transformation matrices (projection, view, and transform).
    /// </summary>
    private SimpleBuffer<Matrix4x4> _matrixBuffer;

    /// <summary>
    /// The pipeline description used to configure the graphics pipeline for rendering.
    /// </summary>
    private SimplePipelineDescription _pipelineDescription;
    
    /// <summary>
    /// Indicates whether the rendering process has begun.
    /// </summary>
    private bool _begun;
    
    /// <summary>
    /// The current command list used for recording rendering commands.
    /// </summary>
    private CommandList _currentCommandList;
    
    /// <summary>
    /// The currently bound texture.
    /// </summary>
    private Texture2D _currentTexture;
    
    /// <summary>
    /// The currently active sampler.
    /// </summary>
    private Sampler _currentSampler;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ImmediateRenderer"/> class with the specified graphics device, output, effect, and capacity.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering.</param>
    /// <param name="output">The output description for rendering.</param>
    /// <param name="effect">An optional effect (shader) to use; if null, the default immediate renderer effect is used.</param>
    /// <param name="capacity">The maximum number of vertices that can be batched. Defaults to 30720.</param>
    public ImmediateRenderer(GraphicsDevice graphicsDevice, OutputDescription output, Effect? effect = null, uint capacity = 30720) {
        this.GraphicsDevice = graphicsDevice;
        this.Output = output;
        this.Effect = effect ?? GlobalResource.ImmediateRendererEffect;
        this.Capacity = capacity;
        
        // Create vertex buffer.
        uint vertexBufferSize = capacity * (uint) Marshal.SizeOf<ImmediateVertex3D>();
        this._vertices = new ImmediateVertex3D[capacity];
        this._tempVertices = new List<ImmediateVertex3D>();
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        
        // Create index buffer.
        uint indexBufferSize = capacity * 3 * sizeof(uint);
        this._indices = new uint[capacity * 3];
        this._tempIndices = new List<uint>();
        this._indexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(indexBufferSize, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        
        // Create matrix buffer.
        this._matrixBuffer = new SimpleBuffer<Matrix4x4>(graphicsDevice, 3, SimpleBufferType.Uniform, ShaderStages.Vertex);

        // Create pipeline description.
        this._pipelineDescription = this.CreatePipelineDescription();
        
        // Set default texture and sampler.
        this._currentTexture = GlobalResource.DefaultImmediateRendererTexture;
        this._currentSampler = GraphicsHelper.GetSampler(this.GraphicsDevice, SamplerType.Point);
    }

    /// <summary>
    /// Begins the rendering process by setting up the command list and pipeline state.
    /// </summary>
    /// <param name="commandList">The command list used for recording rendering commands.</param>
    /// <param name="blendState">An optional blend state; if null, the default alpha blend state is used.</param>
    /// <exception cref="Exception">Thrown if the renderer has already begun.</exception>
    public void Begin(CommandList commandList, BlendState? blendState = null) {
        if (this._begun) {
            throw new Exception("The ImmediateRenderer has already begun!");
        }
        
        this._begun = true;
        this._currentCommandList = commandList;
        
        // Update BlendState.
        this._pipelineDescription.BlendState = blendState?.Description ?? BlendState.AlphaBlend.Description;
    }

    /// <summary>
    /// Ends the rendering process, flushing any remaining batched geometry.
    /// </summary>
    /// <exception cref="Exception">Thrown if the renderer has not begun rendering.</exception>
    public void End() {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        this._begun = false;
    }
    
    /// <summary>
    /// Sets the current texture and sampler to be used for rendering.
    /// </summary>
    /// <param name="texture">The texture to use; if null, the default immediate renderer texture is used.</param>
    /// <param name="sampler">The sampler to use; if null, a default point sampler is used.</param>
    public void SetTexture(Texture2D? texture, Sampler? sampler = null) {
        this._currentTexture = texture ?? GlobalResource.DefaultImmediateRendererTexture;
        this._currentSampler = sampler ?? GraphicsHelper.GetSampler(this.GraphicsDevice, SamplerType.Point);
    }
    
    /// <summary>
    /// Draws a cube with the specified transformation, size, and optional color.
    /// </summary>
    /// <param name="transform">The transformation to be applied to the cube.</param>
    /// <param name="size">The size of the cube in 3D space.</param>
    /// <param name="color">An optional color for the cube; if null, white is used.</param>
    public void DrawCube(Transform transform, Vector3 size, Color? color = null) {
        Color finalColor = color ?? Color.White;
    
        Vector2[] texCoords = [
            new Vector2(0.0F, 1.0F), new Vector2(1.0F, 1.0F),
            new Vector2(1.0F, 0.0F), new Vector2(0.0F, 0.0F)
        ];
    
        Vector3[] positions = [
            // Front face.
            new Vector3(-1.0F, -1.0F, -1.0F), new Vector3(1.0F, -1.0F, -1.0F), new Vector3(1.0F, 1.0F, -1.0F), new Vector3(-1.0F, 1.0F, -1.0F),
            // Back face.
            new Vector3(1.0F, -1.0F, 1.0F), new Vector3(-1.0F, -1.0F, 1.0F), new Vector3(-1.0F, 1.0F, 1.0F), new Vector3(1.0F, 1.0F, 1.0F),
            // Left face.
            new Vector3(-1.0F, -1.0F, 1.0F), new Vector3(-1.0F, -1.0F, -1.0F), new Vector3(-1.0F, 1.0F, -1.0F), new Vector3(-1.0F, 1.0F, 1.0F),
            // Right face.
            new Vector3(1.0F, -1.0F, -1.0F), new Vector3(1.0F, -1.0F, 1.0F), new Vector3(1.0F, 1.0F, 1.0F), new Vector3(1.0F, 1.0F, -1.0F),
            // Top face.
            new Vector3(-1.0F, 1.0F, -1.0F), new Vector3(1.0F, 1.0F, -1.0F), new Vector3(1.0F, 1.0F, 1.0F), new Vector3(-1.0F, 1.0F, 1.0F),
            // Bottom face.
            new Vector3(-1.0F, -1.0F, 1.0F), new Vector3(1.0F, -1.0F, 1.0F), new Vector3(1.0F, -1.0F, -1.0F), new Vector3(-1.0F, -1.0F, -1.0F)
        ];
        
        for (int i = 0; i < 6; i++) {
            for (int j = 0; j < 4; j++) {
                this._tempVertices.Add(new ImmediateVertex3D() {
                    Position = positions[i * 4 + j] * new Vector3(size.X / 2.0F, size.Y / 2.0F, size.Z / 2.0F),
                    TexCoords = texCoords[j],
                    Color = finalColor.ToRgbaFloatVec4()
                });
            }
        }

        uint[] indices = [
            // Front face.
            0, 1, 2,
            2, 3, 0,
            
            // Back face.
            4, 5, 6,
            6, 7, 4,
            
            // Left face.
            8, 9, 10,
            10, 11, 8,
            
            // Right face.
            12, 13, 14,
            14, 15, 12,
            
            // Top face.
            16, 17, 18,
            18, 19, 16,
            
            // Bottom face.
            20, 21, 22,
            22, 23, 20
        ];

        for (int i = 0; i < indices.Length; i++) {
            this._tempIndices.Add(indices[i]);
        }
        
        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.TriangleList);
    }

    /// <summary>
    /// Draws the wireframe of a cube with the specified transform, size, and optional color.
    /// </summary>
    /// <param name="transform">The transformation to apply to the cube, which includes position, rotation, and scale.</param>
    /// <param name="size">The dimensions of the cube to draw.</param>
    /// <param name="color">An optional color for the wireframe. If null, the default color is white.</param>
    public void DrawCubeWires(Transform transform, Vector3 size, Color? color = null) {
        Color finalColor = color ?? Color.White;
    
        Vector3[] positions = [
            // Front face.
            new Vector3(-1.0F, -1.0F, -1.0F), new Vector3(1.0F, -1.0F, -1.0F),
            new Vector3(1.0F, 1.0F, -1.0F), new Vector3(-1.0F, 1.0F, -1.0F),
            // Back face.
            new Vector3(-1.0F, -1.0F, 1.0F), new Vector3(1.0F, -1.0F, 1.0F),
            new Vector3(1.0F, 1.0F, 1.0F), new Vector3(-1.0F, 1.0F, 1.0F)
        ];
    
        foreach (Vector3 pos in positions) {
            this._tempVertices.Add(new ImmediateVertex3D {
                Position = pos * new Vector3(size.X / 2.0F, size.Y / 2.0F, size.Z / 2.0F),
                Color = finalColor.ToRgbaFloatVec4()
            });
        }
        
        uint[] indices = [
            // Front edges.
            0, 1, 1, 2, 2, 3, 3, 0,
            
            // Back edges.
            4, 5, 5, 6, 6, 7, 7, 4,
            
            // Connecting edges.
            0, 4, 1, 5, 2, 6, 3, 7
        ];
    
        foreach (var index in indices) {
            this._tempIndices.Add(index);
        }
    
        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }
    
    /// <summary>
    /// Draws a sphere with the specified transformation, radius, number of rings, slices, and optional color.
    /// </summary>
    /// <param name="transform">The transformation to be applied to the sphere.</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <param name="rings">The number of horizontal subdivisions of the sphere.</param>
    /// <param name="slices">The number of vertical subdivisions of the sphere.</param>
    /// <param name="color">An optional color for the sphere; defaults to white.</param>
    public void DrawSphere(Transform transform, float radius, int rings, int slices, Color? color = null) {
        Color finalColor = color ?? Color.White;
    
        // Generate vertices.
        for (int ring = 0; ring <= rings; ring++) {
            float ringAngle = MathF.PI * ring / rings;
    
            for (int slice = 0; slice <= slices; slice++) {
                float sliceAngle = MathF.PI * 2 * slice / slices;
    
                float x = MathF.Sin(ringAngle) * MathF.Cos(sliceAngle);
                float y = MathF.Cos(ringAngle);
                float z = MathF.Sin(ringAngle) * MathF.Sin(sliceAngle);
    
                this._tempVertices.Add(new ImmediateVertex3D() {
                    Position = new Vector3(x, y, z) * (radius / 2),
                    TexCoords = new Vector2(slice / (float) slices, ring / (float) rings),
                    Color = finalColor.ToRgbaFloatVec4()
                });
            }
        }
    
        // Generate indices.
        for (int ring = 0; ring < rings; ring++) {
            for (int slice = 0; slice < slices; slice++) {
                int current = ring * (slices + 1) + slice;
                int next = current + slices + 1;
    
                // Two triangles per quad.
                this._tempIndices.Add((uint) current);
                this._tempIndices.Add((uint) (next + 1));
                this._tempIndices.Add((uint) (current + 1));
    
                this._tempIndices.Add((uint) current);
                this._tempIndices.Add((uint) next);
                this._tempIndices.Add((uint) (next + 1));
            }
        }
        
        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.TriangleList);
    }
    
    /// <summary>
    /// Draws the wireframe of a sphere with the given transform, radius, number of rings, slices, and optional color.
    /// </summary>
    /// <param name="transform">The transformation to apply to the sphere (position, rotation, and scaling).</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <param name="rings">The number of horizontal subdivisions of the sphere.</param>
    /// <param name="slices">The number of vertical subdivisions of the sphere.</param>
    /// <param name="color">An optional color for the wireframe; defaults to white.</param>
    public void DrawSphereWires(Transform transform, float radius, int rings, int slices, Color? color = null) {
        Color finalColor = color ?? Color.White;
    
        // Generate wireframe vertices.
        for (int ring = 0; ring <= rings; ring++) {
            float ringAngle = MathF.PI * ring / rings;
            float y = MathF.Cos(ringAngle) * (radius / 2);
            float ringRadius = MathF.Sin(ringAngle) * (radius / 2);
    
            for (int slice = 0; slice <= slices; slice++) {
                float sliceAngle = MathF.PI * 2 * slice / slices;
    
                float x = MathF.Cos(sliceAngle) * ringRadius;
                float z = MathF.Sin(sliceAngle) * ringRadius;
    
                this._tempVertices.Add(new ImmediateVertex3D {
                    Position = new Vector3(x, y, z),
                    Color = finalColor.ToRgbaFloatVec4()
                });
            }
        }
    
        // Generate wireframe indices.
        for (int ring = 0; ring < rings; ring++) {
            for (int slice = 0; slice < slices; slice++) {
                int current = ring * (slices + 1) + slice;
                int next = current + slices + 1;
    
                // Connect horizontal lines.
                this._tempIndices.Add((uint) current);
                this._tempIndices.Add((uint) (current + 1));
    
                // Connect vertical lines.
                this._tempIndices.Add((uint) current);
                this._tempIndices.Add((uint) next);
            }
        }
    
        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }
    
    public void DrawHemisphere(Transform transform, float radius, int rings, int slices, Color? color = null) {
        Color finalColor = color ?? Color.White;
    
        // Generate vertices for the upper half of the sphere.
        for (int ring = 0; ring <= rings; ring++) {
            float ringAngle = MathF.PI * ring / (2 * rings);
    
            for (int slice = 0; slice <= slices; slice++) {
                float sliceAngle = MathF.PI * 2 * slice / slices;
    
                float x = MathF.Sin(ringAngle) * MathF.Cos(sliceAngle);
                float y = MathF.Cos(ringAngle);
                float z = MathF.Sin(ringAngle) * MathF.Sin(sliceAngle);
    
                this._tempVertices.Add(new ImmediateVertex3D() {
                    Position = new Vector3(x, y, z) * (radius / 2),
                    TexCoords = new Vector2(slice / (float) slices, ring / (float) rings),
                    Color = finalColor.ToRgbaFloatVec4()
                });
            }
        }
    
        // Generate indices.
        for (int ring = 0; ring < rings; ring++) {
            for (int slice = 0; slice < slices; slice++) {
                int current = ring * (slices + 1) + slice;
                int next = current + slices + 1;
    
                // Two triangles per quad.
                this._tempIndices.Add((uint) current);
                this._tempIndices.Add((uint) (next + 1));
                this._tempIndices.Add((uint) (current + 1));
                
                this._tempIndices.Add((uint) current);
                this._tempIndices.Add((uint) next);
                this._tempIndices.Add((uint) (next + 1));
            }
        }
    
        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.TriangleList);
    }
    
    public void DrawHemisphereWires() {
        throw new NotImplementedException("DrawHemisphereWires is not implemented yet.");
    }
    
    public void DrawCylinder() {
        throw new NotImplementedException("DrawCylinder is not implemented yet.");
    }
    
    public void DrawCylinderWires() {
        throw new NotImplementedException("DrawCylinderWires is not implemented yet.");
    }
    
    public void DrawCapsule() {
        throw new NotImplementedException("DrawCapsule is not implemented yet.");
    }
    
    public void DrawCapsuleWires() {
        throw new NotImplementedException("DrawCapsuleWires is not implemented yet.");
    }
    
    public void DrawGird() {
        throw new NotImplementedException("DrawGird is not implemented yet.");
    }
    
    public void DrawBoundingBox() {
        throw new NotImplementedException("DrawBoundingBox is not implemented yet.");
    }
    
    public void DrawBillboard() {
        throw new NotImplementedException("DrawBillboard is not implemented yet.");
    }

    /// <summary>
    /// Draws a set of vertices using the specified transformation, vertex data, indices, and topology.
    /// </summary>
    /// <param name="transform">The transformation matrix to apply to the vertices.</param>
    /// <param name="vertices">The list of vertices to be rendered.</param>
    /// <param name="indices">The list of indices defining the order in which the vertices are connected.</param>
    /// <param name="topology">The primitive topology specifying how the vertices are interpreted (e.g., triangle list, line strip).</param>
    public void DrawVertices(Transform transform, List<ImmediateVertex3D> vertices, List<uint> indices, PrimitiveTopology topology) {
        if (!this._begun) {
            throw new Exception("You must begin the ImmediateRenderer before calling draw methods!");
        }
        
        Cam3D? cam3D = Cam3D.ActiveCamera;

        if (cam3D == null) {
            return;
        }
        
        if (vertices.Count > this.Capacity) {
            Logger.Fatal(new InvalidOperationException($"The number of provided vertices exceeds the maximum batch size! [{vertices.Count} > {this.Capacity}]"));
        }

        if (indices.Count > this.Capacity * 3) {
            Logger.Fatal(new InvalidOperationException($"The number of provided indices exceeds the maximum batch size! [{indices.Count} > {this.Capacity * 3}]"));
        }
        
        // Add vertices.
        for (int i = 0; i < vertices.Count; i++) {
            this._vertices[i] = vertices[i];
        }
        
        // Add indices.
        for (int i = 0; i < indices.Count; i++) {
            this._indices[i] = indices[i];
        }

        // Set vertices and indices count.
        this._vertexCount = vertices.Count;
        this._indexCount = indices.Count;
        
        // Update matrix buffer.
        this._matrixBuffer.SetValue(0, cam3D.GetProjection());
        this._matrixBuffer.SetValue(1, cam3D.GetView());
        this._matrixBuffer.SetValue(2, transform.GetTransform());
        this._matrixBuffer.UpdateBuffer(this._currentCommandList);
        
        // Update topology.
        this._pipelineDescription.PrimitiveTopology = topology;
        
        if (this._indexCount > 0) {
            
            // Update vertex and index buffer.
            this._currentCommandList.UpdateBuffer(this._vertexBuffer, 0, new ReadOnlySpan<ImmediateVertex3D>(this._vertices, 0, this._vertexCount));
            this._currentCommandList.UpdateBuffer(this._indexBuffer, 0, new ReadOnlySpan<uint>(this._indices, 0, this._indexCount));
            
            // Set vertex and index buffer.
            this._currentCommandList.SetVertexBuffer(0, this._vertexBuffer);
            this._currentCommandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt32);

            // Set pipeline.
            this._currentCommandList.SetPipeline(this.Effect.GetPipeline(this._pipelineDescription).Pipeline);
        
            // Set matrix buffer.
            this._currentCommandList.SetGraphicsResourceSet(0, this._matrixBuffer.GetResourceSet(this.Effect.GetBufferLayout("MatrixBuffer")));

            // Set resourceSet of the texture.
            this._currentCommandList.SetGraphicsResourceSet(1, this._currentTexture.GetResourceSet(this._currentSampler, this.Effect.GetTextureLayout("fTexture")));
            
            // Draw.
            this._currentCommandList.DrawIndexed((uint) this._indexCount);
        }
        else {
            
            // Update vertex buffer.
            this._currentCommandList.UpdateBuffer(this._vertexBuffer, 0, new ReadOnlySpan<ImmediateVertex3D>(this._vertices, 0, this._vertexCount));
            
            // Set vertex buffer.
            this._currentCommandList.SetVertexBuffer(0, this._vertexBuffer);

            // Set pipeline.
            this._currentCommandList.SetPipeline(this.Effect.GetPipeline(this._pipelineDescription).Pipeline);
        
            // Set matrix buffer.
            this._currentCommandList.SetGraphicsResourceSet(0, this._matrixBuffer.GetResourceSet(this.Effect.GetBufferLayout("MatrixBuffer")));
        
            // Set resourceSet of the texture.
            this._currentCommandList.SetGraphicsResourceSet(1, this._currentTexture.GetResourceSet(this._currentSampler, this.Effect.GetTextureLayout("fTexture")));
            
            // Draw.
            this._currentCommandList.Draw((uint) this._vertexCount);
        }
        
        // Reset indexer.
        this._vertexCount = 0;
        this._indexCount = 0;
        
        // Clear data.
        Array.Clear(this._vertices);
        Array.Clear(this._indices);
        
        // Clear temp data.
        this._tempVertices.Clear();
        this._tempIndices.Clear();
    }
    
    /// <summary>
    /// Creates a new pipeline description used for configuring the graphics pipeline.
    /// </summary>
    /// <returns>A <see cref="SimplePipelineDescription"/> configured with depth/stencil, rasterizer, topology, and shader settings.</returns>
    private SimplePipelineDescription CreatePipelineDescription() {
        return new SimplePipelineDescription() {
            DepthStencilState = new DepthStencilStateDescription(true, true, ComparisonKind.LessEqual),
            RasterizerState = new RasterizerStateDescription() {
                CullMode = FaceCullMode.Back,
                FillMode = PolygonFillMode.Solid,
                FrontFace = FrontFace.Clockwise,
                DepthClipEnabled = true,
                ScissorTestEnabled = false
            },
            BufferLayouts = this.Effect.GetBufferLayouts(),
            TextureLayouts = this.Effect.GetTextureLayouts(),
            ShaderSet = new ShaderSetDescription() {
                VertexLayouts = [
                    this.Effect.VertexLayout
                ],
                Shaders = [
                    this.Effect.Shader.Item1,
                    this.Effect.Shader.Item2
                ]
            },
            Outputs = this.Output
        };
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._vertexBuffer.Dispose();
            this._indexBuffer.Dispose();
            this._matrixBuffer.Dispose();
        }
    }
}