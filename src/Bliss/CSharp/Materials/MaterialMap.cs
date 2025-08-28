using Bliss.CSharp.Colors;
using Bliss.CSharp.Graphics.Pipelines.Textures;
using Bliss.CSharp.Textures;
using Veldrid;

namespace Bliss.CSharp.Materials;

public class MaterialMap {
    
    /// <summary>
    /// Represents a 2D texture used in a material map. This texture can include properties such as
    /// height, width, pixel format, and mip levels. It is used for rendering purposes in a graphics device.
    /// </summary>
    public Texture2D? Texture;

    /// <summary>
    /// Represents a sampler used in a material map for texture sampling operations.
    /// A sampler defines how textures are sampled, including settings such as filtering,
    /// addressing mode, and LOD behavior, which influence rendering outcomes and texture mapping techniques.
    /// </summary>
    public Sampler? Sampler;

    /// <summary>
    /// Represents color information with properties for red, green, blue, and alpha components.
    /// This structure is used for defining the color attributes of various graphical elements such as materials and textures.
    /// </summary>
    public Color? Color;

    /// <summary>
    /// Represents a float value associated with a material map. This value can be used for various purposes such as setting
    /// shader parameters or controlling material properties in a rendering engine.
    /// </summary>
    public float Value;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="MaterialMap"/> class.
    /// </summary>
    /// <param name="texture">The texture associated with the material map. Can be <c>null</c>.</param>
    /// <param name="sampler">The sampler to use with the texture, or <c>null</c> if not used.</param>
    /// <param name="color">The color associated with the material map. Can be <c>null</c>.</param>
    /// <param name="value">The scalar value associated with the material map, defaulting to <c>0.0F</c>.</param>
    public MaterialMap(Texture2D? texture = null, Sampler? sampler = null, Color? color = null, float value = 0.0F) {
        this.Texture = texture;
        this.Sampler = sampler;
        this.Color = color;
        this.Value = value;
    }

    /// <summary>
    /// The texture resource set associated with the specified sampler and texture layout.
    /// </summary>
    /// <param name="sampler">The sampler used for creating the resource set.</param>
    /// <param name="layout">The texture layout used for creating the resource set.</param>
    /// <returns>The resource set associated with the texture, or <c>null</c> if the texture is not available.</returns>
    public ResourceSet? GetTextureResourceSet(Sampler sampler, SimpleTextureLayout layout) {
        return this.Texture?.GetResourceSet(sampler, layout);
    }
}