using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Fonts;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.Pipelines.Textures;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
using Bliss.CSharp.Windowing;
using FontStashSharp;
using FontStashSharp.Interfaces;
using Veldrid;

namespace Bliss.CSharp.Graphics.Rendering.Batches.Sprites;

public class SpriteBatch : Disposable {
    
    /// <summary>
    /// Defines an index template for rendering two triangles as a quad.
    /// The array contains six <see cref="ushort"/> values, representing the vertex indices for two triangles.
    /// </summary>
    private static readonly ushort[] IndicesTemplate = new ushort[] {
        2, 1, 0,
        2, 3, 1
    };

    /// <summary>
    /// Represents the number of vertices used to define a single quad in the SpriteBatch.
    /// Each quad is made up of four vertices.
    /// </summary>
    private const uint VerticesPerQuad = 4;

    /// <summary>
    /// Represents the number of indices used to define a single quad in the SpriteBatch.
    /// Each quad is made up of six indices.
    /// </summary>
    private const uint IndicesPerQuad = 6;

    /// <summary>
    /// Gets the <see cref="GraphicsDevice"/> associated with the <see cref="SpriteBatch"/>.
    /// This device is responsible for managing and rendering graphics resources such as buffers, shaders, and textures.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }

    /// <summary>
    /// Represents the window used for rendering graphics.
    /// </summary>
    public IWindow Window { get; private set; }

    /// <summary>
    /// Retrieves the current output configuration for the SpriteBatch.
    /// This includes details such as the pixel format and any associated debug information
    /// for rendering outputs.
    /// </summary>
    public OutputDescription Output { get; private set; }

    /// <summary>
    /// Specifies the maximum number of sprites that the SpriteBatch can process in a single draw call.
    /// </summary>
    public uint Capacity { get; private set; }

    /// <summary>
    /// Gets the number of draw calls made during the current batch rendering session.
    /// This count is reset to zero each time <see cref="Begin"/> is called and increments with each call to <see cref="Flush"/>.
    /// </summary>
    public int DrawCallCount { get; private set; }

    /// <summary>
    /// Adapter class for integrating the FontStash font rendering library with the SpriteBatch rendering system.
    /// It implements <see cref="ITexture2DManager"/> and <see cref="IFontStashRenderer"/> interfaces to manage textures and render fonts respectively.
    /// </summary>
    internal readonly FontStashAdapter FontStashAdapter;

    /// <summary>
    /// Stores a collection of reusable pipeline objects, keyed by a combination of <see cref="Effect"/> and <see cref="BlendState"/>.
    /// This cache helps optimize the rendering process by avoiding the recreation of pipelines during rendering operations.
    /// </summary>
    private Dictionary<(Effect, BlendState), SimplePipeline> _cachedPipelines;

    /// <summary>
    /// Represents the default <see cref="Effect"/> used by the <see cref="SpriteBatch"/> when no specific effect is provided.
    /// </summary>
    private Effect _defaultEffect;

    /// <summary>
    /// An array of <see cref="Vertex2D"/> structures representing the vertices used for rendering.
    /// The array is initialized with a specified capacity and holds vertex data for drawing 2D sprites or shapes.
    /// </summary>
    private SpriteVertex2D[] _vertices;

    /// <summary>
    /// An array of <see cref="ushort"/> values representing the indices used for indexing vertices.
    /// This array defines the order in which vertices are connected to form primitives like triangles or lines.
    /// </summary>
    private ushort[] _indices;

    /// <summary>
    /// The buffer used to store vertex data on the GPU. This buffer contains information about vertices such as their positions, colors, and texture coordinates.
    /// </summary>
    private DeviceBuffer _vertexBuffer;
    
    /// <summary>
    /// The buffer used to store index data on the GPU. This buffer defines the order in which vertices are used to construct geometric primitives.
    /// </summary>
    private DeviceBuffer _indexBuffer;

    /// <summary>
    /// A buffer used to store and update the projection-view matrix for the shader.
    /// It is an instance of <see cref="SimpleBuffer{Matrix4x4}"/> and is used in the rendering process to transform sprite coordinates for rendering on the screen.
    /// </summary>
    private SimpleBuffer<Matrix4x4> _projViewBuffer;
    
    /// <summary>
    /// The resource layout that describes how texture resources are bound in the shader. It specifies the layout of texture and sampler resources for rendering.
    /// </summary>
    private SimpleTextureLayout _textureLayout;

    /// <summary>
    /// Indicates whether a sprite batch operation has begun.
    /// </summary>
    private bool _begun;

    /// <summary>
    /// Represents the current graphics command list used by the SpriteBatch during rendering.
    /// </summary>
    private CommandList _currentCommandList;

    /// <summary>
    /// Tracks the number of quads that have been batched in the current draw call cycle.
    /// This count helps determine when to flush the batch to the GPU.
    /// </summary>
    private uint _currentBatchCount;

    /// <summary>
    /// Holds the currently active effect used for rendering operations in the <see cref="SpriteBatch"/>.
    /// It determines the shaders and graphical transformations applied to rendered sprites.
    /// </summary>
    private Effect _currentEffect;

    /// <summary>
    /// Holds the current blend state used by the <see cref="SpriteBatch"/> for rendering operations.
    /// Determines how the colors of the rendered sprites are blended with the background.
    /// </summary>
    private BlendState _currentBlendState;
    
    /// <summary>
    /// The currently active texture that is being used for rendering. This field may be null if no texture is currently set.
    /// </summary>
    private Texture2D? _currentTexture;
    
    /// <summary>
    /// The currently active sampler that is being used with the texture. This field may be null if no sampler is currently set.
    /// </summary>
    private Sampler? _currentSampler;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpriteBatch"/> class, setting up graphics resources and buffers for sprite rendering.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering.</param>
    /// <param name="window">The window associated with the graphics device.</param>
    /// <param name="output">The output description defining the render target.</param>
    /// <param name="capacity">The maximum number of quads (sprite batches) that can be handled by this sprite batch instance. Default is 15360.</param>
    public SpriteBatch(GraphicsDevice graphicsDevice, IWindow window, OutputDescription output, uint capacity = 15360) {
        this.GraphicsDevice = graphicsDevice;
        this.Window = window;
        this.Output = output;
        this.Capacity = capacity;
        this.FontStashAdapter = new FontStashAdapter(graphicsDevice, this);
        
        this._cachedPipelines = new Dictionary<(Effect, BlendState), SimplePipeline>();
        
        // Create default effect.
        this._defaultEffect = new Effect(this.GraphicsDevice.ResourceFactory, SpriteVertex2D.VertexLayout, "content/shaders/sprite.vert", "content/shaders/sprite.frag");
        
        // Create vertex buffer.
        this._vertices = new SpriteVertex2D[capacity * VerticesPerQuad];
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint) (capacity * VerticesPerQuad * Marshal.SizeOf<SpriteVertex2D>()), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        
        // Create indices buffer.
        this._indices = new ushort[capacity * IndicesPerQuad];
        this._indexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(capacity * IndicesPerQuad * sizeof(ushort), BufferUsage.IndexBuffer | BufferUsage.Dynamic));

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
        
        // Create projection view buffer.
        this._projViewBuffer = new SimpleBuffer<Matrix4x4>(graphicsDevice, "ProjectionViewBuffer", 1, SimpleBufferType.Uniform, ShaderStages.Vertex);
        
        // Create texture layout.
        this._textureLayout = new SimpleTextureLayout(graphicsDevice, "fTexture");
    }
    
    /// <summary>
    /// Begins a sprite batch operation, preparing the command list for sprite rendering.
    /// </summary>
    /// <param name="commandList">The command list to record rendering commands to.</param>
    /// <param name="effect">Optional effect to apply; if null, the default effect is used.</param>
    /// <param name="blendState">Optional blend state to use; if null, the default blend state is used.</param>
    /// <param name="view">Optional view matrix; if null, an identity matrix is used.</param>
    /// <param name="projection">Optional projection matrix; if null, an orthographic projection matching the window dimensions is used.</param>
    /// <exception cref="Exception">Thrown if Begin is called while a previous Begin has not been followed by an End.</exception>
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
        Matrix4x4 finalProj = projection ?? Matrix4x4.CreateOrthographicOffCenter(0.0F, this.Window.GetWidth(), this.Window.GetHeight(), 0.0F, 0.0F, 1.0F);
        
        this._projViewBuffer.SetValueImmediate(0, finalView * finalProj);
        this.DrawCallCount = 0;
    }

    /// <summary>
    /// Ends the drawing session that was started with the <see cref="Begin(CommandList, Effect?, BlendState?, Matrix4x4?, Matrix4x4?)"/> method.
    /// This method should be called after all the draw operations are completed for the current batch.
    /// </summary>
    /// <exception cref="Exception">Thrown if the <see cref="Begin(CommandList, Effect?, BlendState?, Matrix4x4?, Matrix4x4?)"/> method has not been called before calling this method.</exception>
    public void End() {
        if (!this._begun) {
            throw new Exception("The SpriteBatch has not begun yet!");
        }

        this._begun = false;
        this.Flush();
    }

    /// <summary>
    /// Draws the specified text at the given position with the provided font and styling options.
    /// </summary>
    /// <param name="font">The font to be used for drawing the text.</param>
    /// <param name="text">The text to be drawn.</param>
    /// <param name="position">The position on the screen where the text will be drawn.</param>
    /// <param name="size">The size of the text.</param>
    /// <param name="characterSpacing">Optional spacing between characters. Default is 0.0F.</param>
    /// <param name="lineSpacing">Optional spacing between lines of text. Default is 0.0F.</param>
    /// <param name="scale">Optional scale applied to the text. Default is null.</param>
    /// <param name="origin">Optional origin point for rotation and scaling. Default is null.</param>
    /// <param name="rotation">Optional rotation angle in radians. Default is 0.0F.</param>
    /// <param name="color">Optional color of the text. Default is null.</param>
    /// <param name="style">Optional text style. Default is TextStyle.None.</param>
    /// <param name="effect">Optional effect applied to the text. Default is FontSystemEffect.None.</param>
    /// <param name="effectAmount">Optional amount for the effect applied. Default is 0.</param>
    public void DrawText(Font font, string text, Vector2 position, int size, float characterSpacing = 0.0F, float lineSpacing = 0.0F, Vector2? scale = null, Vector2? origin = null, float rotation = 0.0F, Color? color = null, TextStyle style = TextStyle.None, FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0) {
        font.Draw(this, text, position, size, characterSpacing, lineSpacing, scale, origin, rotation, color, style, effect, effectAmount);
    }
    
    /// <summary>
    /// Draws a texture using the specified parameters, including position, source rectangle, scale, origin, rotation, color, and flipping.
    /// </summary>
    /// <param name="texture">The texture to be drawn.</param>
    /// <param name="position">The position where the texture will be drawn.</param>
    /// <param name="sourceRect">The source rectangle within the texture. If null, the entire texture is used.</param>
    /// <param name="scale">The scale factor for resizing the texture. If null, the texture is drawn at its original size.</param>
    /// <param name="origin">The origin point for rotation and scaling. If null, the origin is set to the top-left corner.</param>
    /// <param name="rotation">The rotation angle in radians.</param>
    /// <param name="color">The color to tint the texture. If null, the texture is drawn with its original colors.</param>
    /// <param name="flip">Specifies how the texture should be flipped horizontally or vertically.</param>
    public void DrawTexture(Texture2D texture, Vector2 position, Rectangle? sourceRect = null, Vector2? scale = null, Vector2? origin = null, float rotation = 0.0F, Color? color = null, SpriteFlip flip = SpriteFlip.None) {
        Rectangle finalSource = sourceRect ?? new Rectangle(0, 0, (int) texture.Width, (int) texture.Height);
        Vector2 finalScale = scale ?? new Vector2(1.0F, 1.0F);
        Vector2 finalOrigin = origin ?? new Vector2(0.0F, 0.0F);
        float finalRotation = float.DegreesToRadians(rotation);
        Color finalColor = color ?? Color.White;
        
        Vector2 spriteScale = new Vector2(finalSource.Width, finalSource.Height) * finalScale;
        Vector2 spriteOrigin = finalOrigin * finalScale;
        
        float texelWidth = 1.0F / texture.Width;
        float texelHeight = 1.0F / texture.Height;
        
        bool flipX = flip == SpriteFlip.Horizontal || flip == SpriteFlip.Both;
        bool flipY = flip == SpriteFlip.Vertical || flip == SpriteFlip.Both;
        
        Matrix3x2 transform = Matrix3x2.CreateRotation(finalRotation, position);

        SpriteVertex2D topLeft = new SpriteVertex2D() {
            Position = Vector2.Transform(new Vector2(position.X, position.Y) - spriteOrigin, transform),
            TexCoords = new Vector2(
                flipX ? (finalSource.X + finalSource.Width) * texelWidth : finalSource.X * texelWidth,
                flipY ? (finalSource.Y + finalSource.Height) * texelHeight : finalSource.Y * texelHeight),
            Color = finalColor.ToRgbaFloat().ToVector4()
        };
        
        SpriteVertex2D topRight = new SpriteVertex2D() {
            Position = Vector2.Transform(new Vector2(position.X + spriteScale.X, position.Y) - spriteOrigin, transform),
            TexCoords = new Vector2(
                flipX ? finalSource.X * texelWidth : (finalSource.X + finalSource.Width) * texelWidth,
                flipY ? (finalSource.Y + finalSource.Height) * texelHeight : finalSource.Y * texelHeight),
            Color = finalColor.ToRgbaFloat().ToVector4()
        };
        
        SpriteVertex2D bottomLeft = new SpriteVertex2D() {
            Position = Vector2.Transform(new Vector2(position.X, position.Y + spriteScale.Y) - spriteOrigin, transform),
            TexCoords = new Vector2(
                flipX ? (finalSource.X + finalSource.Width) * texelWidth : finalSource.X * texelWidth,
                flipY ? finalSource.Y * texelHeight : (finalSource.Y + finalSource.Height) * texelHeight),
            Color = finalColor.ToRgbaFloat().ToVector4()
        };
        
        SpriteVertex2D bottomRight = new SpriteVertex2D() {
            Position = Vector2.Transform(new Vector2(position.X + spriteScale.X, position.Y + spriteScale.Y) - spriteOrigin, transform),
            TexCoords = new Vector2(
                flipX ? finalSource.X * texelWidth : (finalSource.X + finalSource.Width) * texelWidth,
                flipY ? finalSource.Y * texelHeight : (finalSource.Y + finalSource.Height) * texelHeight),
            Color = finalColor.ToRgbaFloat().ToVector4()
        };
        
        this.AddQuad(texture, topLeft, topRight, bottomLeft, bottomRight);
    }
    
    /// <summary>
    /// Adds a quad to the sprite batch using the provided texture, sampler, and sprite vertices.
    /// </summary>
    /// <param name="texture">The texture to be applied to the quad.</param>
    /// <param name="topLeft">The vertex at the top-left corner of the quad.</param>
    /// <param name="topRight">The vertex at the top-right corner of the quad.</param>
    /// <param name="bottomLeft">The vertex at the bottom-left corner of the quad.</param>
    /// <param name="bottomRight">The vertex at the bottom-right corner of the quad.</param>
    /// <exception cref="Exception">Thrown if the SpriteBatch has not been begun before drawing.</exception>
    public void AddQuad(Texture2D texture, SpriteVertex2D topLeft, SpriteVertex2D topRight, SpriteVertex2D bottomLeft, SpriteVertex2D bottomRight) {
        if (!this._begun) {
            throw new Exception("You must begin the SpriteBatch before calling draw methods!");
        }
        
        if (this._currentTexture != texture || this._currentSampler != texture.GetSampler()) {
            this.Flush();
        }
        
        this._currentTexture = texture;
        this._currentSampler = texture.GetSampler();
        
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
    /// Flushes the current batch of sprites to the GPU for rendering.
    /// </summary>
    public void Flush() {
        if (this._currentBatchCount == 0) {
            return;
        }
        
        // Update vertex buffer.
        this._currentCommandList.UpdateBuffer(this._vertexBuffer, 0, this._vertices);
        
        // Set vertex and index buffer.
        this._currentCommandList.SetVertexBuffer(0, this._vertexBuffer);
        this._currentCommandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt16);
        
        // Set pipeline.
        this._currentCommandList.SetPipeline(this.GetOrCreatePipeline(this._currentEffect, this._currentBlendState).Pipeline);
        
        // Set projection view buffer.
        this._currentCommandList.SetGraphicsResourceSet(0, this._projViewBuffer.ResourceSet);

        // Create or get texture.
        if (this._currentTexture != null && this._currentSampler != null) {
            this._currentCommandList.SetGraphicsResourceSet(1, this._currentTexture.GetResourceSet(this._currentSampler, this._textureLayout.Layout));
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
    private SimplePipeline GetOrCreatePipeline(Effect effect, BlendState blendState) {
        if (!this._cachedPipelines.TryGetValue((effect, blendState), out SimplePipeline? pipeline)) {
            SimplePipeline newPipeline = new SimplePipeline(this.GraphicsDevice, new SimplePipelineDescription() {
                BlendState = blendState.Description,
                DepthStencilState = new DepthStencilStateDescription(false, false, ComparisonKind.LessEqual),
                RasterizerState = new RasterizerStateDescription() {
                    DepthClipEnabled = true,
                    CullMode = FaceCullMode.None
                },
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                Buffers = [
                    this._projViewBuffer
                ],
                TextureLayouts = [
                    this._textureLayout
                ],
                ShaderSet = new ShaderSetDescription() {
                    VertexLayouts = [
                        effect.VertexLayout
                    ],
                    Shaders = [
                        effect.Shader.Item1,
                        effect.Shader.Item2
                    ]
                },
                Outputs = this.Output
            });
            
            this._cachedPipelines.Add((effect, blendState), newPipeline);
            return newPipeline;
        }

        return pipeline;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            foreach (SimplePipeline pipeline in this._cachedPipelines.Values) {
                pipeline.Dispose();
            }
            
            this._defaultEffect.Dispose();
            
            this._vertexBuffer.Dispose();
            this._indexBuffer.Dispose();
            
            this._textureLayout.Dispose();
            this._projViewBuffer.Dispose();
        }
    }
}