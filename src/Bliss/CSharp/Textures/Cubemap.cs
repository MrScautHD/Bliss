using Bliss.CSharp.Images;
using Veldrid;

namespace Bliss.CSharp.Textures;

public class Cubemap : Disposable {

    /// <summary>
    /// Gets the mipmap levels of images for each face of the cubemap.
    /// </summary>
    public Image[][] Images { get; }

    /// <summary>
    /// Gets the width of the cubemap.
    /// </summary>
    public uint Width => (uint) this.Images[0][0].Width;

    /// <summary>
    /// Gets the height of the cubemap.
    /// </summary>
    public uint Height => (uint) this.Images[0][0].Height;

    /// <summary>
    /// Gets the pixel format of the cubemap.
    /// </summary>
    public PixelFormat Format { get; }

    /// <summary>
    /// Gets the size of a pixel in bytes.
    /// </summary>
    public uint PixelSizeInBytes => sizeof(byte) * 4;

    /// <summary>
    /// Gets the number of mip levels of the cubemap.
    /// </summary>
    public uint MipLevels => (uint) this.Images[0].Length;

    /// <summary>
    /// Gets the device texture.
    /// </summary>
    public Texture DeviceTexture { get; private set; }
    
    private const int PositiveXArrayLayer = 0;
    private const int NegativeXArrayLayer = 1;
    private const int PositiveYArrayLayer = 2;
    private const int NegativeYArrayLayer = 3;
    private const int PositiveZArrayLayer = 4;
    private const int NegativeZArrayLayer = 5;

    /// <summary>
    /// Initializes a new instance of the <see cref="Cubemap"/> class from file paths.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device.</param>
    /// <param name="positiveXPath">The file path for the positive X face of the cubemap.</param>
    /// <param name="negativeXPath">The file path for the negative X face of the cubemap.</param>
    /// <param name="positiveYPath">The file path for the positive Y face of the cubemap.</param>
    /// <param name="negativeYPath">The file path for the negative Y face of the cubemap.</param>
    /// <param name="positiveZPath">The file path for the positive Z face of the cubemap.</param>
    /// <param name="negativeZPath">The file path for the negative Z face of the cubemap.</param>
    /// <param name="mipmap">Specifies whether to generate mipmaps.</param>
    /// <param name="srgb">Specifies whether the images should be loaded as sRGB.</param>
    public Cubemap(GraphicsDevice graphicsDevice, string positiveXPath, string negativeXPath, string positiveYPath, string negativeYPath, string positiveZPath, string negativeZPath, bool mipmap = true, bool srgb = false) : this(
        graphicsDevice,
        new Image(positiveXPath),
        new Image(negativeXPath),
        new Image(positiveYPath),
        new Image(negativeYPath),
        new Image(positiveZPath), 
        new Image(negativeZPath),
        mipmap,
        srgb) { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Cubemap"/> class from streams.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device.</param>
    /// <param name="positiveXStream">The stream for the positive X face of the cubemap.</param>
    /// <param name="negativeXStream">The stream for the negative X face of the cubemap.</param>
    /// <param name="positiveYStream">The stream for the positive Y face of the cubemap.</param>
    /// <param name="negativeYStream">The stream for the negative Y face of the cubemap.</param>
    /// <param name="positiveZStream">The stream for the positive Z face of the cubemap.</param>
    /// <param name="negativeZStream">The stream for the negative Z face of the cubemap.</param>
    /// <param name="mipmap">Specifies whether to generate mipmaps.</param>
    /// <param name="srgb">Specifies whether the images should be loaded as sRGB.</param>
    public Cubemap(GraphicsDevice graphicsDevice, Stream positiveXStream, Stream negativeXStream, Stream positiveYStream, Stream negativeYStream, Stream positiveZStream, Stream negativeZStream, bool mipmap = true, bool srgb = false) : this(
        graphicsDevice,
        new Image(positiveXStream),
        new Image(negativeXStream),
        new Image(positiveYStream),
        new Image(negativeYStream),
        new Image(positiveZStream),
        new Image(negativeZStream),
        mipmap,
        srgb) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Cubemap"/> class from images.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device.</param>
    /// <param name="positiveX">The image for the positive X face of the cubemap.</param>
    /// <param name="negativeX">The image for the negative X face of the cubemap.</param>
    /// <param name="positiveY">The image for the positive Y face of the cubemap.</param>
    /// <param name="negativeY">The image for the negative Y face of the cubemap.</param>
    /// <param name="positiveZ">The image for the positive Z face of the cubemap.</param>
    /// <param name="negativeZ">The image for the negative Z face of the cubemap.</param>
    /// <param name="mipmap">Specifies whether to generate mipmaps.</param>
    /// <param name="srgb">Specifies whether the images should be loaded as sRGB.</param>
    public Cubemap(GraphicsDevice graphicsDevice, Image positiveX, Image negativeX, Image positiveY, Image negativeY, Image positiveZ, Image negativeZ, bool mipmap = true, bool srgb = false) {
        this.Images = new Image[6][];
        
        if (mipmap) {
            this.Images[0] = MipmapHelper.GenerateMipmaps(positiveX);
            this.Images[1] = MipmapHelper.GenerateMipmaps(negativeX);
            this.Images[2] = MipmapHelper.GenerateMipmaps(positiveY);
            this.Images[3] = MipmapHelper.GenerateMipmaps(negativeY);
            this.Images[4] = MipmapHelper.GenerateMipmaps(positiveZ);
            this.Images[5] = MipmapHelper.GenerateMipmaps(negativeZ);
        }
        else {
            this.Images[0] = [positiveX];
            this.Images[1] = [negativeX];
            this.Images[2] = [positiveY];
            this.Images[3] = [negativeY];
            this.Images[4] = [positiveZ];
            this.Images[5] = [negativeZ];
        }
        
        this.Format = srgb ? PixelFormat.R8G8B8A8UNormSRgb : PixelFormat.R8G8B8A8UNorm;
        this.CreateDeviceTexture(graphicsDevice);
    }

    /// <summary>
    /// Creates a device texture for the Cubemap.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device.</param>
    private void CreateDeviceTexture(GraphicsDevice graphicsDevice) {
        this.DeviceTexture = graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(this.Width, this.Height, this.MipLevels, 1, this.Format, TextureUsage.Sampled | TextureUsage.Cubemap));
        
        for (int level = 0; level < this.MipLevels; level++) {
            Image image = this.Images[0][level];
            uint width = (uint) image.Width;
            uint height = (uint) image.Height;
                
            graphicsDevice.UpdateTexture(this.DeviceTexture, this.Images[PositiveXArrayLayer][level].Data, 0, 0, 0, width, height, 1, (uint) level, PositiveXArrayLayer);
            graphicsDevice.UpdateTexture(this.DeviceTexture, this.Images[NegativeXArrayLayer][level].Data, 0, 0, 0, width, height, 1, (uint) level, NegativeXArrayLayer);
            graphicsDevice.UpdateTexture(this.DeviceTexture, this.Images[PositiveYArrayLayer][level].Data, 0, 0, 0, width, height, 1, (uint) level, PositiveYArrayLayer);
            graphicsDevice.UpdateTexture(this.DeviceTexture, this.Images[NegativeYArrayLayer][level].Data, 0, 0, 0, width, height, 1, (uint) level, NegativeYArrayLayer);
            graphicsDevice.UpdateTexture(this.DeviceTexture, this.Images[PositiveZArrayLayer][level].Data, 0, 0, 0, width, height, 1, (uint) level, PositiveZArrayLayer);
            graphicsDevice.UpdateTexture(this.DeviceTexture, this.Images[NegativeZArrayLayer][level].Data, 0, 0, 0, width, height, 1, (uint) level, NegativeZArrayLayer);            
        }
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.DeviceTexture.Dispose();
        }
    }
}