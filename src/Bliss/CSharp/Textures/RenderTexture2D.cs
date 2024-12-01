/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

using Bliss.CSharp.Logging;
using Veldrid;

namespace Bliss.CSharp.Textures;

public class RenderTexture2D : Disposable {
    
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    public uint Width { get; private set; }
    public uint Height { get; private set; }
    
    public Texture DepthTexture { get; private set; }
    public Texture ColorTexture { get; private set; }
    public Texture DestinationTexture { get; private set; }
    public Framebuffer Framebuffer { get; private set; }

    private TextureSampleCount _sampleCount;
    private Dictionary<(Sampler, ResourceLayout), ResourceSet> _cachedResourceSets;
    
    public RenderTexture2D(GraphicsDevice graphicsDevice, uint width, uint height, TextureSampleCount sampleCount = TextureSampleCount.Count1) {
        this.GraphicsDevice = graphicsDevice;
        this.Width = width;
        this.Height = height;
        this._sampleCount = this.GetValidSampleCount(sampleCount);
        this._cachedResourceSets = new Dictionary<(Sampler, ResourceLayout), ResourceSet>();
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
        this.DepthTexture = this.GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(this.Width, this.Height, 1, 1, 1, PixelFormat.D32FloatS8UInt, TextureUsage.DepthStencil | TextureUsage.Sampled, TextureType.Texture2D, this.SampleCount));
        this.ColorTexture = this.GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(this.Width, this.Height, 1, 1, 1, PixelFormat.R8G8B8A8UNorm, TextureUsage.RenderTarget | TextureUsage.Sampled, TextureType.Texture2D, this.SampleCount));
        this.DestinationTexture = this.GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(this.Width, this.Height, 1, 1, 1, PixelFormat.R8G8B8A8UNorm, TextureUsage.Sampled, TextureType.Texture2D));
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
            ResourceSet newResourceSet;
            
            if (this.SampleCount == TextureSampleCount.Count1) {
                newResourceSet = this.GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(layout, this.ColorTexture, sampler));
            }
            else {
                newResourceSet = this.GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(layout, this.DestinationTexture, sampler));
            }
            
            this._cachedResourceSets.Add((sampler, layout), newResourceSet);
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
        this.DepthTexture.Dispose();
        this.ColorTexture.Dispose();
        this.DestinationTexture.Dispose();
        this.Framebuffer.Dispose();
        this._cachedResourceSets.Clear();
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.ClearResources();
        }
    }
}