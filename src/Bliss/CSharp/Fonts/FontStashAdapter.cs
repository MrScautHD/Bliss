using System.Numerics;
using Bliss.CSharp.Graphics;
using Bliss.CSharp.Graphics.Rendering.Sprites;
using Bliss.CSharp.Textures;
using FontStashSharp;
using FontStashSharp.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Point = System.Drawing.Point;
using Rectangle = Veldrid.Rectangle;
using SRectangle = System.Drawing.Rectangle;
using Color = Bliss.CSharp.Colors.Color;

namespace Bliss.CSharp.Fonts;

internal class FontStashAdapter : ITexture2DManager, IFontStashRenderer {

    public GraphicsDevice GraphicsDevice { get; private set; }
    public SpriteBatch SpriteBatch { get; private set; }
    public ITexture2DManager TextureManager { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="FontStashAdapter"/> class, associating it with a <see cref="GraphicsDevice"/> and a <see cref="SpriteBatch"/>.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering.</param>
    /// <param name="spriteBatch">The sprite batch used for drawing.</param>
    public FontStashAdapter(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
        this.GraphicsDevice = graphicsDevice;
        this.SpriteBatch = spriteBatch;
        this.TextureManager = this;
    }

    /// <summary>
    /// Creates a new texture with the specified width and height.
    /// </summary>
    /// <param name="width">The width of the new texture.</param>
    /// <param name="height">The height of the new texture.</param>
    /// <returns>A new texture object.</returns>
    public object CreateTexture(int width, int height) {
        return new Texture2D(this.GraphicsDevice, new Image<Rgba32>(width, height));
    }

    /// <summary>
    /// Retrieves the size of the specified texture.
    /// </summary>
    /// <param name="texture">The texture object for which to get the size.</param>
    /// <returns>The size of the texture as a Point, with X representing the width and Y representing the height.</returns>
    public Point GetTextureSize(object texture) {
        Texture2D texture2D = (Texture2D) texture;
        return new Point((int) texture2D.Width, (int) texture2D.Height);
    }

    /// <summary>
    /// Sets the texture data for a specified region within a texture.
    /// </summary>
    /// <param name="texture">The texture object where the data will be set.</param>
    /// <param name="bounds">The bounds within the texture where the data will be applied.</param>
    /// <param name="data">The byte array containing the texture data to be set.</param>
    public void SetTextureData(object texture, SRectangle bounds, byte[] data) {
        Texture2D texture2D = (Texture2D) texture;
        texture2D.UpdateData(data, new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height));
    }

    /// <summary>
    /// Draws a texture at the specified position with given parameters for source rectangle, color, rotation, scale, and depth.
    /// </summary>
    /// <param name="texture">The texture to draw.</param>
    /// <param name="position">The position where the texture will be drawn.</param>
    /// <param name="src">The source rectangle within the texture to draw. If null, the entire texture will be drawn.</param>
    /// <param name="fsColor">The color to apply to the texture.</param>
    /// <param name="rotation">The rotation angle in radians.</param>
    /// <param name="scale">The scale factor to apply to the texture.</param>
    /// <param name="depth">The depth at which to draw the texture.</param>
    public void Draw(object texture, Vector2 position, SRectangle? src, FSColor fsColor, float rotation, Vector2 scale, float depth) {
        Texture2D texture2D = (Texture2D) texture;
        Rectangle? source = src != null ? new Rectangle(src.Value.X, src.Value.Y, src.Value.Width, src.Value.Height) : null;
        Color color = new Color(fsColor.R, fsColor.G, fsColor.B, fsColor.A);

        this.SpriteBatch.DrawTexture(texture2D, SamplerType.Point, position, source, scale, Vector2.Zero, Single.RadiansToDegrees(rotation), color);
    }
}