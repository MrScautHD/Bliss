using Bliss.CSharp.Images;
using Bliss.CSharp.Textures;
using FontStashSharp.Interfaces;
using Veldrid;
using Point = System.Drawing.Point;
using Rectangle = Bliss.CSharp.Transformations.Rectangle;
using SRectangle = System.Drawing.Rectangle;

namespace Bliss.CSharp.Fonts;

public class FontStashTextureManager : ITexture2DManager {
    
    /// <summary>
    /// The graphics device used to create and manage textures.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="FontStashTextureManager"/> class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for texture operations.</param>
    public FontStashTextureManager(GraphicsDevice graphicsDevice) {
        this.GraphicsDevice = graphicsDevice;
    }
    
    /// <summary>
    /// Creates a new 2D texture with the specified dimensions.
    /// </summary>
    /// <param name="width">The width of the texture.</param>
    /// <param name="height">The height of the texture.</param>
    /// <returns>A new texture object.</returns>
    public object CreateTexture(int width, int height) {
        return new Texture2D(this.GraphicsDevice, new Image(width, height));
    }
    
    /// <summary>
    /// Gets the size of the specified texture.
    /// </summary>
    /// <param name="texture">The texture to query.</param>
    /// <returns>A <see cref="Point"/> representing the width and height of the texture.</returns>
    public Point GetTextureSize(object texture) {
        Texture2D texture2D = (Texture2D) texture;
        return new Point((int) texture2D.Width, (int) texture2D.Height);
    }
    
    /// <summary>
    /// Updates a region of the texture with the provided raw byte data.
    /// </summary>
    /// <param name="texture">The texture to update.</param>
    /// <param name="bounds">The region of the texture to update.</param>
    /// <param name="data">The raw byte data to apply to the texture.</param>
    public void SetTextureData(object texture, SRectangle bounds, byte[] data) {
        Texture2D texture2D = (Texture2D) texture;
        texture2D.SetData(data, new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height));
    }
}