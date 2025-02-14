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
    public IWindow Window { get; private set; }
    
    /// <summary>
    /// Represents the output description utilized by the <see cref="PrimitiveBatch"/> for rendering configurations.
    /// The <see cref="Output"/> property specifies how the rendering results are presented on the screen.
    /// </summary>
    public OutputDescription Output { get; private set; }

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
    /// Initializes a new instance of the PrimitiveBatch class for rendering 2D primitives.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering.</param>
    /// <param name="window">The window representing the rendering context.</param>
    /// <param name="output">The output description defining the render target.</param>
    /// <param name="capacity">Optional. The initial capacity of the vertex buffer.</param>
    public PrimitiveBatch(GraphicsDevice graphicsDevice, IWindow window, OutputDescription output, uint capacity = 30720) {
        this.GraphicsDevice = graphicsDevice;
        this.Window = window;
        this.Output = output;
        this.Capacity = capacity;
        
        // Create effects.
        this._effect = GlobalResource.PrimitiveEffect;
        
        // Create projection view buffer.
        this._projViewBuffer = new SimpleBuffer<Matrix4x4>(graphicsDevice, 2, SimpleBufferType.Uniform, ShaderStages.Vertex);

        // Create pipelines.
        SimplePipelineDescription pipelineDescription = new SimplePipelineDescription() {
            BlendState = BlendState.AlphaBlend.Description,
            DepthStencilState = new DepthStencilStateDescription(false, false, ComparisonKind.LessEqual),
            RasterizerState = new RasterizerStateDescription() {
                DepthClipEnabled = true,
                CullMode = FaceCullMode.None
            },
            BufferLayouts = this._effect.GetBufferLayouts(),
            ShaderSet = new ShaderSetDescription() {
                VertexLayouts = [
                    PrimitiveVertex2D.VertexLayout
                ],
                Shaders = [
                    this._effect.Shader.Item1,
                    this._effect.Shader.Item2
                ]
            },
            Outputs = output
        };

        pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleList;
        this._pipelineTriangleList = this._effect.GetPipeline(pipelineDescription);
        
        pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
        this._pipelineTriangleStrip = this._effect.GetPipeline(pipelineDescription);
        
        // Create vertex buffer.
        this._vertices = new PrimitiveVertex2D[capacity];
        this._tempVertices = new PrimitiveVertex2D[capacity];
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint) (capacity * Marshal.SizeOf<PrimitiveVertex2D>()), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
    }

    /// <summary>
    /// Begins a new batch of primitive drawing operations.
    /// </summary>
    /// <param name="commandList">The command list to record drawing commands.</param>
    /// <param name="projection">Optional projection transformation matrix. If null, defaults to an orthographic projection matrix.</param>
    /// <param name="view">Optional view transformation matrix. If null, defaults to the identity matrix.</param>
    /// <exception cref="Exception">Thrown when the method is called before the previous batch is ended.</exception>
    public void Begin(CommandList commandList, Matrix4x4? projection = null, Matrix4x4? view = null) {
        if (this._begun) {
            throw new Exception("The PrimitiveBatch has already begun!");
        }
        
        this._begun = true;
        this._currentCommandList = commandList;
        
        Matrix4x4 finalProj = projection ?? Matrix4x4.CreateOrthographicOffCenter(0.0F, this.Window.GetWidth(), this.Window.GetHeight(), 0.0F, 0.0F, 1.0F);
        Matrix4x4 finalView = view ?? Matrix4x4.Identity;

        this._projViewBuffer.SetValue(0, finalProj);
        this._projViewBuffer.SetValue(1, finalView);
        this._projViewBuffer.UpdateBufferImmediate();
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
    public void DrawLine(Vector2 start, Vector2 end, float thickness, Color? color = null) {
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
            Color = finalColor.ToRgbaFloatVec4()
        };
        
        PrimitiveVertex2D topRight = new PrimitiveVertex2D() {
            Position = Vector2.Transform(new Vector2(rectangle.X + rectangle.Width, rectangle.Y) - finalOrigin, transform),
            Color = finalColor.ToRgbaFloatVec4()
        };
        
        PrimitiveVertex2D bottomLeft = new PrimitiveVertex2D() {
            Position = Vector2.Transform(new Vector2(rectangle.X, rectangle.Y + rectangle.Height) - finalOrigin, transform),
            Color = finalColor.ToRgbaFloatVec4()
        };
        
        PrimitiveVertex2D bottomRight = new PrimitiveVertex2D() {
            Position = Vector2.Transform(new Vector2(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height) - finalOrigin, transform),
            Color = finalColor.ToRgbaFloatVec4()
        };

        this._tempVertices[0] = bottomLeft;
        this._tempVertices[1] = topRight;
        this._tempVertices[2] = topLeft;
        this._tempVertices[3] = bottomLeft;
        this._tempVertices[4] = bottomRight;
        this._tempVertices[5] = topRight;
        
        this.AddVertices(this._pipelineTriangleList, 6);
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
    /// <param name="color">The color of the circle sector line. Defaults to white if null.</param>
    public void DrawEmptyCircleSector(Vector2 position, float radius, float startAngle, float endAngle, int thickness, int segments, Color? color = null) {
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
            
            this.DrawLine(lastPoint, currentPoint, thickness, finalColor);
            lastPoint = currentPoint;
        }

        // Draw the sector edges to the center.
        Vector2 lineOffsetX = new Vector2(thickness / 2.0F, 0);
        Vector2 lineOffsetY = new Vector2(0, thickness / 2.0F);

        this.DrawLine(position - lineOffsetX, firstPoint + lineOffsetX, thickness, finalColor);
        this.DrawLine(position + lineOffsetY, lastPoint - lineOffsetY, thickness, finalColor);
    }

    /// <summary>
    /// Draws a filled sector of a circle on the screen at the specified position with the given parameters.
    /// </summary>
    /// <param name="position">The center position of the circle sector.</param>
    /// <param name="radius">The radius of the circle sector.</param>
    /// <param name="startAngle">The starting angle of the sector in degrees.</param>
    /// <param name="endAngle">The ending angle of the sector in degrees.</param>
    /// <param name="segments">Number of segments to use for drawing the sector.</param>
    /// <param name="color">Optional color to use for the sector. Defaults to white if null.</param>
    public void DrawFilledCircleSector(Vector2 position, float radius, float startAngle, float endAngle, int segments, Color? color = null) {
        float finalStartAngle = float.DegreesToRadians(startAngle);
        float finalEndAngle = float.DegreesToRadians(endAngle);
        Color finalColor = color ?? Color.White;

        // Calculate the angular range and the number of segments
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

            this._tempVertices[0] = new PrimitiveVertex2D() {
                Position = position,
                Color = finalColor.ToRgbaFloatVec4()
            };
            this._tempVertices[1] = new PrimitiveVertex2D() {
                Position = lastPoint,
                Color = finalColor.ToRgbaFloatVec4()
            };
            this._tempVertices[2] = new PrimitiveVertex2D() {
                Position = currentPoint,
                Color = finalColor.ToRgbaFloatVec4()
            };
        
            this.AddVertices(this._pipelineTriangleList, 3);
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
    /// <param name="color">The color of the circle's outline. Defaults to white if not specified.</param>
    public void DrawEmptyCircle(Vector2 position, float radius, int thickness, int segments, Color? color = null) {
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
            
            this.DrawLine(adjustedStartPoint, adjustedEndPoint, thickness, finalColor);
        }
    }

    /// <summary>
    /// Draws a filled circle at the specified position with the given radius, number of segments, and optional color.
    /// </summary>
    /// <param name="position">The position of the center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="segments">The number of segments to use for drawing the circle.</param>
    /// <param name="color">The optional color of the circle. If null, defaults to white.</param>
    public void DrawFilledCircle(Vector2 position, float radius, int segments, Color? color = null) {
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

            this._tempVertices[0] = new PrimitiveVertex2D() {
                Position = position,
                Color = finalColor.ToRgbaFloatVec4()
            };
            this._tempVertices[1] = new PrimitiveVertex2D() {
                Position = lastPoint,
                Color = finalColor.ToRgbaFloatVec4()
            };
            this._tempVertices[2] = new PrimitiveVertex2D() {
                Position = currentPoint,
                Color = finalColor.ToRgbaFloatVec4()
            };
            
            this.AddVertices(this._pipelineTriangleList, 3);
            lastPoint = currentPoint;
        }
    }

    /// <summary>
    /// Draws an empty ring at the specified position with given inner and outer radii, thickness, segments, and optional color.
    /// </summary>
    /// <param name="position">The position where the ring will be drawn.</param>
    /// <param name="innerRadius">The inner radius of the ring.</param>
    /// <param name="outerRadius">The outer radius of the ring.</param>
    /// <param name="thickness">The thickness of the ring.</param>
    /// <param name="segments">The number of segments to use for drawing the ring. Minimum is 4.</param>
    /// <param name="color">Optional color to use for drawing the ring. Defaults to white if not provided.</param>
    public void DrawEmptyRing(Vector2 position, float innerRadius, float outerRadius, int thickness, int segments, Color? color = null) {
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
            this.DrawLine(adjustedInnerStartPoint, adjustedInnerEndPoint, thickness, finalColor);
            
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
            this.DrawLine(adjustedOuterStartPoint, adjustedOuterEndPoint, thickness, finalColor);
        }
    }

    /// <summary>
    /// Draws a filled ring at the specified position with the given inner and outer radii, segment count, and optional color.
    /// </summary>
    /// <param name="position">The center position of the ring.</param>
    /// <param name="innerRadius">The inner radius of the ring.</param>
    /// <param name="outerRadius">The outer radius of the ring.</param>
    /// <param name="segments">The number of segments to use for drawing the ring.</param>
    /// <param name="color">The color of the ring. If not provided, defaults to white.</param>
    public void DrawFilledRing(Vector2 position, float innerRadius, float outerRadius, int segments, Color? color = null) {
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
            this._tempVertices[0] = new PrimitiveVertex2D() {
                Position = innerStart,
                Color = finalColor.ToRgbaFloatVec4()
            };
            this._tempVertices[1] = new PrimitiveVertex2D() {
                Position = outerStart,
                Color = finalColor.ToRgbaFloatVec4()
            };
            this._tempVertices[2] = new PrimitiveVertex2D() {
                Position = outerEnd,
                Color = finalColor.ToRgbaFloatVec4()
            };
    
            this._tempVertices[3] = new PrimitiveVertex2D() {
                Position = innerStart,
                Color = finalColor.ToRgbaFloatVec4()
            };
            this._tempVertices[4] = new PrimitiveVertex2D() {
                Position = outerEnd,
                Color = finalColor.ToRgbaFloatVec4()
            };
            this._tempVertices[5] = new PrimitiveVertex2D() {
                Position = innerEnd,
                Color = finalColor.ToRgbaFloatVec4()
            };
    
            this.AddVertices(this._pipelineTriangleList, 6);
        }
    }

    /// <summary>
    /// Draws an empty ellipse at the specified position with the given radius, thickness, and number of segments.
    /// </summary>
    /// <param name="position">The center position of the ellipse in 2D space.</param>
    /// <param name="radius">The radius of the ellipse along the X and Y axes.</param>
    /// <param name="thickness">The thickness of the ellipse outline.</param>
    /// <param name="segments">The number of segments to use for drawing the ellipse. Minimum value is 4.</param>
    /// <param name="color">The color to use for the ellipse outline. If not specified, defaults to white.</param>
    public void DrawEmptyEllipse(Vector2 position, Vector2 radius, int thickness, int segments, Color? color = null) {
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
            this.DrawLine(adjustedStartPoint, adjustedEndPoint, thickness, finalColor);
        }
    }

    /// <summary>
    /// Draws a filled ellipse at the specified position with the given radius, number of segments, and optional color.
    /// </summary>
    /// <param name="position">The center position of the ellipse.</param>
    /// <param name="radius">The horizontal and vertical radii of the ellipse.</param>
    /// <param name="segments">The number of segments to divide the ellipse into.</param>
    /// <param name="color">The color used to fill the ellipse. Defaults to white if not specified.</param>
    public void DrawFilledEllipse(Vector2 position, Vector2 radius, int segments, Color? color = null) {
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

            this._tempVertices[0] = new PrimitiveVertex2D {
                Position = position,
                Color = finalColor.ToRgbaFloatVec4()
            };
            
            this._tempVertices[1] = new PrimitiveVertex2D {
                Position = startPoint,
                Color = finalColor.ToRgbaFloatVec4()
            };
            
            this._tempVertices[2] = new PrimitiveVertex2D {
                Position = endPoint,
                Color = finalColor.ToRgbaFloatVec4()
            };

            this.AddVertices(this._pipelineTriangleList, 3);
        }
    }

    /// <summary>
    /// Draws an empty triangle between the specified points with a given thickness and color.
    /// </summary>
    /// <param name="point1">The first vertex of the triangle.</param>
    /// <param name="point2">The second vertex of the triangle.</param>
    /// <param name="point3">The third vertex of the triangle.</param>
    /// <param name="thickness">The thickness of the triangle edges.</param>
    /// <param name="color">The color of the triangle edges. Defaults to white if not specified.</param>
    public void DrawEmptyTriangle(Vector2 point1, Vector2 point2, Vector2 point3, int thickness, Color? color = null) {
        Color finalColor = color ?? Color.White;
        
        this.DrawLine(point1, point2, thickness, finalColor);
        this.DrawLine(point2, point3, thickness, finalColor);
        this.DrawLine(point3, point1, thickness, finalColor);
    }

    /// <summary>
    /// Draws a filled triangle using the specified vertices and an optional color.
    /// </summary>
    /// <param name="point1">The first vertex of the triangle.</param>
    /// <param name="point2">The second vertex of the triangle.</param>
    /// <param name="point3">The third vertex of the triangle.</param>
    /// <param name="color">The color of the triangle. If null, the default color is white.</param>
    public void DrawFilledTriangle(Vector2 point1, Vector2 point2, Vector2 point3, Color? color = null) {
        Color finalColor = color ?? Color.White;

        this._tempVertices[0] = new PrimitiveVertex2D {
            Position = point1,
            Color = finalColor.ToRgbaFloatVec4()
        };

        this._tempVertices[1] = new PrimitiveVertex2D {
            Position = point2,
            Color = finalColor.ToRgbaFloatVec4()
        };

        this._tempVertices[2] = new PrimitiveVertex2D {
            Position = point3,
            Color = finalColor.ToRgbaFloatVec4()
        };

        this.AddVertices(this._pipelineTriangleList, 3);
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
            this._currentBatchCount++;
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
        this._currentCommandList.UpdateBuffer(this._vertexBuffer, 0, new ReadOnlySpan<PrimitiveVertex2D>(this._vertices, 0, (int) this._currentBatchCount));
        
        // Set vertex buffer.
        this._currentCommandList.SetVertexBuffer(0, this._vertexBuffer);
        
        // Set pipeline.
        this._currentCommandList.SetPipeline(this._currentPipeline.Pipeline);
        
        // Set projection view buffer.
        this._currentCommandList.SetGraphicsResourceSet(0, this._projViewBuffer.GetResourceSet(this._effect.GetBufferLayout("ProjectionViewBuffer")));
        
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
            this._vertexBuffer.Dispose();
            this._projViewBuffer.Dispose();
        }
    }
}