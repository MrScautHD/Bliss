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
    /// Contains the configuration details for setting up a graphics pipeline, including states and resource layouts.
    /// </summary>
    public PipelineDescSimpl PipelineDescription;

    /// <summary>
    /// Represents the array of resource layouts used by the pipeline to manage buffer and texture resources.
    /// </summary>
    public ResourceLayout[] ResourceLayouts { get; private set; }

    /// <summary>
    /// Represents the graphics pipeline used to render graphics objects.
    /// </summary>
    public Pipeline Pipeline { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SimplePipeline"/> class using the provided graphics device and pipeline description.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device that will be used to create the pipeline.</param>
    /// <param name="pipelineDescription">The description of the pipeline, containing configurations like blend state, shader set, and resource layouts.</param>
    public SimplePipeline(GraphicsDevice graphicsDevice, PipelineDescSimpl pipelineDescription) {
        this.GraphicsDevice = graphicsDevice;
        this.PipelineDescription = pipelineDescription;
        this.ResourceLayouts = new ResourceLayout[pipelineDescription.BufferLayouts.Count() + pipelineDescription.TextureLayouts.Count()];
        
        int layoutIndex = 0;

        foreach (SimpleBufferLayout bufferLayout in pipelineDescription.BufferLayouts) {
            this.ResourceLayouts[layoutIndex] = bufferLayout.Layout;
            layoutIndex += 1;
        }

        foreach (SimpleTextureLayout textureLayout in pipelineDescription.TextureLayouts) {
            this.ResourceLayouts[layoutIndex] = textureLayout.Layout;
            layoutIndex += 1;
        }

        this.Pipeline = graphicsDevice.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription() {
           BlendState = pipelineDescription.BlendState,
           DepthStencilState = pipelineDescription.DepthStencilState,
           RasterizerState = pipelineDescription.RasterizerState,
           PrimitiveTopology = pipelineDescription.PrimitiveTopology,
           ResourceLayouts = this.ResourceLayouts,
           ShaderSet = pipelineDescription.ShaderSet,
           Outputs = pipelineDescription.Outputs,
           ResourceBindingModel = pipelineDescription.ResourceBindingModel
        });
    }

    /// <summary>
    /// Retrieves a buffer layout from the pipeline description by its name.
    /// </summary>
    /// <param name="name">The name of the buffer layout to retrieve.</param>
    /// <returns>A <see cref="SimpleBufferLayout"/> object if a matching buffer layout is found; otherwise, null.</returns>
    public SimpleBufferLayout? GetBufferLayout(string name) {
        return this.PipelineDescription.BufferLayouts.FirstOrDefault(layout => layout.Name == name);
    }

    /// <summary>
    /// Retrieves the <see cref="SimpleTextureLayout"/> with the specified name from the pipeline description's texture layouts.
    /// </summary>
    /// <param name="name">The name of the texture layout to retrieve.</param>
    /// <returns>The <see cref="SimpleTextureLayout"/> if a matching layout is found; otherwise, null.</returns>
    public SimpleTextureLayout? GetTextureLayout(string name) {
        return this.PipelineDescription.TextureLayouts.FirstOrDefault(layout => layout.Name == name);
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.Pipeline.Dispose();
        }
    }
}