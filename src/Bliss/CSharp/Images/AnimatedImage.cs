using Bliss.CSharp.Logging;
using StbImageSharp;

namespace Bliss.CSharp.Images;

public class AnimatedImage {
    
    /// <summary>
    /// A private dictionary that maps each frame of the animated image to its associated delay time in milliseconds.
    /// </summary>
    private Dictionary<Image, int> _frames;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimatedImage"/> class from a file path.
    /// </summary>
    /// <param name="path">The path to the animated image file.</param>
    public AnimatedImage(string path) {
        if (!File.Exists(path)) {
            Logger.Fatal($"Failed to find path [{path}]!");
        }
        
        this._frames = new Dictionary<Image, int>();

        using (FileStream stream = File.OpenRead(path)) {
            foreach (AnimatedFrameResult result in ImageResult.AnimatedGifFramesFromStream(stream)) {
                Image frame = new Image(result.Width, result.Height, result.Data);
                
                if (!this._frames.TryAdd(frame, result.DelayInMs)) {
                    Logger.Error("Failed to add current Frame!");
                }
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimatedImage"/> class from a stream.
    /// </summary>
    /// <param name="stream">The stream containing the animated image data.</param>
    public AnimatedImage(Stream stream) {
        if (!stream.CanRead) {
            Logger.Fatal($"Failed to read stream [{stream}]!");
        }
        
        this._frames = new Dictionary<Image, int>();
        
        foreach (AnimatedFrameResult result in ImageResult.AnimatedGifFramesFromStream(stream)) {
            Image frame = new Image(result.Width, result.Height, result.Data);
            
            if (!this._frames.TryAdd(frame, result.DelayInMs)) {
                Logger.Error("Failed to add current Frame!");
            }
        }
    }

    /// <summary>
    /// Gets the total number of frames in the animated image.
    /// </summary>
    /// <returns>The total number of frames.</returns>
    public int GetFramesCount() {
        return this._frames.Count;
    }

    /// <summary>
    /// Gets all the frames and their associated delays.
    /// </summary>
    /// <returns>A dictionary where keys are <see cref="Image"/> instances representing frames and values are delays in milliseconds.</returns>
    public Dictionary<Image, int> GetFrames() {
        return this._frames;
    }

    /// <summary>
    /// Gets the frame and its delay at the specified index.
    /// </summary>
    /// <param name="index">The index of the frame to retrieve.</param>
    /// <returns>A tuple containing the <see cref="Image"/> and its delay in milliseconds.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range.</exception>
    public (Image, int) GetFrame(int index) {
        if (index > this._frames.Count) {
            Logger.Fatal("An attempt was made to access an index that is out of range. Ensure the index is within the valid bounds.");
        }

        KeyValuePair<Image, int> frame = this._frames.ElementAt(index);
        return (frame.Key, frame.Value);
    }
}