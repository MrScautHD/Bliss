/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

using Bliss.CSharp.Colors;
using Bliss.CSharp.Textures;

namespace Bliss.CSharp.Materials;

public class MaterialMap {
    
    /// <summary>
    /// Represents a 2D texture used in a material map. This texture can include properties such as
    /// height, width, pixel format, and mip levels. It is used for rendering purposes in a graphics device.
    /// </summary>
    public Texture2D? Texture;

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
    /// Initializes a new instance of the <see cref="MaterialMap"/> class with the specified texture, color, and value.
    /// </summary>
    /// <param name="texture">The texture associated with the material map. Can be <c>null</c>.</param>
    /// <param name="color">The color associated with the material map. Can be <c>null</c>.</param>
    /// <param name="value">The scalar value associated with the material map, defaulting to <c>0.0F</c>.</param>
    public MaterialMap(Texture2D? texture = null, Color? color = null, float value = 0.0F) {
        this.Texture = texture;
        this.Color = color;
        this.Value = value;
    }
}