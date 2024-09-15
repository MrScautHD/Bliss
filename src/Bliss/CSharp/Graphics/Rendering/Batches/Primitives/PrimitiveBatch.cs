using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Transformations;
using Bliss.CSharp.Windowing;
using Veldrid;
using Vortice.Mathematics;
using Color = Bliss.CSharp.Colors.Color;

namespace Bliss.CSharp.Graphics.Rendering.Batches.Primitives;

public class PrimitiveBatch : Disposable {

    /// <summary>
    /// Represents the graphics device used for rendering operations within the <see cref="PrimitiveBatch"/> class.
    /// The <see cref="GraphicsDevice"/> is used to create resources, manage rendering pipelines, and issue draw commands.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }

    /// <summary>
    /// Represents the window used for rendering graphics.
    /// </summary>
    public Window Window { get; private set; }

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
    /// The shader effect used to render graphics.
    /// </summary>
    private Effect _effect;
    
    /// <summary>
    /// Buffer storing the combined projection and view matrix for rendering.
    /// </summary>
    private SimpleBuffer<Matrix4x4> _projViewBuffer;
    
    /// <summary>
    /// Pipeline configuration used for rendering a list of triangles.
    /// </summary>
    private SimplePipeline _pipelineTriangleList;
    
    /// <summary>
    /// Pipeline configuration used for rendering a triangle strip.
    /// </summary>
    private SimplePipeline _pipelineTriangleStrip;
    
    /// <summary>
    /// Pipeline configuration used for rendering line loops.
    /// </summary>
    private SimplePipeline _pipelineLineLoop;
    
    /// <summary>
    /// Array of vertices used for rendering 2D primitives.
    /// </summary>
    private PrimitiveVertex2D[] _vertices;
    
    /// <summary>
    /// Temporary array of vertices used during vertex manipulation.
    /// </summary>
    private PrimitiveVertex2D[] _tempVertices;
    
    /// <summary>
    /// Buffer that stores vertex data for rendering.
    /// </summary>
    private DeviceBuffer _vertexBuffer;
    
    /// <summary>
    /// Indicates whether the batch has begun.
    /// </summary>
    private bool _begun;
    
    /// <summary>
    /// Represents the current graphics command list used by the PrimitiveBatch during rendering.
    /// </summary>
    private CommandList _currentCommandList;
    
    /// <summary>
    /// Tracks the number of vertices in the current batch.
    /// </summary>
    private uint _currentBatchCount;
    
    /// <summary>
    /// The pipeline used for the current rendering batch.
    /// </summary>
    private SimplePipeline? _currentPipeline;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PrimitiveBatch"/> class with the specified graphics device, window, and optional capacity.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering operations.</param>
    /// <param name="window">The window used for rendering output.</param>
    /// <param name="capacity">The maximum number of vertices that can process in a single draw call. Defaults is 15360.</param>
    public PrimitiveBatch(GraphicsDevice graphicsDevice, Window window, uint capacity = 15360) {
        this.GraphicsDevice = graphicsDevice;
        this.Window = window;
        this.Capacity = capacity;
        
        // Create effects.
        this._effect = new Effect(graphicsDevice.ResourceFactory, PrimitiveVertex2D.VertexLayout, "content/shaders/primitive.vert", "content/shaders/primitive.frag");
        
        // Create projection view buffer.
        this._projViewBuffer = new SimpleBuffer<Matrix4x4>(graphicsDevice, "ProjectionViewBuffer", 1, SimpleBufferType.Uniform, ShaderStages.Vertex);

        // Create pipelines.
        SimplePipelineDescription pipelineDescription = new SimplePipelineDescription() {
            BlendState = BlendState.AlphaBlend.Description,
            DepthStencilState = new DepthStencilStateDescription(true, true, ComparisonKind.LessEqual),
            RasterizerState = new RasterizerStateDescription() {
                DepthClipEnabled = true,
                CullMode = FaceCullMode.None
            },
            Buffers = [
                this._projViewBuffer
            ],
            ShaderSet = new ShaderSetDescription() {
                VertexLayouts = [
                    PrimitiveVertex2D.VertexLayout
                ],
                Shaders = [
                    this._effect.Shader.Item1,
                    this._effect.Shader.Item2
                ]
            },
            Outputs = graphicsDevice.SwapchainFramebuffer.OutputDescription // TODO: Allow custom output! even for SpriteBatch its for things like MSAA!!!
        };

        pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleList;
        this._pipelineTriangleList = new SimplePipeline(graphicsDevice, pipelineDescription);
        
        pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
        this._pipelineTriangleStrip = new SimplePipeline(graphicsDevice, pipelineDescription);
        
        pipelineDescription.PrimitiveTopology = PrimitiveTopology.LineStrip;
        this._pipelineLineLoop = new SimplePipeline(graphicsDevice, pipelineDescription);
        
        // Create vertex buffer.
        this._vertices = new PrimitiveVertex2D[capacity];
        this._tempVertices = new PrimitiveVertex2D[capacity];
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint) (capacity * Marshal.SizeOf<PrimitiveVertex2D>()), BufferUsage.VertexBuffer));
    }

    /// <summary>
    /// Begins a new batch of primitive drawing operations.
    /// </summary>
    /// <param name="commandList">The command list to record drawing commands.</param>
    /// <param name="view">Optional view transformation matrix. If null, defaults to the identity matrix.</param>
    /// <param name="projection">Optional projection transformation matrix. If null, defaults to an orthographic projection matrix.</param>
    /// <exception cref="Exception">Thrown when the method is called before the previous batch is ended.</exception>
    public void Begin(CommandList commandList, Matrix4x4? view = null, Matrix4x4? projection = null) {
        if (this._begun) {
            throw new Exception("The PrimitiveBatch has already begun!");
        }
        
        this._begun = true;
        this._currentCommandList = commandList;
        
        Matrix4x4 finalView = view ?? Matrix4x4.Identity;
        Matrix4x4 finalProj = projection ?? Matrix4x4.CreateOrthographicOffCenter(0.0F, this.Window.Width, this.Window.Height, 0.0F, 0.0F, 1.0F);
        
        this._projViewBuffer.SetValue(0, finalView * finalProj, true);
        this.DrawCallCount = 0;
    }
    
    /// <summary>
    /// Ends the current batch of primitive drawing operations.
    /// </summary>
    /// <exception cref="Exception">Thrown when the method is called before calling Begin().</exception>
    public void End() {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }

        this._begun = false;
        this.Flush();
    }

    /// <summary>
    /// Draws a line between two points with the specified thickness and color.
    /// </summary>
    /// <param name="start">The start point of the line.</param>
    /// <param name="end">The end point of the line.</param>
    /// <param name="thickness">The thickness of the line. Default is 1.0.</param>
    /// <param name="color">The color of the line. If null, defaults to white.</param>
    public void DrawLine(Vector2 start, Vector2 end, float thickness = 1.0F, Color? color = null) {
        float distance = Vector2.Distance(start, end);
        float angle = float.RadiansToDegrees(MathF.Atan2(end.Y - start.Y, end.X - start.X));
        
        RectangleF rectangle = new RectangleF(start.X, start.Y, distance, thickness);
        this.DrawFilledRectangle(rectangle, new Vector2(0, thickness / 2.0F), angle, color ?? Color.White);
    }

    /// <summary>
    /// Draws an empty rectangle with the specified dimensions, outline size, origin point, rotation, and color.
    /// </summary>
    /// <param name="rectangle">Specifies the position and size of the rectangle.</param>
    /// <param name="thickness">Width of the rectangle's outline.</param>
    /// <param name="origin">Optional origin point for rotation and positioning. Defaults to (0,0).</param>
    /// <param name="rotation">Optional rotation angle in degrees. Defaults to 0.0F.</param>
    /// <param name="color">Optional color for the rectangle's outline. Defaults to white.</param>
    public void DrawEmptyRectangle(RectangleF rectangle, float thickness, Vector2? origin = null, float rotation = 0.0F, Color? color = null) {
        Vector2 finalOrigin = origin ?? Vector2.Zero;
        float finalRotation = float.DegreesToRadians(rotation);
        Color finalColor = color ?? Color.White;

        Matrix3x2 transform = Matrix3x2.CreateRotation(finalRotation, rectangle.Position);
        
        // Calculate the four corners of the rectangle
        Vector2 topLeft = Vector2.Transform(new Vector2(rectangle.X, rectangle.Y) - finalOrigin, transform);
        Vector2 topRight = Vector2.Transform(new Vector2(rectangle.X + rectangle.Width, rectangle.Y) - finalOrigin, transform);
        Vector2 bottomLeft = Vector2.Transform(new Vector2(rectangle.X, rectangle.Y + rectangle.Height) - finalOrigin, transform);
        Vector2 bottomRight = Vector2.Transform(new Vector2(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height) - finalOrigin, transform);
        
        // Line offset
        Vector2 lineOffsetX = new Vector2(thickness / 2.0F, 0);
        Vector2 lineOffsetY = new Vector2(0, thickness / 2.0F);
        
        // Top side
        this.DrawLine(topLeft - lineOffsetX, topRight + lineOffsetX, thickness, finalColor);

        // Bottom side
        this.DrawLine(bottomLeft - lineOffsetX, bottomRight + lineOffsetX, thickness, finalColor);

        // Left side
        this.DrawLine(topLeft + lineOffsetY, bottomLeft - lineOffsetY, thickness, finalColor);

        // Right side
        this.DrawLine(topRight + lineOffsetY, bottomRight - lineOffsetY, thickness, finalColor);
    }

    /// <summary>
    /// Draws a rectangle with optional origin point, rotation, and color.
    /// </summary>
    /// <param name="rectangle">The rectangle specifying the position and size.</param>
    /// <param name="origin">Optional origin point for the rectangle, defaults to the top-left corner.</param>
    /// <param name="rotation">Optional rotation angle in radians, defaults to 0.0F.</param>
    /// <param name="color">Optional color for the rectangle, defaults to White.</param>
    public void DrawFilledRectangle(RectangleF rectangle, Vector2? origin = null, float rotation = 0.0F, Color? color = null) {
        Vector2 finalOrigin = origin ?? Vector2.Zero;
        Color finalColor = color ?? Color.White;
        float finalRotation = float.DegreesToRadians(rotation);
        
        Matrix3x2 transform = Matrix3x2.CreateRotation(finalRotation, rectangle.Position);

        PrimitiveVertex2D topLeft = new PrimitiveVertex2D() {
            Position = Vector2.Transform(new Vector2(rectangle.X, rectangle.Y) - finalOrigin, transform),
            Color = finalColor.ToRgbaFloat().ToVector4()
        };
        
        PrimitiveVertex2D topRight = new PrimitiveVertex2D() {
            Position = Vector2.Transform(new Vector2(rectangle.X + rectangle.Width, rectangle.Y) - finalOrigin, transform),
            Color = finalColor.ToRgbaFloat().ToVector4()
        };
        
        PrimitiveVertex2D bottomLeft = new PrimitiveVertex2D() {
            Position = Vector2.Transform(new Vector2(rectangle.X, rectangle.Y + rectangle.Height) - finalOrigin, transform),
            Color = finalColor.ToRgbaFloat().ToVector4()
        };
        
        PrimitiveVertex2D bottomRight = new PrimitiveVertex2D() {
            Position = Vector2.Transform(new Vector2(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height) - finalOrigin, transform),
            Color = finalColor.ToRgbaFloat().ToVector4()
        };

        this._tempVertices[0] = bottomLeft;
        this._tempVertices[1] = topRight;
        this._tempVertices[2] = topLeft;
        this._tempVertices[3] = bottomLeft;
        this._tempVertices[4] = bottomRight;
        this._tempVertices[5] = topRight;
        
        this.AddVertices(this._pipelineTriangleList, 6);
    }
    
    public void DrawEmptyCircleSector(Vector2 position, float radius, float startAngle, float endAngle, int thickness, int segments, Color? color = null) {
        
    }
    
    public void DrawCircleSector(Vector2 position, float radius, float startAngle, float endAngle, int segments, Color? color = null) {
        
    }

    public void DrawEmptyCircle(Vector2 position, float radius, int thickness, int segments, Color? color = null) {
        
    }

    public void DrawFilledCircle(Vector2 position, float radius, int segments, Color? color = null) {
        
    }
    
    public void DrawEmptyRing(Vector2 position, float innerRadius, float outerRadius, int thickness, int segments, Color? color = null) {
        
    }
    
    public void DrawFilledRing(Vector2 position, float innerRadius, float outerRadius, int segments, Color? color = null) {
        
    }

    public void DrawEmptyEllipse(Vector2 position, Vector2 radius, int thickness, int segments, Color? color = null) {
        
    }
    
    public void DrawFilledEllipse(Vector2 position, Vector2 radius, int segments, Color? color = null) {
        
    }

    public void DrawEmptyTriangle(Vector2 point1, Vector2 point2, Vector2 point3, int thickness, Color? color = null) {
        
    }
    
    public void DrawFilledTriangle(Vector2 point1, Vector2 point2, Vector2 point3, Color? color = null) {
        
    }

    /// <summary>
    /// Adds a specified number of vertices to the current batch for the given pipeline.
    /// </summary>
    /// <param name="pipeline">The rendering pipeline to use for this batch of vertices.</param>
    /// <param name="count">The number of vertices to add to the batch.</param>
    /// <exception cref="Exception">Thrown if the batch has not been begun before calling this method.</exception>
    private void AddVertices(SimplePipeline pipeline, int count) {
        if (!this._begun) {
            throw new Exception("You must begin the PrimitiveBatch before calling draw methods!");
        }
        
        if (this._currentPipeline != pipeline) {
            this.Flush();
        }

        this._currentPipeline = pipeline;
        
        if (this._currentBatchCount + count >= this._vertices.Length) {
            this.Flush();
        }

        for (int i = 0; i < count; i++) {
            this._vertices[this._currentBatchCount] = this._tempVertices[i];
            this._currentBatchCount += 1;
        }
        
        Array.Clear(this._tempVertices);
    }

    /// <summary>
    /// Flushes the current batch of primitives to the GPU for rendering.
    /// </summary>
    private void Flush() {
        if (this._currentBatchCount == 0 || this._currentPipeline == null) {
            return;
        }
        
        // Update vertex buffer.
        this._currentCommandList.UpdateBuffer(this._vertexBuffer, 0, this._vertices);
        
        // Set vertex buffer.
        this._currentCommandList.SetVertexBuffer(0, this._vertexBuffer);
        
        // Set pipeline.
        this._currentCommandList.SetPipeline(this._currentPipeline.Pipeline);
        
        // Set projection view buffer.
        this._currentCommandList.SetGraphicsResourceSet(0, this._projViewBuffer.ResourceSet);
        
        // Draw.
        this._currentCommandList.Draw(this._currentBatchCount);

        // Clean up.
        this._currentBatchCount = 0;
        this._currentPipeline = null;
        Array.Clear(this._vertices);
        
        this.DrawCallCount++;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._effect.Dispose();
            this._projViewBuffer.Dispose();
            this._pipelineTriangleList.Dispose();
            this._pipelineTriangleStrip.Dispose();
            this._pipelineLineLoop.Dispose();
        }
    }
}