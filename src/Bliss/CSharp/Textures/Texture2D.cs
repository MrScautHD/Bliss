using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;

namespace Bliss.CSharp.Textures;

// TODO: Take a look if ImageSharp can get replaced with that one that just returns ImageResult.
public class Texture2D : Disposable {

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
        this.Images = mipmap ? MipmapHelper.GenerateMipmaps(image) : [image];
        this.Format = srgb ? PixelFormat.R8_G8_B8_A8_UNorm_SRgb : PixelFormat.R8_G8_B8_A8_UNorm;
        this.CreateDeviceTexture(graphicsDevice);
    }

    /// <summary>
    /// Creates a device texture from provided images and graphics device.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device on which to create the texture.</param>
    private unsafe void CreateDeviceTexture(GraphicsDevice graphicsDevice) {
        this.DeviceTexture = graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(this.Width, this.Height, this.MipLevels, 1, this.Format, TextureUsage.Sampled));
        
        for (int i = 0; i < this.MipLevels; i++) {
            Image<Rgba32> image = this.Images[i];
            
            if (!image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixelMemory)) {
                throw new VeldridException("Unable to get image pixel data!");
            }

            fixed (void* dataPtr = &MemoryMarshal.GetReference(pixelMemory.Span)) {
                graphicsDevice.UpdateTexture(this.DeviceTexture, (nint) dataPtr, (uint) (this.PixelSizeInBytes * image.Width * image.Height), 0, 0, 0, (uint) image.Width, (uint) image.Height, 1, (uint) i, 0);
            }
        }
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.DeviceTexture.Dispose();
        }
    }
}