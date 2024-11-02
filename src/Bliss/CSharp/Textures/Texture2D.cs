using System.Runtime.InteropServices;
using Bliss.CSharp.Graphics;
using Bliss.CSharp.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Rectangle = Bliss.CSharp.Transformations.Rectangle;

namespace Bliss.CSharp.Textures;

// TODO: Take a look if ImageSharp can get replaced with that one that just returns ImageResult. (Make a gif system)
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
    /// Represents the sampler associated with the `Texture2D` instance, used for sampling textures.
    /// </summary>
    private Sampler _sampler;
    
    /// <summary>
    /// A dictionary that caches resource sets associated with samplers, used to avoid redundant resource set creation.
    /// </summary>
    private Dictionary<(Sampler, ResourceLayout), ResourceSet> _cachedResourceSets;

    /// <summary>
    /// Initializes a new instance of the <see cref="Texture2D"/> class using an image file path.
    /// Loads the texture from the specified path and applies the given sampler, mipmap, and sRGB settings.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering the texture.</param>
    /// <param name="path">The file path of the image to load as a texture.</param>
    /// <param name="sampler">The sampler used for texture sampling.</param>
    /// <param name="mipmap">Indicates whether to generate mipmaps for the texture.</param>
    /// <param name="srgb">Indicates whether to use sRGB color space for the texture.</param>
    public Texture2D(GraphicsDevice graphicsDevice, string path, Sampler? sampler = null, bool mipmap = true, bool srgb = false) : this(graphicsDevice, Image.Load<Rgba32>(path), sampler, mipmap, srgb) {
        Logger.Info($"Texture loaded successfully from path: [{path}]");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Texture2D"/> class using an image stream.
    /// Loads the texture from the specified stream and applies the given sampler, mipmap, and sRGB settings.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering the texture.</param>
    /// <param name="stream">The image stream to load as a texture.</param>
    /// <param name="sampler">The sampler used for texture sampling.</param>
    /// <param name="mipmap">Indicates whether to generate mipmaps for the texture.</param>
    /// <param name="srgb">Indicates whether to use sRGB color space for the texture.</param>
    public Texture2D(GraphicsDevice graphicsDevice, Stream stream, Sampler? sampler = null, bool mipmap = true, bool srgb = false) : this(graphicsDevice, Image.Load<Rgba32>(stream), sampler, mipmap, srgb) {
        Logger.Info($"Texture loaded successfully from stream: [{stream}]");
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Texture2D"/> class using an <see cref="Image{Rgba32}"/> object.
    /// Creates a device texture, applies the specified sampler, and stores mipmap and color format information.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering the texture.</param>
    /// <param name="image">The image object used to create the texture.</param>
    /// <param name="sampler">The sampler used for texture sampling.</param>
    /// <param name="mipmap">Indicates whether to generate mipmaps for the texture.</param>
    /// <param name="srgb">Indicates whether to use sRGB color space for the texture.</param>
    public Texture2D(GraphicsDevice graphicsDevice, Image<Rgba32> image, Sampler? sampler = null, bool mipmap = true, bool srgb = false) {
        this.GraphicsDevice = graphicsDevice;
        this.Images = mipmap ? MipmapHelper.GenerateMipmaps(image) : [image];
        this.Format = srgb ? PixelFormat.R8G8B8A8UNormSRgb : PixelFormat.R8G8B8A8UNorm;
        this.DeviceTexture = this.CreateDeviceTexture(graphicsDevice);
        this._sampler = sampler ?? graphicsDevice.PointSampler;
        this._cachedResourceSets = new Dictionary<(Sampler, ResourceLayout), ResourceSet>();
    }

    /// <summary>
    /// Gets the sampler associated with this texture.
    /// </summary>
    /// <returns>The sampler instance used by this texture.</returns>
    public Sampler GetSampler() {
        return this._sampler;
    }

    /// <summary>
    /// Sets the sampler for the texture.
    /// </summary>
    /// <param name="sampler">The sampler to set.</param>
    public void SetSampler(Sampler sampler) {
        this._sampler = sampler;
    }

    /// <summary>
    /// Sets the sampler for the texture using the specified sampler type.
    /// </summary>
    /// <param name="samplerType">The type of the sampler to be set for the texture.</param>
    public void SetSampler(SamplerType samplerType) {
        this._sampler = GraphicsHelper.GetSampler(this.GraphicsDevice, samplerType);
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
    public void UpdateData<T>(T[] data, Rectangle? rectangle = null) where T : unmanaged { // TODO: Make it possible if you change the texture data that the SpriteBatch updates to like 1. texture1 drawing 2. Update Data 3. texture1 drawing again but still uses the old data because it use the old texture (i think maybe it will works anyways)
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