using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Transformations;
using Bliss.CSharp.Windowing;
using Veldrid;
using Color = Bliss.CSharp.Colors.Color;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Batches.Primitives;

public class PrimitiveBatch : Disposable {

    /// <summary>
    /// Represents the graphics device used for rendering operations within the <see cref="PrimitiveBatch"/> class.
    /// The <see cref="GraphicsDevice"/> is used to create resources, manage rendering pipelines, and issue draw commands.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }

    /// <summary>
    /// Represents the window used for rendering graphics.
    /// </summary>
    public IWindow Window { get; private set; }

    /// <summary>
    /// Specifies the maximum number of sprites that the PrimitiveBatch can process in a single draw call.
    /// </summary>
    public uint Capacity { get; private set; }

    /// <summary>
    /// Gets the number of draw calls made during the current batch rendering session.
    /// This count is reset to zero each time <see cref="Begin"/> is called and increments with each call to <see cref="Flush"/>.
    /// </summary>
    public int DrawCallCount { get; private set; }
    
    /// <summary>
    /// Buffer storing the combined projection and view matrix for rendering.
    /// </summary>
    private SimpleUniformBuffer<Matrix4x4> _projViewBuffer;
    
    /// <summary>
    /// Array of vertices used for rendering 2D primitives.
    /// </summary>
    private PrimitiveVertex2D[] _vertices;
    
    /// <summary>
    /// Temporary array of vertices used during vertex manipulation.
    /// </summary>
    private List<PrimitiveVertex2D> _tempVertices;
    
    /// <summary>
    /// Buffer that stores vertex data for rendering.
    /// </summary>
    private DeviceBuffer _vertexBuffer;

    /// <summary>
    /// Stores the description of the graphics pipeline, defining its configuration and behavior.
    /// </summary>
    private SimplePipelineDescription _pipelineDescription;
    
    /// <summary>
    /// Indicates whether the batch has begun.
    /// </summary>
    private bool _begun;
    
    /// <summary>
    /// Represents the current graphics command list used by the PrimitiveBatch during rendering.
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
    /// The main <see cref="Matrix4x4"/> projection.
    /// </summary>
    private Matrix4x4 _mainProjection;
    
    /// <summary>
    /// The current <see cref="Matrix4x4"/> projection.
    /// </summary>
    private Matrix4x4 _currentProjection;

    /// <summary>
    /// The requested <see cref="Matrix4x4"/> projection.
    /// </summary>
    private Matrix4x4 _requestedProjection;
    
    /// <summary>
    /// The main <see cref="Matrix4x4"/> view.
    /// </summary>
    private Matrix4x4 _mainView;

    /// <summary>
    /// The current <see cref="Matrix4x4"/> view.
    /// </summary>
    private Matrix4x4 _currentView;

    /// <summary>
    /// The requested <see cref="Matrix4x4"/> view.
    /// </summary>
    private Matrix4x4 _requestedView;
    
    /// <summary>
    /// The main <see cref="RectangleF"/> scissor rectangle.
    /// </summary>
    private Rectangle? _mainScissorRect;
    
    /// <summary>
    /// The current <see cref="RectangleF"/> scissor rectangle.
    /// </summary>
    private Rectangle? _currentScissorRect;
    
    /// <summary>
    /// The requested <see cref="RectangleF"/> scissor rectangle.
    /// </summary>
    private Rectangle? _requestedScissorRect;
    
    /// <summary>
    /// Tracks the number of vertices in the current batch.
    /// </summary>
    private uint _currentBatchCount;
    
    /// <summary>
    /// Initializes a new instance of the PrimitiveBatch class for rendering 2D primitives.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering.</param>
    /// <param name="window">The window representing the rendering context.</param>
    /// <param name="capacity">Optional. The initial capacity of the vertex buffer.</param>
    public PrimitiveBatch(GraphicsDevice graphicsDevice, IWindow window, uint capacity = 4096) {
        this.GraphicsDevice = graphicsDevice;
        this.Window = window;
        this.Capacity = capacity;
        
        // Create vertex buffer.
        this._vertices = new PrimitiveVertex2D[capacity];
        this._tempVertices = new List<PrimitiveVertex2D>();
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint) (capacity * Marshal.SizeOf<PrimitiveVertex2D>()), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        
        // Create projection view buffer.
        this._projViewBuffer = new SimpleUniformBuffer<Matrix4x4>(graphicsDevice, 2, ShaderStages.Vertex);
        
        // Create pipeline description.
        this._pipelineDescription = new SimplePipelineDescription();
    }

    /// <summary>
    /// Begins a new batch of primitive drawing operations with specified rendering configurations.
    /// </summary>
    /// <param name="commandList">The command list to record drawing commands.</param>
    /// <param name="output">The output description defining the render target configuration.</param>
    /// <param name="effect">Optional. The effect to use for rendering operations. Defaults to the global default primitive effect if not specified.</param>
    /// <param name="blendState">Optional. The blend state description used for rendering. Defaults to a single alpha blend if not specified.</param>
    /// <param name="depthStencilState">Optional. The depth stencil state description used for rendering. Defaults to disabled depth-stencil testing if not specified.</param>
    /// <param name="rasterizerState">Optional. The rasterizer state description used for rendering. Defaults to cull none if not specified.</param>
    /// <param name="projection">Optional. The projection matrix for the rendering. Defaults to an orthographic projection matrix if not specified.</param>
    /// <param name="view">Optional. The view matrix for the rendering. Defaults to the identity matrix if not specified.</param>
    /// <param name="scissorRect">An optional <see cref="Rectangle"/> that defines the scissor rectangle for rendering. No scissor rect is applied if not specified.</param>
    /// <exception cref="Exception">Thrown when the method is called before the previous batch has been properly ended.</exception>
    public void Begin(CommandList commandList, OutputDescription output, Effect? effect = null, BlendStateDescription? blendState = null, DepthStencilStateDescription? depthStencilState = null, RasterizerStateDescription? rasterizerState = null, Matrix4x4? projection = null, Matrix4x4? view = null, Rectangle? scissorRect = null) {
        if (this._begun) {
            throw new Exception("The PrimitiveBatch has already begun!");
        }

        this._begun = true;
        this._currentCommandList = commandList;
        this._mainOutput = this._currentOutput = this._requestedOutput = output;
        this._mainEffect = this._currentEffect = this._requestedEffect = effect ?? GlobalResource.DefaultPrimitiveEffect;
        this._mainBlendState = this._currentBlendState = this._requestedBlendState = blendState ?? BlendStateDescription.SINGLE_ALPHA_BLEND;
        this._mainDepthStencilState = this._currentDepthStencilState = this._requestedDepthStencilState = depthStencilState ?? DepthStencilStateDescription.DEPTH_ONLY_LESS_EQUAL;
        this._mainRasterizerState = this._currentRasterizerState = this._requestedRasterizerState = rasterizerState ?? RasterizerStateDescription.CULL_NONE;
        this._mainProjection = this._currentProjection = this._requestedProjection = projection ?? Matrix4x4.CreateOrthographicOffCenter(0.0F, this.Window.GetWidth(), this.Window.GetHeight(), 0.0F, -1.0F, 1.0F);
        this._mainView = this._currentView = this._requestedView = view ?? Matrix4x4.Identity;
        this._mainScissorRect = this._currentScissorRect = this._requestedScissorRect = scissorRect;
        
        this.DrawCallCount = 0;
    }
    
    /// <summary>
    /// Ends the current batch of primitive drawing operations.
    /// </summary>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public void End() {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }

        this._begun = false;
        this.Flush();
    }

    /// <summary>
    /// Retrieves the current output description for the primitive batch.
    /// </summary>
    /// <returns>The current <see cref="OutputDescription"/> associated with the batch.</returns>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>  
    public OutputDescription GetCurrentOutput() {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }

        return this._currentOutput;
    }
    
    /// <summary>
    /// Push the requested <see cref="OutputDescription"/> for the <see cref="PrimitiveBatch"/>.
    /// </summary>
    /// <param name="output">The <see cref="OutputDescription"/> to apply for rendering.</param>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public void PushOutput(OutputDescription output) {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }

        this._requestedOutput = output;
    }
    
    /// <summary>
    /// Pop the output of the <see cref="OutputDescription"/> for the <see cref="PrimitiveBatch"/>.
    /// </summary>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public void PopOutput() {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }

        this._requestedOutput = this._mainOutput;
    }

    /// <summary>
    /// Retrieves the currently active effect for the PrimitiveBatch.
    /// </summary>
    /// <returns>The <see cref="Effect"/> that is currently being used by the PrimitiveBatch.</returns>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public Effect GetCurrentEffect() {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }
        
        return this._currentEffect;
    }

    /// <summary>
    /// Push the requested <see cref="Effect"/> for the <see cref="PrimitiveBatch"/>.
    /// </summary>
    /// <param name="effect">The <see cref="Effect"/> to apply for rendering.</param>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public void PushEffect(Effect effect) {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }
        
        this._requestedEffect = effect;
    }
    
    /// <summary>
    /// Pop the current <see cref="Effect"/> to the main effect used by the <see cref="PrimitiveBatch"/>.
    /// </summary>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public void PopEffect() {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }
        
        this._requestedEffect = this._mainEffect;
    }

    /// <summary>
    /// Retrieves the current blend state configuration used for rendering operations.
    /// </summary>
    /// <returns>The current blend state configuration as a <see cref="BlendStateDescription"/>.</returns>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public BlendStateDescription GetCurrentBlendState() {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }
        
        return this._currentBlendState;
    }

    /// <summary>
    /// Push the requested <see cref="BlendStateDescription"/> for the <see cref="PrimitiveBatch"/>.
    /// </summary>
    /// <param name="blendState">The <see cref="BlendStateDescription"/> to apply for rendering.</param>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public void PushBlendState(BlendStateDescription blendState) {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }
        
        this._requestedBlendState = blendState;
    }

    /// <summary>
    /// Pop the current <see cref="BlendStateDescription"/> to the main blend state used by the <see cref="PrimitiveBatch"/>.
    /// </summary>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public void PopBlendState() {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }
        
        this._requestedBlendState = this._mainBlendState;
    }
    
    /// <summary>
    /// Retrieves the current depth-stencil state configuration used for rendering.
    /// </summary>
    /// <returns>The current <see cref="DepthStencilStateDescription"/> being used in the rendering pipeline.</returns>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public DepthStencilStateDescription GetCurrentDepthStencilState() {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }
        
        return this._currentDepthStencilState;
    }

    /// <summary>
    /// Push the requested <see cref="DepthStencilStateDescription"/> for the <see cref="PrimitiveBatch"/>.
    /// </summary>
    /// <param name="depthStencilState">The <see cref="DepthStencilStateDescription"/> to apply for rendering.</param>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public void PushDepthStencilState(DepthStencilStateDescription depthStencilState) {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }
        
        this._requestedDepthStencilState = depthStencilState;
    }
    
    /// <summary>
    /// Pop the current <see cref="DepthStencilStateDescription"/> to the main depth stencil state used by the <see cref="PrimitiveBatch"/>.
    /// </summary>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public void PopDepthStencilState() {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }
        
        this._requestedDepthStencilState = this._mainDepthStencilState;
    }

    /// <summary>
    /// Retrieves the current rasterizer state description used for rendering operations.
    /// </summary>
    /// <returns>The current <see cref="RasterizerStateDescription"/> instance.</returns>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public RasterizerStateDescription GetCurrentRasterizerState() {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }
        
        return this._currentRasterizerState;
    }
    
    /// <summary>
    /// Push the requested <see cref="RasterizerStateDescription"/> for the <see cref="PrimitiveBatch"/>.
    /// </summary>
    /// <param name="rasterizerState">The <see cref="RasterizerStateDescription"/> to apply for rendering.</param>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public void PushRasterizerState(RasterizerStateDescription rasterizerState) {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }
        
        this._requestedRasterizerState = rasterizerState;
    }
    
    /// <summary>
    /// Pop the current <see cref="RasterizerStateDescription"/> to the main rasterizer state used by the <see cref="PrimitiveBatch"/>.
    /// </summary>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public void PopRasterizerState() {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }
        
        this._requestedRasterizerState = this._mainRasterizerState;
    }

    /// <summary>
    /// Retrieves the current projection matrix being used for rendering operations.
    /// </summary>
    /// <returns>The current projection as a <see cref="Matrix4x4"/>.</returns>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public Matrix4x4 GetCurrentProjection() {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }
        
        return this._currentProjection;
    }

    /// <summary>
    /// Push the requested <see cref="Matrix4x4"/> projection for the <see cref="PrimitiveBatch"/>.
    /// </summary>
    /// <param name="projection">The <see cref="Matrix4x4"/> projection to apply for rendering.</param>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public void PushProjection(Matrix4x4 projection) {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }
        
        this._requestedProjection = projection;
    }
    
    /// <summary>
    /// Pop the current <see cref="Matrix4x4"/> to the main projection used by the <see cref="PrimitiveBatch"/>.
    /// </summary>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public void PopProjection() {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }
        
        this._requestedProjection = this._mainProjection;
    }

    /// <summary>
    /// Retrieves the current view matrix being used for rendering operations.
    /// </summary>
    /// <returns>The current <see cref="Matrix4x4"/> view matrix.</returns>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public Matrix4x4 GetCurrentView() {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }
        
        return this._currentView;
    }

    /// <summary>
    /// Push the requested <see cref="Matrix4x4"/> view for the <see cref="PrimitiveBatch"/>.
    /// </summary>
    /// <param name="view">The <see cref="Matrix4x4"/> view to apply for rendering.</param>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public void PushView(Matrix4x4 view) {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }
        
        this._requestedView = view;
    }
    
    /// <summary>
    /// Pop the current <see cref="Matrix4x4"/> to the main view used by the <see cref="PrimitiveBatch"/>.
    /// </summary>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public void PopView() {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }
        
        this._requestedView = this._mainView;
    }
    
    /// <summary>
    /// Retrieves the current scissor rectangle being used by the <see cref="PrimitiveBatch"/>.
    /// </summary>
    /// <returns>The current scissor rectangle, or null if no scissor rectangle is set.</returns>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public Rectangle? GetCurrentScissorRect() {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }
        
        return this._currentScissorRect;
    }
    
    /// <summary>
    /// Push the requested <see cref="Rectangle"/> scissor for the <see cref="PrimitiveBatch"/>.
    /// </summary>
    /// <param name="rectangle">The <see cref="Rectangle"/> scissor to apply for rendering.</param>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public void PushScissorRect(Rectangle? rectangle) {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }

        this._requestedScissorRect = rectangle;
    }

    /// <summary>
    /// Pop the current <see cref="Rectangle"/> scissor to the main scissor used by the <see cref="PrimitiveBatch"/>.
    /// </summary>
    /// <exception cref="Exception">Thrown if the <see cref="PrimitiveBatch"/> has not begun.</exception>
    public void PopScissorRect() {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }

        this._requestedScissorRect = this._mainScissorRect;
    }
    
    /// <summary>
    /// Draws a line between two points with the specified thickness, layer depth, and color.
    /// </summary>
    /// <param name="start">The starting point of the line.</param>
    /// <param name="end">The ending point of the line.</param>
    /// <param name="thickness">The thickness of the line. Default is 1.0.</param>
    /// <param name="layerDepth">The depth at which the line is drawn in the layer. Default is 0.5.</param>
    /// <param name="color">The color of the line. If null, defaults to white.</param>
    public void DrawLine(Vector2 start, Vector2 end, float thickness, float layerDepth = 0.5F, Color? color = null) {
        float distance = Vector2.Distance(start, end);
        float angle = float.RadiansToDegrees(MathF.Atan2(end.Y - start.Y, end.X - start.X));
        
        RectangleF rectangle = new RectangleF(start.X, start.Y, distance, thickness);
        this.DrawFilledRectangle(rectangle, new Vector2(0, thickness / 2.0F), angle, layerDepth, color ?? Color.White);
    }

    /// <summary>
    /// Draws an empty rectangle with specified dimensions, thickness, rotation, and color.
    /// </summary>
    /// <param name="rectangle">The rectangle defining the dimensions and position of the empty rectangle.</param>
    /// <param name="thickness">The thickness of the rectangle's outline.</param>
    /// <param name="origin">Optional origin point for rotation and positioning. Defaults to (0,0).</param>
    /// <param name="rotation">Optional rotation angle in degrees. Defaults to 0.0F.</param>
    /// <param name="layerDepth">Optional depth layer for rendering. Defaults to 0.5F.</param>
    /// <param name="color">Optional color for the rectangle's outline. Defaults to white.</param>
    public void DrawEmptyRectangle(RectangleF rectangle, float thickness, Vector2? origin = null, float rotation = 0.0F, float layerDepth = 0.5F, Color? color = null) {
        Vector2 finalOrigin = origin ?? Vector2.Zero;
        float finalRotation = float.DegreesToRadians(rotation);
        Color finalColor = color ?? Color.White;
        
        Matrix3x2 transform = Matrix3x2.CreateRotation(finalRotation, rectangle.Position);
        
        // Calculate the four corners of the rectangle.
        Vector2 topLeft = Vector2.Transform(new Vector2(rectangle.X, rectangle.Y) - finalOrigin, transform);
        Vector2 topRight = Vector2.Transform(new Vector2(rectangle.X + rectangle.Width, rectangle.Y) - finalOrigin, transform);
        Vector2 bottomLeft = Vector2.Transform(new Vector2(rectangle.X, rectangle.Y + rectangle.Height) - finalOrigin, transform);
        Vector2 bottomRight = Vector2.Transform(new Vector2(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height) - finalOrigin, transform);
        
        // Top side.
        Vector2 topDirection = topRight - topLeft;
        Vector2 topPerpendicular = Vector2.Normalize(new Vector2(-topDirection.Y, topDirection.X)) * (thickness / 2.0F);
        this.DrawLine(topLeft + topPerpendicular, topRight + topPerpendicular, thickness, layerDepth, finalColor);
        
        // Bottom side.
        Vector2 bottomDirection = bottomRight - bottomLeft;
        Vector2 bottomPerpendicular = Vector2.Normalize(new Vector2(-bottomDirection.Y, bottomDirection.X)) * (thickness / 2.0F);
        this.DrawLine(bottomLeft - bottomPerpendicular, bottomRight - bottomPerpendicular, thickness, layerDepth, finalColor);
        
        // Left side.
        Vector2 leftDirection = bottomLeft - topLeft;
        Vector2 leftPerpendicular = Vector2.Normalize(new Vector2(-leftDirection.Y, leftDirection.X)) * (thickness / 2.0F);
        this.DrawLine(topLeft - leftPerpendicular, bottomLeft - leftPerpendicular, thickness, layerDepth, finalColor);
        
        // Right side.
        Vector2 rightDirection = bottomRight - topRight;
        Vector2 rightPerpendicular = Vector2.Normalize(new Vector2(-rightDirection.Y, rightDirection.X)) * (thickness / 2.0F);
        this.DrawLine(topRight + rightPerpendicular, bottomRight + rightPerpendicular, thickness, layerDepth, finalColor);
    }

    /// <summary>
    /// Draws a filled rectangle with optional origin, rotation, layer depth, and color.
    /// </summary>
    /// <param name="rectangle">The rectangular region to render.</param>
    /// <param name="origin">Optional. The origin point of the rectangle for transformations, defaults to the top-left corner.</param>
    /// <param name="rotation">Optional. The rotation angle of the rectangle in radians, defaults to 0.0F.</param>
    /// <param name="layerDepth">Optional. The z-depth for rendering order, defaults to 0.5F.</param>
    /// <param name="color">Optional. The fill color of the rectangle, defaults to White.</param>
    public void DrawFilledRectangle(RectangleF rectangle, Vector2? origin = null, float rotation = 0.0F, float layerDepth = 0.5F, Color? color = null) {
        Vector2 finalOrigin = origin ?? Vector2.Zero;
        Color finalColor = color ?? Color.White;
        float finalRotation = float.DegreesToRadians(rotation);

        Matrix3x2 transform = Matrix3x2.CreateRotation(finalRotation, rectangle.Position);

        PrimitiveVertex2D topLeft = new PrimitiveVertex2D() {
            Position = new Vector3(Vector2.Transform(new Vector2(rectangle.X, rectangle.Y) - finalOrigin, transform), layerDepth),
            Color = finalColor.ToRgbaFloatVec4()
        };
        
        PrimitiveVertex2D topRight = new PrimitiveVertex2D() {
            Position = new Vector3(Vector2.Transform(new Vector2(rectangle.X + rectangle.Width, rectangle.Y) - finalOrigin, transform), layerDepth),
            Color = finalColor.ToRgbaFloatVec4()
        };
        
        PrimitiveVertex2D bottomLeft = new PrimitiveVertex2D() {
            Position = new Vector3(Vector2.Transform(new Vector2(rectangle.X, rectangle.Y + rectangle.Height) - finalOrigin, transform), layerDepth),
            Color = finalColor.ToRgbaFloatVec4()
        };
        
        PrimitiveVertex2D bottomRight = new PrimitiveVertex2D() {
            Position = new Vector3(Vector2.Transform(new Vector2(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height) - finalOrigin, transform), layerDepth),
            Color = finalColor.ToRgbaFloatVec4()
        };
        
        this._tempVertices.Add(bottomLeft);
        this._tempVertices.Add(topRight);
        this._tempVertices.Add(topLeft);
        this._tempVertices.Add(bottomLeft);
        this._tempVertices.Add(bottomRight);
        this._tempVertices.Add(topRight);
        
        this.AddVertices(this._tempVertices);
    }

    /// <summary>
    /// Draws an empty circle sector using the specified parameters.
    /// </summary>
    /// <param name="position">The position of the center of the circle.</param>
    /// <param name="radius">The radius of the circle sector.</param>
    /// <param name="startAngle">The starting angle of the sector in degrees.</param>
    /// <param name="endAngle">The ending angle of the sector in degrees.</param>
    /// <param name="thickness">The thickness of the circle sector line.</param>
    /// <param name="segments">The number of segments used to draw the sector. Minimum value is 4.</param>
    /// <param name="layerDepth">The depth of the layer for rendering. Defaults to 0.5.</param>
    /// <param name="color">The color of the circle sector line. Defaults to white if null.</param>
    public void DrawEmptyCircleSector(Vector2 position, float radius, float startAngle, float endAngle, int thickness, int segments, float layerDepth = 0.5F, Color? color = null) {
        float finalStartAngle = float.DegreesToRadians(startAngle);
        float finalEndAngle = float.DegreesToRadians(endAngle);
        Color finalColor = color ?? Color.White;

        // Calculate angular range and number of segments
        float angularRange = finalEndAngle - finalStartAngle;
        int segmentCount = (int) (Math.Max(4, segments) * (angularRange / (2 * MathF.PI)));
        int finalSegments = Math.Max(4, segmentCount);

        float angleIncrement = angularRange / finalSegments;
        Vector2 firstPoint = position + new Vector2(radius * MathF.Cos(finalStartAngle), radius * MathF.Sin(finalStartAngle));
        Vector2 lastPoint = firstPoint;

        for (int i = 1; i <= finalSegments; i++) {
            float angle = finalStartAngle + i * angleIncrement;
            Vector2 currentPoint = new Vector2(
                position.X + radius * MathF.Cos(angle),
                position.Y + radius * MathF.Sin(angle)
            );
            
            this.DrawLine(lastPoint, currentPoint, thickness, layerDepth, finalColor);
            lastPoint = currentPoint;
        }

        // Draw the sector edges to the center.
        Vector2 lineOffsetX = new Vector2(thickness / 2.0F, 0);
        Vector2 lineOffsetY = new Vector2(0, thickness / 2.0F);

        this.DrawLine(position - lineOffsetX, firstPoint + lineOffsetX, thickness, layerDepth, finalColor);
        this.DrawLine(position + lineOffsetY, lastPoint - lineOffsetY, thickness, layerDepth, finalColor);
    }

    /// <summary>
    /// Draws a filled sector of a circle at a specified position with defined parameters.
    /// </summary>
    /// <param name="position">The center position of the circle sector.</param>
    /// <param name="radius">The radius of the circle sector.</param>
    /// <param name="startAngle">The starting angle of the sector in degrees.</param>
    /// <param name="endAngle">The ending angle of the sector in degrees.</param>
    /// <param name="segments">The number of segments to use for drawing the sector.</param>
    /// <param name="layerDepth">The depth layer for rendering the sector. Defaults to 0.5 if not provided.</param>
    /// <param name="color">The color to use for the sector. Defaults to white if not provided.</param>
    public void DrawFilledCircleSector(Vector2 position, float radius, float startAngle, float endAngle, int segments, float layerDepth = 0.5F, Color? color = null) {
        float finalStartAngle = float.DegreesToRadians(startAngle);
        float finalEndAngle = float.DegreesToRadians(endAngle);
        Color finalColor = color ?? Color.White;

        // Calculate the angular range and the number of segments
        float angularRange = finalEndAngle - finalStartAngle;
        int segmentCount = (int)(Math.Max(4, segments) * (angularRange / (2 * MathF.PI)));
        int finalSegments = Math.Max(4, segmentCount);

        float angleIncrement = angularRange / finalSegments;
        Vector2 firstPoint = position + new Vector2(radius * MathF.Cos(finalStartAngle), radius * MathF.Sin(finalStartAngle));
        Vector2 lastPoint = firstPoint;
    
        for (int i = 1; i <= finalSegments; i++) {
            float angle = finalStartAngle + i * angleIncrement;
            Vector2 currentPoint = new Vector2(
                position.X + radius * MathF.Cos(angle),
                position.Y + radius * MathF.Sin(angle)
            );

            this._tempVertices.Add(new PrimitiveVertex2D() {
                Position = new Vector3(position, layerDepth),
                Color = finalColor.ToRgbaFloatVec4()
            });
            this._tempVertices.Add(new PrimitiveVertex2D() {
                Position = new Vector3(lastPoint, layerDepth),
                Color = finalColor.ToRgbaFloatVec4()
            });
            this._tempVertices.Add(new PrimitiveVertex2D() {
                Position = new Vector3(currentPoint, layerDepth),
                Color = finalColor.ToRgbaFloatVec4()
            });
        
            this.AddVertices(this._tempVertices);
            lastPoint = currentPoint;
        }
    }

    /// <summary>
    /// Draws an empty circle at the specified position with the given radius, thickness, and number of segments.
    /// </summary>
    /// <param name="position">The center position of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="thickness">The thickness of the circle's outline.</param>
    /// <param name="segments">The number of segments to divide the circle into. Must be at least 4.</param>
    /// <param name="layerDepth">The depth of the circle in the rendering pass. Defaults to 0.5.</param>
    /// <param name="color">The color of the circle's outline. Defaults to white if not specified.</param>
    public void DrawEmptyCircle(Vector2 position, float radius, int thickness, int segments, float layerDepth = 0.5F, Color? color = null) {
        int finalSegments = Math.Max(4, segments);
        Color finalColor = color ?? Color.White;

        float angleIncrement = 2 * MathF.PI / finalSegments;
        float lineOffset = thickness / 2.0F;

        for (int i = 0; i < finalSegments; i++) {
            float startAngle = i * angleIncrement;
            float endAngle = (i + 1) * angleIncrement;

            Vector2 startPoint = new Vector2(
                position.X + radius * MathF.Cos(startAngle),
                position.Y + radius * MathF.Sin(startAngle)
            );
            
            Vector2 endPoint = new Vector2(
                position.X + radius * MathF.Cos(endAngle),
                position.Y + radius * MathF.Sin(endAngle)
            );
            
            // Calculate the direction of the segment.
            Vector2 direction = Vector2.Normalize(endPoint - startPoint);
        
            // Perpendicular vector for offset (rotate 90 degrees).
            Vector2 perpendicular = new Vector2(-direction.Y, direction.X);

            // Apply offset to start and end points.
            Vector2 startOffset = perpendicular * lineOffset;
            Vector2 endOffset = perpendicular * lineOffset;

            // Adjusted start and end points.
            Vector2 adjustedStartPoint = startPoint + startOffset;
            Vector2 adjustedEndPoint = endPoint + endOffset;
            
            this.DrawLine(adjustedStartPoint, adjustedEndPoint, thickness, layerDepth, finalColor);
        }
    }

    /// <summary>
    /// Draws a filled circle at the specified position with the given radius, number of segments, and optional color.
    /// </summary>
    /// <param name="position">The position of the center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="segments">The number of segments to use for drawing the circle.</param>
    /// <param name="layerDepth">The drawing layer depth. Defaults to 0.5.</param>
    /// <param name="color">The optional color of the circle. If null, defaults to white.</param>
    public void DrawFilledCircle(Vector2 position, float radius, int segments, float layerDepth = 0.5F, Color? color = null) {
        int finalSegments = Math.Max(4, segments);
        Color finalColor = color ?? Color.White;

        float angleIncrement = MathF.PI * 2.0f / finalSegments;
        Vector2 firstPoint = position + new Vector2(radius, 0);
        Vector2 lastPoint = firstPoint;

        for (int i = 1; i <= finalSegments; i++) {
            float angle = i * angleIncrement;
            Vector2 currentPoint = new Vector2(
                position.X + radius * MathF.Cos(angle),
                position.Y + radius * MathF.Sin(angle)
            );

            this._tempVertices.Add(new PrimitiveVertex2D() {
                Position = new Vector3(position, layerDepth),
                Color = finalColor.ToRgbaFloatVec4()
            });
            this._tempVertices.Add(new PrimitiveVertex2D() {
                Position = new Vector3(lastPoint, layerDepth),
                Color = finalColor.ToRgbaFloatVec4()
            });
            this._tempVertices.Add(new PrimitiveVertex2D() {
                Position = new Vector3(currentPoint, layerDepth),
                Color = finalColor.ToRgbaFloatVec4()
            });
            
            this.AddVertices(this._tempVertices);
            lastPoint = currentPoint;
        }
    }

    /// <summary>
    /// Draws an empty ring at the specified position with the given inner radius, outer radius, thickness, segments, and optional color.
    /// </summary>
    /// <param name="position">The position where the ring will be drawn.</param>
    /// <param name="innerRadius">The inner radius of the ring.</param>
    /// <param name="outerRadius">The outer radius of the ring.</param>
    /// <param name="thickness">The thickness of the ring.</param>
    /// <param name="segments">The number of segments used to construct the ring. Must be at least 4.</param>
    /// <param name="layerDepth">The depth layer for rendering the ring.</param>
    /// <param name="color">Optional. The color of the ring. Defaults to white if not provided.</param>
    public void DrawEmptyRing(Vector2 position, float innerRadius, float outerRadius, int thickness, int segments, float layerDepth = 0.5F, Color? color = null) {
        int finalSegments = Math.Max(4, segments);
        Color finalColor = color ?? Color.White;

        float angleIncrement = MathF.PI * 2.0f / finalSegments;
        float lineOffset = thickness / 2.0F;

        for (int i = 0; i < finalSegments; i++) {
            float startAngle = i * angleIncrement;
            float endAngle = (i + 1) * angleIncrement;

            Vector2 innerStart = new Vector2(
                position.X + innerRadius * MathF.Cos(startAngle),
                position.Y + innerRadius * MathF.Sin(startAngle)
            );

            Vector2 innerEnd = new Vector2(
                position.X + innerRadius * MathF.Cos(endAngle),
                position.Y + innerRadius * MathF.Sin(endAngle)
            );

            Vector2 outerStart = new Vector2(
                position.X + outerRadius * MathF.Cos(startAngle),
                position.Y + outerRadius * MathF.Sin(startAngle)
            );

            Vector2 outerEnd = new Vector2(
                position.X + outerRadius * MathF.Cos(endAngle),
                position.Y + outerRadius * MathF.Sin(endAngle)
            );
            
            // Calculate the direction of the segment.
            Vector2 innerDirection = Vector2.Normalize(innerEnd - innerStart);
            Vector2 innerPerpendicular = new Vector2(-innerDirection.Y, innerDirection.X);

            // Apply offset to start and end points.
            Vector2 innerStartOffset = innerPerpendicular * lineOffset;
            Vector2 innerEndOffset = innerPerpendicular * lineOffset;

            // Adjusted start and end points.
            Vector2 adjustedInnerStartPoint = innerStart + innerStartOffset;
            Vector2 adjustedInnerEndPoint = innerEnd + innerEndOffset;

            // Draw the inner ring.
            this.DrawLine(adjustedInnerStartPoint, adjustedInnerEndPoint, thickness, layerDepth, finalColor);
            
            // Calculate the direction of the segment.
            Vector2 outerDirection = Vector2.Normalize(outerEnd - outerStart);
            Vector2 outerPerpendicular = new Vector2(-outerDirection.Y, outerDirection.X);

            // Apply offset to start and end points.
            Vector2 outerStartOffset = outerPerpendicular * lineOffset;
            Vector2 outerEndOffset = outerPerpendicular * lineOffset;

            // Adjusted start and end points.
            Vector2 adjustedOuterStartPoint = outerStart + outerStartOffset;
            Vector2 adjustedOuterEndPoint = outerEnd + outerEndOffset;
            
            // Draw the outer ring.
            this.DrawLine(adjustedOuterStartPoint, adjustedOuterEndPoint, thickness, layerDepth, finalColor);
        }
    }

    /// <summary>
    /// Draws a filled ring at a specified position with given inner and outer radii, segment count, layer depth, and optional color.
    /// </summary>
    /// <param name="position">The center position of the ring.</param>
    /// <param name="innerRadius">The inner radius of the ring.</param>
    /// <param name="outerRadius">The outer radius of the ring.</param>
    /// <param name="segments">The number of segments to use for constructing the ring. Must be at least 4.</param>
    /// <param name="layerDepth">The layer depth for rendering the ring, where 0 is frontmost and 1 is backmost. Defaults to 0.5.</param>
    /// <param name="color">The color of the ring. If not provided, defaults to white.</param>
    public void DrawFilledRing(Vector2 position, float innerRadius, float outerRadius, int segments, float layerDepth = 0.5F, Color? color = null) {
        int finalSegments = Math.Max(4, segments);
        Color finalColor = color ?? Color.White;

        float angleIncrement = MathF.PI * 2.0f / finalSegments;

        for (int i = 0; i < finalSegments; i++) {
            float startAngle = i * angleIncrement;
            float endAngle = (i + 1) * angleIncrement;

            Vector2 innerStart = new Vector2(
                position.X + innerRadius * MathF.Cos(startAngle),
                position.Y + innerRadius * MathF.Sin(startAngle)
            );
    
            Vector2 innerEnd = new Vector2(
                position.X + innerRadius * MathF.Cos(endAngle),
                position.Y + innerRadius * MathF.Sin(endAngle)
            );
    
            Vector2 outerStart = new Vector2(
                position.X + outerRadius * MathF.Cos(startAngle),
                position.Y + outerRadius * MathF.Sin(startAngle)
            );
    
            Vector2 outerEnd = new Vector2(
                position.X + outerRadius * MathF.Cos(endAngle),
                position.Y + outerRadius * MathF.Sin(endAngle)
            );
    
            // Define the vertices for the triangle as part of the ring segment.
            this._tempVertices.Add(new PrimitiveVertex2D() {
                Position = new Vector3(innerStart, layerDepth),
                Color = finalColor.ToRgbaFloatVec4()
            });
            this._tempVertices.Add(new PrimitiveVertex2D() {
                Position = new Vector3(outerStart, layerDepth),
                Color = finalColor.ToRgbaFloatVec4()
            });
            this._tempVertices.Add(new PrimitiveVertex2D() {
                Position = new Vector3(outerEnd, layerDepth),
                Color = finalColor.ToRgbaFloatVec4()
            });
    
            this._tempVertices.Add(new PrimitiveVertex2D() {
                Position = new Vector3(innerStart, layerDepth),
                Color = finalColor.ToRgbaFloatVec4()
            });
            this._tempVertices.Add(new PrimitiveVertex2D() {
                Position = new Vector3(outerEnd, layerDepth),
                Color = finalColor.ToRgbaFloatVec4()
            });
            this._tempVertices.Add(new PrimitiveVertex2D() {
                Position = new Vector3(innerEnd, layerDepth),
                Color = finalColor.ToRgbaFloatVec4()
            });
    
            this.AddVertices(this._tempVertices);
        }
    }

    /// <summary>
    /// Draws an empty ellipse at the specified position with the given radius, thickness, and number of segments.
    /// </summary>
    /// <param name="position">The center position of the ellipse in 2D space.</param>
    /// <param name="radius">The radius of the ellipse along the X and Y axes.</param>
    /// <param name="thickness">The thickness of the ellipse outline.</param>
    /// <param name="segments">The number of segments to use for drawing the ellipse. Minimum value is 4.</param>
    /// <param name="layerDepth">The depth layer for rendering the ellipse. Defaults to 0.5.</param>
    /// <param name="color">The color to use for the ellipse outline. If not specified, defaults to white.</param>
    public void DrawEmptyEllipse(Vector2 position, Vector2 radius, int thickness, int segments, float layerDepth = 0.5F, Color? color = null) {
        int finalSegments = Math.Max(4, segments);
        Color finalColor = color ?? Color.White;

        float angleIncrement = MathF.PI * 2.0f / finalSegments;
        float lineOffset = thickness / 2.0F;

        for (int i = 0; i < finalSegments; i++) {
            float startAngle = i * angleIncrement;
            float endAngle = (i + 1) * angleIncrement;

            Vector2 startPoint = new Vector2(
                position.X + radius.X * MathF.Cos(startAngle),
                position.Y + radius.Y * MathF.Sin(startAngle)
            );

            Vector2 endPoint = new Vector2(
                position.X + radius.X * MathF.Cos(endAngle),
                position.Y + radius.Y * MathF.Sin(endAngle)
            );
            
            // Calculate the direction of the segment.
            Vector2 direction = Vector2.Normalize(endPoint - startPoint);
        
            // Perpendicular vector for offset (rotate 90 degrees).
            Vector2 perpendicular = new Vector2(-direction.Y, direction.X);

            // Apply offset to start and end points.
            Vector2 startOffset = perpendicular * lineOffset;
            Vector2 endOffset = perpendicular * lineOffset;

            // Adjusted start and end points.
            Vector2 adjustedStartPoint = startPoint + startOffset;
            Vector2 adjustedEndPoint = endPoint + endOffset;

            // Draw the line between the start and end points.
            this.DrawLine(adjustedStartPoint, adjustedEndPoint, thickness, layerDepth, finalColor);
        }
    }

    /// <summary>
    /// Draws a filled ellipse at the specified position with the given radius, number of segments, and an optional color.
    /// </summary>
    /// <param name="position">The center position of the ellipse.</param>
    /// <param name="radius">The horizontal and vertical radii of the ellipse.</param>
    /// <param name="segments">The number of segments to approximate the ellipse. Minimum is 4.</param>
    /// <param name="layerDepth">The depth of the layer at which to draw the ellipse.</param>
    /// <param name="color">The color to fill the ellipse. Defaults to white if not specified.</param>
    public void DrawFilledEllipse(Vector2 position, Vector2 radius, int segments, float layerDepth = 0.5F, Color? color = null) {
        int finalSegments = Math.Max(4, segments);
        Color finalColor = color ?? Color.White;

        float angleIncrement = MathF.PI * 2.0f / finalSegments;

        for (int i = 0; i < finalSegments; i++) {
            float startAngle = i * angleIncrement;
            float endAngle = (i + 1) * angleIncrement;

            Vector2 startPoint = new Vector2(
                position.X + radius.X * MathF.Cos(startAngle),
                position.Y + radius.Y * MathF.Sin(startAngle)
            );

            Vector2 endPoint = new Vector2(
                position.X + radius.X * MathF.Cos(endAngle),
                position.Y + radius.Y * MathF.Sin(endAngle)
            );

            this._tempVertices.Add(new PrimitiveVertex2D {
                Position = new Vector3(position, layerDepth),
                Color = finalColor.ToRgbaFloatVec4()
            });
            
            this._tempVertices.Add(new PrimitiveVertex2D {
                Position = new Vector3(startPoint, layerDepth),
                Color = finalColor.ToRgbaFloatVec4()
            });
            
            this._tempVertices.Add(new PrimitiveVertex2D {
                Position = new Vector3(endPoint, layerDepth),
                Color = finalColor.ToRgbaFloatVec4()
            });

            this.AddVertices(this._tempVertices);
        }
    }

    /// <summary>
    /// Draws an empty triangle between the specified points with a given thickness and color.
    /// </summary>
    /// <param name="point1">The first vertex of the triangle.</param>
    /// <param name="point2">The second vertex of the triangle.</param>
    /// <param name="point3">The third vertex of the triangle.</param>
    /// <param name="thickness">The thickness of the triangle edges.</param>
    /// <param name="layerDepth">The depth of the triangle layer in the rendering order. Defaults to 0.5.</param>
    /// <param name="color">The color of the triangle edges. Defaults to white if not specified.</param>
    public void DrawEmptyTriangle(Vector2 point1, Vector2 point2, Vector2 point3, int thickness, float layerDepth = 0.5F, Color? color = null) {
        Color finalColor = color ?? Color.White;

        this.DrawLine(point1, point2, thickness, layerDepth, finalColor);
        this.DrawLine(point2, point3, thickness, layerDepth, finalColor);
        this.DrawLine(point3, point1, thickness, layerDepth, finalColor);
    }

    /// <summary>
    /// Draws a filled triangle using the specified vertices, color, and layer depth.
    /// </summary>
    /// <param name="point1">The first vertex position of the triangle.</param>
    /// <param name="point2">The second vertex position of the triangle.</param>
    /// <param name="point3">The third vertex position of the triangle.</param>
    /// <param name="layerDepth">The layer depth of the triangle, determining its rendering order. Default is 0.5.</param>
    /// <param name="color">The color of the triangle. If null, the default color is white.</param>
    public void DrawFilledTriangle(Vector2 point1, Vector2 point2, Vector2 point3, float layerDepth = 0.5F, Color? color = null) {
        Color finalColor = color ?? Color.White;

        this._tempVertices.Add(new PrimitiveVertex2D {
            Position = new Vector3(point1, layerDepth), Color = finalColor.ToRgbaFloatVec4()
        });

        this._tempVertices.Add(new PrimitiveVertex2D {
            Position = new Vector3(point2, layerDepth),
            Color = finalColor.ToRgbaFloatVec4()
        });

        this._tempVertices.Add(new PrimitiveVertex2D {
            Position = new Vector3(point3, layerDepth),
            Color = finalColor.ToRgbaFloatVec4()
        });

        this.AddVertices(this._tempVertices);
    }

    /// <summary>
    /// Adds a collection of vertices to the current batch for rendering.
    /// </summary>
    /// <param name="vertices">The list of vertices to be added to the batch.</param>
    public void AddVertices(List<PrimitiveVertex2D> vertices) {
        if (!this._begun) {
            throw new Exception("You must begin the PrimitiveBatch before calling draw methods!");
        }
        
        if (!this._currentOutput.Equals(this._requestedOutput) ||
            this._currentEffect != this._requestedEffect ||
            !this._currentBlendState.Equals(this._requestedBlendState) ||
            !this._currentDepthStencilState.Equals(this._requestedDepthStencilState) ||
            !this._currentRasterizerState.Equals(this._requestedRasterizerState) ||
            this._currentProjection != this._requestedProjection ||
            this._currentView != this._requestedView ||
            this._currentScissorRect != this._requestedScissorRect) {
            this.Flush();
        }

        this._currentOutput = this._requestedOutput;
        this._currentEffect = this._requestedEffect;
        this._currentBlendState = this._requestedBlendState;
        this._currentDepthStencilState = this._requestedDepthStencilState;
        this._currentRasterizerState = this._requestedRasterizerState;
        this._currentProjection = this._requestedProjection;
        this._currentView = this._requestedView;
        this._currentScissorRect = this._requestedScissorRect;
        
        // Update pipeline description.
        this._pipelineDescription.BlendState = this._currentBlendState;
        this._pipelineDescription.DepthStencilState = this._currentDepthStencilState;
        this._pipelineDescription.RasterizerState = this._currentRasterizerState;
        this._pipelineDescription.BufferLayouts = this._currentEffect.GetBufferLayouts();
        this._pipelineDescription.TextureLayouts = this._currentEffect.GetTextureLayouts();
        this._pipelineDescription.ShaderSet = this._currentEffect.ShaderSet;
        this._pipelineDescription.Outputs = this._currentOutput;
        
        if (this._currentBatchCount + vertices.Count >= this._vertices.Length) {
            this.Flush();
        }

        for (int i = 0; i < vertices.Count; i++) {
            this._vertices[this._currentBatchCount] = vertices[i];
            this._currentBatchCount++;
        }
        
        // Clear temp data.
        this._tempVertices.Clear();
    }

    /// <summary>
    /// Flushes the current batch of primitives to the GPU for rendering.
    /// </summary>
    private void Flush() {
        if (this._currentBatchCount == 0) {
            return;
        }
        
        // Update projection/view buffer.
        this._projViewBuffer.SetValue(0, this._currentProjection);
        this._projViewBuffer.SetValue(1, this._currentView);
        this._projViewBuffer.UpdateBufferDeferred(this._currentCommandList);
        
        // Update vertex buffer.
        this._currentCommandList.UpdateBuffer(this._vertexBuffer, 0, new ReadOnlySpan<PrimitiveVertex2D>(this._vertices, 0, (int) this._currentBatchCount));
        
        // Set vertex buffer.
        this._currentCommandList.SetVertexBuffer(0, this._vertexBuffer);
        
        // Set pipeline.
        this._currentCommandList.SetPipeline(this._currentEffect.GetPipeline(this._pipelineDescription).Pipeline);
        
        // Set projection view buffer.
        this._currentCommandList.SetGraphicsResourceSet(this._currentEffect.GetBufferLayoutSlot("ProjectionViewBuffer"), this._projViewBuffer.GetResourceSet(this._currentEffect.GetBufferLayout("ProjectionViewBuffer")));
        
        // Set scissor rect.
        if (this._pipelineDescription.RasterizerState.ScissorTestEnabled && this._currentScissorRect != null) {
            Rectangle scissorRect = this._currentScissorRect.Value;
            this._currentCommandList.SetScissorRect(0, (uint) scissorRect.X, (uint) scissorRect.Y, (uint) scissorRect.Width, (uint) scissorRect.Height);
        }
        
        // Apply effect.
        this._currentEffect.Apply(this._currentCommandList);
        
        // Draw.
        this._currentCommandList.Draw(this._currentBatchCount);
        
        // Reset scissor.
        if (this._pipelineDescription.RasterizerState.ScissorTestEnabled && this._currentScissorRect != null) {
            this._currentCommandList.SetFullScissorRect(0);
        }

        // Clear data.
        this._currentBatchCount = 0;
        Array.Clear(this._vertices);
        
        this.DrawCallCount++;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._vertexBuffer.Dispose();
            this._projViewBuffer.Dispose();
        }
    }
}