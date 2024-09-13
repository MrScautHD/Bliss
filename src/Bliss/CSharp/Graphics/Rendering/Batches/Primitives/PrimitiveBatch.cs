using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Windowing;
using Veldrid;

namespace Bliss.CSharp.Graphics.Rendering.Batches.Primitives;

public class PrimitiveBatch : Disposable {
    
    /// <summary>
    /// Defines a template for vertex positions used to create a quad. 
    /// The array contains four <see cref="Vector2"/> instances representing the corners of the quad.
    /// </summary>
    private static readonly Vector2[] QuadVertexTemplate = new Vector2[] {
        new Vector2(0.0F, 0.0F),
        new Vector2(1.0F, 0.0F),
        new Vector2(0.0F, 1.0F),
        new Vector2(1.0F, 1.0F),
    };
    
    public GraphicsDevice GraphicsDevice { get; private set; }
    public Window Window { get; private set; }
    public uint Capacity { get; private set; }
    public int DrawCallCount { get; private set; }
    
    private Effect _effect;
    private SimpleBuffer<Matrix4x4> _projViewBuffer;
    private SimplePipeline _pipelineTriangleList;
    private SimplePipeline _pipelineTriangleStrip;
    private SimplePipeline _pipelineLineLoop;
    
    private PrimitiveVertex2D[] _vertices;
    private PrimitiveVertex2D[] _tempVertices;
    private DeviceBuffer _vertexBuffer;
    
    private bool _begun;
    
    private CommandList _currentCommandList;
    private uint _currentBatchCount;
    private SimplePipeline? _currentPipeline;
    
    public PrimitiveBatch(GraphicsDevice graphicsDevice, Window window, uint capacity = 15360) {
        this.GraphicsDevice = graphicsDevice;
        this.Window = window;
        this.Capacity = capacity;
        
        // Create effects.
        this._effect = new Effect(graphicsDevice.ResourceFactory, PrimitiveVertex2D.VertexLayout, "content/shaders/primitive.vert", "content/shaders/primitive.frag");
        
        // Create projection view buffer.
        this._projViewBuffer = new SimpleBuffer<Matrix4x4>(graphicsDevice, "ProjectionViewBuffer", (uint) Marshal.SizeOf<Matrix4x4>(), SimpleBufferType.Uniform, ShaderStages.Vertex);

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

    public void End() {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }

        this._begun = false;
        this.Flush();
    }

    public void DrawLine() {
        
    }

    public void DrawRectangle(Rectangle rectangle, Vector2? origin = null, float rotation = 0.0F, Color? color = null) {
        Vector2 finalOrigin = origin ?? Vector2.Zero;
        Color finalColor = color ?? Color.White;
        
        float sin = 0;
        float cos = 0;
        float nOriginX = -finalOrigin.X;
        float nOriginY = -finalOrigin.Y;

        if (rotation != 0.0F) {
            float radiansRot = Single.DegreesToRadians(rotation);
            sin = MathF.Sin(radiansRot);
            cos = MathF.Cos(radiansRot);
        }

        PrimitiveVertex2D topLeft = new PrimitiveVertex2D() {
            Position = rotation == 0.0F
                ? new Vector2(
                    rectangle.X - finalOrigin.X,
                    rectangle.Y - finalOrigin.Y)
                : new Vector2(
                    rectangle.X + nOriginX * cos - nOriginY * sin, 
                    rectangle.Y + nOriginX * sin + nOriginY * cos),
            Color = finalColor.ToRgbaFloat().ToVector4()
        };

        float x = QuadVertexTemplate[(int) VertexTemplateType.TopRight].X;
        float w = rectangle.Width * x;
        
        PrimitiveVertex2D topRight = new PrimitiveVertex2D() {
            Position = rotation == 0.0F
                ? new Vector2(
                    rectangle.X - finalOrigin.X + w,
                    rectangle.Y - finalOrigin.Y)
                : new Vector2(
                    rectangle.X + (nOriginX + w) * cos - nOriginY * sin,
                    rectangle.Y + (nOriginX + w) * sin + nOriginY * cos),
            Color = finalColor.ToRgbaFloat().ToVector4()
        };
        
        float y = QuadVertexTemplate[(int) VertexTemplateType.BottomLeft].Y;
        float h = rectangle.Height * y;

        PrimitiveVertex2D bottomLeft = new PrimitiveVertex2D() {
            Position = rotation == 0.0F
                ? new Vector2(
                    rectangle.X - finalOrigin.X,
                    rectangle.Y - finalOrigin.Y + h)
                : new Vector2(
                    rectangle.X + nOriginX * cos - (nOriginY + h) * sin,
                    rectangle.Y + nOriginX * sin + (nOriginY + h) * cos),
            Color = finalColor.ToRgbaFloat().ToVector4()
        };
        
        x = QuadVertexTemplate[(int) VertexTemplateType.BottomRight].X;
        y = QuadVertexTemplate[(int) VertexTemplateType.BottomRight].Y;
        w = rectangle.Width * x;
        h = rectangle.Height * y;

        PrimitiveVertex2D bottomRight = new PrimitiveVertex2D() {
            Position = rotation == 0.0F
                ? new Vector2(
                    rectangle.X - finalOrigin.X + w,
                    rectangle.Y - finalOrigin.Y + h)
                : new Vector2(
                    rectangle.X + (nOriginX + w) * cos - (nOriginY + h) * sin,
                    rectangle.Y + (nOriginX + w) * sin + (nOriginY + h) * cos),
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

    public void DrawCircle() {
        
    }

    private void AddVertices(SimplePipeline pipeline, int count) {
        if (!this._begun) {
            throw new Exception("You must begin the PrimitiveBatch before calling draw methods!");
        }
        
        if (this._currentPipeline != pipeline) {
            this.Flush();
        }

        this._currentPipeline = pipeline;
        
        if (this._currentBatchCount + count >= this.Capacity) {
            this.Flush();
        }

        for (int i = 0; i < count; i++) {
            this._vertices[this._currentBatchCount] = this._tempVertices[count]; // Temp vertices (because clear a array is way more efficent then creating everytime a new one!)
            this._currentBatchCount += 1;
        }
    }
    
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