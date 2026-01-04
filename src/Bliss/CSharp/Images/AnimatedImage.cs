using Bliss.CSharp.Logging;
using StbImageSharp;

namespace Bliss.CSharp.Images;

public class AnimatedImage {
    
    /// <summary>
    /// Gets a read-only dictionary containing the frames of the animated image and their respective delays (in milliseconds).
    /// </summary>
    public IReadOnlyDictionary<Image, int> Frames { get; private set; }
    
    /// <summary>
    /// Gets the sprite sheet representation of the animated image, where all frames are arranged in a single image.
    /// </summary>
    public Image SpriteSheet { get; private set; }

    /// <summary>
    /// Gets the number of columns in the sprite sheet layout used for organizing frames of the animated image.
    /// </summary>
    public int Columns { get; private set; }

    /// <summary>
    /// Gets the number of rows in the sprite sheet layout used for the animated image.
    /// </summary>
    public int Rows { get; private set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AnimatedImage"/> class from a file path.
    /// </summary>
    /// <param name="path">The file path to the animated image (e.g., a GIF).</param>
    public AnimatedImage(string path) {
        if (!File.Exists(path)) {
            Logger.Fatal($"Failed to find path [{path}]!");
        }
        
        Dictionary<Image, int> frames = new Dictionary<Image, int>();
        
        using (FileStream stream = File.OpenRead(path)) {
            foreach (AnimatedFrameResult result in ImageResult.AnimatedGifFramesFromStream(stream)) {
                byte[] frameData = new byte[result.Data.Length];
                Array.Copy(result.Data, frameData, result.Data.Length);
                
                Image frame = new Image(result.Width, result.Height, frameData);
                
                if (!frames.TryAdd(frame, result.DelayInMs)) {
                    Logger.Error("Failed to add current Frame!");
                }
            }
        }
        
        this.Frames = frames;
        this.SpriteSheet = this.CreateSpriteSheet();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimatedImage"/> class from a stream.
    /// </summary>
    /// <param name="stream">The stream containing the animated image data.</param>
    public AnimatedImage(Stream stream) {
        if (!stream.CanRead) {
            Logger.Fatal($"Failed to read stream [{stream}]!");
        }
        
        Dictionary<Image, int> frames = new Dictionary<Image, int>();
        
        foreach (AnimatedFrameResult result in ImageResult.AnimatedGifFramesFromStream(stream)) {
            byte[] frameData = new byte[result.Data.Length];
            Array.Copy(result.Data, frameData, result.Data.Length);
            
            Image frame = new Image(result.Width, result.Height, frameData);
            
            if (!frames.TryAdd(frame, result.DelayInMs)) {
                Logger.Error("Failed to add current Frame!");
            }
        }
        
        this.Frames = frames;
        this.SpriteSheet = this.CreateSpriteSheet();
    }

    /// <summary>
    /// Retrieves the total number of frames in the animated image.
    /// </summary>
    /// <returns>The number of frames in the animated image.</returns>
    public int GetFrameCount() {
        return this.Frames.Count;
    }

    /// <summary>
    /// Retrieves information of a specific frame in the animated image, including its dimensions and duration.
    /// </summary>
    /// <param name="frameIndex">The index of the frame to retrieve information for.</param>
    /// <param name="width">The output parameter that returns the width of the specified frame.</param>
    /// <param name="height">The output parameter that returns the height of the specified frame.</param>
    /// <param name="duration">The output parameter that returns the duration of the specified frame in milliseconds.</param>
    public void GetFrameInfo(int frameIndex, out int width, out int height, out float duration) {
        width = this.Frames.ElementAt(frameIndex).Key.Width;
        height = this.Frames.ElementAt(frameIndex).Key.Height;
        duration = this.Frames.ElementAt(frameIndex).Value;
    }

    /// <summary>
    /// Combines all frames of the animated image into a single sprite sheet, where each frame is placed horizontally in the resulting image.
    /// </summary>
    /// <returns>The generated sprite sheet as an <see cref="Image"/> object.</returns>
    private Image CreateSpriteSheet() {
        int frameCount = this.Frames.Count;
        
        // Calculate columns and rows to create a layout as square as possible.
        this.Columns = (int) Math.Ceiling(Math.Sqrt(frameCount));
        this.Rows = (int) Math.Ceiling((double) frameCount / this.Columns);
        
        int maxWidth = this.Frames.Max(frame => frame.Key.Width);
        int maxHeight = this.Frames.Max(frame => frame.Key.Height);
        
        int totalWidth = this.Columns * maxWidth;
        int totalHeight = this.Rows * maxHeight;
        
        // Create a new blank sprite sheet image.
        byte[] spriteSheetData = new byte[totalWidth * totalHeight * 4];
        Image spriteSheet = new Image(totalWidth, totalHeight, spriteSheetData);
        
        int currentFrameIndex = 0;
        foreach (Image frame in this.Frames.Keys) {
            int column = currentFrameIndex % this.Columns;
            int row = currentFrameIndex / this.Columns;
            
            int offsetX = column * maxWidth;
            int offsetY = row * maxHeight;
            
            // Position each frame image in the grid.
            for (int y = 0; y < frame.Height; y++) {
                for (int x = 0; x < frame.Width; x++) {
                    int sourceIndex = (y * frame.Width + x) * 4;
                    int targetIndex = ((y + offsetY) * totalWidth + x + offsetX) * 4;
                    
                    // Copy pixel data.
                    Array.Copy(frame.Data, sourceIndex, spriteSheet.Data, targetIndex, 4);
                }
            }
            
            currentFrameIndex++;
        }
        
        return spriteSheet;
    }
}