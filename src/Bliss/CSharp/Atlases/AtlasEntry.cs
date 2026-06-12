using Bliss.CSharp.Images;

namespace Bliss.CSharp.Atlases;

public readonly struct AtlasEntry {
    
    /// <summary>
    /// The region name.
    /// </summary>
    public readonly string Name;
    
    /// <summary>
    /// The source image (the sprite sheet for animations).
    /// </summary>
    public readonly Image Image;
    
    /// <summary>
    /// The animation metadata, or <c>null</c> for a static image.
    /// </summary>
    public readonly AnimatedImage? Animation;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AtlasEntry"/> struct.
    /// </summary>
    /// <param name="name">The region name.</param>
    /// <param name="image">The source image (the sprite sheet for animations).</param>
    /// <param name="animation">The animation metadata, or <c>null</c> for a static image.</param>
    public AtlasEntry(string name, Image image, AnimatedImage? animation = null) {
        this.Name = name;
        this.Image = image;
        this.Animation = animation;
    }
}