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
    
    private static readonly Vector2[] VertexTemplate = new Vector2[] {
        new Vector2(0.0F, 0.0F),
        new Vector2(1.0F, 0.0F),
        new Vector2(0.0F, 1.0F),
        new Vector2(1.0F, 1.0F),
    };
    
    private static readonly ushort[] IndicesTemplate = new ushort[] {
        2, 1, 0,
        2, 3, 1
    };
    
    public GraphicsDevice GraphicsDevice { get; private set; }
    public uint Capacity { get; }
    
    public int DrawCallCount { get; private set; }
    
    private Dictionary<(Effect, BlendState), SimplePipeline> _cachedPipelines;
    private Dictionary<(Texture2D, Sampler), ResourceSet> _cachedTextures;
    
    private Effect _defaultEffect;
    
    private Vertex2D[] _vertices;
    private ushort[] _indices;
    
    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;

    private DeviceBuffer _transformBuffer;
    private ResourceLayout _transformLayout;
    private ResourceSet _transformSet;
    
    private bool _begun;
    
    private ResourceLayout _pipelineLayout;

    private CommandList _currentCommandList;
    private uint _currentBatchCount;

    private Effect _currentEffect;
    private BlendState _currentBlendState;
    
    private Texture2D? _currentTexture;
    private Sampler? _currentSampler;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SpriteBatch"/> class with the specified graphics device and optional capacity.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create resources and manage rendering.</param>
    /// <param name="capacity">The maximum number of sprites that can be batched together. Defaults to 15360.</param>
    public SpriteBatch(GraphicsDevice graphicsDevice, uint capacity = 15360) {
        this.GraphicsDevice = graphicsDevice;
        this.Capacity = capacity;

        this._cachedPipelines = new Dictionary<(Effect, BlendState), SimplePipeline>();
        this._cachedTextures = new Dictionary<(Texture2D, Sampler), ResourceSet>();
        
        this._defaultEffect = new Effect(this.GraphicsDevice.ResourceFactory, Vertex2D.VertexLayout, "content/shaders/sprite.vert", "content/shaders/sprite.frag");
        
        // Create vertex buffer.
        this._vertices = new Vertex2D[capacity * VerticesPerQuad];
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint) (capacity * VerticesPerQuad * Marshal.SizeOf<Vertex2D>()), BufferUsage.VertexBuffer));
        
        // Create indices buffer.
        this._indexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(capacity * IndicesPerQuad * sizeof(ushort), BufferUsage.IndexBuffer));
        
        this._indices = new ushort[capacity * IndicesPerQuad];

        for (int i = 0; i < capacity; i++) {
            var startIndex = i * IndicesPerQuad;
            var offset = i * VerticesPerQuad;

            this._indices[startIndex + 0] = (ushort) (IndicesTemplate[0] + offset);
            this._indices[startIndex + 1] = (ushort) (IndicesTemplate[1] + offset);
            this._indices[startIndex + 2] = (ushort) (IndicesTemplate[2] + offset);
            
            this._indices[startIndex + 3] = (ushort) (IndicesTemplate[3] + offset);
            this._indices[startIndex + 4] = (ushort) (IndicesTemplate[4] + offset);
            this._indices[startIndex + 5] = (ushort) (IndicesTemplate[5] + offset);
        }
        
        graphicsDevice.UpdateBuffer(this._indexBuffer, 0, this._indices);

        // Create transform buffer. // TODO: TAKE CARE FOR IT!
        this._transformBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint) Marshal.SizeOf<Matrix4x4>(), BufferUsage.UniformBuffer));
        this._transformLayout = graphicsDevice.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription("ProjectionViewBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)));
        this._transformSet = graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(this._transformLayout, this._transformBuffer));
    }

    public void Begin(CommandList commandList, Effect? effect = null, BlendState? blendState = null, Matrix4x4? view = null, Matrix4x4? projection = null) {
        if (this._begun) {
            throw new Exception("The SpriteBatch has already begun!");
        }
        this._begun = true;
        
        this._currentCommandList = commandList;

        if (this._currentEffect != effect || this._currentBlendState != blendState) {
            this.Flush();
        }
        
        this._currentEffect = effect ?? this._defaultEffect;
        this._currentBlendState = blendState ?? BlendState.AlphaBlend;
        
        Matrix4x4 finalView = view ?? Matrix4x4.Identity;
        Matrix4x4 finalProj = projection ?? Matrix4x4.CreateOrthographicOffCenter(0.0F, 1280.0F, 720.0F, 0.0F, 0.0F, 1.0F); // TODO SET RIGHT RES.
        
        this.GraphicsDevice.UpdateBuffer(this._transformBuffer, 0, finalView * finalProj);
        
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
    
    public void DrawDebugRectangle(Vector2 position, Vector2 size, Color color) {
        //Vertex2D[] vertices = new Vertex2D[4];
        //vertices[0] = new Vertex2D { Position = position, TexCoords = new Vector2(0, 0), Color = color.ToVector4() };
        //vertices[1] = new Vertex2D { Position = position + new Vector2(size.X, 0), TexCoords = new Vector2(1, 0), Color = color.ToVector4() };
        //vertices[2] = new Vertex2D { Position = position + new Vector2(0, size.Y), TexCoords = new Vector2(0, 1), Color = color.ToVector4() };
        //vertices[3] = new Vertex2D { Position = position + new Vector2(size.X, size.Y), TexCoords = new Vector2(1, 1), Color = color.ToVector4() };
        
        float texelWidth = 1.0F / 10;
        float texelHeight = 1.0F / 10;

        Vertex2D[] vertices = {
            new Vertex2D(new Vector2(-0.75f, 0.75f), new Vector2(texelWidth, texelHeight), RgbaFloat.Red.ToVector4()),
            new Vertex2D(new Vector2(0.75f, 0.75f), new Vector2(texelWidth, texelHeight), RgbaFloat.Green.ToVector4()),
            new Vertex2D(new Vector2(-0.75f, -0.75f), new Vector2(texelWidth, texelHeight), RgbaFloat.Blue.ToVector4()),
            new Vertex2D(new Vector2(0.75f, -0.75f), new Vector2(texelWidth, texelHeight), RgbaFloat.Yellow.ToVector4())
        };
        
        this.AddQuad(vertices[0], vertices[1], vertices[2], vertices[3]);
    }
    
    // TODO: ADD All Texture drawing methods.
    // TODO: Add All Font drawing methods.
    // TODO: Add Methods like: BeginShaderMode (Replace the Pipeline system), BeginBlendMode(), BeginScissorMode.

    /// <summary>
    /// Draws a texture using the specified parameters, including position, source rectangle, scale, origin, rotation, color, and flipping.
    /// </summary>
    /// <param name="texture">The texture to be drawn.</param>
    /// <param name="samplerType">The sampler state to use for texture sampling.</param>
    /// <param name="position">The position where the texture will be drawn.</param>
    /// <param name="sourceRect">The source rectangle within the texture. If null, the entire texture is used.</param>
    /// <param name="scale">The scale factor for resizing the texture. If null, the texture is drawn at its original size.</param>
    /// <param name="origin">The origin point for rotation and scaling. If null, the origin is set to the top-left corner.</param>
    /// <param name="rotation">The rotation angle in radians.</param>
    /// <param name="color">The color to tint the texture. If null, the texture is drawn with its original colors.</param>
    /// <param name="flip">Specifies how the texture should be flipped horizontally or vertically.</param>
    public void DrawTexture(Texture2D texture, SamplerType samplerType, Vector2 position, Rectangle? sourceRect = null, Vector2? scale = null, Vector2? origin = null, float rotation = 0.0F, Color? color = null, SpriteFlip flip = SpriteFlip.None) {
        if (!this._begun) {
            throw new Exception("You must begin the SpriteBatch before calling draw methods!");
        }

        Sampler sampler = GraphicsHelper.GetSampler(this.GraphicsDevice, samplerType);
        
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
        
        bool flipX = flip == SpriteFlip.Horizontal || flip == SpriteFlip.Both;
        bool flipY = flip == SpriteFlip.Vertical || flip == SpriteFlip.Both;

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
            Color = finalColor.ToRgbaFloat().ToVector4()
        };
        
        float x = VertexTemplate[(int) VertexTemplateType.TopRight].X;
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
            Color = finalColor.ToRgbaFloat().ToVector4()
        };
        
        float y = VertexTemplate[(int) VertexTemplateType.BottomLeft].Y;
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
            Color = finalColor.ToRgbaFloat().ToVector4()
        };
        
        x = VertexTemplate[(int) VertexTemplateType.BottomRight].X;
        y = VertexTemplate[(int) VertexTemplateType.BottomRight].Y;
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
            Color = finalColor.ToRgbaFloat().ToVector4()
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
        if (this._currentBatchCount >= (this.Capacity - 1)) {
            this.Flush();
        }
        
        uint index = this._currentBatchCount * VerticesPerQuad;

        this._vertices[index] = topLeft;
        this._vertices[index + 1] = topRight;
        this._vertices[index + 2] = bottomLeft;
        this._vertices[index + 3] = bottomRight;

        this._currentBatchCount += 1;
    }
    
    /// <summary>
    /// Submits the currently batched sprites to the graphics device for rendering.
    /// This involves updating the vertex and index buffers, setting the necessary pipeline
    /// states, and issuing the draw command to render the sprites.
    /// </summary>
    public void Flush() {
        if (this._currentBatchCount == 0) {
            return;
        }
        
        // Update vertex buffer.
        this._currentCommandList.UpdateBuffer(this._vertexBuffer, 0, this._vertices);
        
        // Set vertex and index buffers.
        this._currentCommandList.SetVertexBuffer(0, this._vertexBuffer);
        this._currentCommandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt16);
        
        // Set pipeline.
        this._currentCommandList.SetPipeline(this.GetOrCreatePipeline(this._currentEffect, this._currentBlendState).Pipeline);
        
        // Set transform buffer.
        this._currentCommandList.SetGraphicsResourceSet(0, this._transformSet);

        // Create or get texture.
        if (this._currentTexture != null && this._currentSampler != null) {
            this._currentCommandList.SetGraphicsResourceSet(1, this.GetOrCreateTextureResourceSet(this._currentTexture, this._currentSampler));
        }
        
        // Draw.
        this._currentCommandList.DrawIndexed(this._currentBatchCount * IndicesPerQuad);
        
        // Clean up.
        this._currentBatchCount = 0;
        Array.Clear(this._vertices);
        
        this.DrawCallCount++;
    }

    /// <summary>
    /// Retrieves an existing pipeline associated with the given effect and blend state, or creates a new one if it doesn't exist.
    /// </summary>
    /// <param name="effect">The effect to be used for the pipeline.</param>
    /// <param name="blendState">The blend state description for the pipeline.</param>
    /// <returns>A SimplePipeline object configured with the provided effect and blend state.</returns>
    private SimplePipeline GetOrCreatePipeline(Effect effect, BlendState blendState) { // TODO: TAKE A LOOK WHAT YOU DO WITH THE LAYOUTS!!!
        if (!this._cachedPipelines.TryGetValue((effect, blendState), out SimplePipeline? pipeline)) {
            this._pipelineLayout = this.GraphicsDevice.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription() {
                Elements = [
                    new ResourceLayoutElementDescription("fTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("fTextureSampler", ResourceKind.Sampler, ShaderStages.Fragment)
                ]
            });

            SimplePipeline newPipeline = new SimplePipeline(this.GraphicsDevice, effect, [this._transformLayout, this._pipelineLayout], this.GraphicsDevice.SwapchainFramebuffer.OutputDescription, blendState.Description, FaceCullMode.None);
            
            this._cachedPipelines.Add((effect, blendState), newPipeline);
            return newPipeline;
        }

        return pipeline;
    }

    /// <summary>
    /// Retrieves an existing texture resource set or creates a new one if it doesn't exist.
    /// </summary>
    /// <param name="texture">The texture to be used in the resource set.</param>
    /// <param name="sampler">The sampler to be used in the resource set.</param>
    /// <returns>The resource set that includes the specified texture and sampler.</returns>
    private ResourceSet GetOrCreateTextureResourceSet(Texture2D texture, Sampler sampler) { // TODO: TAKE A LOOK WHAT YOU DO WITH THE LAYOUTS!!!
        if (!this._cachedTextures.TryGetValue((texture, sampler), out ResourceSet? resourceSet)) {
            ResourceSet newResourceSet = this.GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(this._pipelineLayout, texture.DeviceTexture, sampler));
                
            this._cachedTextures.Add((texture, sampler), newResourceSet);
            return newResourceSet;
        }

        return resourceSet;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            foreach (SimplePipeline pipeline in this._cachedPipelines.Values) {
                pipeline.Dispose();
            }
            
            foreach (ResourceSet resourceSet in this._cachedTextures.Values) {
                resourceSet.Dispose();
            }
            
            this._defaultEffect.Dispose();
            this._pipelineLayout.Dispose();
            
            this._vertexBuffer.Dispose();
            this._indexBuffer.Dispose();
            
            this._transformBuffer.Dispose();
            this._transformLayout.Dispose();
            this._transformSet.Dispose();
        }
    }
}