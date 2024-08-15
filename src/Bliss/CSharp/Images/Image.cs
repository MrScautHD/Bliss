using Silk.NET.Maths;
using Silk.NET.Vulkan;
using StbImageSharp;

namespace Bliss.CSharp.Images;

public class Image {

    public readonly Vector2D<int> Size;
    
    public readonly Format Format;
    
    public readonly byte[] Data;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Image"/> class with the specified size, format, and data.
    /// </summary>
    /// <param name="size">The dimensions of the image.</param>
    /// <param name="format">The format of the image.</param>
    /// <param name="data">The raw pixel data of the image.</param>
    public Image(Vector2D<int> size, Format format, byte[] data) {
        this.Size = size;
        this.Format = format;
        this.Data = data;
    }

    /// <summary>
    /// Loads an image from the specified file path.
    /// </summary>
    /// <param name="path">The path to the image file.</param>
    /// <returns>An instance of the Image class representing the loaded image.</returns>
    public static Image Load(string path) {
        ImageResult result = ImageResult.FromMemory(File.ReadAllBytes(path), ColorComponents.RedGreenBlueAlpha);

        if (result == null) {
            throw new Exception($"Failed to load image from path: {path}");
        }
        
        return new Image(new Vector2D<int>(result.Width, result.Height), Format.R8G8B8A8Srgb, result.Data);
    }

    /// <summary>
    /// Loads an image from the specified raw pixel data.
    /// </summary>
    /// <param name="data">The raw pixel data of the image.</param>
    /// <returns>An instance of the Image class representing the loaded image.</returns>
    public static Image Load(byte[] data) {
        ImageResult result = ImageResult.FromMemory(data, ColorComponents.RedGreenBlueAlpha);

        if (result == null) {
            throw new Exception($"Failed to load image from data: {data}");
        }
        
        return new Image(new Vector2D<int>(result.Width, result.Height), Format.R8G8B8A8Srgb, result.Data);
    }
}