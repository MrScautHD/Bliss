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
    /// The default effect used for rendering 3D models.
    /// </summary>
    public static Effect DefaultModelEffect { get; private set; }
    
    /// <summary>
    /// The default texture used for rendering 3D models.
    /// </summary>
    public static Texture2D DefaultModelTexture { get; private set; }

    /// <summary>
    /// Initializes global resources.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device to be used for resource creation and rendering.</param>
    public static void Init(GraphicsDevice graphicsDevice) {
        GraphicsDevice = graphicsDevice;
        
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
        DefaultModelEffect.Dispose();
        DefaultModelTexture.Dispose();
    }
}