/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

namespace Bliss.CSharp.Graphics.Rendering.Batches.Sprites;

public enum SpriteFlip {
    
    /// <summary>
    /// No flipping applied to the sprite.
    /// </summary>
    None,
    
    /// <summary>
    /// The sprite is flipped vertically.
    /// </summary>
    Vertical,
    
    /// <summary>
    /// The sprite is flipped horizontally.
    /// </summary>
    Horizontal,
    
    /// <summary>
    /// The sprite is flipped both vertically and horizontally.
    /// </summary>
    Both
}