using Bliss.CSharp.Colors;
using Bliss.CSharp.Logging;
using StbImageSharp;
using StbImageWriteSharp;
using ColorComponents = StbImageSharp.ColorComponents;

namespace Bliss.CSharp.Images;

public class Image : ICloneable {
    
    /// <summary>
    /// Gets the width of the image in pixels.
    /// </summary>
    public int Width { get; private set; }
    
    /// <summary>
    /// Gets the height of the image in pixels.
    /// </summary>
    public int Height { get; private set; }
    
    /// <summary>
    /// Gets the raw pixel data of the image in RGBA format.
    /// </summary>
    public byte[] Data { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Image"/> class by loading it from a file path.
    /// </summary>
    /// <param name="path">The path to the image file.</param>
    public Image(string path) {
        if (!File.Exists(path)) {
            Logger.Fatal($"Failed to find path [{path}]!");
        }

        ImageResult result = ImageResult.FromMemory(File.ReadAllBytes(path), ColorComponents.RedGreenBlueAlpha);
        this.Width = result.Width;
        this.Height = result.Height;
        this.Data = result.Data;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Image"/> class by loading it from a stream.
    /// </summary>
    /// <param name="stream">The stream containing image data.</param>
    public Image(Stream stream) {
        if (!stream.CanRead) {
            Logger.Fatal($"Failed to read stream [{stream}]!");
        }
        
        ImageResult result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        this.Width = result.Width;
        this.Height = result.Height;
        this.Data = result.Data;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Image"/> class with a solid color.
    /// </summary>
    /// <param name="width">The width of the image in pixels.</param>
    /// <param name="height">The height of the image in pixels.</param>
    /// <param name="color">The color to fill the image with.</param>
    public Image(int width, int height, Color color) {
        this.Width = width;
        this.Height = height;
        this.Data = new byte[width * height * 4];
        
        for (int i = 0; i < this.Data.Length; i += 4) {
            this.Data[i] = color.R;
            this.Data[i + 1] = color.G;
            this.Data[i + 2] = color.B;
            this.Data[i + 3] = color.A;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Image"/> class from raw byte data.
    /// </summary>
    /// <param name="data">The raw image data in RGBA format.</param>
    public Image(byte[] data) {
        ImageResult result = ImageResult.FromMemory(data, ColorComponents.RedGreenBlueAlpha);
        this.Width = result.Width;
        this.Height = result.Height;
        this.Data = result.Data;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Image"/> class with the specified dimensions and optional data.
    /// </summary>
    /// <param name="width">The width of the image in pixels.</param>
    /// <param name="height">The height of the image in pixels.</param>
    /// <param name="data">The optional raw data to initialize the image with.</param>
    public Image(int width, int height, byte[]? data = null) {
        this.Width = width;
        this.Height = height;
        this.Data = data ?? new byte[width * height * 4];
    }

    /// <summary>
    /// Saves the image as a BMP file.
    /// </summary>
    /// <param name="path">The path where the BMP file should be saved.</param>
    public void SaveAsBmp(string path) {
        using (Stream stream = File.OpenWrite(path)) {
            ImageWriter writer = new ImageWriter();
            writer.WriteBmp(this.Data, this.Width, this.Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, stream);
        }
    }
    
    /// <summary>
    /// Saves the image as a TGA file.
    /// </summary>
    /// <param name="path">The path where the TGA file should be saved.</param>
    public void SaveAsTga(string path) {
        using (Stream stream = File.OpenWrite(path)) {
            ImageWriter writer = new ImageWriter();
            writer.WriteTga(this.Data, this.Width, this.Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, stream);
        }
    }
    
    /// <summary>
    /// Saves the image as an HDR file.
    /// </summary>
    /// <param name="path">The path where the HDR file should be saved.</param>
    public void SaveAsHdr(string path) {
        using (Stream stream = File.OpenWrite(path)) {
            ImageWriter writer = new ImageWriter();
            writer.WriteHdr(this.Data, this.Width, this.Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, stream);
        }
    }
    
    /// <summary>
    /// Saves the image as a PNG file.
    /// </summary>
    /// <param name="path">The path where the PNG file should be saved.</param>
    public void SaveAsPng(string path) {
        using (Stream stream = File.OpenWrite(path)) {
            ImageWriter writer = new ImageWriter();
            writer.WritePng(this.Data, this.Width, this.Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, stream);
        }
    }
    
    /// <summary>
    /// Saves the image as a JPG file with the specified quality.
    /// </summary>
    /// <param name="path">The path where the JPG file should be saved.</param>
    /// <param name="quality">The quality of the JPG image (1-100).</param>
    public void SaveAsJpg(string path, int quality) {
        using (Stream stream = File.OpenWrite(path)) {
            ImageWriter writer = new ImageWriter();
            writer.WriteJpg(this.Data, this.Width, this.Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, stream, quality);
        }
    }
    
    /// <summary>
    /// Creates a deep copy of this image.
    /// </summary>
    /// <returns>A new <see cref="Image"/> instance with identical data.</returns>
    public object Clone() {
        return new Image(this.Width, this.Height, this.Data);
    }
}