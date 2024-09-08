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
    public SimplePipelineDescription PipelineDescription;

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
    public SimplePipeline(GraphicsDevice graphicsDevice, SimplePipelineDescription pipelineDescription) {
        this.GraphicsDevice = graphicsDevice;
        this.PipelineDescription = pipelineDescription;
        this.ResourceLayouts = new ResourceLayout[this.PipelineDescription.Buffers.Length + this.PipelineDescription.TextureLayouts.Length];
        
        int layoutIndex = 0;

        foreach (ISimpleBuffer buffer in this.PipelineDescription.Buffers) {
            this.ResourceLayouts[layoutIndex] = buffer.ResourceLayout;
            layoutIndex += 1;
        }
        
        foreach (SimpleTextureLayout textureLayout in this.PipelineDescription.TextureLayouts) {
            this.ResourceLayouts[layoutIndex] = textureLayout.Layout;
            layoutIndex += 1;
        }
        
        this.Pipeline = this.GraphicsDevice.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription() {
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
    /// Retrieves a buffer by its name from the collection of buffers in the pipeline.
    /// </summary>
    /// <param name="name">The name of the buffer to retrieve.</param>
    /// <returns>The buffer with the specified name, or null if no such buffer exists.</returns>
    public ISimpleBuffer? GetBuffer(string name) {
        return this.PipelineDescription.Buffers.FirstOrDefault(buffer => buffer.Name == name);
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.Pipeline.Dispose();
        }
    }
}