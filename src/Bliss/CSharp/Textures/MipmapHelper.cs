using Bliss.CSharp.Images;

namespace Bliss.CSharp.Textures;

public static class MipmapHelper {
    
    /// <summary>
    /// Generates mipmaps for a given source image.
    /// </summary>
    /// <param name="baseImage">The base image from which the mipmaps are generated.</param>
    /// <returns>A list of images representing the mipmap levels, including the original image as the first level.</returns>
    public static Image[] GenerateMipmaps(Image baseImage) {
        List<Image> mipLevels = new List<Image>();
        mipLevels.Add(baseImage);
        
        int width = baseImage.Width;
        int height = baseImage.Height;
        
        while (width > 1 && height > 1) {
            width = Math.Max(1, width / 2);
            height = Math.Max(1, height / 2);
            
            byte[] newData = Downscale(mipLevels[^1].Data, mipLevels[^1].Width, mipLevels[^1].Height, width, height, 4);
            mipLevels.Add(new Image(width, height, newData));
        }
        
        return mipLevels.ToArray();
    }

    /// <summary>
    /// Downscales an image data buffer to a new width and height using a simple averaging method.
    /// </summary>
    /// <param name="data">The source image data buffer in bytes.</param>
    /// <param name="oldWidth">The width of the source image.</param>
    /// <param name="oldHeight">The height of the source image.</param>
    /// <param name="newWidth">The width of the downscaled image.</param>
    /// <param name="newHeight">The height of the downscaled image.</param>
    /// <param name="channels">The number of color channels in the image.</param>
    /// <returns>A byte array containing the new image data after downscaling.</returns>
    private static byte[] Downscale(byte[] data, int oldWidth, int oldHeight, int newWidth, int newHeight, int channels) {
        byte[] newData = new byte[newWidth * newHeight * channels];

        for (int y = 0; y < newHeight; y++) {
            for (int x = 0; x < newWidth; x++) {
                for (int c = 0; c < channels; c++) {
                    int sum = 0;
                    int pixelCount = 0;

                    // Average the 2x2 pixel block.
                    for (int dy = 0; dy < 2; dy++) {
                        for (int dx = 0; dx < 2; dx++) {
                            int srcX = x * 2 + dx;
                            int srcY = y * 2 + dy;

                            if (srcX < oldWidth && srcY < oldHeight) {
                                int srcIndex = (srcY * oldWidth + srcX) * channels + c;
                                sum += data[srcIndex];
                                pixelCount++;
                            }
                        }
                    }

                    int destIndex = (y * newWidth + x) * channels + c;
                    newData[destIndex] = (byte) (sum / pixelCount);
                }
            }
        }

        return newData;
    }
}