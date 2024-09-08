using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.Pipelines.Textures;
using Veldrid;

namespace Bliss.CSharp.Graphics.Pipelines;

public class SimplePipeline : Disposable {
    
    /// <summary>
    /// Represents the graphics device used by the pipeline for creating and managing graphics resources.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }

    /// <summary>
    /// Represents the shader effect used by the pipeline.
    /// </summary>
    public Effect Effect { get; private set; }

    /// <summary>
    /// Defines the output configuration for the rendering pipeline, including target formats for color and depth-stencil buffers.
    /// </summary>
    public OutputDescription Output { get; private set; }

    /// <summary>
    /// Defines the state associated with blend operations, such as how pixel colors from a source and destination should be combined.
    /// </summary>
    public BlendStateDescription BlendState { get; private set; }

    /// <summary>
    /// Specifies the face culling mode for the graphics pipeline, determining which faces of polygons are culled during rendering.
    /// </summary>
    public FaceCullMode CullMode { get; private set; }

    /// <summary>
    /// Represents an array of buffers used in the graphics pipeline.
    /// Each buffer is utilized for storing and managing data required for rendering operations.
    /// </summary>
    public ISimpleBuffer[] Buffers { get; private set; }

    /// <summary>
    /// Represents an array of texture layouts used in the pipeline.
    /// </summary>
    public SimpleTextureLayout[] TextureLayouts { get; private set; }

    /// <summary>
    /// Represents the array of resource layouts used by the pipeline to manage buffer and texture resources.
    /// </summary>
    public ResourceLayout[] ResourceLayouts { get; private set; }

    /// <summary>
    /// Represents the graphics pipeline used to render graphics objects.
    /// </summary>
    public Pipeline Pipeline { get; private set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SimplePipeline"/> class with the specified graphics device, effect, output description, blend state, cull mode, buffers, and texture layouts.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device to create the pipeline.</param>
    /// <param name="effect">The effect containing the shaders and vertex layout.</param>
    /// <param name="output">The output description specifying the render targets and depth buffer.</param>
    /// <param name="blendState">The blend state description used for blending operations.</param>
    /// <param name="cullMode">The face cull mode determining which faces of the primitives are culled.</param>
    /// <param name="buffers">The array of buffers used for resource layouts in the pipeline.</param>
    /// <param name="textureLayouts">The array of texture layouts used for resource layouts in the pipeline.</param>
    public SimplePipeline(GraphicsDevice graphicsDevice, Effect effect, OutputDescription output, BlendStateDescription blendState, FaceCullMode cullMode, ISimpleBuffer[] buffers, SimpleTextureLayout[] textureLayouts) {
        this.GraphicsDevice = graphicsDevice;
        this.Effect = effect;
        this.Output = output;
        this.BlendState = blendState;
        this.CullMode = cullMode;
        this.Buffers = buffers;
        this.TextureLayouts = textureLayouts;
        this.ResourceLayouts = new ResourceLayout[buffers.Length + textureLayouts.Length];
        
        int layoutIndex = 0;
        
        foreach (ISimpleBuffer buffer in buffers) {
            this.ResourceLayouts[layoutIndex] = buffer.ResourceLayout;
            layoutIndex += 1;
        }
        
        foreach (SimpleTextureLayout textureLayout in textureLayouts) {
            this.ResourceLayouts[layoutIndex] = textureLayout.Layout;
            layoutIndex += 1;
        }
        
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

    /// <summary>
    /// Retrieves a buffer by its name from the collection of buffers in the pipeline.
    /// </summary>
    /// <param name="name">The name of the buffer to retrieve.</param>
    /// <returns>The buffer with the specified name, or null if no such buffer exists.</returns>
    public ISimpleBuffer? GetBuffer(string name) {
        return this.Buffers.FirstOrDefault(buffer => buffer.Name == name);
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.Pipeline.Dispose();
        }
    }
}