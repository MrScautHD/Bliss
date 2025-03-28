using Bliss.CSharp.Images;
using Bliss.CSharp.Transformations;

namespace Bliss.CSharp.Textures.Cubemaps;

public static class CubemapHelper {
    
    /// <summary>
    /// Generates an array of images, each representing one face of a cubemap, based on the input image and specified cubemap layout.
    /// </summary>
    /// <param name="image">The input image to generate cubemap face images from.</param>
    /// <param name="layout">The layout of the cubemap in the source image. Determines how the image should be split into faces.</param>
    /// <returns>An array of six images, each representing one face of the cubemap. The order of the images corresponds to the cubemap faces.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the dimensions of the input image are incompatible with the specified cubemap layout.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the specified cubemap layout is not recognized or supported.
    /// </exception>
    public static Image[] GenCubemapImages(Image image, CubemapLayout layout) {
        int width = image.Width;
        int height = image.Height;

        Image[] cubemapFaces = new Image[6];

        if (layout == CubemapLayout.AutoDetect) {
            layout = DetectLayout(width, height);
        }

        switch (layout) {
            case CubemapLayout.LineVertical:
                if (height % 6 != 0) {
                    throw new ArgumentException("Image height is not divisible by 6 for LineVertical layout.");
                }

                int faceHeight = height / 6;
                
                for (int i = 0; i < 6; i++) {
                    Image croppedImage = (Image) image.Clone();
                    croppedImage.Crop(new Rectangle(0, i * faceHeight, width, faceHeight));
                    
                    cubemapFaces[i] = croppedImage;
                }
                break;

            case CubemapLayout.LineHorizontal:
                if (width % 6 != 0) {
                    throw new ArgumentException("Image width is not divisible by 6 for LineHorizontal layout.");
                }

                int faceWidth = width / 6;
                
                for (int i = 0; i < 6; i++) {
                    Image croppedImage = (Image) image.Clone();
                    croppedImage.Crop(new Rectangle(i * faceWidth, 0, faceWidth, height));
                    
                    cubemapFaces[i] = croppedImage;
                }
                break;

            case CubemapLayout.CrossThreeByFour:
                if (width % 3 != 0 || height % 4 != 0) {
                    throw new ArgumentException("Image dimensions do not match the CrossThreeByFour layout.");
                }
                
                ProcessCrossThreeByFour(image, cubemapFaces, width, height);
                break;

            case CubemapLayout.CrossFourByThree:
                if (width % 4 != 0 || height % 3 != 0) {
                    throw new ArgumentException("Image dimensions do not match the CrossFourByThree layout.");
                }

                ProcessCrossFourByThree(image, cubemapFaces, width, height);
                break;

            case CubemapLayout.Panorama:
                cubemapFaces = ProcessPanorama(image);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(layout), layout, "Unknown cubemap layout.");
        }

        return cubemapFaces;
    }

    /// <summary>
    /// Determines the cubemap layout of an image based on its dimensions.
    /// </summary>
    /// <param name="width">The width of the image to be analyzed.</param>
    /// <param name="height">The height of the image to be analyzed.</param>
    /// <returns>The detected cubemap layout, as an instance of the <see cref="CubemapLayout"/> enum.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the dimensions of the image do not match any known cubemap layout.
    /// </exception>
    private static CubemapLayout DetectLayout(int width, int height) {
        if (height / 6 == width) {
            return CubemapLayout.LineVertical;
        } else if (width / 6 == height) {
            return CubemapLayout.LineHorizontal;
        } else if (width / 4 == height / 3) {
            return CubemapLayout.CrossFourByThree;
        } else if (width / 3 == height / 4) {
            return CubemapLayout.CrossThreeByFour;
        } else if (width == height / 2) {
            return CubemapLayout.Panorama;
        }
        else {
            throw new ArgumentException("Unable to automatically detect the layout of the provided image.");
        }
    }

    /// <summary>
    /// Processes a 3x4 cross-layout input image and extracts six cubemap faces corresponding to the positive and negative x, y, and z axes.
    /// </summary>
    /// <param name="image">The input image in a 3x4 cross layout from which cubemap faces will be extracted.</param>
    /// <param name="cubemapFaces">The output array to populate with six images, where each image corresponds to a cubemap face.</param>
    /// <param name="width">The total width of the input image in pixels, used to calculate the dimensions of each face.</param>
    /// <param name="height">The total height of the input image in pixels, used to calculate the dimensions of each face.</param>
    private static void ProcessCrossThreeByFour(Image image, Image[] cubemapFaces, int width, int height) {
        int crossWidth = width / 3;
        int crossHeight = height / 4;

        // PositiveX
        Image positiveX = (Image) image.Clone();
        positiveX.Crop(new Rectangle(crossWidth * 2, crossHeight, crossWidth, crossHeight));
        cubemapFaces[0] = positiveX;
        
        // NegativeX
        Image negativeX = (Image) image.Clone();
        negativeX.Crop(new Rectangle(0, crossHeight, crossWidth, crossHeight));
        cubemapFaces[1] = negativeX;
        
        // PositiveY
        Image positiveY = (Image) image.Clone();
        positiveY.Crop(new Rectangle(crossWidth, 0, crossWidth, crossHeight));
        cubemapFaces[2] = positiveY;
        
        // NegativeY
        Image negativeY = (Image) image.Clone();
        negativeY.Crop(new Rectangle(crossWidth, crossHeight * 2, crossWidth, crossHeight));
        cubemapFaces[3] = negativeY;
        
        // PositiveZ
        Image positiveZ = (Image) image.Clone();
        positiveZ.Crop(new Rectangle(crossWidth, crossHeight * 1, crossWidth, crossHeight));
        cubemapFaces[4] = positiveZ;

        // NegativeZ
        Image negativeZ = (Image) image.Clone();
        negativeZ.Crop(new Rectangle(crossWidth * 2, crossHeight * 1, crossWidth, crossHeight));
        cubemapFaces[5] = negativeZ;
    }

    /// <summary>
    /// Processes a source image in a 4x3 cross layout and extracts the six corresponding cubemap face images.
    /// </summary>
    /// <param name="image">The source image that contains the 4x3 cross layout representation of the cubemap.</param>
    /// <param name="cubemapFaces">An array to store the six resulting cubemap face images, aligned with cubemap face order.</param>
    /// <param name="width">The width of the input image to be used for calculating individual face dimensions.</param>
    /// <param name="height">The height of the input image to be used for calculating individual face dimensions.</param>
    private static void ProcessCrossFourByThree(Image image, Image[] cubemapFaces, int width, int height) {
        int crossWidth = width / 4;
        int crossHeight = height / 3;

        // PositiveX
        Image positiveX = (Image) image.Clone();
        positiveX.Crop(new Rectangle(crossWidth * 2, crossHeight, crossWidth, crossHeight));
        cubemapFaces[0] = positiveX;
        
        // NegativeX
        Image negativeX = (Image) image.Clone();
        negativeX.Crop(new Rectangle(0, crossHeight, crossWidth, crossHeight));
        cubemapFaces[1] = negativeX;
        
        // PositiveY
        Image positiveY = (Image) image.Clone();
        positiveY.Crop(new Rectangle(crossWidth, 0, crossWidth, crossHeight));
        cubemapFaces[2] = positiveY;
        
        // NegativeY
        Image negativeY = (Image) image.Clone();
        negativeY.Crop(new Rectangle(crossWidth, crossHeight * 2, crossWidth, crossHeight));
        cubemapFaces[3] = negativeY;
        
        // PositiveZ
        Image positiveZ = (Image) image.Clone();
        positiveZ.Crop(new Rectangle(crossWidth * 1, crossHeight * 1, crossWidth, crossHeight));
        cubemapFaces[4] = positiveZ;

        // NegativeZ
        Image negativeZ = (Image) image.Clone();
        negativeZ.Crop(new Rectangle(crossWidth * 3, crossHeight * 1, crossWidth, crossHeight));
        cubemapFaces[5] = negativeZ;
    }

    /// <summary>
    /// Processes a panoramic image to generate an array of six images, each representing one face of a cubemap.
    /// </summary>
    /// <param name="image">The input panoramic image to be processed into cubemap faces. The image must have a 4:2 aspect ratio.</param>
    /// <returns>An array of six images representing the cubemap faces: PositiveX, NegativeX, PositiveY, NegativeY, PositiveZ, and NegativeZ.</returns>
    private static Image[] ProcessPanorama(Image image) {
        int faceWidth = image.Width / 4;
        int faceHeight = image.Height / 2;
        
        Image[] faces = new Image[6];
        
        // PositiveX
        Image positiveX = (Image) image.Clone();
        positiveX.Crop(new Rectangle(faceWidth * 3, 0, faceWidth, faceHeight));
        faces[0] = positiveX;
        
        // NegativeX
        Image negativeX = (Image) image.Clone();
        negativeX.Crop(new Rectangle(faceWidth, 0, faceWidth, faceHeight));
        faces[1] = negativeX;
        
        // PositiveY
        Image positiveY = (Image) image.Clone();
        positiveY.Crop(new Rectangle(faceWidth * 2, 0, faceWidth, faceHeight));
        faces[2] = positiveY;
        
        // NegativeY
        Image negativeY = (Image) image.Clone();
        negativeY.Crop(new Rectangle(0, 0, faceWidth, faceHeight));
        faces[3] = negativeY;
        
        // PositiveZ
        Image positiveZ = (Image) image.Clone();
        positiveZ.Crop(new Rectangle(faceWidth, faceHeight, faceWidth, faceHeight));
        faces[4] = positiveZ;
        
        // NegativeZ
        Image negativeZ = (Image) image.Clone();
        negativeZ.Crop(new Rectangle(faceWidth * 2, faceHeight, faceWidth, faceHeight));
        faces[5] = negativeZ;

        return faces;
    }
}