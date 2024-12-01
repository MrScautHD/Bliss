/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: MIT License
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Bliss.CSharp.Textures;

public static class MipmapHelper {
    
    /// <summary>
    /// Generates mipmaps for the given base image.
    /// </summary>
    /// <param name="baseImage">The base image from which to generate mipmaps.</param>
    /// <returns>An array of images representing the mip levels.</returns>
    public static Image<Rgba32>[] GenerateMipmaps(Image<Rgba32> baseImage) {
        int levelCount = ComputeMipLevels(baseImage.Width, baseImage.Height);
        
        Image<Rgba32>[] mipLevels = new Image<Rgba32>[levelCount];
        mipLevels[0] = baseImage;
        
        int currentWidth = baseImage.Width;
        int currentHeight = baseImage.Height;
        
        int i = 1;
        while (currentWidth != 1 || currentHeight != 1) {
            int newWidth = Math.Max(1, currentWidth / 2);
            int newHeight = Math.Max(1, currentHeight / 2);
            mipLevels[i] = baseImage.Clone(context => context.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));

            i++;
            currentWidth = newWidth;
            currentHeight = newHeight;
        }
        
        return mipLevels;
    }
    
    /// <summary>
    /// Computes the number of mip levels for a texture based on its width and height.
    /// </summary>
    /// <param name="width">The width of the texture.</param>
    /// <param name="height">The height of the texture.</param>
    /// <returns>The number of mip levels.</returns>
    private static int ComputeMipLevels(int width, int height) {
        return 1 + (int) Math.Floor(Math.Log(Math.Max(width, height), 2));
    }
}