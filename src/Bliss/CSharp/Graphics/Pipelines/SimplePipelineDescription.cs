using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.Pipelines.Textures;
using Veldrid;

namespace Bliss.CSharp.Graphics.Pipelines;

public struct SimplePipelineDescription {

    /// <summary>
    /// Defines the blend state configuration, controlling how colors are blended in the pipeline.
    /// </summary>
    public BlendStateDescription BlendState;

    /// <summary>
    /// Defines the depth and stencil state configuration, controlling depth testing and stencil operations in the pipeline.
    /// </summary>
    public DepthStencilStateDescription DepthStencilState;

    /// <summary>
    /// Defines the rasterizer state configuration, which determines how primitives are rasterized (e.g., culling mode, fill mode).
    /// </summary>
    public RasterizerStateDescription RasterizerState;

    /// <summary>
    /// Specifies the primitive topology, which defines how the input vertices are interpreted (e.g., triangles, lines).
    /// </summary>
    public PrimitiveTopology PrimitiveTopology;

    /// <summary>
    /// An array of buffers that are bound to the pipeline, containing vertex data and possibly other information.
    /// </summary>
    public ISimpleBuffer[] Buffers;

    /// <summary>
    /// An array of texture layouts, describing how textures are arranged and accessed in the pipeline.
    /// </summary>
    public SimpleTextureLayout[] TextureLayouts;

    /// <summary>
    /// Describes the shader set, including the vertex and fragment shaders used by the pipeline.
    /// </summary>
    public ShaderSetDescription ShaderSet;

    /// <summary>
    /// Defines the output configuration, specifying render targets and the depth-stencil buffer for the pipeline.
    /// </summary>
    public OutputDescription Outputs;

    /// <summary>
    /// An optional resource binding model that maps resources such as textures and buffers to the pipeline.
    /// </summary>
    public ResourceBindingModel? ResourceBindingModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimplePipelineDescription"/> struct with the provided pipeline configuration details.
    /// </summary>
    /// <param name="blendState">The blend state configuration of the pipeline.</param>
    /// <param name="depthStencilState">The depth and stencil state configuration of the pipeline.</param>
    /// <param name="rasterizerState">The rasterizer state configuration of the pipeline.</param>
    /// <param name="primitiveTopology">The primitive topology that defines how vertex data is interpreted.</param>
    /// <param name="buffers">An array of buffers to be used by the pipeline.</param>
    /// <param name="textureLayouts">An array of texture layouts to be used in the pipeline.</param>
    /// <param name="shaderSet">The shader set description that defines the vertex and fragment shaders.</param>
    /// <param name="outputs">The output configuration of the pipeline, specifying the render targets and depth-stencil buffer.</param>
    public SimplePipelineDescription(BlendStateDescription blendState, DepthStencilStateDescription depthStencilState, RasterizerStateDescription rasterizerState, PrimitiveTopology primitiveTopology, ISimpleBuffer[] buffers, SimpleTextureLayout[] textureLayouts, ShaderSetDescription shaderSet, OutputDescription outputs) {
        this.BlendState = blendState;
        this.DepthStencilState = depthStencilState;
        this.RasterizerState = rasterizerState;
        this.PrimitiveTopology = primitiveTopology;
        this.Buffers = buffers;
        this.TextureLayouts = textureLayouts;
        this.ShaderSet = shaderSet;
        this.Outputs = outputs;
        this.ResourceBindingModel = null;
    }
}