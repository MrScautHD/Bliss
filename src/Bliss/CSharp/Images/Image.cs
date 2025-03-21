using Bliss.CSharp.Logging;
using Bliss.CSharp.Transformations;
using StbImageSharp;
using StbImageWriteSharp;
using Color = Bliss.CSharp.Colors.Color;
using ColorComponents = StbImageSharp.ColorComponents;
using WriteColorComponents = StbImageWriteSharp.ColorComponents;

namespace Bliss.CSharp.Images;

public class Image : ICloneable {
    
    /// <summary>
    /// Stores the width of the image in pixels.
    /// </summary>
    private int _width;
    
    /// <summary>
    /// Stores the height of the image in pixels.
    /// </summary>
    private int _height;
    
    /// <summary>
    /// Stores the raw pixel data of the image. The data is in RGBA format, where each pixel is represented by 4 bytes.
    /// </summary>
    private byte[] _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="Image"/> class by loading an image from the specified file path.
    /// </summary>
    /// <param name="path">The file path of the image to load.</param>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    public Image(string path) {
        if (!File.Exists(path)) {
            Logger.Fatal($"Failed to find path! [{path}]");
        }
        
        ImageResult result = ImageResult.FromMemory(File.ReadAllBytes(path), ColorComponents.RedGreenBlueAlpha);
        this.Width = result.Width;
        this.Height = result.Height;
        this.Data = result.Data;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Image"/> class by loading an image from the specified stream.
    /// </summary>
    /// <param name="stream">The input stream containing image data to load.</param>
    /// <exception cref="ArgumentException">Thrown if the stream cannot be read.</exception>
    public Image(Stream stream) {
        if (!stream.CanRead) {
            Logger.Fatal($"Failed to read stream! [{stream}]");
        }
        
        ImageResult result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        this.Width = result.Width;
        this.Height = result.Height;
        this.Data = result.Data;
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
    /// Gets or sets the width of the image in pixels.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a negative or zero value is assigned to the width.
    /// </exception>
    public int Width {
        get => this._width;
        set {
            if (value > 0) {
                this._width = value;
            }
            else {
                throw new ArgumentOutOfRangeException($"Width: {value} must be positive.");
            }
        }
    }

    /// <summary>
    /// Gets or sets the height of the image in pixels.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a non-positive value is assigned to the height.
    /// </exception>
    public int Height {
        get => this._height;
        set {
            if (value > 0) {
                this._height = value;
            }
            else {
                throw new ArgumentOutOfRangeException($"Height: {value} must be positive.");
            }
        }
    }

    /// <summary>
    /// Gets or sets the raw image data as a byte array, typically in RGBA format.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the assigned data size does not match the product of the image's width, height, and the number of color components (4 for RGBA).
    /// </exception>
    public byte[] Data {
        get => this._data;
        set {
            if (value.Length == this.Width * this.Height * 4) {
                this._data = value;
            }
            else {
                throw new ArgumentException("Invalid data size.");
            }
        }
    }

    /// <summary>
    /// Gets the color of a specific pixel in the image.
    /// </summary>
    /// <param name="x">The x-coordinate of the pixel.</param>
    /// <param name="y">The y-coordinate of the pixel.</param>
    /// <returns>The color of the specified pixel.</returns>
    public Color GetColor(int x, int y) {
        if (x < 0 || x >= this.Width || y < 0 || y >= this.Height) {
            throw new ArgumentOutOfRangeException($"X: {x} or Y: {y}");
        }
    
        int index = (y * this.Width + x) * 4;
        return new Color(this.Data[index], this.Data[index + 1], this.Data[index + 2], this.Data[index + 3]);
    }
    
    /// <summary>
    /// Sets the color of a specific pixel in the image.
    /// </summary>
    /// <param name="x">The x-coordinate of the pixel.</param>
    /// <param name="y">The y-coordinate of the pixel.</param>
    /// <param name="color">The new color of the pixel.</param>
    public void SetPixel(int x, int y, Color color) {
        if (x < 0 || x >= this.Width || y < 0 || y >= this.Height) {
            throw new ArgumentOutOfRangeException($"X: {x} or Y: {y}");
        }
        
        int index = (y * this.Width + x) * 4;
        this.Data[index] = color.R;
        this.Data[index + 1] = color.G;
        this.Data[index + 2] = color.B;
        this.Data[index + 3] = color.A;
    }

    /// <summary>
    /// Resizes the image to the specified width and height, maintaining proportional scaling.
    /// </summary>
    /// <param name="newWidth">The desired width of the resized image.</param>
    /// <param name="newHeight">The desired height of the resized image.</param>
    public void Resize(int newWidth, int newHeight) {
        byte[] resizedData = new byte[newWidth * newHeight * 4];
        float xRatio = (float) (this.Width - 1) / newWidth;
        float yRatio = (float) (this.Height - 1) / newHeight;
    
        for (int y = 0; y < newHeight; y++) {
            float yLerp = y * yRatio;
            int yBase = (int) yLerp;
            float yWeight = yLerp - yBase;
    
            for (int x = 0; x < newWidth; x++) {
                float xLerp = x * xRatio;
                int xBase = (int) xLerp;
                float xWeight = xLerp - xBase;
    
                int topLeft = (yBase * this.Width + xBase) * 4;
                int topRight = (yBase * this.Width + Math.Min(xBase + 1, this.Width - 1)) * 4;
                int bottomLeft = (Math.Min(yBase + 1, this.Height - 1) * this.Width + xBase) * 4;
                int bottomRight = (Math.Min(yBase + 1, this.Height - 1) * this.Width + Math.Min(xBase + 1, this.Width - 1)) * 4;
    
                for (int i = 0; i < 4; i++) {
                    float top = this.Data[topLeft + i] * (1 - xWeight) + this.Data[topRight + i] * xWeight;
                    float bottom = this.Data[bottomLeft + i] * (1 - xWeight) + this.Data[bottomRight + i] * xWeight;
                    resizedData[(y * newWidth + x) * 4 + i] = (byte) (top * (1 - yWeight) + bottom * yWeight);
                }
            }
        }
    
        this.Width = newWidth;
        this.Height = newHeight;
        this.Data = resizedData;
    }

    /// <summary>
    /// Resizes the image to the specified width and height using the nearest-neighbor algorithm.
    /// </summary>
    /// <param name="newWidth">The new width of the image.</param>
    /// <param name="newHeight">The new height of the image.</param>
    public void ResizeNN(int newWidth, int newHeight) {
        byte[] resizedData = new byte[newWidth * newHeight * 4];
        float xRatio = (float)this.Width / newWidth;
        float yRatio = (float)this.Height / newHeight;
        
        for (int y = 0; y < newHeight; y++) {
            int nearestY = (int)(y * yRatio);
            for (int x = 0; x < newWidth; x++) {
                int nearestX = (int)(x * xRatio);
                int originalIndex = (nearestY * this.Width + nearestX) * 4;
                int newIndex = (y * newWidth + x) * 4;
    
                resizedData[newIndex] = this.Data[originalIndex];
                resizedData[newIndex + 1] = this.Data[originalIndex + 1];
                resizedData[newIndex + 2] = this.Data[originalIndex + 2];
                resizedData[newIndex + 3] = this.Data[originalIndex + 3];
            }
        }
    
        this.Width = newWidth;
        this.Height = newHeight;
        this.Data = resizedData;
    }

    /// <summary>
    /// Flips the image vertically by inverting the order of rows in the image data.
    /// </summary>
    public void FlipVertical() {
        byte[] flippedData = new byte[this.Data.Length];
    
        for (int y = 0; y < this.Height; y++) {
            int flippedY = this.Height - y - 1;
            for (int x = 0; x < this.Width; x++) {
                int originalIndex = (y * this.Width + x) * 4;
                int flippedIndex = (flippedY * this.Width + x) * 4;
    
                flippedData[flippedIndex] = this.Data[originalIndex];
                flippedData[flippedIndex + 1] = this.Data[originalIndex + 1];
                flippedData[flippedIndex + 2] = this.Data[originalIndex + 2];
                flippedData[flippedIndex + 3] = this.Data[originalIndex + 3];
            }
        }
    
        this.Data = flippedData;
    }

    /// <summary>
    /// Flips the image horizontally by mirroring its pixel data across the vertical axis.
    /// </summary>
    public void FlipHorizontal() {
        byte[] flippedData = new byte[this.Data.Length];
    
        for (int y = 0; y < this.Height; y++) {
            for (int x = 0; x < this.Width; x++) {
                int flippedX = this.Width - x - 1;
                int originalIndex = (y * this.Width + x) * 4;
                int flippedIndex = (y * this.Width + flippedX) * 4;
    
                flippedData[flippedIndex] = this.Data[originalIndex];
                flippedData[flippedIndex + 1] = this.Data[originalIndex + 1];
                flippedData[flippedIndex + 2] = this.Data[originalIndex + 2];
                flippedData[flippedIndex + 3] = this.Data[originalIndex + 3];
            }
        }
    
        this.Data = flippedData;
    }

    /// <summary>
    /// Rotates the image by the specified degrees in a clockwise direction.
    /// </summary>
    /// <param name="degrees">The angle in degrees to rotate the image. Must be a multiple of 90.</param>
    public void Rotate(int degrees) {
        if (degrees % 90 != 0) {
            throw new ArgumentException("Rotation angle must be a multiple of 90 degrees.");
        }
    
        int numRotations = (degrees / 90) % 4;
        for (int i = 0; i < numRotations; i++) {
            this.RotateCW();
        }
    }

    /// <summary>
    /// Rotates the current <see cref="Image"/> 90 degrees clockwise. Updates the image's dimensions and pixel data accordingly.
    /// </summary>
    public void RotateCW() {
        int newWidth = this.Height;
        int newHeight = this.Width;
        byte[] rotatedData = new byte[newWidth * newHeight * 4];
    
        for (int y = 0; y < this.Height; y++) {
            for (int x = 0; x < this.Width; x++) {
                int originalIndex = (y * this.Width + x) * 4;
                int rotatedX = this.Height - y - 1;
                int rotatedY = x;
                int rotatedIndex = (rotatedY * newWidth + rotatedX) * 4;
    
                rotatedData[rotatedIndex] = this.Data[originalIndex];
                rotatedData[rotatedIndex + 1] = this.Data[originalIndex + 1];
                rotatedData[rotatedIndex + 2] = this.Data[originalIndex + 2];
                rotatedData[rotatedIndex + 3] = this.Data[originalIndex + 3];
            }
        }
    
        this.Width = newWidth;
        this.Height = newHeight;
        this.Data = rotatedData;
    }

    /// <summary>
    /// Rotates the current <see cref="Image"/> instance counterclockwise by 90 degrees.
    /// </summary>
    public void RotateCCW() {
        int newWidth = this.Height;
        int newHeight = this.Width;
        byte[] rotatedData = new byte[newWidth * newHeight * 4];
    
        for (int y = 0; y < this.Height; y++) {
            for (int x = 0; x < this.Width; x++) {
                int originalIndex = (y * this.Width + x) * 4;
                int rotatedX = y;
                int rotatedY = this.Width - x - 1;
                int rotatedIndex = (rotatedY * newWidth + rotatedX) * 4;
    
                rotatedData[rotatedIndex] = this.Data[originalIndex];
                rotatedData[rotatedIndex + 1] = this.Data[originalIndex + 1];
                rotatedData[rotatedIndex + 2] = this.Data[originalIndex + 2];
                rotatedData[rotatedIndex + 3] = this.Data[originalIndex + 3];
            }
        }
    
        this.Width = newWidth;
        this.Height = newHeight;
        this.Data = rotatedData;
    }

    /// <summary>
    /// Crops the image to the specified rectangular region.
    /// </summary>
    /// <param name="crop">The rectangular area to crop the image to.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified rectangle is outside the bounds of the image.</exception>
    public void Crop(Rectangle crop) {
        if (crop.X < 0 || crop.Y < 0 || crop.X + crop.Width > this.Width || crop.Y + crop.Height > this.Height) {
            throw new ArgumentOutOfRangeException(nameof(crop), $"The crop rectangle (X: {crop.X}, Y: {crop.Y}, Width: {crop.Width}, Height: {crop.Height}) is out of bounds for the image dimensions (Width: {this.Width}, Height: {this.Height}).");
        }
        
        int newWidth = crop.Width;
        int newHeight = crop.Height;
        byte[] croppedData = new byte[newWidth * newHeight * 4];
        
        for (int y = 0; y < newHeight; y++) {
            for (int x = 0; x < newWidth; x++) {
                int originalIndex = ((crop.Y + y) * this.Width + (crop.X + x)) * 4;
                int croppedIndex = (y * newWidth + x) * 4;
                
                croppedData[croppedIndex] = this.Data[originalIndex];
                croppedData[croppedIndex + 1] = this.Data[originalIndex + 1];
                croppedData[croppedIndex + 2] = this.Data[originalIndex + 2];
                croppedData[croppedIndex + 3] = this.Data[originalIndex + 3];
            }
        }
        
        this.Width = newWidth;
        this.Height = newHeight;
        this.Data = croppedData;
    }

    /// <summary>
    /// Applies a tint to the image by adjusting its pixel color values based on the specified tint color.
    /// </summary>
    /// <param name="tint">The color tint to be applied to the image.</param>
    public void Tint(Color tint) {
        for (int i = 0; i < this.Data.Length; i += 4) {
            this.Data[i] = (byte) (this.Data[i] * tint.R / 255);
            this.Data[i + 1] = (byte) (this.Data[i + 1] * tint.G / 255);
            this.Data[i + 2] = (byte) (this.Data[i + 2] * tint.B / 255);
            this.Data[i + 3] = (byte) (this.Data[i + 3] * tint.A / 255);
        }
    }
    
    /// <summary>
    /// Saves the image as a BMP file.
    /// </summary>
    /// <param name="path">The path where the BMP file should be saved.</param>
    public void SaveAsBmp(string path) {
        using (Stream stream = File.OpenWrite(path)) {
            ImageWriter writer = new ImageWriter();
            writer.WriteBmp(this.Data, this.Width, this.Height, WriteColorComponents.RedGreenBlueAlpha, stream);
        }
    }
    
    /// <summary>
    /// Saves the image as a TGA file.
    /// </summary>
    /// <param name="path">The path where the TGA file should be saved.</param>
    public void SaveAsTga(string path) {
        using (Stream stream = File.OpenWrite(path)) {
            ImageWriter writer = new ImageWriter();
            writer.WriteTga(this.Data, this.Width, this.Height, WriteColorComponents.RedGreenBlueAlpha, stream);
        }
    }
    
    /// <summary>
    /// Saves the image as an HDR file.
    /// </summary>
    /// <param name="path">The path where the HDR file should be saved.</param>
    public void SaveAsHdr(string path) {
        using (Stream stream = File.OpenWrite(path)) {
            ImageWriter writer = new ImageWriter();
            writer.WriteHdr(this.Data, this.Width, this.Height, WriteColorComponents.RedGreenBlueAlpha, stream);
        }
    }
    
    /// <summary>
    /// Saves the image as a PNG file.
    /// </summary>
    /// <param name="path">The path where the PNG file should be saved.</param>
    public void SaveAsPng(string path) {
        using (Stream stream = File.OpenWrite(path)) {
            ImageWriter writer = new ImageWriter();
            writer.WritePng(this.Data, this.Width, this.Height, WriteColorComponents.RedGreenBlueAlpha, stream);
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
            writer.WriteJpg(this.Data, this.Width, this.Height, WriteColorComponents.RedGreenBlueAlpha, stream, quality);
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