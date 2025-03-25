using Bliss.CSharp.Images;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Transformations;
using Veldrid;

namespace Bliss.CSharp.Textures.Cubemaps;

public class Cubemap : Disposable {
    
    /// <summary>
    /// Gets the graphics device associated with the cubemap instance.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }

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

    /// <summary>
    /// Gets the texture view associated with the cubemap, allowing shaders to access the cubemap texture data.
    /// </summary>
    public TextureView TextureView { get; private set; }

    /// <summary>
    /// Caches resource sets for combinations of sampler objects and texture layouts.
    /// </summary>
    private Dictionary<(Sampler, ResourceLayout), ResourceSet> _cachedResourceSets;

    /// <summary>
    /// Initializes a new instance of the <see cref="Cubemap"/> class by loading cubemap faces from a file path.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for creating resources.</param>
    /// <param name="path">The file path to the image containing cubemap faces.</param>
    /// <param name="layout">The layout of the cubemap faces within the image.</param>
    /// <param name="mipmap">Specifies whether to generate mipmaps for the cubemap.</param>
    /// <param name="srgb">Specifies whether to load the image as sRGB.</param>
    public Cubemap(GraphicsDevice graphicsDevice, string path, CubemapLayout layout = CubemapLayout.AutoDetect, bool mipmap = true, bool srgb = false) : this(graphicsDevice, new Image(path), layout, mipmap, srgb) {
        Logger.Info($"Loading cubemap from path: [{path}]");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Cubemap"/> class by loading cubemap faces from a stream.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for creating resources.</param>
    /// <param name="stream">The stream containing the image data for the cubemap faces.</param>
    /// <param name="layout">The layout of the cubemap faces within the image.</param>
    /// <param name="mipmap">Specifies whether to generate mipmaps for the cubemap.</param>
    /// <param name="srgb">Specifies whether to load the image as sRGB.</param>
    public Cubemap(GraphicsDevice graphicsDevice, Stream stream, CubemapLayout layout = CubemapLayout.AutoDetect, bool mipmap = true, bool srgb = false) : this(graphicsDevice, new Image(stream), layout, mipmap, srgb) {
        Logger.Info($"Loading cubemap from stream: [{stream}]");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Cubemap"/> class by splitting a single image into cubemap faces.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for creating resources.</param>
    /// <param name="image">The source image containing all cubemap faces in the specified layout.</param>
    /// <param name="layout">The layout format used to interpret the cubemap faces in the source image.</param>
    /// <param name="mipmap">Specifies whether to generate mipmaps for each cubemap face.</param>
    /// <param name="srgb">Specifies whether the images should be loaded as sRGB format.</param>
    public Cubemap(GraphicsDevice graphicsDevice, Image image, CubemapLayout layout = CubemapLayout.AutoDetect, bool mipmap = true, bool srgb = false) {
        this.GraphicsDevice = graphicsDevice;
        this.Images = new Image[6][];

        Image[] cubemapFaces = CubemapHelper.GenCubemapImages(image, layout);
    
        for (int i = 0; i < 6; i++) {
            this.Images[i] = mipmap ? MipmapHelper.GenerateMipmaps(cubemapFaces[i]) : [cubemapFaces[i]];
        }
    
        this.Format = srgb ? PixelFormat.R8G8B8A8UNormSRgb : PixelFormat.R8G8B8A8UNorm;
        this.CreateDeviceTexture();
        this._cachedResourceSets = new Dictionary<(Sampler, ResourceLayout), ResourceSet>();
    }

    /// <summary>
    /// Retrieves a resource set from the cache or creates a new one using a specified sampler and texture layout.
    /// </summary>
    /// <param name="sampler">The sampler object to be used for the resource set.</param>
    /// <param name="layout">The texture layout to be used for the resource set.</param>
    /// <returns>A <see cref="ResourceSet"/> object associated with the provided sampler and layout.</returns>
    public ResourceSet GetResourceSet(Sampler sampler, ResourceLayout layout) {
        if (!this._cachedResourceSets.TryGetValue((sampler, layout), out ResourceSet? resourceSet)) {
            ResourceSet newResourceSet = this.GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(layout, this.TextureView, sampler));

            this._cachedResourceSets.Add((sampler, layout), newResourceSet);
            return newResourceSet;
        }

        return resourceSet;
    }

    /// <summary>
    /// Retrieves the raw byte data for a specific array layer of the cubemap.
    /// </summary>
    /// <param name="cubemapLayer">The array layer of the cubemap to retrieve data from.</param>
    /// <returns>An array of bytes containing the pixel data for the specified array layer.</returns>
    public byte[] GetDataFromBytes(CubemapLayer cubemapLayer) {
        return this.Images[(int) cubemapLayer][0].Data;
    }

    /// <summary>
    /// Retrieves the image data associated with the specified array layer of the cubemap.
    /// </summary>
    /// <param name="cubemapLayer">The specific array layer whose image data is to be retrieved.</param>
    /// <returns>An <see cref="Image"/> object containing the image data for the specified array layer.</returns>
    public Image GetDataFromImage(CubemapLayer cubemapLayer) {
        return this.Images[(int) cubemapLayer][0];
    }

    /// <summary>
    /// Updates the cubemap's data for a specified array layer and a rectangular area.
    /// </summary>
    /// <param name="data">The raw byte array containing the pixel data to be set.</param>
    /// <param name="cubemapLayer">The specific cubemap array layer to update.</param>
    /// <param name="area">An optional rectangle specifying the area to update. If null, the whole layer will be updated.</param>
    public void SetData(byte[] data, CubemapLayer cubemapLayer, Rectangle? area = null) {
        Rectangle rect = area ?? new Rectangle(0, 0, (int) this.Width, (int) this.Height);
        Image originalImage = this.GetDataFromImage(cubemapLayer);

        int rowLengthInBytes = rect.Width * (int) this.PixelSizeInBytes;
        for (int y = 0; y < rect.Height; y++) {
            int sourceOffset = y * rowLengthInBytes;
            int destinationOffset = ((rect.Y + y) * originalImage.Width + rect.X) * (int) this.PixelSizeInBytes;

            Array.Copy(data, sourceOffset, originalImage.Data, destinationOffset, rowLengthInBytes);
        }

        this.SetData(originalImage, cubemapLayer);
    }

    /// <summary>
    /// Updates the data of a specified layer in the cubemap with the provided image data.
    /// </summary>
    /// <param name="data">The image data to set, which must match the dimensions of the texture.</param>
    /// <param name="cubemapLayer">The layer of the cubemap to update.</param>
    /// <exception cref="ArgumentException">
    /// Throws if the dimensions of the provided image data do not match the dimensions of the cubemap.
    /// </exception>
    public void SetData(Image data, CubemapLayer cubemapLayer) {
        if (data.Width != this.Width || data.Height != this.Height) {
            throw new ArgumentException("Image size do not match texture size!");
        }
        
        this.Images[(int) cubemapLayer] = this.MipLevels > 1 ? MipmapHelper.GenerateMipmaps(data) : [data];
        
        for (int i = 0; i < this.MipLevels; i++) {
            Image image = this.Images[(int) cubemapLayer][i];
            this.GraphicsDevice.UpdateTexture(this.DeviceTexture, image.Data, 0, 0, 0, (uint) image.Width, (uint) image.Height, 1, (uint) i, 0);
        }
    }

    /// <summary>
    /// Creates a device texture for the Cubemap.
    /// </summary>
    private void CreateDeviceTexture() {
        this.DeviceTexture = this.GraphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(this.Width, this.Height, this.MipLevels, 1, this.Format, TextureUsage.Sampled | TextureUsage.Cubemap));
        this.TextureView = this.GraphicsDevice.ResourceFactory.CreateTextureView(new TextureViewDescription(this.DeviceTexture));
        
        for (int level = 0; level < this.MipLevels; level++) {
            Image image = this.Images[0][level];
            uint width = (uint) image.Width;
            uint height = (uint) image.Height;
            
            this.GraphicsDevice.UpdateTexture(this.DeviceTexture, this.Images[(int) CubemapLayer.PositiveX][level].Data, 0, 0, 0, width, height, 1, (uint) level, (int) CubemapLayer.PositiveX);
            this.GraphicsDevice.UpdateTexture(this.DeviceTexture, this.Images[(int) CubemapLayer.NegativeX][level].Data, 0, 0, 0, width, height, 1, (uint) level, (int) CubemapLayer.NegativeX);
            this.GraphicsDevice.UpdateTexture(this.DeviceTexture, this.Images[(int) CubemapLayer.PositiveY][level].Data, 0, 0, 0, width, height, 1, (uint) level, (int) CubemapLayer.PositiveY);
            this.GraphicsDevice.UpdateTexture(this.DeviceTexture, this.Images[(int) CubemapLayer.NegativeY][level].Data, 0, 0, 0, width, height, 1, (uint) level, (int) CubemapLayer.NegativeY);
            this.GraphicsDevice.UpdateTexture(this.DeviceTexture, this.Images[(int) CubemapLayer.PositiveZ][level].Data, 0, 0, 0, width, height, 1, (uint) level, (int) CubemapLayer.PositiveZ);
            this.GraphicsDevice.UpdateTexture(this.DeviceTexture, this.Images[(int) CubemapLayer.NegativeZ][level].Data, 0, 0, 0, width, height, 1, (uint) level, (int) CubemapLayer.NegativeZ);            
        }
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.DeviceTexture.Dispose();
        }
    }
}