using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Geometry;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.VertexTypes;
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
    /// Gets the maximum number of vertices that can be drawn.
    /// </summary>
    public uint Capacity { get; private set; }
    
    /// <summary>
    /// Gets the number of draw calls performed during the current frame.
    /// </summary>
    public int DrawCallCount { get; private set; }
    
    /// <summary>
    /// The array of vertices used for batching immediate mode geometry.
    /// </summary>
    private ImmediateVertex3D[] _vertices;
    
    /// <summary>
    /// The array of indices used for batching immediate mode geometry.
    /// </summary>
    private uint[] _indices;
    
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
    /// The pipeline description used to configure the graphics pipeline for rendering.
    /// </summary>
    private SimplePipelineDescription _pipelineDescription;
    
    /// <summary>
    /// Indicates whether a draw session has begun.
    /// </summary>
    private bool _begun;
    
    /// <summary>
    /// The command list currently used for rendering.
    /// </summary>
    private CommandList _currentCommandList;
    
    /// <summary>
    /// The main <see cref="Effect"/>.
    /// </summary>
    private Effect _mainEffect;
    
    /// <summary>
    /// The current <see cref="Effect"/>.
    /// </summary>
    private Effect _currentEffect;

    /// <summary>
    /// The requested <see cref="Effect"/>.
    /// </summary>
    private Effect _requestedEffect;
    
    /// <summary>
    /// The main <see cref="OutputDescription"/>.
    /// </summary>
    private OutputDescription _mainOutput;
    
    /// <summary>
    /// The current <see cref="OutputDescription"/>.
    /// </summary>
    private OutputDescription _currentOutput;
    
    /// <summary>
    /// The requested <see cref="OutputDescription"/>.
    /// </summary>
    private OutputDescription _requestedOutput;
    
    /// <summary>
    /// The main <see cref="BlendStateDescription"/>.
    /// </summary>
    private BlendStateDescription _mainBlendState;
    
    /// <summary>
    /// The current <see cref="BlendStateDescription"/>.
    /// </summary>
    private BlendStateDescription _currentBlendState;
    
    /// <summary>
    /// The requested <see cref="BlendStateDescription"/>.
    /// </summary>
    private BlendStateDescription _requestedBlendState;
    
    /// <summary>
    /// The main <see cref="DepthStencilStateDescription"/>.
    /// </summary>
    private DepthStencilStateDescription _mainDepthStencilState;
    
    /// <summary>
    /// The current <see cref="DepthStencilStateDescription"/>.
    /// </summary>
    private DepthStencilStateDescription _currentDepthStencilState;
    
    /// <summary>
    /// The requested <see cref="DepthStencilStateDescription"/>.
    /// </summary>
    private DepthStencilStateDescription _requestedDepthStencilState;
    
    /// <summary>
    /// The main <see cref="RasterizerStateDescription"/>.
    /// </summary>
    private RasterizerStateDescription _mainRasterizerState;
    
    /// <summary>
    /// The current <see cref="RasterizerStateDescription"/>.
    /// </summary>
    private RasterizerStateDescription _currentRasterizerState;
    
    /// <summary>
    /// The requested <see cref="RasterizerStateDescription"/>.
    /// </summary>
    private RasterizerStateDescription _requestedRasterizerState;
    
    /// <summary>
    /// The current <see cref="Texture2D"/>.
    /// </summary>
    private Texture2D _mainTexture;
    
    /// <summary>
    /// The requested <see cref="Texture2D"/>.
    /// </summary>
    private Texture2D _currentTexture;
    
    /// <summary>
    /// The requested <see cref="Texture2D"/>.
    /// </summary>
    private Texture2D _requestedTexture;
    
    /// <summary>
    /// The main <see cref="Sampler"/>.
    /// </summary>
    private Sampler _mainSampler;
    
    /// <summary>
    /// The current <see cref="Sampler"/>.
    /// </summary>
    private Sampler _currentSampler;
    
    /// <summary>
    /// The requested <see cref="Sampler"/>.
    /// </summary>
    private Sampler _requestedSampler;
    
    /// <summary>
    /// The main <see cref="RectangleF"/> source rectangle.
    /// </summary>
    private Rectangle _mainSourceRect;
    
    /// <summary>
    /// The current <see cref="RectangleF"/> source rectangle.
    /// </summary>
    private Rectangle _currentSourceRect;
    
    /// <summary>
    /// The requested <see cref="RectangleF"/> source rectangle.
    /// </summary>
    private Rectangle _requestedSourceRect ;
    
    /// <summary>
    /// The current <see cref="PrimitiveTopology"/>.
    /// </summary>
    private PrimitiveTopology _currentTopology;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ImmediateRenderer"/> class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering.</param>
    /// <param name="capacity">The maximum number of vertices that can be batched.</param>
    public ImmediateRenderer(GraphicsDevice graphicsDevice, uint capacity = 10240) {
        this.GraphicsDevice = graphicsDevice;
        this.Capacity = capacity;
        
        // Create vertex buffer.
        uint vertexBufferSize = capacity * (uint) Marshal.SizeOf<ImmediateVertex3D>();
        this._vertices = new ImmediateVertex3D[capacity];
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        this._vertexBuffer.Name = "VertexBuffer";
        
        // Create index buffer.
        uint indexBufferSize = capacity * 3 * sizeof(uint);
        this._indices = new uint[capacity * 3];
        this._indexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(indexBufferSize, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        this._indexBuffer.Name = "IndexBuffer";
        
        // Create pipeline description.
        this._pipelineDescription = new SimplePipelineDescription();
    }
    
    /// <summary>
    /// Begins a new rendering session with the specified pipeline state.
    /// </summary>
    /// <param name="commandList">The command list used for issuing draw commands.</param>
    /// <param name="output">The output description defining render targets.</param>
    /// <param name="effect">Optional effect used for rendering.</param>
    /// <param name="blendState">Optional blend state.</param>
    /// <param name="depthStencilState">Optional depth-stencil state.</param>
    /// <param name="rasterizerState">Optional rasterizer state.</param>
    /// <param name="sampler">Optional texture sampler.</param>
    /// <param name="sourceRect">Optional texture source rectangle.</param>
    public void Begin(CommandList commandList, OutputDescription output, Effect? effect = null, BlendStateDescription? blendState = null, DepthStencilStateDescription? depthStencilState = null, RasterizerStateDescription? rasterizerState = null, Sampler? sampler = null, Rectangle? sourceRect = null) {
        if (this._begun) {
            throw new Exception("The ImmediateRenderer has already begun!");
        }
        
        this._begun = true;
        this._currentCommandList = commandList;
        this._mainOutput = this._currentOutput = this._requestedOutput = output;
        this._mainEffect = this._currentEffect = this._requestedEffect = effect ?? GlobalResource.DefaultImmediateRendererEffect;
        this._mainBlendState = this._currentBlendState = this._requestedBlendState = blendState ?? BlendStateDescription.SINGLE_DISABLED;
        this._mainDepthStencilState = this._currentDepthStencilState = this._requestedDepthStencilState = depthStencilState ?? DepthStencilStateDescription.DEPTH_ONLY_LESS_EQUAL;
        this._mainRasterizerState = this._currentRasterizerState = this._requestedRasterizerState = rasterizerState ?? RasterizerStateDescription.DEFAULT;
        this._mainTexture = this._currentTexture = this._requestedTexture = GlobalResource.DefaultImmediateRendererTexture;
        this._mainSampler = this._currentSampler = this._requestedSampler = sampler ?? GraphicsHelper.GetSampler(this.GraphicsDevice, SamplerType.PointClamp);
        this._mainSourceRect = this._currentSourceRect = this._requestedSourceRect = sourceRect ?? new Rectangle(0, 0, (int) this._mainTexture.Width, (int) this._mainTexture.Height);
        
        this.DrawCallCount = 0;
    }
    
    /// <summary>
    /// Ends the current rendering session and flushes all remaining batched geometry.
    /// </summary>
    public void End() {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        this._begun = false;
        this.Flush();
    }
    
    /// <summary>
    /// Gets the currently active output description.
    /// </summary>
    /// <returns>The current <see cref="OutputDescription"/>.</returns>
    public OutputDescription GetCurrentOutput() {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        return this._currentOutput;
    }
    
    /// <summary>
    /// Sets a new output description to be used for subsequent draw calls.
    /// </summary>
    /// <param name="output">The output description to apply.</param>
    public void PushOutput(OutputDescription output) {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        this._requestedOutput = output;
    }
    
    /// <summary>
    /// Restores the default output description.
    /// </summary>
    public void PopOutput() {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        this._requestedOutput = this._mainOutput;
    }
    
    /// <summary>
    /// Gets the currently active effect.
    /// </summary>
    /// <returns>The current <see cref="Effect"/>.</returns>
    public Effect GetCurrentEffect() {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        return this._currentEffect;
    }
    
    /// <summary>
    /// Sets a new effect to be used for subsequent draw calls.
    /// </summary>
    /// <param name="effect">The effect to apply.</param>
    public void PushEffect(Effect effect) {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        this._requestedEffect = effect;
    }
    
    /// <summary>
    /// Restores the default effect.
    /// </summary>
    public void PopEffect() {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        this._requestedEffect = this._mainEffect;
    }
    
    /// <summary>
    /// Gets the currently active blend state.
    /// </summary>
    /// <returns>The current <see cref="BlendStateDescription"/>.</returns>
    public BlendStateDescription GetCurrentBlendState() {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        return this._currentBlendState;
    }
    
    /// <summary>
    /// Sets a new blend state to be used for subsequent draw calls.
    /// </summary>
    /// <param name="blendState">The blend state to apply.</param>
    public void PushBlendState(BlendStateDescription blendState) {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        this._requestedBlendState = blendState;
    }
    
    /// <summary>
    /// Restores the default blend state.
    /// </summary>
    public void PopBlendState() {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        this._requestedBlendState = this._mainBlendState;
    }
    
    /// <summary>
    /// Gets the currently active depth-stencil state.
    /// </summary>
    /// <returns>The current <see cref="DepthStencilStateDescription"/>.</returns>
    public DepthStencilStateDescription GetCurrentDepthStencilState() {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        return this._currentDepthStencilState;
    }
    
    /// <summary>
    /// Sets a new depth-stencil state to be used for subsequent draw calls.
    /// </summary>
    /// <param name="depthStencilState">The depth-stencil state to apply.</param>
    public void PushDepthStencilState(DepthStencilStateDescription depthStencilState) {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        this._requestedDepthStencilState = depthStencilState;
    }
    
    /// <summary>
    /// Restores the default depth-stencil state.
    /// </summary>
    public void PopDepthStencilState() {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        this._requestedDepthStencilState = this._mainDepthStencilState;
    }
    
    /// <summary>
    /// Gets the currently active rasterizer state.
    /// </summary>
    /// <returns>The current <see cref="RasterizerStateDescription"/>.</returns>
    public RasterizerStateDescription GetCurrentRasterizerState() {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        return this._currentRasterizerState;
    }
    
    /// <summary>
    /// Sets a new rasterizer state to be used for subsequent draw calls.
    /// </summary>
    /// <param name="rasterizerState">The rasterizer state to apply.</param>
    public void PushRasterizerState(RasterizerStateDescription rasterizerState) {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        this._requestedRasterizerState = rasterizerState;
    }
    
    /// <summary>
    /// Restores the default rasterizer state.
    /// </summary>
    public void PopRasterizerState() {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        this._requestedRasterizerState = this._mainRasterizerState;
    }
    
    /// <summary>
    /// Gets the currently bound texture.
    /// </summary>
    /// <returns>The current <see cref="Texture2D"/>.</returns>
    public Texture2D GetCurrentTexture() {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        return this._currentTexture;
    }
    
    /// <summary>
    /// Sets a new texture and optional source rectangle for subsequent draw calls.
    /// </summary>
    /// <param name="texture">The texture to apply.</param>
    /// <param name="sourceRect">Optional source rectangle. Defaults to the full texture.</param>
    public void PushTexture(Texture2D texture, Rectangle? sourceRect = null) {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        this._requestedTexture = texture;
        this._requestedSourceRect = sourceRect ?? new Rectangle(0, 0, (int) texture.Width, (int) texture.Height);
    }
    
    /// <summary>
    /// Restores the default texture and source rectangle.
    /// </summary>
    public void PopTexture() {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        this._requestedTexture = this._mainTexture;
        this._requestedSourceRect = this._mainSourceRect;
    }
    
    /// <summary>
    /// Gets the currently active sampler.
    /// </summary>
    /// <returns>The current <see cref="Sampler"/>.</returns>
    public Sampler GetCurrentSampler() {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        return this._currentSampler;
    }
    
    /// <summary>
    /// Sets a new sampler to be used for subsequent draw calls.
    /// </summary>
    /// <param name="sampler">The sampler to apply.</param>
    public void PushSampler(Sampler sampler) {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        this._requestedSampler = sampler;
    }
    
    /// <summary>
    /// Restores the default sampler.
    /// </summary>
    public void PopSampler() {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        this._requestedSampler = this._mainSampler;
    }

    /// <summary>
    /// Renders a cube with the specified transform, size, and optional color.
    /// </summary>
    /// <param name="transform">The transformation to apply to the cube, defining its position, rotation, and scale.</param>
    /// <param name="size">The size dimensions of the cube to be rendered.</param>
    /// <param name="color">An optional color for the cube. If not provided, the default color is white.</param>
    public void DrawCube(Transform transform, Vector3 size, Color? color = null) {
        Vector4 colorVec4 = color?.ToRgbaFloatVec4() ?? Color.White.ToRgbaFloatVec4();
        Texture2D texture = this._currentTexture;
        Rectangle sourceRec = this._currentSourceRect;
        
        Matrix4x4 transformMatrix = transform.GetMatrix();
        
        Vector3 half = new Vector3(size.X / 2.0F, size.Y / 2.0F, size.Z / 2.0F);
        
        // Calculate source rectangle UVs.
        float uLeft = sourceRec.X / (float) texture.Width;
        float uRight = (sourceRec.X + sourceRec.Width) / (float) texture.Width;
        float vTop = sourceRec.Y / (float) texture.Height;
        float vBottom = (sourceRec.Y + sourceRec.Height) / (float) texture.Height;
        
        int vertexCount = 6 * 4;
        int indexCount = 6 * 6;
        
        int baseVertex = this.Prepare(PrimitiveTopology.TriangleList, vertexCount, indexCount);
        
        // Cube faces.
        for (int face = 0; face < 6; face++) {
            
            // Face normal.
            Vector3 faceNormal = face switch {
                0 => new Vector3(0.0F, 0.0F, -1.0F),
                1 => new Vector3(0.0F, 0.0F, 1.0F),
                2 => new Vector3(-1.0F, 0.0F, 0.0F),
                3 => new Vector3(1.0F, 0.0F, 0.0F),
                4 => new Vector3(0.0F, 1.0F, 0.0F),
                5 => new Vector3(0.0F, -1.0F, 0.0F),
                _ => Vector3.Zero
            };
            
            // Face tangent.
            Vector3 tangent = face switch {
                0 => new Vector3(1.0F, 0.0F, 0.0F),
                1 => new Vector3(-1.0F, 0.0F, 0.0F),
                2 => new Vector3(0.0F, 0.0F, -1.0F),
                3 => new Vector3(0.0F, 0.0F, 1.0F),
                4 => new Vector3(1.0F, 0.0F, 0.0F),
                5 => new Vector3(1.0F, 0.0F, 0.0F),
                _ => Vector3.Zero
            };
            
            // Bitangent.
            Vector3 bitangent = Vector3.Cross(faceNormal, tangent);
            
            int faceStart = this._vertexCount;
            
            // Face vertices.
            for (int corner = 0; corner < 4; corner++) {
                Vector3 position = faceNormal
                    + (corner == 0 || corner == 3 ? -tangent : tangent)
                    + (corner == 0 || corner == 1 ? -bitangent : bitangent);
                
                Vector2 texCoord = corner switch {
                    0 => new Vector2(uLeft, vTop),
                    1 => new Vector2(uRight, vTop),
                    2 => new Vector2(uRight, vBottom),
                    3 => new Vector2(uLeft, vBottom),
                    _ => Vector2.Zero
                };
                
                this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                    Position = Vector3.Transform(position * half, transformMatrix),
                    TexCoords = texCoord,
                    Color = colorVec4
                };
            }
            
            // Face triangles.
            this._indices[this._indexCount++] = (uint) (faceStart + 0);
            this._indices[this._indexCount++] = (uint) (faceStart + 3);
            this._indices[this._indexCount++] = (uint) (faceStart + 2);
            
            this._indices[this._indexCount++] = (uint) (faceStart + 2);
            this._indices[this._indexCount++] = (uint) (faceStart + 1);
            this._indices[this._indexCount++] = (uint) (faceStart + 0);
        }
    }
    
    /// <summary>
    /// Draws the wireframe of a cube with the specified transform, size, and optional color.
    /// </summary>
    /// <param name="transform">The transformation to apply to the cube, including position, rotation, and scale.</param>
    /// <param name="size">The dimensions of the cube to be drawn.</param>
    /// <param name="color">An optional color for the wireframe. If not provided, the default color is white.</param>
    public void DrawCubeWires(Transform transform, Vector3 size, Color? color = null) {
        Vector4 colorVec4 = color?.ToRgbaFloatVec4() ?? Color.White.ToRgbaFloatVec4();
        Matrix4x4 transformMatrix = transform.GetMatrix();
        
        Vector3 half = new Vector3(size.X / 2.0F, size.Y / 2.0F, size.Z / 2.0F);
        
        int vertexCount = 8;
        int indexCount = 24;
        
        int baseVertex = this.Prepare(PrimitiveTopology.LineList, vertexCount, indexCount);
        
        // Cube vertices.
        int cubeStart = baseVertex;
        
        for (int i = 0; i < 8; i++) {
            
            float x = (i & 1) == 0 ? -1.0F : 1.0F;
            float y = (i & 2) == 0 ? -1.0F : 1.0F;
            float z = (i & 4) == 0 ? -1.0F : 1.0F;
            
            this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                Position = Vector3.Transform(new Vector3(x, y, z) * half, transformMatrix),
                Color = colorVec4
            };
        }
        
        // Cube edges.
        for (int i = 0; i < 8; i++) {
            for (int bit = 0; bit < 3; bit++) {
                if ((i & (1 << bit)) == 0) {
                    int neighbor = i | (1 << bit);
                    
                    this._indices[this._indexCount++] = (uint) (cubeStart + i);
                    this._indices[this._indexCount++] = (uint) (cubeStart + neighbor);
                }
            }
        }
    }
    
    /// <summary>
    /// Draws a sphere with the specified transformation, radius, number of rings, slices, and optional color.
    /// </summary>
    /// <param name="transform">The transformation to be applied to the sphere.</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <param name="rings">The number of horizontal subdivisions of the sphere.</param>
    /// <param name="slices">The number of vertical subdivisions of the sphere.</param>
    /// <param name="color">An optional color for the sphere; defaults to white if not provided.</param>
    public void DrawSphere(Transform transform, float radius, int rings, int slices, Color? color = null) {
        Vector4 colorVec4 = color?.ToRgbaFloatVec4() ?? Color.White.ToRgbaFloatVec4();
        Texture2D texture = this._currentTexture;
        Rectangle sourceRec = this._currentSourceRect;
        Matrix4x4 transformMatrix = transform.GetMatrix();
        
        if (rings < 3) {
            rings = 3;
        }
        
        if (slices < 3) {
            slices = 3;
        }
        
        int stride = slices + 1;
        
        // Calculate source rectangle UVs.
        float uLeft = sourceRec.X / (float) texture.Width;
        float uRight = (sourceRec.X + sourceRec.Width) / (float) texture.Width;
        float vTop = sourceRec.Y / (float) texture.Height;
        float vBottom = (sourceRec.Y + sourceRec.Height) / (float) texture.Height;
        
        int vertexCount = (rings + 1) * stride;
        int indexCount = rings * slices * 6;
        
        int baseVertex = this.Prepare(PrimitiveTopology.TriangleList, vertexCount, indexCount);
        
        // Sphere vertices.
        int sphereStart = baseVertex;
        
        for (int ring = 0; ring <= rings; ring++) {
            float ringAngle = MathF.PI * ring / rings;
            
            float sinTheta = MathF.Sin(ringAngle);
            float cosTheta = MathF.Cos(ringAngle);
            
            for (int slice = 0; slice <= slices; slice++) {
                float sliceAngle = MathF.Tau * slice / slices;
                
                float sinPhi = MathF.Sin(sliceAngle);
                float cosPhi = MathF.Cos(sliceAngle);
                
                Vector3 position = new Vector3(sinTheta * cosPhi, cosTheta, sinTheta * sinPhi) * (radius / 2.0F);
                
                this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                    Position = Vector3.Transform(position, transformMatrix),
                    TexCoords = new Vector2(
                        uLeft + (uRight - uLeft) * (slice / (float) slices),
                        vTop + (vBottom - vTop) * (ring / (float) rings)
                    ),
                    Color = colorVec4
                };
            }
        }
        
        // Sphere triangles.
        for (int ring = 0; ring < rings; ring++) {
            for (int slice = 0; slice < slices; slice++) {
                int current = sphereStart + ring * stride + slice;
                int next = current + stride;
                
                this._indices[this._indexCount++] = (uint) current;
                this._indices[this._indexCount++] = (uint) (next + 1);
                this._indices[this._indexCount++] = (uint) (current + 1);
                
                this._indices[this._indexCount++] = (uint) current;
                this._indices[this._indexCount++] = (uint) next;
                this._indices[this._indexCount++] = (uint) (next + 1);
            }
        }
    }
    
    /// <summary>
    /// Draws the wireframe of a sphere with the specified transform, radius, number of rings, slices, and optional color.
    /// </summary>
    /// <param name="transform">The transformation to apply to the sphere, including position, rotation, and scale.</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <param name="rings">The number of horizontal subdivisions (rings) of the sphere.</param>
    /// <param name="slices">The number of vertical subdivisions (slices) of the sphere.</param>
    /// <param name="color">The optional color for the sphere's wireframe. Defaults to white if not specified.</param>
    public void DrawSphereWires(Transform transform, float radius, int rings, int slices, Color? color = null) {
        Vector4 colorVec4 = color?.ToRgbaFloatVec4() ?? Color.White.ToRgbaFloatVec4();
        Matrix4x4 transformMatrix = transform.GetMatrix();
        
        if (rings < 3) {
            rings = 3;
        }
        
        if (slices < 3) {
            slices = 3;
        }
        
        int stride = slices + 1;
        
        int vertexCount = (rings + 1) * stride;
        int indexCount = rings * slices * 4;
        
        int baseVertex = this.Prepare(PrimitiveTopology.LineList, vertexCount, indexCount);
        
        // Sphere vertices.
        int sphereStart = baseVertex;
        
        for (int ring = 0; ring <= rings; ring++) {
            float ringAngle = MathF.PI * ring / rings;
            
            float y = MathF.Cos(ringAngle) * (radius / 2.0F);
            float ringRadius = MathF.Sin(ringAngle) * (radius / 2.0F);
            
            for (int slice = 0; slice <= slices; slice++) {
                float sliceAngle = MathF.Tau * slice / slices;
                
                float x = MathF.Cos(sliceAngle) * ringRadius;
                float z = MathF.Sin(sliceAngle) * ringRadius;
                
                this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                    Position = Vector3.Transform(new Vector3(x, y, z), transformMatrix),
                    Color = colorVec4
                };
            }
        }
        
        // Wireframe lines.
        for (int ring = 0; ring < rings; ring++) {
            for (int slice = 0; slice < slices; slice++) {
                int current = sphereStart + ring * stride + slice;
                int next = current + stride;
                
                // Horizontal lines.
                this._indices[this._indexCount++] = (uint) current;
                this._indices[this._indexCount++] = (uint) (current + 1);
                
                // Vertical lines.
                this._indices[this._indexCount++] = (uint) current;
                this._indices[this._indexCount++] = (uint) next;
            }
        }
    }
    
    /// <summary>
    /// Renders a 3D hemisphere with the specified transformation, radius, rings, slices, and optional color.
    /// </summary>
    /// <param name="transform">The transformation to apply to the hemisphere.</param>
    /// <param name="radius">The radius of the hemisphere.</param>
    /// <param name="rings">The number of rings used to construct the hemisphere. Must be 3 or greater.</param>
    /// <param name="slices">The number of slices used to construct the hemisphere. Must be 3 or greater.</param>
    /// <param name="color">An optional color to apply to the hemisphere. If null, the default color will be white.</param>
    public void DrawHemisphere(Transform transform, float radius, int rings, int slices, Color? color = null) {
        Vector4 colorVec4 = color?.ToRgbaFloatVec4() ?? Color.White.ToRgbaFloatVec4();
        Texture2D texture = this._currentTexture;
        Rectangle sourceRec = this._currentSourceRect;
        Matrix4x4 transformMatrix = transform.GetMatrix();
        
        if (rings < 3) {
            rings = 3;
        }
        
        if (slices < 3) {
            slices = 3;
        }
        
        float halfHeight = radius / 4.0F;
        int hemiRings = rings / 2;
        int stride = slices + 1;
        
        // Calculate source rectangle UVs.
        float uLeft = sourceRec.X / (float) texture.Width;
        float uRight = (sourceRec.X + sourceRec.Width) / (float) texture.Width;
        float vTop = sourceRec.Y / (float) texture.Height;
        float vBottom = (sourceRec.Y + sourceRec.Height) / (float) texture.Height;
        
        int vertexCount = (hemiRings + 1) * stride + 1 + stride;
        int indexCount = hemiRings * slices * 6 + slices * 3;
        
        int baseVertex = this.Prepare(PrimitiveTopology.TriangleList, vertexCount, indexCount);
        
        // Hemisphere.
        int hemiStart = baseVertex;
        
        for (int ring = 0; ring <= hemiRings; ring++) {
            float theta = ring * MathF.PI / (rings % 2 == 0 ? rings : rings - 1);
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);
            
            for (int slice = 0; slice <= slices; slice++) {
                float phi = slice * MathF.Tau / slices;
                float sinPhi = MathF.Sin(phi);
                float cosPhi = MathF.Cos(phi);
                
                Vector3 position = new Vector3(
                    (radius / 2.0F) * sinTheta * cosPhi,
                    (radius / 2.0F) * cosTheta - halfHeight,
                    (radius / 2.0F) * sinTheta * sinPhi
                );
                
                this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                    Position = Vector3.Transform(position, transformMatrix),
                    TexCoords = new Vector2(
                        uLeft + (uRight - uLeft) * (0.5F + cosPhi * sinTheta * 0.5F),
                        vTop + (vBottom - vTop) * (0.5F + sinPhi * sinTheta * 0.5F)
                    ),
                    Color = colorVec4
                };
            }
        }
        
        // Base center.
        int baseCenter = this._vertexCount;
        
        this._vertices[this._vertexCount++] = new ImmediateVertex3D {
            Position = Vector3.Transform(new Vector3(0.0F, -halfHeight, 0.0F), transformMatrix),
            TexCoords = new Vector2(
                (uLeft + uRight) * 0.5F,
                (vTop + vBottom) * 0.5F
            ),
            Color = colorVec4
        };
        
        // Base ring.
        int baseRingStart = this._vertexCount;
        
        for (int slice = 0; slice <= slices; slice++) {
            float angle = slice * MathF.Tau / slices;
            
            float x = MathF.Cos(angle) * (radius / 2.0F);
            float z = MathF.Sin(angle) * (radius / 2.0F);
            
            this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                Position = Vector3.Transform(new Vector3(x, -halfHeight, z), transformMatrix),
                TexCoords = new Vector2(
                    uLeft + (uRight - uLeft) * (0.5F + x / radius),
                    vTop + (vBottom - vTop) * (0.5F + z / radius)
                ),
                Color = colorVec4
            };
        }
        
        // Hemisphere triangles.
        for (int ring = 0; ring < hemiRings; ring++) {
            for (int slice = 0; slice < slices; slice++) {
                int current = hemiStart + ring * stride + slice;
                int next = current + stride;
                
                this._indices[this._indexCount++] = (uint) current;
                this._indices[this._indexCount++] = (uint) (next + 1);
                this._indices[this._indexCount++] = (uint) (current + 1);
                
                this._indices[this._indexCount++] = (uint) current;
                this._indices[this._indexCount++] = (uint) next;
                this._indices[this._indexCount++] = (uint) (next + 1);
            }
        }
        
        // Base cap.
        for (int slice = 0; slice < slices; slice++) {
            int current = baseRingStart + slice;
            
            this._indices[this._indexCount++] = (uint) baseCenter;
            this._indices[this._indexCount++] = (uint) (current + 1);
            this._indices[this._indexCount++] = (uint) current;
        }
    }
    
    /// <summary>
    /// Draws the wireframe outline of a hemisphere using the specified parameters.
    /// </summary>
    /// <param name="transform">The transformation to apply to the hemisphere.</param>
    /// <param name="radius">The radius of the hemisphere.</param>
    /// <param name="rings">The number of horizontal subdivisions (rings) for the hemisphere.</param>
    /// <param name="slices">The number of vertical subdivisions (slices) for the hemisphere.</param>
    /// <param name="color">An optional color for the hemisphere. If null, it defaults to white.</param>
    public void DrawHemisphereWires(Transform transform, float radius, int rings, int slices, Color? color = null) {
        Vector4 colorVec4 = color?.ToRgbaFloatVec4() ?? Color.White.ToRgbaFloatVec4();
        Matrix4x4 transformMatrix = transform.GetMatrix();
        
        if (rings < 2) {
            rings = 2;
        }
        
        if (slices < 3) {
            slices = 3;
        }
        
        float halfHeight = radius / 4.0F;
        
        int hemiRings = rings / 2;
        int stride = slices + 1;
        
        int vertexCount = (hemiRings + 1) * stride + 1 + stride;
        int indexCount = vertexCount * 4;
        
        int baseVertex = this.Prepare(PrimitiveTopology.LineList, vertexCount, indexCount);
        
        int hemiStart = baseVertex;
        
        for (int ring = 0; ring <= hemiRings; ring++) {
            float ringAngle = ring * MathF.PI / (rings % 2 == 0 ? rings : rings - 1);
            
            float y = MathF.Cos(ringAngle) * (radius / 2.0F);
            float ringRadius = MathF.Sin(ringAngle) * (radius / 2.0F);
            
            for (int slice = 0; slice <= slices; slice++) {
                float sliceAngle = MathF.Tau * slice / slices;
                
                float x = MathF.Cos(sliceAngle) * ringRadius;
                float z = MathF.Sin(sliceAngle) * ringRadius;
                
                this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                    Position = Vector3.Transform(new Vector3(x, y - halfHeight, z), transformMatrix),
                    Color = colorVec4
                };
            }
        }
        
        int baseCenter = this._vertexCount;
        
        // Center vertex
        this._vertices[this._vertexCount++] = new ImmediateVertex3D {
            Position = Vector3.Transform(new Vector3(0.0F, -halfHeight, 0.0F), transformMatrix),
            Color = colorVec4
        };
        
        int baseRingStart = this._vertexCount;
        
        // Base ring vertices
        for (int slice = 0; slice <= slices; slice++) {
            float sliceAngle = MathF.Tau * slice / slices;
            
            float x = MathF.Cos(sliceAngle) * (radius / 2.0F);
            float z = MathF.Sin(sliceAngle) * (radius / 2.0F);
            
            this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                Position = Vector3.Transform(new Vector3(x, -halfHeight, z), transformMatrix),
                Color = colorVec4
            };
            
            // Radial lines (center → ring)
            this._indices[this._indexCount++] = (uint) baseCenter;
            this._indices[this._indexCount++] = (uint) (baseRingStart + slice);
        }
        
        // Hemisphere wireframe.
        for (int ring = 0; ring < hemiRings; ring++) {
            for (int slice = 0; slice < slices; slice++) {
                int current = hemiStart + ring * stride + slice;
                int nextRing = current + stride;
                
                // Horizontal ring lines
                this._indices[this._indexCount++] = (uint) current;
                this._indices[this._indexCount++] = (uint) (current + 1);
                
                // Vertical connections between rings
                this._indices[this._indexCount++] = (uint) current;
                this._indices[this._indexCount++] = (uint) nextRing;
            }
        }
        
        // Base circle ring.
        for (int slice = 0; slice < slices; slice++) {
            int current = baseRingStart + slice;
            
            this._indices[this._indexCount++] = (uint) current;
            this._indices[this._indexCount++] = (uint) (current + 1);
        }
    }
    
    /// <summary>
    /// Renders a cylinder using the specified command list, output description, transform, dimensions, slice count, and optional color.
    /// </summary>
    /// <param name="transform">The transform used to position and orient the cylinder in 3D space.</param>
    /// <param name="radius">The radius of the cylinder's top and bottom caps.</param>
    /// <param name="height">The height of the cylinder from bottom to top cap.</param>
    /// <param name="slices">The number of segments used to approximate the cylindrical surface. Minimum value is 3.</param>
    /// <param name="color">The optional color of the cylinder. If null, a default white color is applied.</param>
    public void DrawCylinder(Transform transform, float radius, float height, int slices, Color? color = null) {
        Vector4 colorVec4 = color?.ToRgbaFloatVec4() ?? Color.White.ToRgbaFloatVec4();
        Texture2D texture = this._currentTexture;
        Rectangle sourceRec = this._currentSourceRect;
        Matrix4x4 transformMatrix = transform.GetMatrix();
        
        if (slices < 3) {
            slices = 3;
        }
        
        float halfHeight = height / 2.0F;
        
        // Calculate source rectangle UVs.
        float uLeft = sourceRec.X / (float) texture.Width;
        float uRight = (sourceRec.X + sourceRec.Width) / (float) texture.Width;
        float vTop = sourceRec.Y / (float) texture.Height;
        float vBottom = (sourceRec.Y + sourceRec.Height) / (float) texture.Height;
        
        int vertexCount = (slices + 1) * 2 + 2;
        int indexCount = slices * 6 + slices * 3 + slices * 3;
        
        int baseVertex = this.Prepare(PrimitiveTopology.TriangleList, vertexCount, indexCount);
        
        // Side vertices.
        for (int slice = 0; slice <= slices; slice++) {
            float angle = slice * MathF.Tau / slices;
            float x = MathF.Cos(angle) * (radius / 2.0F);
            float z = MathF.Sin(angle) * (radius / 2.0F);
            
            float u = (float) slice / slices;
            float uCoord = uLeft + (uRight - uLeft) * u;
            
            // Bottom vertex.
            this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                Position = Vector3.Transform(new Vector3(x, -halfHeight, z), transformMatrix),
                TexCoords = new Vector2(uCoord, vBottom),
                Color = colorVec4
            };
            
            // Top vertex.
            this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                Position = Vector3.Transform(new Vector3(x, halfHeight, z), transformMatrix),
                TexCoords = new Vector2(uCoord, vTop),
                Color = colorVec4
            };
        }
        
        // Side triangles.
        for (int slice = 0; slice < slices; slice++) {
            int baseIndex = baseVertex + slice * 2;
            int nextIndex = baseVertex + (slice + 1) * 2;
            
            this._indices[this._indexCount++] = (uint) (baseIndex + 1);
            this._indices[this._indexCount++] = (uint) baseIndex;
            this._indices[this._indexCount++] = (uint) nextIndex;
            
            this._indices[this._indexCount++] = (uint) (baseIndex + 1);
            this._indices[this._indexCount++] = (uint) nextIndex;
            this._indices[this._indexCount++] = (uint) (nextIndex + 1);
        }
        
        // Bottom cap center.
        int bottomCenter = this._vertexCount;
        
        this._vertices[this._vertexCount++] = new ImmediateVertex3D {
            Position = Vector3.Transform(new Vector3(0.0F, -halfHeight, 0.0F), transformMatrix),
            TexCoords = new Vector2(
                (uLeft + uRight) * 0.5F,
                (vBottom + vTop) * 0.5F
            ),
            Color = colorVec4
        };
        
        // Bottom cap triangles.
        for (int slice = 0; slice < slices; slice++) {
            int baseIndex = baseVertex + slice * 2;
            int nextIndex = baseVertex + (slice + 1) * 2;
            
            this._indices[this._indexCount++] = (uint) bottomCenter;
            this._indices[this._indexCount++] = (uint) nextIndex;
            this._indices[this._indexCount++] = (uint) baseIndex;
        }
        
        // Top cap center.
        int topCenter = this._vertexCount;
        
        this._vertices[this._vertexCount++] = new ImmediateVertex3D {
            Position = Vector3.Transform(new Vector3(0.0F, halfHeight, 0.0F), transformMatrix),
            TexCoords = new Vector2(
                (uLeft + uRight) * 0.5F,
                (vBottom + vTop) * 0.5F
            ),
            Color = colorVec4
        };
        
        // Top cap triangles.
        for (int slice = 0; slice < slices; slice++) {
            int baseIndex = baseVertex + slice * 2 + 1;
            int nextIndex = baseVertex + (slice + 1) * 2 + 1;
            
            this._indices[this._indexCount++] = (uint) topCenter;
            this._indices[this._indexCount++] = (uint) baseIndex;
            this._indices[this._indexCount++] = (uint) nextIndex;
        }
    }

    /// <summary>
    /// Draws the wireframe representation of a cylinder using the specified transformation, radius, height, and number of slices.
    /// </summary>
    /// <param name="transform">The transformation applied to position, rotate, and scale the cylinder in the scene.</param>
    /// <param name="radius">The radius of the cylinder.</param>
    /// <param name="height">The height of the cylinder.</param>
    /// <param name="slices">The number of divisions around the cylinder's circumference. Must be 3 or greater.</param>
    /// <param name="color">The color of the cylinder's wireframe. Defaults to white if not specified.</param>
    public void DrawCylinderWires(Transform transform, float radius, float height, int slices, Color? color = null) {
        Vector4 colorVec4 = color?.ToRgbaFloatVec4() ?? Color.White.ToRgbaFloatVec4();
        Matrix4x4 transformMatrix = transform.GetMatrix();
        
        if (slices < 3) {
            slices = 3;
        }
        
        float halfHeight = height / 2.0F;
        
        int vertexCount = (slices + 1) * 2 + 2;
        int indexCount = vertexCount * 4;
        
        int baseVertex = this.Prepare(PrimitiveTopology.LineList, vertexCount, indexCount);
        
        // Circle vertices (bottom + top).
        for (int slice = 0; slice <= slices; slice++) {
            float angle = slice * MathF.Tau / slices;
            float x = MathF.Cos(angle) * (radius / 2.0F);
            float z = MathF.Sin(angle) * (radius / 2.0F);
            
            // Bottom vertex.
            this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                Position = Vector3.Transform(new Vector3(x, -halfHeight, z), transformMatrix),
                Color = colorVec4
            };
            
            // Top vertex.
            this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                Position = Vector3.Transform(new Vector3(x, halfHeight, z), transformMatrix),
                Color = colorVec4
            };
        }
        
        // Circle edges.
        for (int slice = 0; slice < slices; slice++) {
            int bottomIndex = baseVertex + slice * 2;
            int topIndex = bottomIndex + 1;
            
            int nextBottom = baseVertex + ((slice + 1) * 2);
            int nextTop = nextBottom + 1;
            
            // Bottom circle.
            this._indices[this._indexCount++] = (uint) bottomIndex;
            this._indices[this._indexCount++] = (uint) nextBottom;
            
            // Top circle.
            this._indices[this._indexCount++] = (uint) topIndex;
            this._indices[this._indexCount++] = (uint) nextTop;
        }
        
        // Center vertices.
        int bottomCenter = this._vertexCount;
        this._vertices[this._vertexCount++] = new ImmediateVertex3D {
            Position = Vector3.Transform(new Vector3(0.0F, -halfHeight, 0.0F), transformMatrix),
            Color = colorVec4
        };
        
        int topCenter = this._vertexCount;
        this._vertices[this._vertexCount++] = new ImmediateVertex3D {
            Position = Vector3.Transform(new Vector3(0.0F, halfHeight, 0.0F), transformMatrix),
            Color = colorVec4
        };
        
        // Center to edge lines.
        for (int slice = 0; slice <= slices; slice++) {
            int bottomEdge = baseVertex + slice * 2;
            int topEdge = bottomEdge + 1;
            
            // Bottom spokes.
            this._indices[this._indexCount++] = (uint) bottomCenter;
            this._indices[this._indexCount++] = (uint) bottomEdge;
            
            // Top spokes.
            this._indices[this._indexCount++] = (uint) topCenter;
            this._indices[this._indexCount++] = (uint) topEdge;
        }
        
        // Vertical edges.
        for (int slice = 0; slice < slices; slice++) {
            int bottomIndex = baseVertex + slice * 2;
            int topIndex = bottomIndex + 1;
            
            this._indices[this._indexCount++] = (uint) bottomIndex;
            this._indices[this._indexCount++] = (uint) topIndex;
        }
    }

    /// <summary>
    /// Renders a 3D capsule using the specified transformation, dimensions, and visual properties.
    /// </summary>
    /// <param name="transform">The transformation to apply to the capsule, including position, rotation, and scale.</param>
    /// <param name="radius">The radius of the capsule's hemispherical ends and cylindrical body.</param>
    /// <param name="height">The total height of the capsule.</param>
    /// <param name="slices">The number of subdivisions around the circumference of the capsule. Must be 3 or greater.</param>
    /// <param name="color">The color of the capsule. If null, defaults to white.</param>
    public void DrawCapsule(Transform transform, float radius, float height, int slices, Color? color = null) {
        Vector4 colorVec4 = color?.ToRgbaFloatVec4() ?? Color.White.ToRgbaFloatVec4();
        Texture2D texture = this._currentTexture;
        Rectangle sourceRec = this._currentSourceRect;
        Matrix4x4 transformMatrix = transform.GetMatrix();
        
        if (slices < 3) {
            slices = 3;
        }
        
        float halfRadius = radius / 2.0F;
        float halfHeight = height / 2.0F;
        int rings = slices / 2;
        
        int stride = slices + 1;
        
        // Calculate source rectangle UVs.
        float uLeft = sourceRec.X / (float) texture.Width;
        float uRight = (sourceRec.X + sourceRec.Width) / (float) texture.Width;
        float vTop = sourceRec.Y / (float) texture.Height;
        float vBottom = (sourceRec.Y + sourceRec.Height) / (float) texture.Height;
        
        int vertexCount = (rings + 1) * stride + 2 * stride + (rings + 1) * stride;
        int indexCount = vertexCount * 6;
        
        int baseVertex = this.Prepare(PrimitiveTopology.TriangleList, vertexCount, indexCount);
        
        // Top hemisphere.
        int topStart = baseVertex;
        
        for (int ring = 0; ring <= rings; ring++) {
            float theta = ring * MathF.PI / (rings * 2.0F);
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);
            
            for (int slice = 0; slice <= slices; slice++) {
                float phi = slice * MathF.Tau / slices;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);
                
                float x = halfRadius * sinTheta * cosPhi;
                float y = halfRadius * cosTheta + halfHeight;
                float z = halfRadius * sinTheta * sinPhi;
                
                this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                    Position = Vector3.Transform(new Vector3(x, y, z), transformMatrix),
                    TexCoords = new Vector2(
                        uLeft + (uRight - uLeft) * slice / slices,
                        vTop + (vBottom - vTop) * ring / rings
                    ),
                    Color = colorVec4
                };
            }
        }
        
        // Cylinder.
        int cylinderStart = this._vertexCount;
        
        for (int yStep = 0; yStep <= 1; yStep++) {
            float y = yStep == 0 ? -halfHeight : halfHeight;
            
            for (int slice = 0; slice <= slices; slice++) {
                float phi = slice * MathF.Tau / slices;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);
                
                float x = halfRadius * cosPhi;
                float z = halfRadius * sinPhi;
                
                this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                    Position = Vector3.Transform(new Vector3(x, y, z), transformMatrix),
                    TexCoords = new Vector2(
                        uLeft + (uRight - uLeft) * slice / slices,
                        yStep == 0 ? vBottom : vTop
                    ),
                    Color = colorVec4
                };
            }
        }
        
        // Bottom hemisphere.
        int bottomStart = this._vertexCount;
        
        for (int ring = 0; ring <= rings; ring++) {
            float theta = ring * MathF.PI / (rings * 2.0F) + MathF.PI;
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);
            
            for (int slice = 0; slice <= slices; slice++) {
                float phi = slice * MathF.Tau / slices;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);
                
                float x = halfRadius * sinTheta * cosPhi;
                float y = halfRadius * cosTheta - halfHeight;
                float z = halfRadius * sinTheta * sinPhi;
                
                this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                    Position = Vector3.Transform(new Vector3(-x, y, -z), transformMatrix),
                    TexCoords = new Vector2(
                        uLeft + (uRight - uLeft) * slice / slices,
                        vBottom - (vBottom - vTop) * ring / rings
                    ),
                    Color = colorVec4
                };
            }
        }
        
        // Top hemisphere indices.
        for (int ring = 0; ring < rings; ring++) {
            for (int slice = 0; slice < slices; slice++) {
                uint first = (uint)(topStart + ring * stride + slice);
                uint second = first + (uint)stride;
                
                this._indices[this._indexCount++] = first;
                this._indices[this._indexCount++] = second;
                this._indices[this._indexCount++] = first + 1;
                
                this._indices[this._indexCount++] = second;
                this._indices[this._indexCount++] = second + 1;
                this._indices[this._indexCount++] = first + 1;
            }
        }
        
        // Cylinder indices.
        for (int slice = 0; slice < slices; slice++) {
            uint first = (uint)(cylinderStart + slice);
            uint second = first + (uint)stride;
            
            this._indices[this._indexCount++] = first;
            this._indices[this._indexCount++] = first + 1;
            this._indices[this._indexCount++] = second;
            
            this._indices[this._indexCount++] = first + 1;
            this._indices[this._indexCount++] = second + 1;
            this._indices[this._indexCount++] = second;
        }
        
        // Bottom hemisphere indices.
        for (int ring = 0; ring < rings; ring++) {
            for (int slice = 0; slice < slices; slice++) {
                uint first = (uint)(bottomStart + ring * stride + slice);
                uint second = first + (uint)stride;
                
                this._indices[this._indexCount++] = first;
                this._indices[this._indexCount++] = first + 1;
                this._indices[this._indexCount++] = second;
                
                this._indices[this._indexCount++] = second;
                this._indices[this._indexCount++] = first + 1;
                this._indices[this._indexCount++] = second + 1;
            }
        }
    }
    
    /// <summary>
    /// Draws the wireframe of a capsule based on the specified transform, dimensions, and color.
    /// </summary>
    /// <param name="transform">The transformation applied to the capsule, such as position, rotation, and scale.</param>
    /// <param name="radius">The radius of the capsule's hemispherical ends and cylindrical body.</param>
    /// <param name="height">The height of the cylindrical body of the capsule.</param>
    /// <param name="slices">The number of divisions for rendering the capsule's rounded surface. Must be at least 3.</param>
    /// <param name="color">The optional color of the capsule wireframe. Defaults to white if not specified.</param>
    public void DrawCapsuleWires(Transform transform, float radius, float height, int slices, Color? color = null) {
        Vector4 colorVec4 = color?.ToRgbaFloatVec4() ?? Color.White.ToRgbaFloatVec4();
        Matrix4x4 transformMatrix = transform.GetMatrix();
        
        if (slices < 3) {
            slices = 3;
        }
        
        float halfRadius = radius / 2.0F;
        float halfHeight = height / 2.0F;
        int rings = slices / 2;
    
        int stride = slices + 1;
        
        int vertexCount = (rings + 1) * stride + 2 * stride + (rings + 1) * stride;
        int indexCount = vertexCount * 4;
        
        int baseVertex = this.Prepare(PrimitiveTopology.LineList, vertexCount, indexCount);
        
        // Top hemisphere.
        int topStart = baseVertex;
    
        for (int ring = 0; ring <= rings; ring++) {
            float theta = ring * MathF.PI / (rings * 2.0F);
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);
            
            for (int slice = 0; slice <= slices; slice++) {
                float phi = slice * MathF.Tau / slices;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);
                
                float x = halfRadius * sinTheta * cosPhi;
                float y = halfRadius * cosTheta + halfHeight;
                float z = halfRadius * sinTheta * sinPhi;
                
                this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                    Position = Vector3.Transform(new Vector3(x, y, z), transformMatrix),
                    Color = colorVec4
                };
                
                // Vertical lines (between rings)
                if (ring < rings) {
                    int current = topStart + ring * stride + slice;
                    int next = current + stride;
    
                    this._indices[this._indexCount++] = (uint) current;
                    this._indices[this._indexCount++] = (uint) next;
                }
            }
        }
        
        // Cylinder.
        int cylinderStart = this._vertexCount;
        
        for (int yStep = 0; yStep <= 1; yStep++) {
            float y = (yStep == 0) ? -halfHeight : halfHeight;
            
            for (int slice = 0; slice <= slices; slice++) {
                float phi = slice * MathF.Tau / slices;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);
                
                float x = halfRadius * cosPhi;
                float z = halfRadius * sinPhi;
                
                this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                    Position = Vector3.Transform(new Vector3(x, y, z), transformMatrix),
                    Color = colorVec4
                };
                
                // Vertical lines (bottom ring → top ring)
                if (yStep == 0) {
                    int current = cylinderStart + slice;
                    int next = current + stride;
                    
                    this._indices[this._indexCount++] = (uint) current;
                    this._indices[this._indexCount++] = (uint) next;
                }
            }
        }
        
        // Bottom hemisphere.
        int bottomStart = this._vertexCount;
        
        for (int ring = 0; ring <= rings; ring++) {
            float theta = MathF.PI - ring * MathF.PI / (rings * 2.0F);
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);
            
            for (int slice = 0; slice <= slices; slice++) {
                float phi = slice * MathF.Tau / slices;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);
                
                float x = halfRadius * sinTheta * cosPhi;
                float y = halfRadius * cosTheta - halfHeight;
                float z = halfRadius * sinTheta * sinPhi;
                
                this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                    Position = Vector3.Transform(new Vector3(x, y, z), transformMatrix),
                    Color = colorVec4
                };
                
                // Vertical lines (between rings).
                if (ring < rings) {
                    int current = bottomStart + ring * stride + slice;
                    int next = current + stride;
                    
                    this._indices[this._indexCount++] = (uint) current;
                    this._indices[this._indexCount++] = (uint) next;
                }
            }
        }
        
        // Horizontal rings.
        for (int slice = 0; slice < slices; slice++) {
            
            // Top hemisphere rings.
            for (int ring = 0; ring <= rings; ring++) {
                int current = topStart + ring * stride + slice;
    
                this._indices[this._indexCount++] = (uint) current;
                this._indices[this._indexCount++] = (uint) (current + 1);
            }
            
            // Cylinder rings (bottom + top).
            for (int step = 0; step <= 1; step++) {
                int current = cylinderStart + step * stride + slice;
    
                this._indices[this._indexCount++] = (uint) current;
                this._indices[this._indexCount++] = (uint) (current + 1);
            }
            
            // Bottom hemisphere rings.
            for (int ring = 0; ring <= rings; ring++) {
                int current = bottomStart + ring * stride + slice;
                
                this._indices[this._indexCount++] = (uint) current;
                this._indices[this._indexCount++] = (uint) (current + 1);
            }
        }
    }
    
    /// <summary>
    /// Draws a 3D cone using the specified parameters.
    /// </summary>
    /// <param name="transform">The transform specifying the position, rotation, and scale of the cone in world space.</param>
    /// <param name="radius">The radius of the base of the cone.</param>
    /// <param name="height">The height of the cone from its base to its apex.</param>
    /// <param name="slices">The number of slices used to construct the cone's base. Must be at least 3.</param>
    /// <param name="color">An optional parameter to define the color of the cone. Defaults to white if not provided.</param>
    public void DrawCone(Transform transform, float radius, float height, int slices, Color? color = null) {
        Vector4 colorVec4 = color?.ToRgbaFloatVec4() ?? Color.White.ToRgbaFloatVec4();
        Texture2D texture = this._currentTexture;
        Rectangle sourceRec = this._currentSourceRect;
        Matrix4x4 transformMatrix = transform.GetMatrix();
        
        if (slices < 3) {
            slices = 3;
        }
        
        // Calculate source rectangle UVs.
        float uLeft = sourceRec.X / (float) texture.Width;
        float uRight = (sourceRec.X + sourceRec.Width) / (float) texture.Width;
        float vTop = sourceRec.Y / (float) texture.Height;
        float vBottom = (sourceRec.Y + sourceRec.Height) / (float) texture.Height;
        
        float halfHeight = height / 2.0F;
        
        int vertexCount = (slices + 1) * 2 + 1;
        int indexCount = slices * 6;
        
        int baseVertex = this.Prepare(PrimitiveTopology.TriangleList, vertexCount, indexCount);
        
        // Generate side vertices.
        for (int slice = 0; slice <= slices; slice++) {
            float angle = slice * MathF.Tau / slices;
            float x = MathF.Cos(angle) * (radius / 2.0F);
            float z = MathF.Sin(angle) * (radius / 2.0F);
            float u = uLeft + (uRight - uLeft) * ((float) slice / slices);
            
            // Bottom vertex.
            this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                Position = Vector3.Transform(new Vector3(x, -halfHeight, z), transformMatrix),
                TexCoords = new Vector2(u, vBottom),
                Color = colorVec4
            };
            
            // Top vertex (tip).
            this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                Position = Vector3.Transform(new Vector3(0.0F, halfHeight, 0.0F), transformMatrix),
                TexCoords = new Vector2(u, vTop),
                Color = colorVec4
            };
        }
        
        // Local index of center vertex
        int bottomCenterLocal = (slices + 1) * 2;
    
        // Add bottom center vertex.
        this._vertices[this._vertexCount++] = new ImmediateVertex3D {
            Position = Vector3.Transform(new Vector3(0.0F, -halfHeight, 0.0F), transformMatrix),
            TexCoords = new Vector2((uLeft + uRight) * 0.5F, (vTop + vBottom) * 0.5F),
            Color = colorVec4
        };
        
        // Side triangles.
        for (int slice = 0; slice < slices; slice++) {
            int baseIndex = slice * 2;
            int nextIndex = (slice + 1) * 2;
            
            this._indices[this._indexCount++] = (uint)(baseVertex + baseIndex + 1);
            this._indices[this._indexCount++] = (uint)(baseVertex + baseIndex);
            this._indices[this._indexCount++] = (uint)(baseVertex + nextIndex);
        }
        
        // Bottom cap.
        for (int slice = 0; slice < slices; slice++) {
            int baseIndex = slice * 2;
            
            this._indices[this._indexCount++] = (uint)(baseVertex + bottomCenterLocal);
            this._indices[this._indexCount++] = (uint)(baseVertex + (baseIndex + 2));
            this._indices[this._indexCount++] = (uint)(baseVertex + baseIndex);
        }
    }
    
    /// <summary>
    /// Draws a wireframe representation of a cone in 3D space.
    /// </summary>
    /// <param name="transform">The transformation to apply to the cone, including position, rotation, and scale.</param>
    /// <param name="radius">The radius of the cone's base.</param>
    /// <param name="height">The height of the cone from the base to its tip.</param>
    /// <param name="slices">The number of slices used to approximate the circular base. Must be at least 3.</param>
    /// <param name="color">An optional color for the wireframe. Defaults to white if null.</param>
    public void DrawConeWires(Transform transform, float radius, float height, int slices, Color? color = null) {
        Vector4 colorVec4 = color?.ToRgbaFloatVec4() ?? Color.White.ToRgbaFloatVec4();
        Matrix4x4 transformMatrix = transform.GetMatrix();
        
        if (slices < 3) {
            slices = 3;
        }
        
        float halfHeight = height / 2.0F;
        
        int vertexCount = (slices + 1) * 2 + 1;
        int indexCount = slices * 6;
        
        int baseVertex = this.Prepare(PrimitiveTopology.LineList, vertexCount, indexCount);
        
        // Generate vertices for the cone.
        for (int slice = 0; slice <= slices; slice++) {
            float angle = slice * MathF.Tau / slices;
            float x = MathF.Cos(angle) * (radius / 2.0F);
            float z = MathF.Sin(angle) * (radius / 2.0F);
            
            // Bottom edge vertex.
            this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                Position = Vector3.Transform(new Vector3(x, -halfHeight, z), transformMatrix),
                Color = colorVec4
            };
            
            // Top vertex (tip of the cone).
            this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                Position = Vector3.Transform(new Vector3(0.0F, halfHeight, 0.0F), transformMatrix),
                Color = colorVec4
            };
        }
        
        // Save center index before adding it
        int bottomCenterIndex = this._vertexCount;
        
        // Add center vertex for the bottom cap.
        this._vertices[this._vertexCount++] = new ImmediateVertex3D {
            Position = Vector3.Transform(new Vector3(0.0F, -halfHeight, 0.0F), transformMatrix),
            Color = colorVec4
        };
        
        // Generate indices for the cone edges.
        for (int slice = 0; slice < slices; slice++) {
            int baseIndex = slice * 2;
            
            // Edge from bottom to tip.
            this._indices[this._indexCount++] = (uint) (baseVertex + baseIndex);
            this._indices[this._indexCount++] = (uint) (baseVertex + baseIndex + 1);
            
            // Edge along the base ring.
            this._indices[this._indexCount++] = (uint) (baseVertex + baseIndex);
            this._indices[this._indexCount++] = (uint) (baseVertex + ((baseIndex + 2) % (slices * 2)));
            
            // Line from center to edge (bottom cap).
            this._indices[this._indexCount++] = (uint) (baseVertex + (bottomCenterIndex - baseVertex));
            this._indices[this._indexCount++] = (uint) (baseVertex + baseIndex);
        }
    }
    
    /// <summary>
    /// Renders a torus shape using the specified transformation, dimensions, and color.
    /// </summary>
    /// <param name="transform">The transformation to apply to the torus, including position, rotation, and scale.</param>
    /// <param name="radius">The radius of the inner circle of the torus.</param>
    /// <param name="size">The thickness of the torus.</param>
    /// <param name="radSeg">The number of segments along the radial direction of the torus. Must be 3 or greater.</param>
    /// <param name="sides">The number of sides to approximate the circular cross-section of the torus. Must be 3 or greater.</param>
    /// <param name="color">The color of the torus. If null, the default color will be white.</param>
    public void DrawTorus(Transform transform, float radius, float size, int radSeg, int sides, Color? color = null) {
        Vector4 colorVec4 = color?.ToRgbaFloatVec4() ?? Color.White.ToRgbaFloatVec4();
        Texture2D texture = this._currentTexture;
        Rectangle sourceRec = this._currentSourceRect;
        Matrix4x4 transformMatrix = transform.GetMatrix();
        
        if (radSeg < 3) {
            radSeg = 3;
        }
        
        if (sides < 3) {
            sides = 3;
        }
        
        // Calculate source rectangle UVs.
        float uLeft = sourceRec.X / (float) texture.Width;
        float uRight = (sourceRec.X + sourceRec.Width) / (float) texture.Width;
        float vTop = sourceRec.Y / (float) texture.Height;
        float vBottom = (sourceRec.Y + sourceRec.Height) / (float) texture.Height;
        
        float circusStep = MathF.Tau / radSeg;
        float sideStep = MathF.Tau / sides;
    
        // Total counts for Prepare.
        int vertexCount = (radSeg + 1) * (sides + 1);
        int indexCount = radSeg * sides * 6;
    
        // Prepare renderer and get base vertex offset.
        int baseVertex = this.Prepare(PrimitiveTopology.TriangleList, vertexCount, indexCount);
        
        // Calculate the vertices.
        for (int rad = 0; rad <= radSeg; rad++) {
            float radAngle = rad * circusStep;
            float cosRad = MathF.Cos(radAngle);
            float sinRad = MathF.Sin(radAngle);
            
            for (int side = 0; side <= sides; side++) {
                float sideAngle = side * sideStep;
                float cosSide = MathF.Cos(sideAngle);
                float sinSide = MathF.Sin(sideAngle);
                
                // Calculate torus vertex position.
                Vector3 position = new Vector3(cosSide * cosRad, sinSide, cosSide * sinRad) * (size / 4.0F) +
                                   new Vector3(cosRad * (radius / 4.0F), 0.0F, sinRad * (radius / 4.0F));
                
                // Calculate texture coordinates.
                Vector2 texCoords = new Vector2(
                    uLeft + (uRight - uLeft) * ((float) rad / radSeg),
                    vTop + (vBottom - vTop) * ((float) side / sides)
                );
                
                // Add vertex.
                this._vertices[this._vertexCount++] = new ImmediateVertex3D() {
                    Position = Vector3.Transform(position, transformMatrix),
                    TexCoords = texCoords,
                    Color = colorVec4
                };
            }
        }
        
        // Calculate the indices.
        for (int rad = 0; rad < radSeg; rad++) {
            for (int side = 0; side < sides; side++) {
                int current = rad * (sides + 1) + side;
                int next = current + sides + 1;
                
                // First triangle.
                this._indices[this._indexCount++] = (uint) (baseVertex + current);
                this._indices[this._indexCount++] = (uint) (baseVertex + next);
                this._indices[this._indexCount++] = (uint) (baseVertex + next + 1);
                
                // Second triangle.
                this._indices[this._indexCount++] = (uint) (baseVertex + current);
                this._indices[this._indexCount++] = (uint) (baseVertex + next + 1);
                this._indices[this._indexCount++] = (uint) (baseVertex + current + 1);
            }
        }
    }

    /// <summary>
    /// Renders a wireframe torus using the specified transformation, dimensions, and color.
    /// </summary>
    /// <param name="transform">The transformation applied to the torus, including position, rotation, and scale.</param>
    /// <param name="radius">The radius of the inner circle of the torus.</param>
    /// <param name="size">The thickness of the torus.</param>
    /// <param name="radSeg">The number of radial segments. Must be 3 or greater.</param>
    /// <param name="sides">The number of subdivisions around the circular cross-section. Must be 3 or greater.</param>
    /// <param name="color">The optional color used for rendering the torus wireframe. Defaults to white if not specified.</param>
    public void DrawTorusWires(Transform transform, float radius, float size, int radSeg, int sides, Color? color = null) {
        Vector4 colorVec4 = color?.ToRgbaFloatVec4() ?? Color.White.ToRgbaFloatVec4();
        Matrix4x4 transformMatrix = transform.GetMatrix();
        
        if (radSeg < 3) {
            radSeg = 3;
        }
        
        if (sides < 3) {
            sides = 3;
        }
        
        float circusStep = MathF.Tau / radSeg;
        float sideStep = MathF.Tau / sides;
        
        // Total counts for Prepare.
        int vertexCount = (radSeg + 1) * (sides + 1);
        
        // Each vertex can generate up to 2 lines (radial + side), each = 2 indices.
        int indexCount = radSeg * sides * 4;
    
        // Prepare renderer and get base vertex offset.
        int baseVertex = this.Prepare(PrimitiveTopology.LineList, vertexCount, indexCount);
        
        // Calculate the vertices for wireframe.
        for (int rad = 0; rad <= radSeg; rad++) {
            float radAngle = rad * circusStep;
            float cosRad = MathF.Cos(radAngle);
            float sinRad = MathF.Sin(radAngle);
            
            for (int side = 0; side <= sides; side++) {
                float sideAngle = side * sideStep;
                float cosSide = MathF.Cos(sideAngle);
                float sinSide = MathF.Sin(sideAngle);
                
                // Calculate torus position.
                Vector3 position = new Vector3(cosSide * cosRad, sinSide, cosSide * sinRad) * (size / 4.0F) +
                                   new Vector3(cosRad * (radius / 4.0F), 0.0F, sinRad * (radius / 4.0F));
                
                // Calculate local vertex index.
                int localIndex = rad * (sides + 1) + side;
                
                // Add vertex.
                this._vertices[this._vertexCount++] = new ImmediateVertex3D() {
                    Position = Vector3.Transform(position, transformMatrix),
                    Color = colorVec4
                };
                
                // Add indices for connecting radial and side segments.
                if (rad < radSeg && side < sides) {
                    int nextSide = localIndex + 1;
                    int nextRad = localIndex + (sides + 1);
                    
                    // Side connection.
                    this._indices[this._indexCount++] = (uint) (baseVertex + localIndex);
                    this._indices[this._indexCount++] = (uint) (baseVertex + nextSide);
                    
                    // Radial connection.
                    this._indices[this._indexCount++] = (uint) (baseVertex + localIndex);
                    this._indices[this._indexCount++] = (uint) (baseVertex + nextRad);
                }
            }
        }
    }
    
    /// <summary>
    /// Draws a knot shape with the specified parameters using a command list and defined properties such as transformations, radii, number of segments, and optional color.
    /// </summary>
    /// <param name="transform">The transformation to apply to the knot during rendering.</param>
    /// <param name="radius">The overall radius of the knot.</param>
    /// <param name="tubeRadius">The radius of the tube forming the knot structure.</param>
    /// <param name="radSeg">The number of radial segments forming the knot. Must be 3 or greater.</param>
    /// <param name="sides">The number of sides of the tube forming the knot. Must be 3 or greater.</param>
    /// <param name="color">An optional color to apply to the knot. Defaults to white when null.</param>
    public void DrawKnot(Transform transform, float radius, float tubeRadius, int radSeg, int sides, Color? color = null) {
        Vector4 colorVec4 = color?.ToRgbaFloatVec4() ?? Color.White.ToRgbaFloatVec4();
        Texture2D texture = this._currentTexture;
        Rectangle sourceRec = this._currentSourceRect;
        Matrix4x4 transformMatrix = transform.GetMatrix();
        
        if (radSeg < 3) {
            radSeg = 3;
        }
        
        if (sides < 3) {
            sides = 3;
        }
        
        // Calculate source rectangle UVs.
        float uLeft = sourceRec.X / (float) texture.Width;
        float uRight = (sourceRec.X + sourceRec.Width) / (float) texture.Width;
        float vTop = sourceRec.Y / (float) texture.Height;
        float vBottom = (sourceRec.Y + sourceRec.Height) / (float) texture.Height;
        
        float step = MathF.Tau / radSeg;
        float sideStep = MathF.Tau / sides;
        
        // Total counts for Prepare.
        int vertexCount = (radSeg + 1) * (sides + 1);
        int indexCount = radSeg * sides * 6;
        
        int baseVertex = this.Prepare(PrimitiveTopology.TriangleList, vertexCount, indexCount);
        
        // Calculate the vertices.
        for (int rad = 0; rad <= radSeg; rad++) {
            float t = rad * step;
            
            float x = MathF.Sin(t) + 2.0F * MathF.Sin(2.0F * t);
            float y = MathF.Cos(t) - 2.0F * MathF.Cos(2.0F * t);
            float z = -MathF.Sin(3.0F * t);
            
            Vector3 center = new Vector3(x, y, z) * (radius / 6.0F);
            
            Vector3 tangent = Vector3.Normalize(new Vector3(
                MathF.Cos(t) + 4.0F * MathF.Cos(2.0F * t),
                -MathF.Sin(t) + 4.0F * MathF.Sin(2.0F * t),
                -3.0F * MathF.Cos(3.0F * t)
            ));
            
            Vector3 normal = Vector3.Normalize(new Vector3(-tangent.Y, tangent.X, 0.0F));
            Vector3 binormal = Vector3.Cross(tangent, normal);
            
            for (int side = 0; side <= sides; side++) {
                float sideAngle = side * sideStep;
                float cosAngle = MathF.Cos(sideAngle);
                float sinAngle = MathF.Sin(sideAngle);
                
                Vector3 offset = normal * cosAngle * (tubeRadius / 6.0F) + binormal * sinAngle * (tubeRadius / 6.0F);
                Vector3 position = center + offset;
                
                Vector2 texCoords = new Vector2(
                    float.Lerp(uLeft, uRight, (float) rad / radSeg),
                    float.Lerp(vTop, vBottom, (float) side / sides)
                );
                
                this._vertices[this._vertexCount++] = new ImmediateVertex3D() {
                    Position = Vector3.Transform(position, transformMatrix),
                    TexCoords = texCoords,
                    Color = colorVec4
                };
            }
        }
        
        // Calculate the indices.
        for (int rad = 0; rad < radSeg; rad++) {
            for (int side = 0; side < sides; side++) {
                int current = rad * (sides + 1) + side;
                int next = current + sides + 1;
                
                // First triangle.
                this._indices[this._indexCount++] = (uint)(baseVertex + current);
                this._indices[this._indexCount++] = (uint)(baseVertex + next);
                this._indices[this._indexCount++] = (uint)(baseVertex + next + 1);
                
                // Second triangle.
                this._indices[this._indexCount++] = (uint)(baseVertex + current);
                this._indices[this._indexCount++] = (uint)(baseVertex + next + 1);
                this._indices[this._indexCount++] = (uint)(baseVertex + current + 1);
            }
        }
    }
    
    /// <summary>
    /// Renders the wireframe of a knot shape using the specified transformation, radii, and segments.
    /// </summary>
    /// <param name="transform">The transformation applied to the knot wireframe.</param>
    /// <param name="radius">The radius of the knot.</param>
    /// <param name="tubeRadius">The radius of the tube comprising the knot's wireframe.</param>
    /// <param name="radSeg">The number of radial segments making up the knot. Minimum value is 3.</param>
    /// <param name="sides">The number of sides for the tube cross-section. Minimum value is 3.</param>
    /// <param name="color">An optional color to use for the wireframe; if null, the default is white.</param>
    public void DrawKnotWires(Transform transform, float radius, float tubeRadius, int radSeg, int sides, Color? color = null) {
        Vector4 colorVec4 = color?.ToRgbaFloatVec4() ?? Color.White.ToRgbaFloatVec4();
        Matrix4x4 transformMatrix = transform.GetMatrix();
        
        if (radSeg < 3) {
            radSeg = 3;
        }
        
        if (sides < 3) {
            sides = 3;
        }
        
        // Step sizes for radial progression and tube sides.
        float step = MathF.Tau / radSeg;
        float sideStep = MathF.Tau / sides;
        
        // Total vertex and index counts (used for buffer preparation).
        int vertexCount = (radSeg + 1) * (sides + 1);
        int indexCount = (radSeg * (sides + 1)) * 2 + ((radSeg + 1) * sides) * 2;
        
        // Prepare renderer and get base vertex offset.
        int baseVertex = this.Prepare(PrimitiveTopology.LineList, vertexCount, indexCount);
        
        // Generate vertices and edges for the wireframe knot.
        for (int rad = 0; rad <= radSeg; rad++) {
            float t = rad * step;
            
            // Parametric equation for knot center.
            float x = MathF.Sin(t) + 2.0F * MathF.Sin(2.0F * t);
            float y = MathF.Cos(t) - 2.0F * MathF.Cos(2.0F * t);
            float z = -MathF.Sin(3.0F * t);
            
            Vector3 center = new Vector3(x, y, z) * (radius / 6.0F);
            
            // Calculate tangent direction.
            Vector3 tangent = Vector3.Normalize(new Vector3(
                MathF.Cos(t) + 4.0F * MathF.Cos(2.0F * t),
                -MathF.Sin(t) + 4.0F * MathF.Sin(2.0F * t),
                -3.0F * MathF.Cos(3.0F * t)
            ));
            
            // Generate normal and binormal vectors.
            Vector3 normal = Vector3.Normalize(new Vector3(-tangent.Y, tangent.X, 0.0F));
            Vector3 binormal = Vector3.Cross(tangent, normal);
            
            for (int side = 0; side <= sides; side++) {
                float sideAngle = side * sideStep;
                float cosAngle = MathF.Cos(sideAngle);
                float sinAngle = MathF.Sin(sideAngle);
                
                // Offset from center to form tube surface.
                Vector3 offset = normal * cosAngle * (tubeRadius / 6.0F) + binormal * sinAngle * (tubeRadius / 6.0F);
                Vector3 position = center + offset;
                
                // Calculate local vertex index (relative to this draw call).
                int localIndex = rad * (sides + 1) + side;
                
                // Add vertex to the buffer.
                this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                    Position = Vector3.Transform(position, transformMatrix),
                    Color = colorVec4
                };
                
                // Connect to next radial segment.
                if (rad < radSeg) {
                    int nextLocal = (rad + 1) * (sides + 1) + side;
                    
                    this._indices[this._indexCount++] = (uint)(baseVertex + localIndex);
                    this._indices[this._indexCount++] = (uint)(baseVertex + nextLocal);
                }
                
                // Connect to the next side (wrapping around at the end).
                if (side < sides) {
                    int nextLocal = rad * (sides + 1) + (side + 1);
                    
                    this._indices[this._indexCount++] = (uint)(baseVertex + localIndex);
                    this._indices[this._indexCount++] = (uint)(baseVertex + nextLocal);
                }
            }
        }
    }

    /// <summary>
    /// Renders a grid using the specified command list, output description, transformation matrix, and grid parameters.
    /// </summary>
    /// <param name="transform">The transformation matrix applied to the grid.</param>
    /// <param name="slices">The number of grid divisions or slices. Must be greater than or equal to 1.</param>
    /// <param name="spacing">The distance between adjacent grid lines. Must be greater than or equal to 1.</param>
    /// <param name="majorLineSpacing">The interval of major grid lines, which are visually distinct. Must be greater than or equal to 1.</param>
    /// <param name="color">The optional color of the grid lines. Defaults to white if not provided.</param>
    /// <param name="axisColorX">The optional color for the X-axis grid line. Defaults to red if not provided.</param>
    /// <param name="axisColorZ">The optional color for the Z-axis grid line. Defaults to blue if not provided.</param>
    public void DrawGrid(Transform transform, int slices, int spacing, int majorLineSpacing, Color? color = null, Color? axisColorX = null, Color? axisColorZ = null) {
        Vector4 colorVec4 = color?.ToRgbaFloatVec4() ?? Color.White.ToRgbaFloatVec4();
        Vector4 colorLightVec4 = new RgbaFloat(colorVec4.X * 1.5F, colorVec4.Y * 1.5F, colorVec4.Z * 1.5F, colorVec4.W).ToVector4();
        Vector4 axisColorXVec4 = axisColorX?.ToRgbaFloatVec4() ?? Color.Red.ToRgbaFloatVec4();
        Vector4 axisColorZVec4 = axisColorZ?.ToRgbaFloatVec4() ?? Color.Blue.ToRgbaFloatVec4();
        
        Matrix4x4 transformMatrix = transform.GetMatrix();
        
        if (slices < 1) {
            slices = 1;
        }
        
        if (spacing < 1) {
            spacing = 1;
        }
        
        if (majorLineSpacing < 1) {
            majorLineSpacing = 1;
        }
        
        float halfSize = slices * spacing * 0.5F;
        
        int totalVertices = (slices + 1) * 4;
        int totalIndices = (slices + 1) * 4;
        
        // Prepare.
        int baseVertex = this.Prepare(PrimitiveTopology.LineList, totalVertices, totalIndices);
        
        for (int i = 0; i <= slices; i++) {
            float offset = -halfSize + i * spacing;
            
            Vector4 currentColor;
            
            if (i == slices / 2) {
                currentColor = Vector4.Zero;
            }
            else if (i % majorLineSpacing == 0) {
                currentColor = colorLightVec4;
            }
            else {
                currentColor = colorVec4;
            }
            
            int vStart = this._vertexCount;
            
            this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                Position = Vector3.Transform(new Vector3(offset, 0.0F, -halfSize), transformMatrix),
                Color = (i == slices / 2) ? axisColorXVec4 : currentColor
            };

            this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                Position = Vector3.Transform(new Vector3(offset, 0.0F, halfSize), transformMatrix),
                Color = (i == slices / 2) ? axisColorXVec4 : currentColor
            };
            
            this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                Position = Vector3.Transform(new Vector3(-halfSize, 0.0F, offset), transformMatrix),
                Color = (i == slices / 2) ? axisColorZVec4 : currentColor
            };
            
            this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                Position = Vector3.Transform(new Vector3(halfSize, 0.0F, offset), transformMatrix),
                Color = (i == slices / 2) ? axisColorZVec4 : currentColor
            };
            
            // Add indices for line pairs along X and Z.
            this._indices[this._indexCount++] = (uint) (vStart + 0);
            this._indices[this._indexCount++] = (uint) (vStart + 1);
            
            this._indices[this._indexCount++] = (uint) (vStart + 2);
            this._indices[this._indexCount++] = (uint) (vStart + 3);
        }
    }

    /// <summary>
    /// Draws the edges of the specified bounding box using the given transformation and color.
    /// </summary>
    /// <param name="transform">The transformation to apply to the bounding box before rendering.</param>
    /// <param name="box">The bounding box to be drawn.</param>
    /// <param name="color">The color to use for the bounding box edges. If null, white is used as the default color.</param>
    public void DrawBoundingBox(Transform transform, BoundingBox box, Color? color = null) {
        Vector4 colorVec4 = color?.ToRgbaFloatVec4() ?? Color.White.ToRgbaFloatVec4();
        Matrix4x4 transformMatrix = transform.GetMatrix();
        
        // Prepare.
        int baseVertex = this.Prepare(PrimitiveTopology.LineList, 8, 24);
        
        for (int i = 0; i < 8; i++) {
            
            // Calculate the x, y, z coordinates.
            float x = (i & 1) == 0 ? box.Min.X : box.Max.X;
            float y = (i & 2) == 0 ? box.Min.Y : box.Max.Y;
            float z = (i & 4) == 0 ? box.Min.Z : box.Max.Z;
            
            // Add the vertex to the list.
            this._vertices[this._vertexCount++] = new ImmediateVertex3D {
                Position = Vector3.Transform(new Vector3(x, y, z), transformMatrix),
                Color = colorVec4
            };
            
            // Connect the vertex to its neighbors.
            for (int bit = 0; bit < 3; bit++) {
                if ((i & (1 << bit)) == 0) {
                    int neighbor = i | (1 << bit);
                    this._indices[this._indexCount++] = (uint) (baseVertex + i);
                    this._indices[this._indexCount++] = (uint) (baseVertex + neighbor);
                }
            }
        }
    }
    
    /// <summary>
    /// Draws a line between two points in 3D space with a specified optional color.
    /// </summary>
    /// <param name="startPos">The starting position of the line in 3D space.</param>
    /// <param name="endPos">The ending position of the line in 3D space.</param>
    /// <param name="color">The optional color of the line. If not provided, defaults to white.</param>
    public void DrawLine(Vector3 startPos, Vector3 endPos, Color? color = null) {
        Vector4 colorVec4 = color?.ToRgbaFloatVec4() ?? Color.White.ToRgbaFloatVec4();
        
        // Prepare.
        int baseVertex = this.Prepare(PrimitiveTopology.LineList, 2, 2);
        
        // Add start vertex.
        this._vertices[this._vertexCount++] = new ImmediateVertex3D {
            Position = startPos,
            Color = colorVec4
        };
        
        // Add end vertex.
        this._vertices[this._vertexCount++] = new ImmediateVertex3D {
            Position = endPos,
            Color = colorVec4
        };
        
        // Add indices for the line.
        this._indices[this._indexCount++] = (uint) (baseVertex + 0);
        this._indices[this._indexCount++] = (uint) (baseVertex + 1);
    }
    
    /// <summary>
    /// Draws a billboard at the specified position with optional scaling and color parameters.
    /// </summary>
    /// <param name="position">The 3D position where the billboard will be rendered.</param>
    /// <param name="scale">The optional scale factor for the billboard. Defaults to a scale of 1 if not specified.</param>
    /// <param name="color">The optional color of the billboard. Defaults to white if not specified.</param>
    public void DrawBillboard(Vector3 position, Vector2? scale = null, Color? color = null) {
        Vector4 colorVec4 = color?.ToRgbaFloatVec4() ?? Color.White.ToRgbaFloatVec4();
        Vector2 finalScale = scale ?? Vector2.One;
        
        Cam3D? cam3D = Cam3D.ActiveCamera;
        
        if (cam3D == null) {
            return;
        }
        
        // Calculate the direction from billboard position to camera position.
        Vector3 directionToCamera = -Vector3.Normalize(cam3D.Position - position);
        
        // Project the camera's forward direction onto the horizontal plane (yaw axis).
        Vector3 cameraForwardFlat = -Vector3.Normalize(new Vector3(cam3D.GetForward().X, 0, cam3D.GetForward().Z));
        
        // Calculate the rotation angle between the projected camera forward and the direction to camera.
        float angle = (float) Math.Atan2(directionToCamera.X - cameraForwardFlat.X, directionToCamera.Z - cameraForwardFlat.Z);
        
        // Create billboard transform.
        Transform billboardTransform = new Transform {
            Translation = position,
            Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle),
            Scale = new Vector3(finalScale, 0)
        };
        
        Matrix4x4 transformMatrix = billboardTransform.GetMatrix();
        
        // Prepare.
        int baseVertex = this.Prepare(PrimitiveTopology.TriangleList, 4, 6);
        
        Texture2D texture = this._currentTexture;
        Rectangle sourceRec = this._currentSourceRect;
        
        // Calculate source rectangle UVs.
        float uLeft = sourceRec.X / (float) texture.Width;
        float uRight = (sourceRec.X + sourceRec.Width) / (float) texture.Width;
        float vTop = sourceRec.Y / (float) texture.Height;
        float vBottom = (sourceRec.Y + sourceRec.Height) / (float) texture.Height;
        
        // Calculate half size with aspect ratio preserved.
        Vector3 halfSize = new Vector3(sourceRec.Width / 100.0F, sourceRec.Height / 100.0F, 0.0F) / 2.0F;
        
        // Add vertices.
        this._vertices[this._vertexCount++] = new ImmediateVertex3D {
            Position = Vector3.Transform(new Vector3(-halfSize.X, -halfSize.Y, 0.0F), transformMatrix),
            TexCoords = new Vector2(uRight, vBottom),
            Color = colorVec4
        };
        
        this._vertices[this._vertexCount++] = new ImmediateVertex3D {
            Position = Vector3.Transform(new Vector3(halfSize.X, -halfSize.Y, 0.0F), transformMatrix),
            TexCoords = new Vector2(uLeft, vBottom),
            Color = colorVec4
        };
        
        this._vertices[this._vertexCount++] = new ImmediateVertex3D {
            Position = Vector3.Transform(new Vector3(halfSize.X, halfSize.Y, 0.0F), transformMatrix),
            TexCoords = new Vector2(uLeft, vTop),
            Color = colorVec4
        };
        
        this._vertices[this._vertexCount++] = new ImmediateVertex3D {
            Position = Vector3.Transform(new Vector3(-halfSize.X, halfSize.Y, 0.0F), transformMatrix),
            TexCoords = new Vector2(uRight, vTop),
            Color = colorVec4
        };
        
        // Add indices for 2 triangles making up the quad.
        this._indices[this._indexCount++] = (uint) (baseVertex + 0);
        this._indices[this._indexCount++] = (uint) (baseVertex + 1);
        this._indices[this._indexCount++] = (uint) (baseVertex + 2);
        
        this._indices[this._indexCount++] = (uint) (baseVertex + 2);
        this._indices[this._indexCount++] = (uint) (baseVertex + 3);
        this._indices[this._indexCount++] = (uint) (baseVertex + 0);
    }
    
    /// <summary>
    /// Submits arbitrary geometry to the renderer using the specified topology.
    /// </summary>
    /// <param name="vertices">The list of vertices that define the geometry.</param>
    /// <param name="indices">Optional index list. If null or empty, sequential indices are generated automatically.</param>
    /// <param name="topology">The primitive topology used to interpret the geometry (e.g., TriangleList, LineList).</param>
    public void DrawGeometry(ReadOnlySpan<ImmediateVertex3D> vertices, ReadOnlySpan<uint> indices = default, PrimitiveTopology topology = PrimitiveTopology.TriangleList) {
        if (vertices.IsEmpty) {
            return;
        }
        
        int baseVertex = this.Prepare(topology, vertices.Length, indices.IsEmpty ? vertices.Length : indices.Length);
        
        vertices.CopyTo(new Span<ImmediateVertex3D>(this._vertices, this._vertexCount, vertices.Length));
        this._vertexCount += vertices.Length;

        if (!indices.IsEmpty) {
            for (int i = 0; i < indices.Length; i++) {
                this._indices[this._indexCount++] = (uint) (baseVertex + indices[i]);
            }
        }
        else {
            for (int i = 0; i < vertices.Length; i++) {
                this._indices[this._indexCount++] = (uint) (baseVertex + i);
            }
        }
    }
    
    /// <summary>
    /// Prepares the renderer for drawing by ensuring sufficient buffer space and synchronizing pipeline state.
    /// Flushes the current batch if required due to state changes or capacity limits.
    /// </summary>
    /// <param name="topology">The primitive topology for the upcoming draw call.</param>
    /// <param name="vertexCount">The number of vertices that will be submitted.</param>
    /// <param name="indexCount">The number of indices that will be submitted.</param>
    /// <returns>The base vertex index for the new geometry within the batch.</returns>
    private int Prepare(PrimitiveTopology topology, int vertexCount, int indexCount) {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        if (vertexCount > this.Capacity) {
            throw new InvalidOperationException($"The number of provided vertices exceeds the capacity! [{vertexCount} > {this.Capacity}]");
        }
        
        if (indexCount > this.Capacity * 3) {
            throw new InvalidOperationException($"The number of provided indices exceeds the capacity! [{indexCount} > {this.Capacity * 3}]");
        }
        
        bool stateChanged = !this._currentOutput.Equals(this._requestedOutput) ||
                            !this._currentEffect.Equals(this._requestedEffect) ||
                            !this._currentBlendState.Equals(this._requestedBlendState) ||
                            !this._currentDepthStencilState.Equals(this._requestedDepthStencilState) ||
                            !this._currentRasterizerState.Equals(this._requestedRasterizerState) ||
                            !this._currentTexture.Equals(this._requestedTexture) ||
                            !this._currentSampler.Equals(this._requestedSampler) ||
                            this._currentTopology != topology;
        
        if (stateChanged) {
            this.Flush();
        }
        
        this._currentOutput = this._requestedOutput;
        this._currentEffect = this._requestedEffect;
        this._currentBlendState = this._requestedBlendState;
        this._currentDepthStencilState = this._requestedDepthStencilState;
        this._currentRasterizerState = this._requestedRasterizerState;
        this._currentTexture = this._requestedTexture;
        this._currentSourceRect = this._requestedSourceRect; 
        this._currentSampler = this._requestedSampler;
        this._currentTopology = topology;
            
        // Update pipeline description.
        this._pipelineDescription.BlendState = this._currentBlendState;
        this._pipelineDescription.DepthStencilState = this._currentDepthStencilState;
        this._pipelineDescription.RasterizerState = this._currentRasterizerState;
        this._pipelineDescription.BufferLayouts = this._currentEffect.GetBufferLayouts();
        this._pipelineDescription.TextureLayouts = this._currentEffect.GetTextureLayouts();
        this._pipelineDescription.ShaderSet = new ShaderSetDescription(ImmediateVertex3D.VertexLayout.Layouts, this._currentEffect.Shaders);
        this._pipelineDescription.PrimitiveTopology = this._currentTopology;
        this._pipelineDescription.Outputs = this._currentOutput;
        
        if (this._vertexCount + vertexCount > this._vertices.Length || this._indexCount + indexCount > this._indices.Length) {
            this.Flush();
        }
        
        return this._vertexCount;
    }
    
    /// <summary>
    /// Submits the currently batched geometry to the GPU and resets the batch buffers.
    /// Applies the active pipeline state, updates GPU buffers, and issues the appropriate draw call.
    /// </summary>
    private void Flush() {
        if (this._vertexCount == 0) {
            return;
        }
        
        Cam3D? cam3D = Cam3D.ActiveCamera;
        
        if (cam3D == null) {
            
            // Reset indexer.
            this._vertexCount = 0;
            this._indexCount = 0;
            return;
        }
        
        // Set pipeline.
        this._currentCommandList.SetPipeline(this._currentEffect.GetPipeline(this._pipelineDescription).Pipeline);
        
        // Set matrix buffer.
        this._currentCommandList.SetGraphicsResourceSet(this._currentEffect.GetBufferLayoutSlot("MatrixBuffer"), cam3D.GetMatrixBuffer().GetResourceSet(this._currentEffect.GetBufferLayout("MatrixBuffer")));
        
        // Set resourceSet of the texture.
        this._currentCommandList.SetGraphicsResourceSet(this._currentEffect.GetTextureLayoutSlot("fTexture"), this._currentTexture.GetResourceSet(this._currentSampler, this._currentEffect.GetTextureLayout("fTexture")));
        
        // Apply effect.
        this._currentEffect.Apply(this._currentCommandList);
        
        // Draw and set vertex/index buffers.
        if (this._indexCount > 0) {
            
            // Update vertex and index buffer.
            this._currentCommandList.UpdateBuffer(this._vertexBuffer, 0, new ReadOnlySpan<ImmediateVertex3D>(this._vertices, 0, this._vertexCount));
            this._currentCommandList.UpdateBuffer(this._indexBuffer, 0, new ReadOnlySpan<uint>(this._indices, 0, this._indexCount));
            
            // Set vertex and index buffer.
            this._currentCommandList.SetVertexBuffer(0, this._vertexBuffer);
            this._currentCommandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt32);
            
            // Draw.
            this._currentCommandList.DrawIndexed((uint) this._indexCount);
        }
        else {
            
            // Update vertex buffer.
            this._currentCommandList.UpdateBuffer(this._vertexBuffer, 0, new ReadOnlySpan<ImmediateVertex3D>(this._vertices, 0, this._vertexCount));
            
            // Set vertex buffer.
            this._currentCommandList.SetVertexBuffer(0, this._vertexBuffer);
            
            // Draw.
            this._currentCommandList.Draw((uint) this._vertexCount);
        }
        
        // Reset indexer.
        this._vertexCount = 0;
        this._indexCount = 0;
        
        this.DrawCallCount++;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._vertexBuffer.Dispose();
            this._indexBuffer.Dispose();
        }
    }
}
