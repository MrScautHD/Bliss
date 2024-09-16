using Veldrid;

namespace Bliss.CSharp.Textures;

public class RenderTexture2D : Disposable {
    
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    public uint Width { get; private set; }
    public uint Height { get; private set; }
    
    public TextureSampleCount SampleCount { get; private set; }
    
    public Texture DepthTexture { get; private set; }
    public Texture ColorTexture { get; private set; }
    public Texture DestinationTexture { get; private set; }
    public Framebuffer Framebuffer { get; private set; }
    
    private Dictionary<(Sampler, ResourceLayout), ResourceSet> _cachedResourceSets;
    
    public RenderTexture2D(GraphicsDevice graphicsDevice, uint width, uint height, TextureSampleCount sampleCount = TextureSampleCount.Count1) {
        this.GraphicsDevice = graphicsDevice;
        this.Width = width;
        this.Height = height;
        this.SampleCount = sampleCount;
        this._cachedResourceSets = new Dictionary<(Sampler, ResourceLayout), ResourceSet>();
        this.CreateFrameBuffer();
    }
    
    /// <summary>
    /// Creates a framebuffer with depth and color textures based on the specified width, height, and sample count.
    /// </summary>
    public void CreateFrameBuffer() {
        this.DepthTexture = this.GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(this.Width, this.Height, 1, 1, 1, PixelFormat.D32_Float_S8_UInt, TextureUsage.DepthStencil | TextureUsage.Sampled, TextureType.Texture2D, this.SampleCount));
        this.ColorTexture = this.GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(this.Width, this.Height, 1, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.RenderTarget | TextureUsage.Sampled, TextureType.Texture2D, this.SampleCount));
        this.DestinationTexture = this.GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(this.Width, this.Height, 1, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled, TextureType.Texture2D));
        this.Framebuffer = this.GraphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription(this.DepthTexture, this.ColorTexture));
    }

    /// <summary>
    /// Resizes the render textures and framebuffer to the new specified width and height.
    /// </summary>
    /// <param name="width">The new width of the render texture.</param>
    /// <param name="height">The new height of the render texture.</param>
    public void Resize(uint width, uint height) {
        this.Width = width;
        this.Height = height;
        
        this.DepthTexture.Dispose();
        this.ColorTexture.Dispose();
        this.DestinationTexture.Dispose();
        this.Framebuffer.Dispose();
        
        this._cachedResourceSets.Clear();

        this.CreateFrameBuffer();
    }

    /// <summary>
    /// Retrieves a <see cref="ResourceSet"/> for the specified <see cref="Sampler"/> and <see cref="ResourceLayout"/>.
    /// If the resource set is not already cached, a new one is created, cached, and then returned.
    /// </summary>
    /// <param name="sampler">The sampler to be used in the resource set.</param>
    /// <param name="layout">The resource layout defining how resources are bound to the pipeline.</param>
    /// <returns>A <see cref="ResourceSet"/> that contains the specified sampler and layout.</returns>
    public ResourceSet GetResourceSet(Sampler sampler, ResourceLayout layout) {
        if (!this._cachedResourceSets.TryGetValue((sampler, layout), out ResourceSet? resourceSet)) {
            ResourceSet newResourceSet = this.GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(layout, this.DestinationTexture, sampler));
                
            this._cachedResourceSets.Add((sampler, layout), newResourceSet);
            return newResourceSet;
        }

        return resourceSet;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.DepthTexture.Dispose();
            this.ColorTexture.Dispose();
            this.Framebuffer.Dispose();
        }
    }
}