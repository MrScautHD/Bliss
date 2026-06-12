using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;

namespace Bliss.CSharp.Atlases;

public class TextureAtlas : Disposable {
    
    /// <summary>
    /// The backing texture containing every packed region.
    /// </summary>
    public Texture2D Texture { get; private set; }
    
    /// <summary>
    /// Maps each region name to its rectangle within <see cref="Texture"/>.
    /// </summary>
    private Dictionary<string, Rectangle> _regions;
    
    /// <summary>
    /// Maps each animation name to its packed sprite sheet metadata.
    /// </summary>
    private Dictionary<string, AtlasAnimation> _animations;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="TextureAtlas"/> class.
    /// </summary>
    /// <param name="texture">The backing texture containing the packed regions.</param>
    /// <param name="regions">The name-to-rectangle map of the packed regions.</param>
    /// <param name="animations">The name-to-animation map of the packed sprite sheets.</param>
    public TextureAtlas(Texture2D texture, Dictionary<string, Rectangle> regions, Dictionary<string, AtlasAnimation> animations) {
        this.Texture = texture;
        this._regions = regions;
        this._animations = animations;
    }
    
    /// <summary>
    /// Gets the rectangle of the named region. Use the result as the <c>sourceRect</c> when drawing.
    /// </summary>
    /// <param name="name">The region name.</param>
    /// <returns>The region's rectangle within <see cref="Texture"/>.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if no region with the given name exists.</exception>
    public Rectangle GetRegion(string name) {
        if (!this._regions.TryGetValue(name, out Rectangle region)) {
            throw new KeyNotFoundException($"The atlas does not contain a region named: [{name}]");
        }
        
        return region;
    }
    
    /// <summary>
    /// Attempts to get the rectangle of the named region.
    /// </summary>
    /// <param name="name">The region name.</param>
    /// <param name="region">When this method returns, contains the region's rectangle if found; otherwise the default rectangle.</param>
    /// <returns><c>true</c> if the region exists; otherwise <c>false</c>.</returns>
    public bool TryGetRegion(string name, out Rectangle region) {
        return this._regions.TryGetValue(name, out region);
    }
    
    /// <summary>
    /// Determines whether a region with the given name exists.
    /// </summary>
    /// <param name="name">The region name.</param>
    /// <returns><c>true</c> if the region exists; otherwise <c>false</c>.</returns>
    public bool HasRegion(string name) {
        return this._regions.ContainsKey(name);
    }
    
    /// <summary>
    /// Gets the animation metadata of the named sprite sheet.
    /// </summary>
    /// <param name="name">The animation name.</param>
    /// <returns>The <see cref="AtlasAnimation"/> for the given name.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if no animation with the given name exists.</exception>
    public AtlasAnimation GetAnimation(string name) {
        if (!this._animations.TryGetValue(name, out AtlasAnimation? animation)) {
            throw new KeyNotFoundException($"The atlas does not contain an animation named: [{name}]");
        }
        
        return animation;
    }
    
    /// <summary>
    /// Attempts to get the animation metadata of the named sprite sheet.
    /// </summary>
    /// <param name="name">The animation name.</param>
    /// <param name="animation">When this method returns, contains the animation if found; otherwise null.</param>
    /// <returns><c>true</c> if the animation exists; otherwise <c>false</c>.</returns>
    public bool TryGetAnimation(string name, out AtlasAnimation? animation) {
        return this._animations.TryGetValue(name, out animation);
    }
    
    /// <summary>
    /// Determines whether an animation with the given name exists.
    /// </summary>
    /// <param name="name">The animation name.</param>
    /// <returns><c>true</c> if the animation exists; otherwise <c>false</c>.</returns>
    public bool HasAnimation(string name) {
        return this._animations.ContainsKey(name);
    }
    
    /// <summary>
    /// Gets the names of all packed static regions.
    /// </summary>
    /// <returns>An enumerable collection of region names.</returns>
    public IEnumerable<string> GetRegionNames() {
        return this._regions.Keys;
    }
    
    /// <summary>
    /// Gets the names of all packed animations.
    /// </summary>
    /// <returns>An enumerable collection of animation names.</returns>
    public IEnumerable<string> GetAnimationNames() {
        return this._animations.Keys;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.Texture.Dispose();
        }
    }
}