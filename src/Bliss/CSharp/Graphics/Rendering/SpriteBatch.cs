using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Geometry;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Textures;
using Veldrid;

namespace Bliss.CSharp.Graphics.Rendering;

public class SpriteBatch : Disposable {
    
    private const uint VertexPerQuad = 4;
    private const uint IndexPerQuad = 6;
    
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    public int DrawCallCount { get; private set; }

    private CommandList _commandList;
    
    private Dictionary<(Texture2D, Sampler), ResourceSet> _cachedTextures;
    
    private Vertex2D[] _vertices;
    private ushort[] _indices;
    
    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;

    private Effect _defaultEffect;
    private ResourceLayout _defaultPipelineResourceLayout;
    private SimplePipeline _defaultPipeline;
    
    private bool _begun;

    private uint _currentSpritesCount;

    private Texture2D? _currentTexture;
    private Sampler? _currentSampler;

    private SimplePipeline _currentPipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpriteBatch"/> class with the specified graphics device and optional capacity.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create resources and manage rendering.</param>
    /// <param name="capacity">The maximum number of sprites that can be batched together. Defaults to 15360.</param>
    public SpriteBatch(GraphicsDevice graphicsDevice, uint capacity = 15360) {
        this.GraphicsDevice = graphicsDevice;
        this._commandList = graphicsDevice.ResourceFactory.CreateCommandList();
        
        this._cachedTextures = new Dictionary<(Texture2D, Sampler), ResourceSet>();
        
        this._vertices = new Vertex2D[capacity * VertexPerQuad];
        this._indices = new ushort[capacity * IndexPerQuad];
        
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint) (capacity * VertexPerQuad * Marshal.SizeOf<Vertex2D>()), BufferUsage.VertexBuffer));
        this._indexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(capacity * IndexPerQuad * sizeof(ushort), BufferUsage.IndexBuffer));
        
        this.CreateDefaultPipeline();
    }

    // TODO: ADD TRANSFORM
    /// <summary>
    /// Begins the SpriteBatch process, allowing sprite drawing commands to be issued.
    /// This method must be called before any draw calls are made.
    /// It sets the transformation matrix and the rendering pipeline to be used for the batch.
    /// Throws an exception if called while a SpriteBatch is already in progress.
    /// </summary>
    /// <param name="transform">The transformation matrix to apply to all sprites in this batch. If null, the identity matrix is used.</param>
    /// <param name="pipeline">The rendering pipeline to use for this batch. If null, the default pipeline is used.</param>
    public void Begin(Matrix4x4? transform = null, SimplePipeline? pipeline = null) {
        if (this._begun) {
            throw new Exception("The SpriteBatch has already begun!");
        }

        this._begun = true;

        this._currentPipeline = pipeline ?? this._defaultPipeline;
        
        this.DrawCallCount = 0;
    }

    /// <summary>
    /// Ends the SpriteBatch process, ensuring that all batched sprites are rendered.
    /// This method concludes the drawing phase and must be called after all draw calls are made.
    /// It will throw an exception if the `Begin` method was not called previously.
    /// </summary>
    public void End() {
        if (!this._begun) {
            throw new Exception("The SpriteBatch has not begun yet!");
        }

        this._begun = false;
    }

    // TODO: you need to set the vertices and indices (Look at the AddQuad method).
    /// <summary>
    /// Draws the specified texture at the given position with specified scale, origin, rotation, and color.
    /// Applies the provided sampler for texture sampling.
    /// </summary>
    /// <param name="texture">The texture to be drawn.</param>
    /// <param name="position">The position where the texture should be drawn.</param>
    /// <param name="scale">The scale factor for the texture.</param>
    /// <param name="origin">The origin point for rotation and drawing.</param>
    /// <param name="rotation">The rotation angle in radians.</param>
    /// <param name="color">The color to tint the texture.</param>
    /// <param name="sampler">The sampler to use for texture sampling.</param>
    public void DrawTexture(Texture2D texture, Vector2 position, Vector2 scale, Vector2 origin, float rotation, Color color, Sampler sampler) {
        this._currentTexture = texture;
        this._currentSampler = sampler;
        
        this.Flush();
    }
    
    // TODO: ADD TRANSFORM
    /// <summary>
    /// Submits the currently batched sprites to the graphics device for rendering.
    /// This involves updating the vertex and index buffers, setting the necessary pipeline
    /// states, and issuing the draw command to render the sprites.
    /// </summary>
    public void Flush() {
        if (this._currentSpritesCount == 0) {
            return;
        }
        
        // Update buffers.
        this._commandList.UpdateBuffer(this._vertexBuffer, 0, ref this._vertices[0], (uint) (this._vertices.Length * Marshal.SizeOf<Vertex2D>()));
        this._commandList.UpdateBuffer(this._indexBuffer, 0, ref this._indices[0], (uint) (this._indices.Length * sizeof(ushort)));
        
        // Set vertex and index buffers.
        this._commandList.SetVertexBuffer(0, this._vertexBuffer);
        this._commandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt16);
        
        // Set pipeline.
        this._commandList.SetPipeline(this._currentPipeline.Pipeline);

        // Create or get texture.
        if (this._currentTexture != null && this._currentSampler != null) {
            if (!this._cachedTextures.TryGetValue((this._currentTexture, this._currentSampler), out ResourceSet? resourceSet)) {
                ResourceSet newResourceSet = this.GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(this._currentPipeline.ResourceLayout, this._currentTexture.DeviceTexture, this._currentSampler));
                
                this._cachedTextures.Add((this._currentTexture, this._currentSampler), newResourceSet);
                this._commandList.SetGraphicsResourceSet(0, newResourceSet);
            }
            else {
                this._commandList.SetGraphicsResourceSet(0, resourceSet);
            }
        }
        
        this._commandList.DrawIndexed(this._currentSpritesCount * IndexPerQuad);
        this._currentSpritesCount = 0;
        
        this.DrawCallCount++;
    }

    /// <summary>
    /// Creates the default rendering pipeline for the SpriteBatch.
    /// This includes setting up the default shader programs and
    /// configuring the necessary resource layouts and pipeline states.
    /// </summary>
    private void CreateDefaultPipeline() {
        this._defaultEffect = new Effect(this.GraphicsDevice.ResourceFactory, new VertexLayoutDescription() {
            Elements = [ // TODO: UPDATE THE SHADER PARAMS
                new VertexElementDescription("vertexPosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("vertexTexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("vertexColor", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4)
            ]
        }, "content/shaders/default_shader.vert", "content/shaders/default_shader.frag");
        
        this._defaultPipelineResourceLayout = this.GraphicsDevice.ResourceFactory.CreateResourceLayout(
            new ResourceLayoutDescription( // TODO: UPDATE THE SHADER PARAMS
                new ResourceLayoutElementDescription("MVP", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("Color", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("texture0", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)
            )
        );

        this._defaultPipeline = new SimplePipeline(this.GraphicsDevice, this._defaultEffect, this._defaultPipelineResourceLayout, this.GraphicsDevice.SwapchainFramebuffer.OutputDescription, BlendStateDescription.SingleOverrideBlend, FaceCullMode.Back);
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._commandList.Dispose();

            foreach (ResourceSet resourceSet in this._cachedTextures.Values) {
                resourceSet.Dispose();
            }
            
            this._vertexBuffer.Dispose();
            this._indexBuffer.Dispose();
            
            this._defaultEffect.Dispose();
            this._defaultPipelineResourceLayout.Dispose();
            this._defaultPipeline.Dispose();
        }
    }
}