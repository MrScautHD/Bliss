using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Fonts;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
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
    private static readonly ushort[] IndicesTemplate = [
        2, 1, 0,
        2, 3, 1
    ];

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
    /// Specifies the maximum number of sprites that the SpriteBatch can process in a single draw call.
    /// </summary>
    public uint Capacity { get; private set; }
    
    /// <summary>
    /// Provides access to a font rendering system within the <see cref="SpriteBatch"/>.
    /// Utilizes <see cref="FontStashRenderer2D"/> for efficient rendering of text with support for various styles and effects.
    /// </summary>
    public FontStashRenderer2D FontStashRenderer { get; private set; }

    /// <summary>
    /// Gets the number of draw calls made during the current batch rendering session.
    /// This count is reset to zero each time <see cref="Begin"/> is called and increments with each call to <see cref="Flush"/>.
    /// </summary>
    public int DrawCallCount { get; private set; }

    /// <summary>
    /// An array of <see cref="SpriteVertex2D"/> structures representing the vertices used for rendering.
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
    /// Stores the description of the graphics pipeline, defining its configuration and behavior.
    /// </summary>
    private SimplePipelineDescription _pipelineDescription;

    /// <summary>
    /// Indicates whether a sprite batch operation has begun.
    /// </summary>
    private bool _begun;

    /// <summary>
    /// Represents the current graphics command list used by the SpriteBatch during rendering.
    /// </summary>
    private CommandList _currentCommandList;
    
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
    /// The current <see cref="Effect"/>.
    /// </summary>
    private Effect _currentEffect;

    /// <summary>
    /// The requested <see cref="Effect"/>.
    /// </summary>
    private Effect _requestedEffect;
    
    /// <summary>
    /// The current <see cref="BlendStateDescription"/>.
    /// </summary>
    private BlendStateDescription _currentBlendState;
    
    /// <summary>
    /// The requested <see cref="BlendStateDescription"/>.
    /// </summary>
    private BlendStateDescription _requestedBlendState;

    /// <summary>
    /// The current <see cref="DepthStencilStateDescription"/>.
    /// </summary>
    private DepthStencilStateDescription _currentDepthStencilState;
    
    /// <summary>
    /// The requested <see cref="DepthStencilStateDescription"/>.
    /// </summary>
    private DepthStencilStateDescription _requestedDepthStencilState;
    
    /// <summary>
    /// The current <see cref="RasterizerStateDescription"/>.
    /// </summary>
    private RasterizerStateDescription _currentRasterizerState;
    
    /// <summary>
    /// The requested <see cref="RasterizerStateDescription"/>.
    /// </summary>
    private RasterizerStateDescription _requestedRasterizerState;

    /// <summary>
    /// The current <see cref="Matrix4x4"/> projection.
    /// </summary>
    private Matrix4x4 _currentProjection;

    /// <summary>
    /// The requested <see cref="Matrix4x4"/> projection.
    /// </summary>
    private Matrix4x4 _requestedProjection;

    /// <summary>
    /// The current <see cref="Matrix4x4"/> view.
    /// </summary>
    private Matrix4x4 _currentView;

    /// <summary>
    /// The requested <see cref="Matrix4x4"/> view.
    /// </summary>
    private Matrix4x4 _requestedView;
    
    /// <summary>
    /// The current <see cref="Sampler"/>.
    /// </summary>
    private Sampler _currentSampler;
    
    /// <summary>
    /// The requested <see cref="Sampler"/>.
    /// </summary>
    private Sampler _requestedSampler;
    
    /// <summary>
    /// The current <see cref="Texture2D"/>.
    /// </summary>
    private Texture2D _currentTexture;
    
    /// <summary>
    /// Tracks the number of quads that have been batched in the current draw call cycle.
    /// </summary>
    private uint _currentBatchCount;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SpriteBatch"/> class for batching and rendering 2D sprites.
    /// </summary>
    /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/> used for rendering.</param>
    /// <param name="window">The <see cref="IWindow"/> associated with the rendering context.</param>
    /// <param name="capacity">The maximum number of sprites the batch can hold. Defaults to 15,360.</param>
    public SpriteBatch(GraphicsDevice graphicsDevice, IWindow window, uint capacity = 15360) {
        this.GraphicsDevice = graphicsDevice;
        this.Window = window;
        this.Capacity = capacity;
        this.FontStashRenderer = new FontStashRenderer2D(graphicsDevice, this);
        
        // Create vertex buffer.
        this._vertices = new SpriteVertex2D[capacity * VerticesPerQuad];
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint) (capacity * VerticesPerQuad * Marshal.SizeOf<SpriteVertex2D>()), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        
        // Create index buffer.
        this._indices = new ushort[capacity * IndicesPerQuad];
        this._indexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(capacity * IndicesPerQuad * sizeof(ushort), BufferUsage.IndexBuffer | BufferUsage.Dynamic));

        for (int i = 0; i < capacity; i++) {
            long startIndex = i * IndicesPerQuad;
            long offset = i * VerticesPerQuad;

            this._indices[startIndex + 0] = (ushort) (IndicesTemplate[0] + offset);
            this._indices[startIndex + 1] = (ushort) (IndicesTemplate[1] + offset);
            this._indices[startIndex + 2] = (ushort) (IndicesTemplate[2] + offset);
            
            this._indices[startIndex + 3] = (ushort) (IndicesTemplate[3] + offset);
            this._indices[startIndex + 4] = (ushort) (IndicesTemplate[4] + offset);
            this._indices[startIndex + 5] = (ushort) (IndicesTemplate[5] + offset);
        }
        
        graphicsDevice.UpdateBuffer(this._indexBuffer, 0, this._indices);
        
        // Create projection view buffer.
        this._projViewBuffer = new SimpleBuffer<Matrix4x4>(graphicsDevice, 2, SimpleBufferType.Uniform, ShaderStages.Vertex);

        // Create pipeline description.
        this._pipelineDescription = new SimplePipelineDescription() {
            PrimitiveTopology = PrimitiveTopology.TriangleList
        };
    }

    /// <summary>
    /// Begins a new sprite batch rendering session with the specified parameters.
    /// </summary>
    /// <param name="commandList">The <see cref="CommandList"/> used to issue rendering commands.</param>
    /// <param name="output">The <see cref="OutputDescription"/> specifying the target render output configuration.</param>
    /// <param name="sampler">The <see cref="Sampler"/> defining sampler state for texture sampling. Defaults to a point sampler if not provided.</param>
    /// <param name="effect">The <see cref="Effect"/> used for rendering sprites. Defaults to the global default sprite effect if not specified.</param>
    /// <param name="blendState">The <see cref="BlendStateDescription"/> describing the blending mode. Defaults to single alpha blend if not specified.</param>
    /// <param name="depthStencilState">The <see cref="DepthStencilStateDescription"/> specifying depth and stencil testing configuration. Defaults to disabled if not provided.</param>
    /// <param name="rasterizerState">The <see cref="RasterizerStateDescription"/> defining rasterization settings. Defaults to no culling if not specified.</param>
    /// <param name="projection">The <see cref="Matrix4x4"/> representing the projection matrix. Defaults to an orthographic projection based on the window dimensions if not specified.</param>
    /// <param name="view">The <see cref="Matrix4x4"/> representing the view matrix. Defaults to the identity matrix if not specified.</param>
    /// <exception cref="Exception">Thrown when the method is called before the previous batch has been ended with a call to <see cref="End"/>.</exception>
    public void Begin(CommandList commandList, OutputDescription output, Sampler? sampler = null, Effect? effect = null, BlendStateDescription? blendState = null, DepthStencilStateDescription? depthStencilState = null, RasterizerStateDescription? rasterizerState = null, Matrix4x4? projection = null, Matrix4x4? view = null) {
        if (this._begun) {
            throw new Exception("The SpriteBatch has already begun!");
        }
        
        this._begun = true;
        this._currentCommandList = commandList;
        this._mainOutput = output;
        this._currentOutput = this._requestedOutput = output;
        this._currentEffect = this._requestedEffect = effect ?? GlobalResource.DefaultSpriteEffect;
        this._currentBlendState = this._requestedBlendState = blendState ?? BlendStateDescription.SINGLE_ALPHA_BLEND;
        this._currentDepthStencilState = this._requestedDepthStencilState = depthStencilState ?? DepthStencilStateDescription.DEPTH_ONLY_LESS_EQUAL;
        this._currentRasterizerState = this._requestedRasterizerState = rasterizerState ?? RasterizerStateDescription.CULL_NONE;
        this._currentProjection = this._requestedProjection = projection ?? Matrix4x4.CreateOrthographicOffCenter(0.0F, this.Window.GetWidth(), this.Window.GetHeight(), 0.0F, -1.0F, 1.0F);
        this._currentView = this._requestedView = view ?? Matrix4x4.Identity;
        this._currentSampler = this._requestedSampler = sampler ?? GraphicsHelper.GetSampler(this.GraphicsDevice, SamplerType.Point);
        
        this.DrawCallCount = 0;
    }

    /// <summary>
    /// Ends the current drawing session that was initiated by a call to <see cref="Begin"/>.
    /// This method finalizes the batch operations by flushing all pending draw calls.
    /// </summary>
    /// <exception cref="Exception">Thrown if the SpriteBatch is in a state where <see cref="Begin"/> was not called beforehand.</exception>
    public void End() {
        if (!this._begun) {
            throw new Exception("The SpriteBatch has not begun yet!");
        }
        
        this._begun = false;
        this.Flush();
    }

    /// <summary>
    /// Retrieves the current <see cref="OutputDescription"/> being used by the sprite batch.
    /// </summary>
    /// <returns>The <see cref="OutputDescription"/> currently associated with the sprite batch.</returns>
    /// <exception cref="Exception">Thrown if the sprite batch operation has not been started.</exception>
    public OutputDescription GetCurrentOutput() {
        if (!this._begun) {
            throw new Exception("The SpriteBatch has not begun yet!");
        }

        return this._currentOutput;
    }

    /// <summary>
    /// Sets the output description for the <see cref="SpriteBatch"/>. If the specified output is null, the default main output is used instead.
    /// </summary>
    /// <param name="output">The optional <see cref="OutputDescription"/> to set. Defaults to the main output if null.</param>
    /// <exception cref="Exception">Thrown if the sprite batch operation has not been started.</exception>
    public void SetOutput(OutputDescription? output) {
        if (!this._begun) {
            throw new Exception("The SpriteBatch has not begun yet!");
        }

        this._requestedOutput = output ?? this._mainOutput;
    }

    /// <summary>
    /// Retrieves the current <see cref="Effect"/> being used by the <see cref="SpriteBatch"/>.
    /// </summary>
    /// <returns>The active <see cref="Effect"/> instance used for rendering, or null if no effect is set.</returns>
    /// <exception cref="Exception">Thrown if the sprite batch operation has not been started.</exception>
    public Effect GetCurrentEffect() {
        if (!this._begun) {
            throw new Exception("The SpriteBatch has not begun yet!");
        }
        
        return this._currentEffect;
    }

    /// <summary>
    /// Sets the rendering effect to be used by the <see cref="SpriteBatch"/> during draw operations.
    /// </summary>
    /// <param name="effect">The <see cref="Effect"/> to set for rendering. If null, the default effect is used.</param>
    /// <exception cref="Exception">Thrown if the sprite batch operation has not been started.</exception>
    public void SetEffect(Effect? effect) {
        if (!this._begun) {
            throw new Exception("The SpriteBatch has not begun yet!");
        }
        
        this._requestedEffect = effect ?? GlobalResource.DefaultSpriteEffect;
    }

    /// <summary>
    /// Retrieves the current blend state used by the <see cref="SpriteBatch"/> for rendering operations.
    /// </summary>
    /// <returns>The <see cref="BlendStateDescription"/> representing the current blending configuration.</returns>
    /// <exception cref="Exception">Thrown if the sprite batch operation has not been started.</exception>
    public BlendStateDescription GetCurrentBlendState() {
        if (!this._begun) {
            throw new Exception("The SpriteBatch has not begun yet!");
        }
        
        return this._currentBlendState;
    }

    /// <summary>
    /// Updates the current blend state of the <see cref="SpriteBatch"/> for rendering sprites.
    /// </summary>
    /// <param name="blendState">The <see cref="BlendStateDescription"/> to set. If null, defaults to <see cref="BlendStateDescription.SINGLE_ALPHA_BLEND"/>.</param>
    /// <exception cref="Exception">Thrown if the sprite batch operation has not been started.</exception>
    public void SetBlendState(BlendStateDescription? blendState) {
        if (!this._begun) {
            throw new Exception("The SpriteBatch has not begun yet!");
        }
        
        this._requestedBlendState = blendState ?? BlendStateDescription.SINGLE_ALPHA_BLEND;
    }

    /// <summary>
    /// Gets the current depth and stencil state configuration used for rendering in the <see cref="SpriteBatch"/>.
    /// </summary>
    /// <returns>The current <see cref="DepthStencilStateDescription"/> used by the <see cref="SpriteBatch"/> for rendering.</returns>
    /// <exception cref="Exception">Thrown if the sprite batch operation has not been started.</exception>
    public DepthStencilStateDescription GetCurrentDepthStencilState() {
        if (!this._begun) {
            throw new Exception("The SpriteBatch has not begun yet!");
        }
        
        return this._currentDepthStencilState;
    }

    /// <summary>
    /// Sets the depth-stencil state to be used by the <see cref="SpriteBatch"/> during rendering operations.
    /// </summary>
    /// <param name="depthStencilState">The <see cref="DepthStencilStateDescription"/> to use. If null, defaults to a disabled depth-stencil state.</param>
    /// <exception cref="Exception">Thrown if the sprite batch operation has not been started.</exception>
    public void SetDepthStencilState(DepthStencilStateDescription? depthStencilState) {
        if (!this._begun) {
            throw new Exception("The SpriteBatch has not begun yet!");
        }
        
        this._requestedDepthStencilState = depthStencilState ?? DepthStencilStateDescription.DEPTH_ONLY_LESS_EQUAL;
    }

    /// <summary>
    /// Gets the current rasterizer state used for configuring rasterization settings in the rendering pipeline.
    /// </summary>
    /// <returns>A <see cref="RasterizerStateDescription"/> representing the current rasterizer state.</returns>
    /// <exception cref="Exception">Thrown if the sprite batch operation has not been started.</exception>
    public RasterizerStateDescription GetCurrentRasterizerState() {
        if (!this._begun) {
            throw new Exception("The SpriteBatch has not begun yet!");
        }
        
        return this._currentRasterizerState;
    }

    /// <summary>
    /// Updates the current rasterizer state of the <see cref="SpriteBatch"/> to the specified value.
    /// </summary>
    /// <param name="rasterizerState">The new <see cref="RasterizerStateDescription"/> to be applied. If null, the default rasterizer state <see cref="RasterizerStateDescription.CULL_NONE"/> will be used.</param>
    /// <exception cref="Exception">Thrown if the sprite batch operation has not been started.</exception>
    public void SetRasterizerState(RasterizerStateDescription? rasterizerState) {
        if (!this._begun) {
            throw new Exception("The SpriteBatch has not begun yet!");
        }
        
        this._requestedRasterizerState = rasterizerState ?? RasterizerStateDescription.CULL_NONE;
    }

    /// <summary>
    /// Retrieves the current projection matrix used by the <see cref="SpriteBatch"/>.
    /// </summary>
    /// <returns>The current <see cref="Matrix4x4"/> projection matrix.</returns>
    /// <exception cref="Exception">Thrown if the sprite batch operation has not been started.</exception>
    public Matrix4x4 GetCurrentProjection() {
        if (!this._begun) {
            throw new Exception("The SpriteBatch has not begun yet!");
        }
        
        return this._currentProjection;
    }

    /// <summary>
    /// Sets the projection matrix for rendering sprites.
    /// </summary>
    /// <param name="projection">The <see cref="Matrix4x4"/> to use as the projection matrix. If null, an orthographic projection will be created based on the window dimensions.</param>
    /// <exception cref="Exception">Thrown if the sprite batch operation has not been started.</exception>
    public void SetProjection(Matrix4x4? projection) {
        if (!this._begun) {
            throw new Exception("The SpriteBatch has not begun yet!");
        }
        
        this._requestedProjection = projection ?? Matrix4x4.CreateOrthographicOffCenter(0.0F, this.Window.GetWidth(), this.Window.GetHeight(), 0.0F, -1.0F, 1.0F);
    }

    /// <summary>
    /// Retrieves the current view matrix used for rendering.
    /// </summary>
    /// <returns>The current <see cref="Matrix4x4"/> view matrix.</returns>
    /// <exception cref="Exception">Thrown if the sprite batch operation has not been started.</exception>
    public Matrix4x4 GetCurrentView() {
        if (!this._begun) {
            throw new Exception("The SpriteBatch has not begun yet!");
        }
        
        return this._currentView;
    }

    /// <summary>
    /// Sets the current view matrix for rendering.
    /// </summary>
    /// <param name="view">The <see cref="Matrix4x4"/> view matrix to apply. If null, the identity matrix will be used.</param>
    /// <exception cref="Exception">Thrown if the sprite batch operation has not been started.</exception>
    public void SetView(Matrix4x4? view) {
        if (!this._begun) {
            throw new Exception("The SpriteBatch has not begun yet!");
        }
        
        this._requestedView = view ?? Matrix4x4.Identity;
    }

    /// <summary>
    /// Retrieves the current <see cref="Sampler"/> used for texture sampling operations in the <see cref="SpriteBatch"/>.
    /// </summary>
    /// <returns>The current <see cref="Sampler"/> instance being used.</returns>
    /// <exception cref="Exception">Thrown if the sprite batch operation has not been started.</exception>
    public Sampler GetCurrentSampler() {
        if (!this._begun) {
            throw new Exception("The SpriteBatch has not begun yet!");
        }
        
        return this._currentSampler;
    }

    /// <summary>
    /// Updates the current sampler state for the <see cref="SpriteBatch"/> to the specified value.
    /// </summary>
    /// <param name="sampler">The new <see cref="Sampler"/> to use, or null to reset to the default sampler.</param>
    /// <exception cref="Exception">Thrown if the sprite batch operation has not been started.</exception>
    public void SetSampler(Sampler? sampler) {
        if (!this._begun) {
            throw new Exception("The SpriteBatch has not begun yet!");
        }
        
        this._requestedSampler = sampler ?? GraphicsHelper.GetSampler(this.GraphicsDevice, SamplerType.Point);
    }

    /// <summary>
    /// Resets the <see cref="SpriteBatch"/> to default settings.
    /// </summary>
    /// <exception cref="Exception">Thrown if the sprite batch operation has not been started.</exception>
    public void ResetSettings() {
        if (!this._begun) {
            throw new Exception("The SpriteBatch has not begun yet!");
        }
        
        this.SetOutput(null);
        this.SetEffect(null);
        this.SetBlendState(null);
        this.SetDepthStencilState(null);
        this.SetRasterizerState(null);
        this.SetProjection(null);
        this.SetView(null);
        this.SetSampler(null);
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
    /// <param name="layerDepth">Optional depth value for sorting layers. Default is 0.5F.</param>
    /// <param name="origin">Optional origin point for rotation and scaling. Default is null.</param>
    /// <param name="rotation">Optional rotation angle in radians. Default is 0.0F.</param>
    /// <param name="color">Optional color of the text. Default is null.</param>
    /// <param name="style">Optional text style. Default is TextStyle.None.</param>
    /// <param name="effect">Optional effect applied to the text. Default is FontSystemEffect.None.</param>
    /// <param name="effectAmount">Optional amount for the effect applied. Default is 0.</param>
    public void DrawText(Font font, string text, Vector2 position, float size, float characterSpacing = 0.0F, float lineSpacing = 0.0F, Vector2? scale = null, float layerDepth = 0.5F, Vector2? origin = null, float rotation = 0.0F, Color? color = null, TextStyle style = TextStyle.None, FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0) {
        font.Draw(this, text, position, size, characterSpacing, lineSpacing, scale, layerDepth, origin, rotation, color, style, effect, effectAmount);
    }

    /// <summary>
    /// Draws a texture onto the screen with configurable position, scaling, rotation, depth, and color.
    /// </summary>
    /// <param name="texture">The <see cref="Texture2D"/> to render.</param>
    /// <param name="position">The position on the screen where the texture will be drawn, specified in world coordinates.</param>
    /// <param name="layerDepth">Optional depth value for sorting layers. Defaults to 0.5F.</param>
    /// <param name="sourceRect">An optional <see cref="Rectangle"/> specifying the region of the texture to be used. If null, the entire texture is used.</param>
    /// <param name="scale">The scaling factor applied to the texture. Defaults to no scaling (1.0F, 1.0F).</param>
    /// <param name="origin">The origin point for rotation and scaling, relative to the source rectangle. Defaults to the top-left corner.</param>
    /// <param name="rotation">The angle, in degrees, to rotate the texture about the origin point. Defaults to 0.0F.</param>
    /// <param name="color">The <see cref="Color"/> to apply to the texture for tinting. Defaults to White (no tint).</param>
    /// <param name="flip">The flip mode to apply to the texture, such as horizontal or vertical flipping. Defaults to <see cref="SpriteFlip.None"/>.</param>
    public void DrawTexture(Texture2D texture, Vector2 position, float layerDepth = 0.5F, Rectangle? sourceRect = null, Vector2? scale = null, Vector2? origin = null, float rotation = 0.0F, Color? color = null, SpriteFlip flip = SpriteFlip.None) {
        Rectangle finalSource = sourceRect ?? new Rectangle(0, 0, (int) texture.Width, (int) texture.Height);
        Vector2 finalScale = scale ?? new Vector2(1.0F, 1.0F);
        Vector2 finalOrigin = origin ?? new Vector2(0.0F, 0.0F);
        float finalRotation = float.DegreesToRadians(rotation);
        Color finalColor = color ?? Color.White;
        
        Vector2 spriteScale = new Vector2(finalSource.Width, finalSource.Height) * finalScale;
        Vector2 spriteOrigin = finalOrigin * finalScale;
        
        float texelWidth = 1.0F / texture.Width;
        float texelHeight = 1.0F / texture.Height;
        float halfTexelX = 0.5F * texelWidth;
        float halfTexelY = 0.5F * texelHeight;
        
        bool flipX = flip == SpriteFlip.Horizontal || flip == SpriteFlip.Both;
        bool flipY = flip == SpriteFlip.Vertical || flip == SpriteFlip.Both;
        
        float u0 = finalSource.X * texelWidth + halfTexelX;
        float v0 = finalSource.Y * texelHeight + halfTexelY;
        float u1 = (finalSource.X + finalSource.Width) * texelWidth - halfTexelX;
        float v1 = (finalSource.Y + finalSource.Height) * texelHeight - halfTexelY;
        
        if (flipX) {
            (u0, u1) = (u1, u0);
        }
        
        if (flipY) {
            (v0, v1) = (v1, v0);
        }
        
        Matrix3x2 transform = Matrix3x2.CreateRotation(finalRotation, position);

        SpriteVertex2D topLeft = new SpriteVertex2D() {
            Position = new Vector3(Vector2.Transform(new Vector2(position.X, position.Y) - spriteOrigin, transform), layerDepth),
            TexCoords = new Vector2(u0, v0),
            Color = finalColor.ToRgbaFloatVec4()
        };
        
        SpriteVertex2D topRight = new SpriteVertex2D() {
            Position = new Vector3(Vector2.Transform(new Vector2(position.X + spriteScale.X, position.Y) - spriteOrigin, transform), layerDepth),
            TexCoords = new Vector2(u1, v0),
            Color = finalColor.ToRgbaFloatVec4()
        };
        
        SpriteVertex2D bottomLeft = new SpriteVertex2D() {
            Position = new Vector3(Vector2.Transform(new Vector2(position.X, position.Y + spriteScale.Y) - spriteOrigin, transform), layerDepth),
            TexCoords = new Vector2(u0, v1),
            Color = finalColor.ToRgbaFloatVec4()
        };
        
        SpriteVertex2D bottomRight = new SpriteVertex2D() {
            Position = new Vector3(Vector2.Transform(new Vector2(position.X + spriteScale.X, position.Y + spriteScale.Y) - spriteOrigin, transform), layerDepth),
            TexCoords = new Vector2(u1, v1),
            Color = finalColor.ToRgbaFloatVec4()
        };
        
        this.AddQuad(texture, topLeft, topRight, bottomLeft, bottomRight);
    }

    /// <summary>
    /// Adds a quad to the sprite batch for rendering.
    /// </summary>
    /// <param name="texture">The <see cref="Texture2D"/> to be used for the quad's rendering.</param>
    /// <param name="topLeft">The <see cref="SpriteVertex2D"/> defining the top-left vertex of the quad.</param>
    /// <param name="topRight">The <see cref="SpriteVertex2D"/> defining the top-right vertex of the quad.</param>
    /// <param name="bottomLeft">The <see cref="SpriteVertex2D"/> defining the bottom-left vertex of the quad.</param>
    /// <param name="bottomRight">The <see cref="SpriteVertex2D"/> defining the bottom-right vertex of the quad.</param>
    /// <exception cref="Exception">Thrown if the SpriteBatch has not been started by calling <c>Begin</c>.</exception>
    public void AddQuad(Texture2D texture, SpriteVertex2D topLeft, SpriteVertex2D topRight, SpriteVertex2D bottomLeft, SpriteVertex2D bottomRight) {
        if (!this._begun) {
            throw new Exception("You must begin the SpriteBatch before calling draw methods!");
        }
        
        if (!this._currentOutput.Equals(this._requestedOutput) ||
            this._currentEffect != this._requestedEffect ||
            !this._currentBlendState.Equals(this._requestedBlendState) ||
            !this._currentDepthStencilState.Equals(this._requestedDepthStencilState) ||
            !this._currentRasterizerState.Equals(this._requestedRasterizerState) ||
            this._currentProjection != this._requestedProjection ||
            this._currentView != this._requestedView ||
            this._currentSampler != this._requestedSampler ||
            this._currentTexture != texture) {
            this.Flush();
        }

        this._currentOutput = this._requestedOutput;
        this._currentEffect = this._requestedEffect;
        this._currentBlendState = this._requestedBlendState;
        this._currentDepthStencilState = this._requestedDepthStencilState;
        this._currentRasterizerState = this._requestedRasterizerState;
        this._currentProjection = this._requestedProjection;
        this._currentView = this._requestedView;
        this._currentSampler = this._requestedSampler;
        this._currentTexture = texture;
        
        // Update pipeline description.
        this._pipelineDescription.BlendState = this._currentBlendState;
        this._pipelineDescription.DepthStencilState = this._currentDepthStencilState;
        this._pipelineDescription.RasterizerState = this._currentRasterizerState;
        this._pipelineDescription.BufferLayouts = this._currentEffect.GetBufferLayouts();
        this._pipelineDescription.TextureLayouts = this._currentEffect.GetTextureLayouts();
        this._pipelineDescription.ShaderSet = this._currentEffect.ShaderSet;
        this._pipelineDescription.Outputs = this._currentOutput;
        
        if (this._currentBatchCount >= (this.Capacity - 1)) {
            this.Flush();
        }
        
        uint index = this._currentBatchCount * VerticesPerQuad;

        this._vertices[index] = topLeft;
        this._vertices[index + 1] = topRight;
        this._vertices[index + 2] = bottomLeft;
        this._vertices[index + 3] = bottomRight;

        this._currentBatchCount++;
    }
    
    /// <summary>
    /// Flushes the current batch of sprites to the GPU for rendering.
    /// </summary>
    private void Flush() {
        if (this._currentBatchCount == 0) {
            return;
        }
        
        // Update projection/view buffer.
        this._projViewBuffer.SetValue(0, this._currentProjection);
        this._projViewBuffer.SetValue(1, this._currentView);
        this._projViewBuffer.UpdateBuffer(this._currentCommandList);
        
        // Update vertex buffer.
        this._currentCommandList.UpdateBuffer(this._vertexBuffer, 0, new ReadOnlySpan<SpriteVertex2D>(this._vertices, 0, (int) (this._currentBatchCount * VerticesPerQuad)));
        
        // Set vertex and index buffer.
        this._currentCommandList.SetVertexBuffer(0, this._vertexBuffer);
        this._currentCommandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt16);
        
        // Set pipeline.
        this._currentCommandList.SetPipeline(this._currentEffect.GetPipeline(this._pipelineDescription).Pipeline);
        
        // Set projection view buffer.
        this._currentCommandList.SetGraphicsResourceSet(0, this._projViewBuffer.GetResourceSet(this._currentEffect.GetBufferLayout("ProjectionViewBuffer")));

        // Set resourceSet of the texture.
        this._currentCommandList.SetGraphicsResourceSet(1, this._currentTexture.GetResourceSet(this._currentSampler, this._currentEffect.GetTextureLayout("fTexture")));
        
        // Apply effect.
        this._currentEffect.Apply();
        
        // Draw.
        this._currentCommandList.DrawIndexed(this._currentBatchCount * IndicesPerQuad);
        
        // Clean up.
        this._currentBatchCount = 0;
        Array.Clear(this._vertices);
        
        this.DrawCallCount++;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.FontStashRenderer.Dispose();
            this._vertexBuffer.Dispose();
            this._indexBuffer.Dispose();
            this._projViewBuffer.Dispose();
        }
    }
}