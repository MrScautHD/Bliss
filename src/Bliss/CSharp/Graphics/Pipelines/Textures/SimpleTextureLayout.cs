using Veldrith;

namespace Bliss.CSharp.Graphics.Pipelines.Textures;

public class SimpleTextureLayout : Disposable {
    
    /// <summary>
    /// The graphics device used to create the resource layout for textures and samplers.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// The name used for the texture and sampler resources.
    /// </summary>
    public string Name { get; private set; }
    
    /// <summary>
    /// The shader stages that can access the texture and sampler.
    /// </summary>
    public ShaderStages ShaderStages { get; private set; }
    
    /// <summary>
    /// The kind of texture resource used by the resource layout.
    /// </summary>
    public ResourceKind ResourceKind { get; private set; }
    
    /// <summary>
    /// The resource layout associated with textures and samplers.
    /// </summary>
    public ResourceLayout Layout { get; private set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleTextureLayout"/> class with the specified graphics device, texture name and shader stages.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create the resource layout.</param>
    /// <param name="name">The name used for the texture and sampler resources.</param>
    /// <param name="stages">The shader stages that can access the texture and sampler. Defaults to the fragment stage.</param>
    /// <param name="kind">The kind of texture resource. Defaults to a read-only texture.</param>
    public SimpleTextureLayout(GraphicsDevice graphicsDevice, string name, ShaderStages stages = ShaderStages.Fragment, ResourceKind kind = ResourceKind.TextureReadOnly) {
        this.GraphicsDevice = graphicsDevice;
        this.Name = name;
        this.ShaderStages = stages;
        this.ResourceKind = kind;
        
        this.Layout = this.GraphicsDevice.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription() {
            Elements = [
                new ResourceLayoutElementDescription(name, kind, stages),
                new ResourceLayoutElementDescription($"{name}Sampler", ResourceKind.Sampler, stages)
            ]
        });
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.Layout.Dispose();
        }
    }
}