/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Textures;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;

namespace Bliss.CSharp;

public static class GlobalResource {
    
    /// <summary>
    /// Provides access to the global graphics device used for rendering operations.
    /// </summary>
    public static GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// Gets the default <see cref="Effect"/> used for rendering sprites.
    /// </summary>
    public static Effect DefaultSpriteEffect { get; private set; }
    
    /// <summary>
    /// Gets the <see cref="Effect"/> used for rendering primitive shapes.
    /// </summary>
    public static Effect PrimitiveEffect { get; private set; }
    
    /// <summary>
    /// The default <see cref="Effect"/> used for rendering 3D models.
    /// </summary>
    public static Effect DefaultModelEffect { get; private set; }
    
    /// <summary>
    /// The default <see cref="Texture2D"/> used for rendering 3D models.
    /// </summary>
    public static Texture2D DefaultModelTexture { get; private set; }

    /// <summary>
    /// Initializes global resources.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device to be used for resource creation and rendering.</param>
    public static void Init(GraphicsDevice graphicsDevice) {
        GraphicsDevice = graphicsDevice;
        
        // Default sprite effect.
        DefaultSpriteEffect = new Effect(graphicsDevice, SpriteVertex2D.VertexLayout, "content/shaders/sprite.vert", "content/shaders/sprite.frag");
        
        // Primitive effect.
        PrimitiveEffect = new Effect(graphicsDevice, PrimitiveVertex2D.VertexLayout, "content/shaders/primitive.vert", "content/shaders/primitive.frag");
        
        // Default model effect.
        DefaultModelEffect = new Effect(graphicsDevice, Vertex3D.VertexLayout, "content/shaders/default_model.vert", "content/shaders/default_model.frag");
        
        // Default model texture.
        using (Image<Rgba32> image = new Image<Rgba32>(1, 1, new Rgba32(128, 128, 128, 255))) {
            DefaultModelTexture = new Texture2D(graphicsDevice, image);
        }
    }

    /// <summary>
    /// Releases and disposes of all global resources.
    /// </summary>
    public static void Destroy() {
        DefaultSpriteEffect.Dispose();
        PrimitiveEffect.Dispose();
        DefaultModelEffect.Dispose();
        DefaultModelTexture.Dispose();
    }
}