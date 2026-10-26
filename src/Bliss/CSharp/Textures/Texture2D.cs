using System.Runtime.InteropServices;
using Bliss.CSharp.Graphics.Pipelines.Textures;
using Bliss.CSharp.Images;
using Bliss.CSharp.Logging;
using Veldrith;
using Rectangle = Bliss.CSharp.Transformations.Rectangle;

namespace Bliss.CSharp.Textures;

public class Texture2D : Disposable {
    
    /// <summary>
    /// Gets the graphics device associated with this texture.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// Gets the width of the texture in pixels.
    /// </summary>
    public uint Width { get; private set; }
    
    /// <summary>
    /// Gets the height of the texture in pixels.
    /// </summary>
    public uint Height { get; private set; }
    
    /// <summary>
    /// Gets the pixel format of the texture.
    /// </summary>
    public PixelFormat Format { get; private set; }
    
    /// <summary>
    /// Gets the size of a pixel in bytes.
    /// </summary>
    public uint PixelSizeInBytes => sizeof(byte) * 4;
    
    /// <summary>
    /// Gets the total number of mipmap levels for this texture.
    /// </summary>
    public uint MipLevels { get; private set; }
    
    /// <summary>
    /// Gets the device texture created from the images.
    /// </summary>
    public Texture DeviceTexture { get; private set; }
    
    /// <summary>
    /// A dictionary that caches resource sets associated with samplers, used to avoid redundant resource set creation.
    /// </summary>
    private Dictionary<(Sampler, SimpleTextureLayout), ResourceSet> _cachedResourceSets;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Texture2D"/> class using an image file path.
    /// Loads the texture from the specified path and applies the given mipmap and sRGB settings.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering the texture.</param>
    /// <param name="path">The file path of the image to load as a texture.</param>
    /// <param name="mipmap">Indicates whether to generate mipmaps for the texture.</param>
    /// <param name="srgb">Indicates whether to use sRGB color space for the texture.</param>
    public Texture2D(GraphicsDevice graphicsDevice, string path, bool mipmap = true, bool srgb = false) : this(graphicsDevice, new Image(path), mipmap, srgb) {
        Logger.Info($"Texture loaded successfully from path: [{path}]");
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Texture2D"/> class using an image stream.
    /// Loads the texture from the specified stream and applies the given mipmap and sRGB settings.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering the texture.</param>
    /// <param name="stream">The stream containing the image data to load as a texture.</param>
    /// <param name="mipmap">Indicates whether to generate mipmaps for the texture.</param>
    /// <param name="srgb">Indicates whether to use sRGB color space for the texture.</param>
    public Texture2D(GraphicsDevice graphicsDevice, Stream stream, bool mipmap = true, bool srgb = false) : this(graphicsDevice, new Image(stream), mipmap, srgb) {
        Logger.Info("Texture loaded successfully from stream.");
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Texture2D"/> class using a byte array.
    /// Loads the texture from the specified encoded image data (e.g., PNG or JPG) and applies the given mipmap and sRGB settings.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering the texture.</param>
    /// <param name="data">The byte array containing the encoded image data to load as a texture.</param>
    /// <param name="mipmap">Indicates whether to generate mipmaps for the texture.</param>
    /// <param name="srgb">Indicates whether to use sRGB color space for the texture.</param>
    public Texture2D(GraphicsDevice graphicsDevice, byte[] data, bool mipmap = true, bool srgb = false) : this(graphicsDevice, new Image(data), mipmap, srgb) {
        Logger.Info($"Texture loaded successfully from byte array: [{data.Length} bytes]");
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Texture2D"/> class using an <see cref="Image"/> object.
    /// Creates the device texture from the image data and sets up its dimensions, mip levels and pixel format.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create the texture and its GPU resources.</param>
    /// <param name="image">The image that provides the pixel data and dimensions of the texture.</param>
    /// <param name="mipmap">Indicates whether to generate a full mipmap chain for the texture. If <c>false</c>, only a single mip level is used.</param>
    /// <param name="srgb">Indicates whether to use the sRGB color space (<c>R8G8B8A8UNormSRgb</c>) instead of linear (<c>R8G8B8A8UNorm</c>).</param>
    public Texture2D(GraphicsDevice graphicsDevice, Image image, bool mipmap = true, bool srgb = false) {
        this.GraphicsDevice = graphicsDevice;
        this.Width = (uint) image.Width;
        this.Height = (uint) image.Height;
        this.MipLevels = mipmap ? (uint) Math.Floor(Math.Log2(Math.Max(this.Width, this.Height))) + 1 : 1;
        this.Format = srgb ? PixelFormat.R8G8B8A8UNormSRgb : PixelFormat.R8G8B8A8UNorm;
        this.DeviceTexture = this.CreateDeviceTexture(image);
        this._cachedResourceSets = new Dictionary<(Sampler, SimpleTextureLayout), ResourceSet>();
    }
    
    /// <summary>
    /// Gets a resource set associated with the specified sampler and resource layout.
    /// If the resource set is already cached, it returns the cached resource set; otherwise, it creates and caches a new one.
    /// </summary>
    /// <param name="sampler">The sampler used for the resource set.</param>
    /// <param name="layout">The resource layout used for the resource set.</param>
    /// <returns>The resource set associated with the specified sampler and resource layout.</returns>
    public ResourceSet GetResourceSet(Sampler sampler, SimpleTextureLayout layout) {
        if (!this._cachedResourceSets.TryGetValue((sampler, layout), out ResourceSet? resourceSet)) {
            ResourceSet newResourceSet = this.GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(layout.Layout, this.DeviceTexture, sampler));
            
            this._cachedResourceSets.Add((sampler, layout), newResourceSet);
            return newResourceSet;
        }
        
        return resourceSet;
    }
    
    /// <summary>
    /// Reads the base mip level of the texture back from the GPU.
    /// The data is copied into a staging texture first and then read row by row, since the mapped row pitch can be larger than the actual row size.
    /// </summary>
    /// <returns>A byte array containing the pixel data in RGBA order, tightly packed without row padding.</returns>
    public byte[] GetDataFromBytes() {
        using Texture staging = this.GraphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(this.Width, this.Height, 1, 1, this.Format, TextureUsage.Staging));
        using CommandList commandList = this.GraphicsDevice.ResourceFactory.CreateCommandList();
        
        commandList.Begin();
        commandList.CopyTexture(this.DeviceTexture, 0, 0, 0, 0, 0, staging, 0, 0, 0, 0, 0, this.Width, this.Height, 1, 1);
        commandList.End();
        
        this.GraphicsDevice.SubmitCommands(commandList);
        this.GraphicsDevice.WaitForIdle();
        
        byte[] result = new byte[this.Width * this.Height * this.PixelSizeInBytes];
        MappedResource map = this.GraphicsDevice.Map(staging, MapMode.Read, 0);
        
        try {
            uint rowSize = this.Width * this.PixelSizeInBytes;
            
            for (uint y = 0; y < this.Height; y++) {
                nint src = map.Data + (int) (y * map.RowPitch);
                Marshal.Copy(src, result, (int) (y * rowSize), (int) rowSize);
            }
        }
        finally {
            this.GraphicsDevice.Unmap(staging, 0);
        }
        
        return result;
    }
    
    /// <summary>
    /// Reads the base mip level of the texture back from the GPU and wraps it in an <see cref="Image"/>.
    /// </summary>
    /// <returns>A new <see cref="Image"/> containing the current pixel data of the texture.</returns>
    public Image GetDataFromImage() {
        return new Image((int) this.Width, (int) this.Height, this.GetDataFromBytes());
    }
    
    /// <summary>
    /// Uploads raw pixel data to the texture, either to the whole texture or to a specific area.
    /// Mipmaps are regenerated afterwards if the texture has more than one mip level.
    /// </summary>
    /// <param name="data">The pixel data in RGBA order. Its length must match the size of the target area.</param>
    /// <param name="area">The region of the texture to update. If null, the entire texture is updated.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the specified area exceeds the texture bounds.</exception>
    /// <exception cref="ArgumentException">Thrown if the length of <paramref name="data"/> does not match the size of the target area.</exception>
    public void SetData(byte[] data, Rectangle? area = null) {
        uint x = 0;
        uint y = 0;
        uint width = this.Width;
        uint height = this.Height;
        
        if (area.HasValue) {
            Rectangle rectangle = area.Value;
            
            if (rectangle.X < 0 || rectangle.Y < 0 || rectangle.X + rectangle.Width > this.Width || rectangle.Y + rectangle.Height > this.Height) {
                throw new ArgumentOutOfRangeException(nameof(area), "Area exceeds texture bounds.");
            }
            
            x = (uint) rectangle.X;
            y = (uint) rectangle.Y;
            width = (uint) rectangle.Width;
            height = (uint) rectangle.Height;
        }
        
        uint requiredSize = width * height * this.PixelSizeInBytes;
        
        if (data.Length < requiredSize) {
            throw new ArgumentException($"Expected at least {requiredSize} bytes, got {data.Length}.", nameof(data));
        }
        
        this.GraphicsDevice.UpdateTexture(this.DeviceTexture, data, x, y, 0, width, height, 1, 0, 0);
        this.GenerateMipmaps();
    }
    
    /// <summary>
    /// Replaces the entire texture content with the pixel data of the given image.
    /// The image must have the same dimensions as the texture.
    /// </summary>
    /// <param name="data">The image whose pixel data is uploaded to the texture.</param>
    /// <exception cref="ArgumentException">Thrown if the image size does not match the texture size.</exception>
    public void SetData(Image data) {
        if (data.Width != this.Width || data.Height != this.Height) {
            throw new ArgumentException("Image size must match the texture size.", nameof(data));
        }
        
        this.SetData(data.Data);
    }
    
    /// <summary>
    /// Creates the device texture and uploads the pixel data of the given image to it.
    /// If the texture has multiple mip levels, the mipmaps are generated after the upload.
    /// </summary>
    /// <param name="image">The image whose pixel data is uploaded to the texture.</param>
    /// <returns>The newly created device texture.</returns>
    private Texture CreateDeviceTexture(Image image) {
        TextureUsage textureUsage = TextureUsage.Sampled;
        
        if (this.MipLevels > 1) {
            textureUsage |= TextureUsage.GenerateMipmaps;
        }
        
        Texture texture = this.GraphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(this.Width, this.Height, this.MipLevels, 1, this.Format, textureUsage));
        this.GraphicsDevice.UpdateTexture(texture, image.Data, 0, 0, 0, (uint) image.Width, (uint) image.Height, 1, 0, 0);
        
        this.GenerateMipmaps(texture);
        return texture;
    }
    
    /// <summary>
    /// Generates the mipmap chain for the given texture, or for <see cref="DeviceTexture"/> if none is specified.
    /// </summary>
    /// <param name="texture">The texture to generate mipmaps for. If null, <see cref="DeviceTexture"/> is used.</param>
    private void GenerateMipmaps(Texture? texture = null) {
        if (this.MipLevels <= 1) {
            return;
        }
        
        using CommandList commandList = this.GraphicsDevice.ResourceFactory.CreateCommandList();
        
        commandList.Begin();
        commandList.GenerateMipmaps(texture ?? this.DeviceTexture);
        commandList.End();
        
        this.GraphicsDevice.SubmitCommands(commandList);
        this.GraphicsDevice.WaitForIdle();
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