using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Geometry;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Textures;
using Veldrid;

namespace Bliss.CSharp.Graphics.Rendering.Sprites;

public class SpriteBatch : Disposable {
    
    private const uint VerticesPerQuad = 4;
    private const uint IndicesPerQuad = 6;
    
    private static Vector2[] _vertexTemplate = new Vector2[] {
        new Vector2(0.0F, 0.0F),
        new Vector2(1.0F, 0.0F),
        new Vector2(0.0F, 1.0F),
        new Vector2(1.0F, 1.0F),
    };
    
    private static ushort[] _indicesTemplate = new ushort[] {
        2, 1, 0,
        2, 3, 1
    };
    
    public GraphicsDevice GraphicsDevice { get; private set; }
    public uint Capacity { get; }
    
    public int DrawCallCount { get; private set; }
    
    private Dictionary<(Texture2D, Sampler), ResourceSet> _cachedTextures;
    
    private Vertex2D[] _vertices;
    private ushort[] _indices;
    
    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;

    private Effect _defaultEffect;
    private ResourceLayout _defaultPipelineResourceLayout;
    private SimplePipeline _defaultPipeline;
    
    private bool _begun;

    private CommandList _currentCommandList;
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
        this.Capacity = capacity;
        
        this._cachedTextures = new Dictionary<(Texture2D, Sampler), ResourceSet>();
        
        this._vertices = new Vertex2D[capacity * VerticesPerQuad];
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint) (capacity * VerticesPerQuad * Marshal.SizeOf<Vertex2D>()), BufferUsage.VertexBuffer));
        
        this._indices = new ushort[capacity * IndicesPerQuad];

        for (int i = 0; i < capacity; i++) {
            var startIndex = i * IndicesPerQuad;
            var offset = i * VerticesPerQuad;

            this._indices[startIndex + 0] = (ushort) (_indicesTemplate[0] + offset);
            this._indices[startIndex + 1] = (ushort) (_indicesTemplate[1] + offset);
            this._indices[startIndex + 2] = (ushort) (_indicesTemplate[2] + offset);
            
            this._indices[startIndex + 3] = (ushort) (_indicesTemplate[3] + offset);
            this._indices[startIndex + 4] = (ushort) (_indicesTemplate[4] + offset);
            this._indices[startIndex + 5] = (ushort) (_indicesTemplate[5] + offset);
        }
        
        this._indexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint) this._indices.Length * sizeof(ushort), BufferUsage.IndexBuffer));
        
        this.CreateDefaultPipeline();
    }

    // TODO: ADD TRANSFORM ( SRY VIEW)

    /// <summary>
    /// Begins a new sprite batch, preparing the specified command list for rendering with optional parameters for view matrix and pipeline.
    /// </summary>
    /// <param name="commandList">The command list used to record rendering commands.</param>
    /// <param name="view">An optional view matrix to transform the sprites. If null, no transformation is applied.</param>
    /// <param name="pipeline">An optional rendering pipeline to use. If null, the default pipeline is used.</param>
    public void Begin(CommandList commandList, Matrix4x4? view = null, SimplePipeline? pipeline = null) {
        if (this._begun) {
            throw new Exception("The SpriteBatch has already begun!");
        }

        this._begun = true;
        this._currentCommandList = commandList;

        if (this._currentPipeline != pipeline) {
            this.Flush();
        }
        
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

    /// <summary>
    /// Draws a texture using the specified parameters, including position, source rectangle, scale, origin, rotation, color, and flipping.
    /// </summary>
    /// <param name="texture">The texture to be drawn.</param>
    /// <param name="sampler">The sampler state to use for texture sampling.</param>
    /// <param name="position">The position where the texture will be drawn.</param>
    /// <param name="sourceRect">The source rectangle within the texture. If null, the entire texture is used.</param>
    /// <param name="scale">The scale factor for resizing the texture. If null, the texture is drawn at its original size.</param>
    /// <param name="origin">The origin point for rotation and scaling. If null, the origin is set to the top-left corner.</param>
    /// <param name="rotation">The rotation angle in radians.</param>
    /// <param name="color">The color to tint the texture. If null, the texture is drawn with its original colors.</param>
    /// <param name="flip">Specifies how the texture should be flipped horizontally or vertically.</param>
    public void DrawTexture(Texture2D texture, Sampler sampler, Vector2 position, Rectangle? sourceRect = null, Vector2? scale = null, Vector2? origin = null, float rotation = 0, Color? color = null, SpriteFlip flip = SpriteFlip.None) {
        if (!this._begun) {
            throw new Exception("You must begin the SpriteBatch before calling draw methods!");
        }
        
        if (this._currentTexture != texture || this._currentSampler != sampler) {
            this.Flush();
        }
        
        this._currentTexture = texture;
        this._currentSampler = sampler;

        Rectangle finalSource = sourceRect ?? new Rectangle(0, 0, (int) texture.Width, (int) texture.Height);
        Color finalColor = color ?? Color.White;
        Vector2 finalScale = scale ?? new Vector2(1.0F, 1.0F);
        Vector2 finalOrigin = origin ?? new Vector2(0.0F, 0.0F);
        
        Vector2 spriteScale = new Vector2(finalSource.Width, finalSource.Height) * finalScale;
        Vector2 spriteOrigin = finalOrigin * finalScale;
        
        float texelWidth = 1.0F / texture.Width;
        float texelHeight = 1.0F / texture.Height;
        
        bool flipX = (flip == SpriteFlip.Horizontal || flip == SpriteFlip.Both);
        bool flipY = (flip == SpriteFlip.Vertical || flip == SpriteFlip.Both);

        float sin = 0;
        float cos = 0;
        float nOriginX = -spriteOrigin.X;
        float nOriginY = -spriteOrigin.Y;

        if (rotation != 0.0F) {
            float radiansRot = Single.DegreesToRadians(rotation);
            sin = MathF.Sin(radiansRot);
            cos = MathF.Cos(radiansRot);
        }
        
        Vertex2D topLeft = new Vertex2D() {
            Position = rotation == 0.0F 
                ? new Vector2(
                    position.X - spriteOrigin.X,
                    position.Y - spriteOrigin.Y)
                : new Vector2(
                    position.X + nOriginX * cos - nOriginY * sin,
                    position.Y + nOriginX * sin + nOriginY * cos),
            TexCoords = new Vector2(
                flipX ? (finalSource.X + finalSource.Width) * texelWidth : finalSource.X * texelWidth,
                flipY ? (finalSource.Y + finalSource.Height) * texelHeight : finalSource.Y * texelHeight),
            Color = finalColor.ToVector4()
        };
        
        float x = _vertexTemplate[(int) VertexTemplate.TopRight].X;
        float w = spriteScale.X * x;

        Vertex2D topRight = new Vertex2D() {
            Position = rotation == 0.0F
                ? new Vector2(
                    (position.X - spriteOrigin.X) + w,
                    position.Y - spriteOrigin.Y)
                : new Vector2(
                    position.X + (nOriginX + w) * cos - nOriginY * sin,
                    position.Y + (nOriginX + w) * sin + nOriginY * cos),
            TexCoords = new Vector2(
                flipX ? finalSource.X * texelWidth : (finalSource.X + finalSource.Width) * texelWidth,
                flipY ? (finalSource.Y + finalSource.Height) * texelHeight : finalSource.Y * texelHeight),
            Color = finalColor.ToVector4()
        };
        
        float y = _vertexTemplate[(int) VertexTemplate.BottomLeft].Y;
        float h = spriteScale.Y * y;

        Vertex2D bottomLeft = new Vertex2D() {
            Position = rotation == 0.0F
                ? new Vector2(
                    position.X - spriteOrigin.X,
                    position.Y - spriteOrigin.Y + h)
                : new Vector2(
                    position.X + nOriginX * cos - (nOriginY + h) * sin,
                    position.Y + nOriginX * sin + (nOriginY + h) * cos),
            TexCoords = new Vector2(
                flipX ? (finalSource.X + finalSource.Width) * texelWidth : finalSource.X * texelWidth,
                flipY ? finalSource.Y * texelHeight : (finalSource.Y + finalSource.Height) * texelHeight),
            Color = finalColor.ToVector4()
        };
        
        x = _vertexTemplate[(int) VertexTemplate.BottomRight].X;
        y = _vertexTemplate[(int) VertexTemplate.BottomRight].Y;
        w = spriteScale.X * x;
        h = spriteScale.Y * y;

        Vertex2D bottomRight = new Vertex2D() {
            Position = rotation == 0.0F
                ? new Vector2(
                    position.X - spriteOrigin.X + w,
                    position.Y - spriteOrigin.Y + h)
                : new Vector2(
                    position.X + (nOriginX + w) * cos - (nOriginY + h) * sin,
                    position.Y + (nOriginX + w) * sin + (nOriginY + h) * cos),
            TexCoords = new Vector2(
                flipX ? finalSource.X * texelWidth : (finalSource.X + finalSource.Width) * texelWidth,
                flipY ? finalSource.Y * texelHeight : (finalSource.Y + finalSource.Height) * texelHeight),
            Color = finalColor.ToVector4()
        };
        
        this.AddQuad(topLeft, topRight, bottomLeft, bottomRight);
    }

    /// <summary>
    /// Adds a quad (a four-sided polygon) to the sprite batch. This method is used to define a quad using its four vertices.
    /// </summary>
    /// <param name="topLeft">The vertex at the top-left corner of the quad.</param>
    /// <param name="topRight">The vertex at the top-right corner of the quad.</param>
    /// <param name="bottomLeft">The vertex at the bottom-left corner of the quad.</param>
    /// <param name="bottomRight">The vertex at the bottom-right corner of the quad.</param>
    public void AddQuad(Vertex2D topLeft, Vertex2D topRight, Vertex2D bottomLeft, Vertex2D bottomRight) {
        if (this._currentSpritesCount >= (this.Capacity - 1)) {
            this.Flush();
        }
        
        uint index = this._currentSpritesCount * VerticesPerQuad;

        this._vertices[index] = topLeft;
        this._vertices[index + 1] = topRight;
        this._vertices[index + 2] = bottomLeft;
        this._vertices[index + 3] = bottomRight;

        this._currentSpritesCount += 1;
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
        
        // Update vertex buffer.
        this._currentCommandList.UpdateBuffer(this._vertexBuffer, 0, ref this._vertices[0], (uint) (this._currentSpritesCount * VerticesPerQuad * Marshal.SizeOf<Vertex2D>()));
        
        // Set vertex and index buffers.
        this._currentCommandList.SetVertexBuffer(0, this._vertexBuffer);
        this._currentCommandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt16);
        
        // Set pipeline.
        this._currentCommandList.SetPipeline(this._currentPipeline.Pipeline);

        // Create or get texture.
        if (this._currentTexture != null && this._currentSampler != null) {
            this._currentCommandList.SetGraphicsResourceSet(0, this.GetOrCreateTextureResourceSet(this._currentTexture, this._currentSampler));
        }
        
        // Draw.
        this._currentCommandList.DrawIndexed(this._currentSpritesCount * IndicesPerQuad);
        
        // Clean up.
        this._currentSpritesCount = 0;
        Array.Clear(this._vertices);
        
        this.DrawCallCount++;
    }

    /// <summary>
    /// Retrieves an existing texture resource set or creates a new one if it doesn't exist.
    /// </summary>
    /// <param name="texture">The texture to be used in the resource set.</param>
    /// <param name="sampler">The sampler to be used in the resource set.</param>
    /// <returns>The resource set that includes the specified texture and sampler.</returns>
    private ResourceSet GetOrCreateTextureResourceSet(Texture2D texture, Sampler sampler) {
        if (!this._cachedTextures.TryGetValue((texture, sampler), out ResourceSet? resourceSet)) {
            ResourceSet newResourceSet = this.GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(this._currentPipeline.ResourceLayout, texture.DeviceTexture, sampler));
                
            this._cachedTextures.Add((texture, sampler), newResourceSet);
            return newResourceSet;
        }

        return resourceSet;
    }

    /// <summary>
    /// Creates the default rendering pipeline for the SpriteBatch.
    /// This includes setting up the default shader programs and
    /// configuring the necessary resource layouts and pipeline states.
    /// </summary>
    private void CreateDefaultPipeline() { // TODO: Replace this with GetOrCreateEffectPipeline.
        this._defaultEffect = new Effect(this.GraphicsDevice.ResourceFactory, Vertex2D.VertexLayout, "content/shaders/sprite.vert", "content/shaders/sprite.frag");
        
        this._defaultPipelineResourceLayout = this.GraphicsDevice.ResourceFactory.CreateResourceLayout(
            new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("fTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("fTextureSampler", ResourceKind.Sampler, ShaderStages.Fragment)
            )
        );

        this._defaultPipeline = new SimplePipeline(this.GraphicsDevice, this._defaultEffect, this._defaultPipelineResourceLayout, this.GraphicsDevice.SwapchainFramebuffer.OutputDescription, BlendStateDescription.SingleOverrideBlend, FaceCullMode.Back);
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
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