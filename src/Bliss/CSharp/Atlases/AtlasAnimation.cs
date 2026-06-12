using Bliss.CSharp.Transformations;

namespace Bliss.CSharp.Atlases;

public class AtlasAnimation {
    
    /// <summary>
    /// The rectangle of the full sprite sheet within the atlas texture.
    /// </summary>
    public Rectangle SheetRegion { get; private set; }
    
    /// <summary>
    /// The number of columns in the sprite sheet grid.
    /// </summary>
    public int Columns { get; private set; }
    
    /// <summary>
    /// The number of rows in the sprite sheet grid.
    /// </summary>
    public int Rows { get; private set; }
    
    /// <summary>
    /// The width of a single frame cell in pixels.
    /// </summary>
    public int FrameWidth { get; private set; }
    
    /// <summary>
    /// The height of a single frame cell in pixels.
    /// </summary>
    public int FrameHeight { get; private set; }
    
    /// <summary>
    /// The total number of frames in the animation.
    /// </summary>
    public int FrameCount { get; private set; }
    
    /// <summary>
    /// The delay of each frame in milliseconds, indexed by frame.
    /// </summary>
    private int[] _frameDelays;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AtlasAnimation"/> class.
    /// </summary>
    /// <param name="sheetRegion">The rectangle of the full sprite sheet within the atlas texture.</param>
    /// <param name="columns">The number of columns in the sprite sheet grid.</param>
    /// <param name="rows">The number of rows in the sprite sheet grid.</param>
    /// <param name="frameCount">The total number of frames.</param>
    /// <param name="frameDelays">The delay of each frame in milliseconds.</param>
    public AtlasAnimation(Rectangle sheetRegion, int columns, int rows, int frameCount, int[] frameDelays) {
        this.SheetRegion = sheetRegion;
        this.Columns = columns;
        this.Rows = rows;
        this.FrameWidth = sheetRegion.Width / columns;
        this.FrameHeight = sheetRegion.Height / rows;
        this.FrameCount = frameCount;
        this._frameDelays = frameDelays;
    }
    
    /// <summary>
    /// Gets the source rectangle of the given frame in atlas space, ready to use as a <c>sourceRect</c>.
    /// </summary>
    /// <param name="frame">The frame index.</param>
    /// <returns>The frame's rectangle within the atlas texture.</returns>
    public Rectangle GetFrameRegion(int frame) {
        int column = frame % this.Columns;
        int row = (frame / this.Columns) % this.Rows;
        
        return new Rectangle(this.SheetRegion.X + column * this.FrameWidth, this.SheetRegion.Y + row * this.FrameHeight, this.FrameWidth, this.FrameHeight);
    }
    
    /// <summary>
    /// Gets the delay of the given frame in milliseconds.
    /// </summary>
    /// <param name="frame">The frame index.</param>
    /// <returns>The frame's delay in milliseconds.</returns>
    public int GetFrameDelay(int frame) {
        return this._frameDelays[frame];
    }
}