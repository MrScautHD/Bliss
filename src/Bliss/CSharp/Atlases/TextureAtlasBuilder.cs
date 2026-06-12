using Bliss.CSharp.Images;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
using Veldrith;

namespace Bliss.CSharp.Atlases;

public class TextureAtlasBuilder {
    
    /// <summary>
    /// The source images accumulated so far.
    /// </summary>
    private List<AtlasEntry> _entries;
    
    /// <summary>
    /// Tracks the names already added so duplicates are rejected before packing.
    /// </summary>
    private HashSet<string> _names;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="TextureAtlasBuilder"/> class.
    /// </summary>
    public TextureAtlasBuilder() {
        this._entries = new List<AtlasEntry>();
        this._names = new HashSet<string>();
    }
    
    /// <summary>
    /// Adds a named image to be packed.
    /// </summary>
    /// <param name="name">The unique region name.</param>
    /// <param name="image">The source image.</param>
    public void Add(string name, Image image) {
        if (!this._names.Add(name)) {
            throw new ArgumentException($"An entry with the name [{name}] was already added to the atlas!", nameof(name));
        }
        
        this._entries.Add(new AtlasEntry(name, image));
    }
    
    /// <summary>
    /// Adds a named image loaded from the given file path.
    /// </summary>
    /// <param name="name">The unique region name.</param>
    /// <param name="path">The image file path.</param>
    public void Add(string name, string path) {
        if (!this._names.Add(name)) {
            throw new ArgumentException($"An entry with the name [{name}] was already added to the atlas!", nameof(name));
        }
        
        this._entries.Add(new AtlasEntry(name, new Image(path)));
    }
    
    /// <summary>
    /// Adds an animated image to be packed. Its sprite sheet is packed as a single region and exposed
    /// as an <see cref="AtlasAnimation"/> on the built atlas.
    /// </summary>
    /// <param name="name">The unique animation name.</param>
    /// <param name="animatedImage">The animated image to pack.</param>
    public void Add(string name, AnimatedImage animatedImage) {
        if (!this._names.Add(name)) {
            throw new ArgumentException($"An entry with the name [{name}] was already added to the atlas!", nameof(name));
        }
        
        this._entries.Add(new AtlasEntry(name, animatedImage.SpriteSheet, animatedImage));
    }
    
    /// <summary>
    /// Packs all accumulated images into a single texture and returns the resulting atlas.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create the backing texture.</param>
    /// <param name="maxWidth">The maximum atlas width in pixels before wrapping to a new row.</param>
    /// <param name="padding">The transparent gap in pixels inserted around each region to prevent bleeding.</param>
    /// <param name="mipmap">Whether the backing texture generates mipmaps.</param>
    /// <param name="srgb">Whether the backing texture is treated as sRGB.</param>
    /// <returns>The packed <see cref="TextureAtlas"/>.</returns>
    public TextureAtlas Build(GraphicsDevice graphicsDevice, int maxWidth = 2048, int padding = 1, bool mipmap = false, bool srgb = false) {
        
        // Sort by height descending so rows stay tightly packed.
        List<AtlasEntry> sorted = new List<AtlasEntry>(this._entries);
        sorted.Sort((a, b) => b.Image.Height.CompareTo(a.Image.Height));
        
        Dictionary<string, Rectangle> regions = new Dictionary<string, Rectangle>(sorted.Count);
        Dictionary<string, AtlasAnimation> animations = new Dictionary<string, AtlasAnimation>();
        
        // Assign positions and measure the final atlas size.
        int penX = padding;
        int penY = padding;
        int rowHeight = 0;
        int atlasWidth = 1;
        
        foreach (AtlasEntry entry in sorted) {
            int width = entry.Image.Width;
            int height = entry.Image.Height;
            
            // Wrap to the next row if this image would overflow the current one.
            if (penX + width + padding > maxWidth && penX > padding) {
                penX = padding;
                penY += rowHeight + padding;
                rowHeight = 0;
            }
            
            Rectangle region = new Rectangle(penX, penY, width, height);
            regions[entry.Name] = region;
            
            // Record animation metadata against the sheet's final atlas position.
            if (entry.Animation != null) {
                animations[entry.Name] = this.CreateAnimation(region, entry.Animation);
            }
            
            penX += width + padding;
            rowHeight = Math.Max(rowHeight, height);
            atlasWidth = Math.Max(atlasWidth, penX);
        }
        
        int atlasHeight = Math.Max(1, penY + rowHeight + padding);
        
        // Blit every source image into one blank RGBA image.
        Image atlasImage = new Image(atlasWidth, atlasHeight);
        
        foreach (AtlasEntry entry in sorted) {
            Rectangle region = regions[entry.Name];
            this.Blit(atlasImage, entry.Image, region.X, region.Y);
        }
        
        Texture2D texture = new Texture2D(graphicsDevice, atlasImage, mipmap, srgb);
        return new TextureAtlas(texture, regions, animations);
    }
    
    /// <summary>
    /// Builds animation metadata for a packed sprite sheet, deriving frame delays from the source animation.
    /// </summary>
    /// <param name="sheetRegion">The sprite sheet's rectangle within the atlas.</param>
    /// <param name="animation">The source animated image.</param>
    /// <returns>The resulting <see cref="AtlasAnimation"/>.</returns>
    private AtlasAnimation CreateAnimation(Rectangle sheetRegion, AnimatedImage animation) {
        int frameCount = animation.GetFrameCount();
        int[] frameDelays = new int[frameCount];
        
        for (int i = 0; i < frameCount; i++) {
            animation.GetFrameInfo(i, out _, out _, out float duration);
            frameDelays[i] = (int) duration;
        }
        
        return new AtlasAnimation(sheetRegion, animation.Columns, animation.Rows, frameCount, frameDelays);
    }
    
    /// <summary>
    /// Copies a source image's pixels into the destination image at the given offset, row by row.
    /// </summary>
    /// <param name="destination">The destination image.</param>
    /// <param name="source">The source image.</param>
    /// <param name="x">The destination x offset in pixels.</param>
    /// <param name="y">The destination y offset in pixels.</param>
    private void Blit(Image destination, Image source, int x, int y) {
        int destinationStride = destination.Width * 4;
        int sourceStride = source.Width * 4;
        
        for (int row = 0; row < source.Height; row++) {
            Buffer.BlockCopy(source.Data, row * sourceStride, destination.Data, (y + row) * destinationStride + x * 4, sourceStride);
        }
    }
}