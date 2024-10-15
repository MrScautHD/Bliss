using Bliss.CSharp.Colors;
using Bliss.CSharp.Textures;

namespace Bliss.CSharp.Materials;

public struct MaterialMap {
    
    /// <summary>
    /// Represents the texture map of a material.
    /// </summary>
    public Texture2D Texture;

    /// <summary>
    /// Defines the color properties of a material.
    /// </summary>
    public Color Color;

    /// <summary>
    /// Represents a numeric value associated with the material.
    /// </summary>
    public float Value;
}