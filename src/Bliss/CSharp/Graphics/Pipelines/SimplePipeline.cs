using Bliss.CSharp.Effects;
using Veldrid;

namespace Bliss.CSharp.Graphics.Pipelines;

public class SimplePipeline : Disposable {
    
    public GraphicsDevice GraphicsDevice { get; private set; }
    public Effect Effect { get; private set; }
    public ResourceLayout[] ResourceLayouts { get; private set; }
    public OutputDescription Output { get; private set; }
    public BlendStateDescription BlendState { get; private set; }
    public FaceCullMode CullMode { get; private set; }
    
    public Pipeline Pipeline { get; private set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SimplePipeline"/> class, setting up the graphics pipeline with the specified configurations.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create GPU resources and manage rendering.</param>
    /// <param name="effect">The effect containing the vertex and fragment shaders to be used in the pipeline.</param>
    /// <param name="resourceLayouts">The resource layouts defining the structure of the resources used in the pipeline.</param>
    /// <param name="output">The output description that defines the render targets and their formats.</param>
    /// <param name="blendState">The blend state description for controlling how colors are blended.</param>
    /// <param name="cullMode">The face culling mode for rendering triangles.</param>
    public SimplePipeline(GraphicsDevice graphicsDevice, Effect effect, ResourceLayout[] resourceLayouts, OutputDescription output, BlendStateDescription blendState, FaceCullMode cullMode) {
        this.GraphicsDevice = graphicsDevice;
        this.Effect = effect;
        this.ResourceLayouts = resourceLayouts;
        this.Output = output;
        this.BlendState = blendState;
        this.CullMode = cullMode;

        this.Pipeline = this.GraphicsDevice.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription() {
                BlendState = this.BlendState,
                DepthStencilState = new DepthStencilStateDescription(true, true, ComparisonKind.LessEqual),
                RasterizerState = new RasterizerStateDescription() {
                    DepthClipEnabled = true,
                    CullMode = this.CullMode,
                    ScissorTestEnabled = true
                },
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                ResourceLayouts = this.ResourceLayouts,
                ShaderSet = new ShaderSetDescription() {
                    VertexLayouts = [
                        this.Effect.VertexLayout
                    ],
                    Shaders = [
                        this.Effect.Shader.Item1,
                        this.Effect.Shader.Item2
                    ]
                },
                Outputs = this.Output
            });
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.Pipeline.Dispose();
        }
    }
}