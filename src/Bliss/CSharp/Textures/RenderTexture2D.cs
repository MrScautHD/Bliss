using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Transformations;
using Veldrid;

namespace Bliss.CSharp.Textures;

public class RenderTexture2D : Disposable {
    
    /// <summary>
    /// Gets the graphics device associated with this render texture.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }

    /// <summary>
    /// Gets the width of the render texture.
    /// </summary>
    public uint Width { get; private set; }

    /// <summary>
    /// Gets the height of the render texture.
    /// </summary>
    public uint Height { get; private set; }
    
    /// <summary>
    /// Gets the pixel format of the color texture.
    /// </summary>
    public PixelFormat Format { get; private set; }
    
    /// <summary>
    /// Gets the color texture used for rendering operations within the render texture.
    /// </summary>
    public Texture ColorTexture { get; private set; }
    
    /// <summary>
    /// Gets the depth texture associated with this render texture, used for depth and stencil operations.
    /// </summary>
    public Texture DepthTexture { get; private set; }
    
    /// <summary>
    /// Gets the framebuffer used for rendering to the textures associated with this render texture.
    /// </summary>
    public Framebuffer Framebuffer { get; private set; }

    /// <summary>
    /// An event that is triggered when the dimensions of the render texture are resized.
    /// </summary>
    public event Action<Rectangle>? Resized;
    
    /// <summary>
    /// Represents the sample count used for the textures within the render target.
    /// </summary>
    private TextureSampleCount _sampleCount;
    
    /// <summary>
    /// Stores cached resource sets associated with the color texture.
    /// </summary>
    private Dictionary<(Sampler, SimpleBufferLayout), ResourceSet> _cachedColorResourceSets;
    
    /// <summary>
    /// Stores cached resource sets associated with the depth texture.
    /// </summary>
    private Dictionary<(Sampler, SimpleBufferLayout), ResourceSet> _cachedDepthResourceSets;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="RenderTexture2D"/> class, creating a render target texture with specified dimensions and sample count.
    /// </summary>
    /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/> used to create and manage the texture.</param>
    /// <param name="width">The width of the render texture in pixels.</param>
    /// <param name="height">The height of the render texture in pixels.</param>
    /// <param name="srgb">Indicates whether to use sRGB color space for the color texture.</param>
    /// <param name="sampleCount">The number of samples for multisampling. Defaults to <see cref="TextureSampleCount.Count1"/>.</param>
    public RenderTexture2D(GraphicsDevice graphicsDevice, uint width, uint height, bool srgb = false, TextureSampleCount sampleCount = TextureSampleCount.Count1) {
        this.GraphicsDevice = graphicsDevice;
        this.Width = width;
        this.Height = height;
        this.Format = srgb ? PixelFormat.R8G8B8A8UNormSRgb : PixelFormat.R8G8B8A8UNorm;
        this._sampleCount = this.GetValidSampleCount(sampleCount);
        this._cachedColorResourceSets = new Dictionary<(Sampler, SimpleBufferLayout), ResourceSet>();
        this._cachedDepthResourceSets = new Dictionary<(Sampler, SimpleBufferLayout), ResourceSet>();
        this.CreateFrameBuffer();
    }
    
    /// <summary>
    /// Gets or sets the sample count for the render texture.
    /// </summary>
    public TextureSampleCount SampleCount {
        get => this._sampleCount;
        set {
            this._sampleCount = this.GetValidSampleCount(value);
            this.ClearResources();
            this.CreateFrameBuffer();
        }
    }
    
    /// <summary>
    /// Creates a framebuffer with depth and color textures based on the specified width, height, and sample count.
    /// </summary>
    public void CreateFrameBuffer() {
        this.ColorTexture = this.GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(this.Width, this.Height, 1, 1, 1, this.Format, TextureUsage.RenderTarget | TextureUsage.Sampled, TextureType.Texture2D, this._sampleCount));
        this.DepthTexture = this.GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(this.Width, this.Height, 1, 1, 1, PixelFormat.D32FloatS8UInt, TextureUsage.DepthStencil | TextureUsage.Sampled, TextureType.Texture2D, this._sampleCount));
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
        
        this.ClearResources();
        this.CreateFrameBuffer();
        
        this.Resized?.Invoke(new Rectangle(0, 0, (int) width, (int) height));
    }
    
    /// <summary>
    /// Retrieves a cached or newly created color <see cref="ResourceSet"/> associated with the specified sampler and resource layout.
    /// </summary>
    /// <param name="sampler">The <see cref="Sampler"/> used for sampling the color texture.</param>
    /// <param name="layout">The <see cref="ResourceLayout"/> defining the resource bindings for the color texture.</param>
    /// <returns>A <see cref="ResourceSet"/> that binds the color texture and provided sampler to the specified resource layout.</returns>
    public ResourceSet GetColorResourceSet(Sampler sampler, SimpleBufferLayout layout) {
        if (!this._cachedColorResourceSets.TryGetValue((sampler, layout), out ResourceSet? resourceSet)) {
            ResourceSet newResourceSet = this.GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(layout.Layout, this.ColorTexture, sampler));
            
            this._cachedColorResourceSets.Add((sampler, layout), newResourceSet);
            return newResourceSet;
        }
        
        return resourceSet;
    }
    
    /// <summary>
    /// Retrieves a <see cref="ResourceSet"/> object used for sampling the depth texture of the render target.
    /// </summary>
    /// <param name="sampler">The sampler object used to define how the depth texture will be sampled.</param>
    /// <param name="layout">The layout that specifies the structure of the resource set.</param>
    /// <returns>A <see cref="ResourceSet"/> used for accessing the depth texture with the specified sampler and layout.</returns>
    public ResourceSet GetDepthResourceSet(Sampler sampler, SimpleBufferLayout layout) {
        if (!this._cachedDepthResourceSets.TryGetValue((sampler, layout), out ResourceSet? resourceSet)) {
            ResourceSet newResourceSet = this.GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(layout.Layout, this.DepthTexture, sampler));
            
            this._cachedDepthResourceSets.Add((sampler, layout), newResourceSet);
            return newResourceSet;
        }
        
        return resourceSet;
    }
    
    /// <summary>
    /// Returns a valid sample count for the current GraphicsDevice. If the specified sample count exceeds the device's limit, the maximum valid sample count is returned.
    /// </summary>
    /// <param name="sampleCount">The desired sample count.</param>
    /// <returns>The valid sample count, which might be equal to or less than the requested sample count.</returns>
    private TextureSampleCount GetValidSampleCount(TextureSampleCount sampleCount) {
        TextureSampleCount maxSamples = this.GraphicsDevice.GetSampleCountLimit(PixelFormat.R8G8B8A8UNorm, false);

        if (sampleCount > maxSamples) {
            Logger.Warn($"The count of [{sampleCount}] samples is to high for this GraphicsDevice, the count will fall back to [{maxSamples}] samples!");
            return maxSamples;
        }
        else {
            return sampleCount;
        }
    }
    
    /// <summary>
    /// Releases the resources allocated for the depth, color, and destination textures, as well as the framebuffer.
    /// Also clears any cached resource sets to ensure no references to disposed resources remain.
    /// </summary>
    private void ClearResources() {
        this.ColorTexture.Dispose();
        this.DepthTexture.Dispose();
        this.Framebuffer.Dispose();
        
        foreach (ResourceSet resourceSet in this._cachedColorResourceSets.Values) {
            resourceSet.Dispose();
        }
        
        foreach (ResourceSet resourceSet in this._cachedDepthResourceSets.Values) {
            resourceSet.Dispose();
        }
        
        this._cachedColorResourceSets.Clear();
        this._cachedDepthResourceSets.Clear();
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.ClearResources();
        }
    }
}