using System.Runtime.InteropServices;
using Bliss.CSharp.Graphics.Pipelines.Textures;
using Bliss.CSharp.Images;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Transformations;
using Veldrith;

namespace Bliss.CSharp.Textures.Cubemaps;

public class Cubemap : Disposable {
    
    /// <summary>
    /// Gets the graphics device associated with the cubemap instance.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// Gets the width of the cubemap.
    /// </summary>
    public uint Width { get; private set; }
    
    /// <summary>
    /// Gets the height of the cubemap.
    /// </summary>
    public uint Height { get; private set; }
    
    /// <summary>
    /// Gets the pixel format of the cubemap.
    /// </summary>
    public PixelFormat Format { get; private set; }
    
    /// <summary>
    /// Gets the size of a pixel in bytes.
    /// </summary>
    public uint PixelSizeInBytes => sizeof(byte) * 4;
    
    /// <summary>
    /// Gets the total number of mipmap levels for this cubemap.
    /// </summary>
    public uint MipLevels { get; private set; }
    
    /// <summary>
    /// Gets the device texture.
    /// </summary>
    public Texture DeviceTexture { get; private set; }
    
    /// <summary>
    /// Caches resource sets for combinations of sampler objects and texture layouts.
    /// </summary>
    private Dictionary<(Sampler, SimpleTextureLayout), ResourceSet> _cachedResourceSets;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Cubemap"/> class by loading cubemap faces from a file path.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for creating resources.</param>
    /// <param name="path">The file path to the image containing cubemap faces.</param>
    /// <param name="layout">The layout of the cubemap faces within the image.</param>
    /// <param name="mipmap">Specifies whether to generate mipmaps for the cubemap.</param>
    /// <param name="srgb">Specifies whether to load the image as sRGB.</param>
    public Cubemap(GraphicsDevice graphicsDevice, string path, CubemapLayout layout = CubemapLayout.AutoDetect, bool mipmap = true, bool srgb = false) : this(graphicsDevice, new Image(path), layout, mipmap, srgb) {
        Logger.Info($"Cubemap loaded successfully from path: [{path}]");
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
        Logger.Info("Cubemap loaded successfully from stream.");
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Cubemap"/> class by loading cubemap faces from a byte array.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for creating resources.</param>
    /// <param name="data">The byte array containing the encoded image data (e.g., PNG or JPG) for the cubemap faces.</param>
    /// <param name="layout">The layout of the cubemap faces within the image.</param>
    /// <param name="mipmap">Specifies whether to generate mipmaps for the cubemap.</param>
    /// <param name="srgb">Specifies whether to load the image as sRGB.</param>
    public Cubemap(GraphicsDevice graphicsDevice, byte[] data, CubemapLayout layout = CubemapLayout.AutoDetect, bool mipmap = true, bool srgb = false) : this(graphicsDevice, new Image(data), layout, mipmap, srgb) {
        Logger.Info($"Cubemap loaded successfully from byte array: [{data.Length} bytes]");
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Cubemap"/> class by splitting a single image into cubemap faces.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for creating resources.</param>
    /// <param name="image">The source image containing all cubemap faces in the specified layout.</param>
    /// <param name="layout">The layout format used to interpret the cubemap faces in the source image.</param>
    /// <param name="mipmap">Specifies whether to generate mipmaps for each cubemap face.</param>
    /// <param name="srgb">Specifies whether the images should be loaded as sRGB format.</param>
    public Cubemap(GraphicsDevice graphicsDevice, Image image, CubemapLayout layout = CubemapLayout.AutoDetect, bool mipmap = true, bool srgb = false) : this(graphicsDevice, CubemapHelper.GenCubemapImages(image, layout), mipmap, srgb) { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Cubemap"/> class from six already loaded face images.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for creating resources.</param>
    /// <param name="faces">The six face images, ordered like <see cref="CubemapLayer"/> (+X, -X, +Y, -Y, +Z, -Z). All must be square and equally sized.</param>
    /// <param name="mipmap">Specifies whether to generate mipmaps for the cubemap.</param>
    /// <param name="srgb">Specifies whether to use the sRGB color space.</param>
    public Cubemap(GraphicsDevice graphicsDevice, Image[] faces, bool mipmap = true, bool srgb = false) {
        if (faces.Length != 6) {
            throw new ArgumentException("A cubemap requires exactly 6 images.", nameof(faces));
        }
        
        int size = faces[0].Width;
        
        foreach (Image face in faces) {
            if (face.Width != size || face.Height != size) {
                throw new ArgumentException("All cubemap faces must be square and have the same size.", nameof(faces));
            }
        }
        
        this.GraphicsDevice = graphicsDevice;
        this.Width = (uint) faces[0].Width;
        this.Height = (uint) faces[0].Height;
        this.MipLevels = mipmap ? (uint) Math.Floor(Math.Log2(Math.Max(this.Width, this.Height))) + 1 : 1;
        this.Format = srgb ? PixelFormat.R8G8B8A8UNormSRgb : PixelFormat.R8G8B8A8UNorm;
        this.DeviceTexture = this.CreateDeviceTexture(faces);
        this._cachedResourceSets = new Dictionary<(Sampler, SimpleTextureLayout), ResourceSet>();
    }
    
    /// <summary>
    /// Retrieves a resource set from the cache or creates a new one using a specified sampler and texture layout.
    /// </summary>
    /// <param name="sampler">The sampler object to be used for the resource set.</param>
    /// <param name="layout">The texture layout to be used for the resource set.</param>
    /// <returns>A <see cref="ResourceSet"/> object associated with the provided sampler and layout.</returns>
    public ResourceSet GetResourceSet(Sampler sampler, SimpleTextureLayout layout) {
        if (!this._cachedResourceSets.TryGetValue((sampler, layout), out ResourceSet? resourceSet)) {
            ResourceSet newResourceSet = this.GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(layout.Layout, this.DeviceTexture, sampler));
            
            this._cachedResourceSets.Add((sampler, layout), newResourceSet);
            return newResourceSet;
        }
        
        return resourceSet;
    }
    
    /// <summary>
    /// Reads a single face of the cubemap (base mip level) back from the GPU.
    /// </summary>
    /// <param name="cubemapLayer">The face to read.</param>
    /// <returns>The pixel data in RGBA order, tightly packed without row padding.</returns>
    public byte[] GetDataFromBytes(CubemapLayer cubemapLayer) {
        using Texture staging = this.GraphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(this.Width, this.Height, 1, 1, this.Format, TextureUsage.Staging));
        using CommandList commandList = this.GraphicsDevice.ResourceFactory.CreateCommandList();
        
        commandList.Begin();
        commandList.CopyTexture(this.DeviceTexture, 0, 0, 0, 0, (uint) cubemapLayer, staging, 0, 0, 0, 0, 0, this.Width, this.Height, 1, 1);
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
    /// Reads a single face of the cubemap back from the GPU and wraps it in an <see cref="Image"/>.
    /// </summary>
    /// <param name="cubemapLayer">The face to read.</param>
    /// <returns>A new <see cref="Image"/> with the current pixel data of the face.</returns>
    public Image GetDataFromImage(CubemapLayer cubemapLayer) {
        return new Image((int) this.Width, (int) this.Height, this.GetDataFromBytes(cubemapLayer));
    }
    
    /// <summary>
    /// Reads all six faces back from the GPU.
    /// </summary>
    /// <returns>An array of six images, indexed by <see cref="CubemapLayer"/>.</returns>
    public Image[] GetDataFromImages() {
        Image[] faces = new Image[6];
        
        foreach (CubemapLayer layer in Enum.GetValues<CubemapLayer>()) {
            faces[(int) layer] = this.GetDataFromImage(layer);
        }
        
        return faces;
    }
    
    /// <summary>
    /// Uploads raw pixel data to a face of the cubemap, either to the whole face or to a specific area.
    /// Mipmaps are regenerated afterwards if the cubemap has more than one mip level.
    /// </summary>
    /// <param name="data">The pixel data in RGBA order. Its length must match the size of the target area.</param>
    /// <param name="cubemapLayer">The face to update.</param>
    /// <param name="area">The region of the face to update. If null, the entire face is updated.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the area exceeds the face bounds.</exception>
    /// <exception cref="ArgumentException">Thrown if the data length does not match the target area.</exception>
    public void SetData(byte[] data, CubemapLayer cubemapLayer, Rectangle? area = null) {
        uint x = 0;
        uint y = 0;
        uint width = this.Width;
        uint height = this.Height;
        
        if (area.HasValue) {
            Rectangle rectangle = area.Value;
            
            if (rectangle.X < 0 || rectangle.Y < 0 || rectangle.X + rectangle.Width > this.Width || rectangle.Y + rectangle.Height > this.Height) {
                throw new ArgumentOutOfRangeException(nameof(area), "Area exceeds cubemap face bounds.");
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
        
        this.GraphicsDevice.UpdateTexture(this.DeviceTexture, data, x, y, 0, width, height, 1, 0, (uint) cubemapLayer);
        this.GenerateMipmaps();
    }
    
    /// <summary>
    /// Replaces the content of a single face with the pixel data of the given image.
    /// </summary>
    /// <param name="data">The image to upload. It must match the face size.</param>
    /// <param name="cubemapLayer">The face to update.</param>
    /// <exception cref="ArgumentException">Thrown if the image size does not match the face size.</exception>
    public void SetData(Image data, CubemapLayer cubemapLayer) {
        if (data.Width != this.Width || data.Height != this.Height) {
            throw new ArgumentException("Image size must match the cubemap face size.", nameof(data));
        }
        
        this.SetData(data.Data, cubemapLayer);
    }
    
    /// <summary>
    /// Replaces all six faces at once and regenerates the mipmaps a single time.
    /// </summary>
    /// <param name="faces">The six face images, indexed by <see cref="CubemapLayer"/>. They must match the face size.</param>
    public void SetData(Image[] faces) {
        if (faces.Length != 6) {
            throw new ArgumentException("A cubemap requires exactly 6 images.", nameof(faces));
        }
        
        int size = faces[0].Width;
        
        foreach (Image face in faces) {
            if (face.Width != size || face.Height != size) {
                throw new ArgumentException("All cubemap faces must be square and have the same size.", nameof(faces));
            }
        }
        
        if (faces[0].Width != this.Width) {
            throw new ArgumentException("Face size must match the cubemap face size.", nameof(faces));
        }
        
        foreach (CubemapLayer layer in Enum.GetValues<CubemapLayer>()) {
            this.GraphicsDevice.UpdateTexture(this.DeviceTexture, faces[(int) layer].Data, 0, 0, 0, this.Width, this.Height, 1, 0, (uint) layer);
        }
        
        this.GenerateMipmaps();
    }
    
    /// <summary>
    /// Creates the device texture of the cubemap and uploads the pixel data of all six faces to it.
    /// The faces are validated first. If the cubemap has multiple mip levels, the mipmaps are generated after the upload.
    /// </summary>
    /// <param name="images">The six face images, indexed by <see cref="CubemapLayer"/>. All must be square and have the same size as the cubemap.</param>
    /// <returns>The newly created cubemap device texture.</returns>
    /// <exception cref="ArgumentException">Thrown if the array does not contain exactly six images, if the cubemap is not square, or if the faces differ in size.</exception>
    private Texture CreateDeviceTexture(Image[] images) {
        if (images.Length != 6) {
            throw new ArgumentException("A cubemap requires exactly 6 images.", nameof(images));
        }
        
        if (this.Width != this.Height) {
            throw new ArgumentException("Cubemap faces must be square.", nameof(images));
        }
        
        foreach (Image image in images) {
            if (image.Width != this.Width || image.Height != this.Height) {
                throw new ArgumentException("All cubemap faces must have the same size.", nameof(images));
            }
        }
        
        TextureUsage textureUsage = TextureUsage.Sampled| TextureUsage.Cubemap;
        
        if (this.MipLevels > 1) {
            textureUsage |= TextureUsage.GenerateMipmaps;
        }
        
        Texture texture = this.GraphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(this.Width, this.Height, this.MipLevels, 1, this.Format, textureUsage));
        
        foreach (CubemapLayer layer in Enum.GetValues<CubemapLayer>()) {
            this.GraphicsDevice.UpdateTexture(texture, images[(int) layer].Data, 0, 0, 0, this.Width, this.Height, 1, 0, (uint) layer);
        }
        
        this.GenerateMipmaps(texture);
        return texture;
    }
    
    /// <summary>
    /// Generates the mipmap chain for all faces of the given texture, or for <see cref="DeviceTexture"/> if none is specified.
    /// Does nothing if the cubemap has only one mip level. This blocks until the GPU has finished.
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