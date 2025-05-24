using System.Numerics;
using Bliss.CSharp.Graphics.Rendering.Batches.Sprites;
using Bliss.CSharp.Textures;
using FontStashSharp;
using FontStashSharp.Interfaces;
using Veldrid;
using Rectangle = Bliss.CSharp.Transformations.Rectangle;
using SRectangle = System.Drawing.Rectangle;
using Color = Bliss.CSharp.Colors.Color;

namespace Bliss.CSharp.Fonts;

public class FontStashRenderer2D : Disposable, IFontStashRenderer {
    
    /// <summary>
    /// The graphics device used for rendering.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// The sprite batch used to draw font glyphs.
    /// </summary>
    public SpriteBatch SpriteBatch { get; private set; }
    
    /// <summary>
    /// Manages the texture used by the font renderer.
    /// </summary>
    public ITexture2DManager TextureManager { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="FontStashRenderer2D"/> class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering.</param>
    /// <param name="spriteBatch">The sprite batch used to draw textures.</param>
    public FontStashRenderer2D(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
        this.GraphicsDevice = graphicsDevice;
        this.SpriteBatch = spriteBatch;
        this.TextureManager = new FontStashTextureManager(graphicsDevice);
    }
    
    /// <summary>
    /// Draws a font glyph texture at the specified position with the given rendering parameters.
    /// </summary>
    /// <param name="texture">The texture object containing the glyph.</param>
    /// <param name="position">The screen position at which to render the texture.</param>
    /// <param name="src">An optional source rectangle within the texture to draw.</param>
    /// <param name="fsColor">The color to apply to the texture.</param>
    /// <param name="rotation">The rotation to apply, in radians.</param>
    /// <param name="scale">The scaling factor to apply to the texture.</param>
    /// <param name="depth">The depth value for draw ordering.</param>
    public void Draw(object texture, Vector2 position, SRectangle? src, FSColor fsColor, float rotation, Vector2 scale, float depth) {
        Texture2D texture2D = (Texture2D) texture;
        Rectangle? source = src != null ? new Rectangle(src.Value.X, src.Value.Y, src.Value.Width, src.Value.Height) : null;
        Color color = new Color(fsColor.R, fsColor.G, fsColor.B, fsColor.A);
        
        this.SpriteBatch.DrawTexture(texture2D, position, depth, source, scale, Vector2.Zero, float.RadiansToDegrees(rotation), color);
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            ((FontStashTextureManager) this.TextureManager).Dispose();
        }
    }
}