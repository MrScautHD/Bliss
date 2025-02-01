using Bliss.CSharp.Logging;
using StbImageSharp;

namespace Bliss.CSharp.Images;

public class AnimatedImage {
    
    public IReadOnlyDictionary<Image, int> Frames { get; private set; }
    public Image SpriteSheet { get; private set; }
    
    private float[] _durationPerFrame;
    
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
        this.SpriteSheet = CreateSpriteSheet(out float[] durationPerFrame);
        this._durationPerFrame = durationPerFrame;
    }

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
        this.SpriteSheet = CreateSpriteSheet(out float[] durationPerFrame);
        this._durationPerFrame = durationPerFrame;
    }

    public int GetFrameCount() {
        return this.Frames.Count;
    }

    public void GetFrameInfo(int frameIndex, out int width, out int height, out float duration) {
        width = this.Frames.ElementAt(frameIndex).Key.Width;
        height = this.Frames.ElementAt(frameIndex).Key.Height;
        duration = this._durationPerFrame[frameIndex];
    }

    private Image CreateSpriteSheet(out float[] durationPerFrame) {
        durationPerFrame = new float[this.Frames.Count];
        durationPerFrame[0] = 0;
        
        int totalWidth = this.Frames.Sum(frame => frame.Key.Width);
        int maxHeight = this.Frames.Max(frame => frame.Key.Height);
    
        // Create a new blank sprite sheet image.
        byte[] spriteSheetData = new byte[totalWidth * maxHeight * 4];
        Image spriteSheet = new Image(totalWidth, maxHeight, spriteSheetData);
    
        // Position each frame image horizontally in the sprite sheet.
        int offsetX = 0;
        foreach (Image frame in this.Frames.Keys) {
            for (int y = 0; y < frame.Height; y++) {
                for (int x = 0; x < frame.Width; x++) {
                    int sourceIndex = (y * frame.Width + x) * 4;
                    int targetIndex = (y * totalWidth + (x + offsetX)) * 4;
    
                    // Copy pixel data.
                    Array.Copy(frame.Data, sourceIndex, spriteSheet.Data, targetIndex, 4);
                }
            }
            
            offsetX += frame.Width;
        }
    
        return spriteSheet;
    }
}