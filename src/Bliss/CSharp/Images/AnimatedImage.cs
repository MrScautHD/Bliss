using Silk.NET.Maths;
using Silk.NET.Vulkan;
using StbImageSharp;

namespace Bliss.CSharp.Images;

public class AnimatedImage {

    public int FrameCount => this._frames.Length;

    private readonly (Image, int)[] _frames;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AnimatedImage"/> class with the specified frames.
    /// </summary>
    /// <param name="frames">An array of tuples where each tuple contains an <see cref="Image"/> and an integer representing the frame delay in milliseconds.</param>
    public AnimatedImage((Image, int)[] frames) {
        this._frames = frames;
    }
    
    /// <summary>
    /// Loads an animated GIF from the specified file path and returns the individual frames as a sequence of AnimatedFrameResult objects.
    /// </summary>
    /// <param name="path">The path to the animated GIF file.</param>
    /// <returns>An IEnumerable of AnimatedFrameResult objects representing the frames of the animated GIF. Each frame contains the size, format, and data of the image.</returns>
    public static AnimatedImage Load(string path) {
        using var stream = File.OpenRead(path);
        List<AnimatedFrameResult> result = (List<AnimatedFrameResult>) ImageResult.AnimatedGifFramesFromStream(stream, ColorComponents.RedGreenBlueAlpha);
        
        if (result == null) {
            throw new Exception($"Failed to load animated image from path: {path}");
        }
        
        (Image, int)[] frames = new (Image, int)[result.Count];
        for (int i = 0; i < result.Count; i++) {
            AnimatedFrameResult frameResult = result[i];
            
            frames[i] = (new Image(new Vector2D<int>(frameResult.Width, frameResult.Height), Format.R8G8B8A8Srgb, frameResult.Data), frameResult.DelayInMs);
        }
        
        return new AnimatedImage(frames);
    }

    /// <summary>
    /// Returns the specific frame of the animated image at the given index along with its delay.
    /// </summary>
    /// <param name="frame">The index of the frame to retrieve.</param>
    /// <param name="delayInMs">The delay in milliseconds of the retrieved frame.</param>
    /// <returns>The specified frame of the animated image as an instance of the Image class.</returns>
    public Image GetFrame(int frame, out int delayInMs) {
        delayInMs = this._frames[frame].Item2;
        return this._frames[frame].Item1;
    }

    /// <summary>
    /// Returns an array of (Image, int) tuples representing the frames of the animated image.
    /// </summary>
    /// <returns>An array of (Image, int) tuples representing the frames of the animated image. Each tuple contains the image data and the delay in milliseconds for the frame.</returns>
    public (Image, int)[] GetFrames() {
        return this._frames;
    }
}