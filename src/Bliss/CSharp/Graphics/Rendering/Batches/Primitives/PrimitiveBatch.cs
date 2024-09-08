using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Geometry;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Windowing;
using Veldrid;

namespace Bliss.CSharp.Graphics.Rendering.Batches.Primitives;

public class PrimitiveBatch : Disposable {
    
    /// <summary>
    /// Defines a template for vertex positions used to create a quad. 
    /// The array contains four <see cref="Vector2"/> instances representing the corners of the quad.
    /// </summary>
    private static readonly Vector2[] VertexTemplate = new Vector2[] {
        new Vector2(0.0F, 0.0F),
        new Vector2(1.0F, 0.0F),
        new Vector2(0.0F, 1.0F),
        new Vector2(1.0F, 1.0F),
    };
    
    /// <summary>
    /// Defines an index template for rendering two triangles as a quad.
    /// The array contains six <see cref="ushort"/> values, representing the vertex indices for two triangles.
    /// </summary>
    private static readonly ushort[] IndicesTemplate = new ushort[] {
        2, 1, 0,
        2, 3, 1
    };
    
    public GraphicsDevice GraphicsDevice { get; private set; }
    public Window Window { get; private set; }
    public uint Capacity { get; private set; }
    public int DrawCallCount { get; private set; }
    
    private SimpleBuffer<Matrix4x4> _projViewBuffer;

    private Effect _effect;
    private SimplePipeline _pipelineTriangleList;
    private SimplePipeline _pipelineTriangleStrip;
    private SimplePipeline _pipelineLineLoop;
    
    private bool _begun;
    
    private CommandList _currentCommandList;

    private SimplePipeline _currentPipeline;

    public PrimitiveBatch(GraphicsDevice graphicsDevice, Window window, uint capacity = 15360) {
        this.GraphicsDevice = graphicsDevice;
        this.Window = window;
        this.Capacity = capacity;
        
        // Create effects.
        this._effect = new Effect(graphicsDevice.ResourceFactory, Vertex2D.VertexLayout, "content/shaders/primitive.vert", "content/shaders/primitive.frag");

        // Create pipelines.
        SimplePipelineDescription pipelineDescription = new SimplePipelineDescription() {
            BlendState = BlendState.AlphaBlend.Description,
            DepthStencilState = new DepthStencilStateDescription(true, true, ComparisonKind.LessEqual),
            RasterizerState = new RasterizerStateDescription() {
                DepthClipEnabled = true,
                CullMode = FaceCullMode.None
            },
            ShaderSet = new ShaderSetDescription() {
                VertexLayouts = [
                    Vertex2D.VertexLayout
                ],
                Shaders = [
                    this._effect.Shader.Item1,
                    this._effect.Shader.Item2
                ]
            },
            Outputs = graphicsDevice.SwapchainFramebuffer.OutputDescription
        };

        pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleList;
        this._pipelineTriangleList = new SimplePipeline(graphicsDevice, pipelineDescription);
        
        pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
        this._pipelineTriangleStrip = new SimplePipeline(graphicsDevice, pipelineDescription);
        
        pipelineDescription.PrimitiveTopology = PrimitiveTopology.LineStrip;
        this._pipelineLineLoop = new SimplePipeline(graphicsDevice, pipelineDescription);
        
        // Create projection view buffer.
        this._projViewBuffer = new SimpleBuffer<Matrix4x4>(graphicsDevice, "ProjectionViewBuffer", (uint) Marshal.SizeOf<Matrix4x4>(), SimpleBufferType.Uniform, ShaderStages.Vertex);
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

    private void Flush() {


        this.DrawCallCount++;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            
        }
    }
}