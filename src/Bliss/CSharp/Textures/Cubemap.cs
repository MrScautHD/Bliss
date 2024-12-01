/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: MIT License
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;

namespace Bliss.CSharp.Textures;

public class Cubemap : Disposable {

    /// <summary>
    /// Gets the mipmap levels of images for each face of the cubemap.
    /// </summary>
    public Image<Rgba32>[][] Images { get; }

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
        Image.Load<Rgba32>(positiveXPath),
        Image.Load<Rgba32>(negativeXPath),
        Image.Load<Rgba32>(positiveYPath),
        Image.Load<Rgba32>(negativeYPath),
        Image.Load<Rgba32>(positiveZPath), 
        Image.Load<Rgba32>(negativeZPath),
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
        Image.Load<Rgba32>(positiveXStream),
        Image.Load<Rgba32>(negativeXStream),
        Image.Load<Rgba32>(positiveYStream),
        Image.Load<Rgba32>(negativeYStream),
        Image.Load<Rgba32>(positiveZStream),
        Image.Load<Rgba32>(negativeZStream),
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
    public Cubemap(GraphicsDevice graphicsDevice, Image<Rgba32> positiveX, Image<Rgba32> negativeX, Image<Rgba32> positiveY, Image<Rgba32> negativeY, Image<Rgba32> positiveZ, Image<Rgba32> negativeZ, bool mipmap = true, bool srgb = false) {
        this.Images = new Image<Rgba32>[6][];
        
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
    private unsafe void CreateDeviceTexture(GraphicsDevice graphicsDevice) {
        this.DeviceTexture = graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(this.Width, this.Height, this.MipLevels, 1, this.Format, TextureUsage.Sampled | TextureUsage.Cubemap));
            
        for (int level = 0; level < this.MipLevels; level++) {
            if (!this.Images[PositiveXArrayLayer][level].DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixelMemoryPosX)) {
                throw new VeldridException("Unable to get positive x image pixel data!");
            }
            if (!this.Images[NegativeXArrayLayer][level].DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixelMemoryNegX)) {
                throw new VeldridException("Unable to get negative x image pixel data!");
            }
            if (!this.Images[PositiveYArrayLayer][level].DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixelMemoryPosY)) {
                throw new VeldridException("Unable to get positive y image pixel data!");
            }
            if (!this.Images[NegativeYArrayLayer][level].DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixelMemoryNegY)) {
                throw new VeldridException("Unable to get negative y image pixel data!");
            }
            if (!this.Images[PositiveZArrayLayer][level].DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixelMemoryPosZ)) {
                throw new VeldridException("Unable to get positive z image pixel data!");
            }
            if (!this.Images[NegativeZArrayLayer][level].DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixelMemoryNegZ)) {
                throw new VeldridException("Unable to get negative z image pixel data!");
            }
            
            fixed (Rgba32* positiveXPin = &MemoryMarshal.GetReference(pixelMemoryPosX.Span)) {
                fixed (Rgba32* negativeXPin = &MemoryMarshal.GetReference(pixelMemoryNegX.Span)) {
                    fixed (Rgba32* positiveYPin = &MemoryMarshal.GetReference(pixelMemoryPosY.Span)) {
                        fixed (Rgba32* negativeYPin = &MemoryMarshal.GetReference(pixelMemoryNegY.Span)) {
                            fixed (Rgba32* positiveZPin = &MemoryMarshal.GetReference(pixelMemoryPosZ.Span)) {
                                fixed (Rgba32* negativeZPin = &MemoryMarshal.GetReference(pixelMemoryNegZ.Span)) {
                                    Image<Rgba32> image = this.Images[0][level];
                                    uint width = (uint) image.Width;
                                    uint height = (uint) image.Height;
                                    uint faceSize = width * height * this.PixelSizeInBytes;
                
                                    graphicsDevice.UpdateTexture(this.DeviceTexture, (nint) positiveXPin, faceSize, 0, 0, 0, width, height, 1, (uint) level, PositiveXArrayLayer);
                                    graphicsDevice.UpdateTexture(this.DeviceTexture, (nint) negativeXPin, faceSize, 0, 0, 0, width, height, 1, (uint) level, NegativeXArrayLayer);
                                    graphicsDevice.UpdateTexture(this.DeviceTexture, (nint) positiveYPin, faceSize, 0, 0, 0, width, height, 1, (uint) level, PositiveYArrayLayer);
                                    graphicsDevice.UpdateTexture(this.DeviceTexture, (nint) negativeYPin, faceSize, 0, 0, 0, width, height, 1, (uint) level, NegativeYArrayLayer);
                                    graphicsDevice.UpdateTexture(this.DeviceTexture, (nint) positiveZPin, faceSize, 0, 0, 0, width, height, 1, (uint) level, PositiveZArrayLayer);
                                    graphicsDevice.UpdateTexture(this.DeviceTexture, (nint) negativeZPin, faceSize, 0, 0, 0, width, height, 1, (uint) level, NegativeZArrayLayer);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.DeviceTexture.Dispose();
        }
    }
}