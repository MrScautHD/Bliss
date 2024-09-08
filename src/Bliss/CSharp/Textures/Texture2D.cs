using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Rectangle = Veldrid.Rectangle;

namespace Bliss.CSharp.Textures;

// TODO: Take a look if ImageSharp can get replaced with that one that just returns ImageResult.
public class Texture2D : Disposable {
    
    /// <summary>
    /// Gets the graphics device associated with this texture.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }

    /// <summary>
    /// Gets the array of images representing the mip levels of the texture.
    /// </summary>
    public Image<Rgba32>[] Images { get; }

    /// <summary>
    /// Gets the width of the texture in pixels.
    /// </summary>
    public uint Width => (uint) this.Images[0].Width;

    /// <summary>
    /// Gets the height of the texture in pixels.
    /// </summary>
    public uint Height => (uint) this.Images[0].Height;

    /// <summary>
    /// Gets the pixel format of the texture.
    /// </summary>
    public PixelFormat Format { get; }

    /// <summary>
    /// Gets the size of a pixel in bytes.
    /// </summary>
    public uint PixelSizeInBytes => sizeof(byte) * 4;

    /// <summary>
    /// Gets the number of mip levels in the texture.
    /// </summary>
    public uint MipLevels => (uint) this.Images.Length;

    /// <summary>
    /// Gets the device texture created from the images.
    /// </summary>
    public Texture DeviceTexture { get; private set; }

    /// <summary>
    /// A dictionary that caches resource sets associated with samplers, used to avoid redundant resource set creation.
    /// </summary>
    private Dictionary<(Sampler, ResourceLayout), ResourceSet> _cachedResourceSets;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Texture2D"/> class with the specified graphics device, image file path, and optional mipmapping and sRGB settings.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create the texture.</param>
    /// <param name="path">The file path of the image to load.</param>
    /// <param name="mipmap">Indicates whether to generate mipmaps for the texture. Default is true.</param>
    /// <param name="srgb">Indicates whether to use sRGB format for the texture. Default is false.</param>
    public Texture2D(GraphicsDevice graphicsDevice, string path, bool mipmap = true, bool srgb = false) : this(graphicsDevice, Image.Load<Rgba32>(path), mipmap, srgb) { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Texture2D"/> class with the specified graphics device, image stream, and optional mipmapping and sRGB settings.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create the texture.</param>
    /// <param name="stream">The stream containing the image to load.</param>
    /// <param name="mipmap">Indicates whether to generate mipmaps for the texture. Default is true.</param>
    /// <param name="srgb">Indicates whether to use sRGB format for the texture. Default is false.</param>
    public Texture2D(GraphicsDevice graphicsDevice, Stream stream, bool mipmap = true, bool srgb = false) : this(graphicsDevice, Image.Load<Rgba32>(stream), mipmap, srgb) { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Texture2D"/> class with the specified graphics device, image, and optional mipmapping and sRGB settings.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create the texture.</param>
    /// <param name="image">The image to use for the texture.</param>
    /// <param name="mipmap">Indicates whether to generate mipmaps for the texture. Default is true.</param>
    /// <param name="srgb">Indicates whether to use sRGB format for the texture. Default is false.</param>
    public Texture2D(GraphicsDevice graphicsDevice, Image<Rgba32> image, bool mipmap = true, bool srgb = false) {
        this.GraphicsDevice = graphicsDevice;
        this.Images = mipmap ? MipmapHelper.GenerateMipmaps(image) : [image];
        this.Format = srgb ? PixelFormat.R8_G8_B8_A8_UNorm_SRgb : PixelFormat.R8_G8_B8_A8_UNorm;
        this.DeviceTexture = this.CreateDeviceTexture(graphicsDevice);
        this._cachedResourceSets = new Dictionary<(Sampler, ResourceLayout), ResourceSet>();
    }

    /// <summary>
    /// Gets a resource set associated with the specified sampler and resource layout.
    /// If the resource set is already cached, it returns the cached resource set; otherwise, it creates and caches a new one.
    /// </summary>
    /// <param name="sampler">The sampler used for the resource set.</param>
    /// <param name="layout">The resource layout used for the resource set.</param>
    /// <returns>The resource set associated with the specified sampler and resource layout.</returns>
    public ResourceSet GetResourceSet(Sampler sampler, ResourceLayout layout) {
        if (!this._cachedResourceSets.TryGetValue((sampler, layout), out ResourceSet? resourceSet)) {
            ResourceSet newResourceSet = this.GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(layout, this.DeviceTexture, sampler));
                
            this._cachedResourceSets.Add((sampler, layout), newResourceSet);
            return newResourceSet;
        }

        return resourceSet;
    }

    /// <summary>
    /// Updates the texture data with the provided array of data within the specified rectangle area.
    /// </summary>
    /// <typeparam name="T">The type of the data being updated. Must be an unmanaged type.</typeparam>
    /// <param name="data">The array of data to be used for updating the texture.</param>
    /// <param name="rectangle">An optional rectangle specifying the region of the texture to update. If null, the entire texture is updated.</param>
    public void UpdateData<T>(T[] data, Rectangle? rectangle = null) where T : unmanaged {
        this.UpdateData(data.AsSpan(), rectangle);
    }

    /// <summary>
    /// Updates the texture with new data specified in the given span and optional rectangle.
    /// </summary>
    /// <param name="data">The new texture data to update.</param>
    /// <param name="rectangle">An optional rectangle specifying the area to update. If null, the entire texture is updated.</param>
    /// <typeparam name="T">The type of data elements, which must be unmanaged.</typeparam>
    public void UpdateData<T>(Span<T> data, Rectangle? rectangle = null) where T : unmanaged {
        this.UpdateData((ReadOnlySpan<T>) data, rectangle);
    }

    /// <summary>
    /// Updates the texture data with the specified data and optional rectangle region.
    /// </summary>
    /// <typeparam name="T">The type of the data. Must be unmanaged.</typeparam>
    /// <param name="data">The data to update the texture with.</param>
    /// <param name="rectangle">The optional rectangle region to update. If not specified, updates the entire texture.</param>
    public unsafe void UpdateData<T>(ReadOnlySpan<T> data, Rectangle? rectangle = null) where T : unmanaged {
        Rectangle rect = rectangle ?? new Rectangle(0, 0, (int) this.Width, (int) this.Height);

        fixed (T* ptr = data) {
            int size = data.Length * Marshal.SizeOf<T>();
            this.GraphicsDevice.UpdateTexture(this.DeviceTexture, (nint) ptr, (uint) size, (uint) rect.X, (uint) rect.Y, 0, (uint) rect.Width, (uint) rect.Height, 1, 0, 0);
        }
    }

    /// <summary>
    /// Creates a device texture from provided images and graphics device.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device on which to create the texture.</param>
    private unsafe Texture CreateDeviceTexture(GraphicsDevice graphicsDevice) {
        // TODO: DO MSSA: this.GraphicsDevice.GetSampleCountLimit(this.Format, false);
        //Texture multiSampledTexture = graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(this.Width, Height, 1, 1, this.Format, TextureUsage.RenderTarget, TextureSampleCount.Count8));
        //Framebuffer framebuffer = graphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription(null, multiSampledTexture));
        
        Texture texture = graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(this.Width, this.Height, this.MipLevels, 1, this.Format, TextureUsage.Sampled));
        
        for (int i = 0; i < this.MipLevels; i++) {
            Image<Rgba32> image = this.Images[i];
            
            if (!image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixelMemory)) {
                throw new VeldridException("Unable to get image pixel data!");
            }

            fixed (void* dataPtr = &MemoryMarshal.GetReference(pixelMemory.Span)) {
                graphicsDevice.UpdateTexture(texture, (nint) dataPtr, (uint) (this.PixelSizeInBytes * image.Width * image.Height), 0, 0, 0, (uint) image.Width, (uint) image.Height, 1, (uint) i, 0);
            }
        }

        return texture;
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            foreach (ResourceSet resourceSet in this._cachedResourceSets.Values) {
                resourceSet.Dispose();
            }
            
            this.DeviceTexture.Dispose();
        }
    }
}