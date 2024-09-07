using System.Numerics;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Graphics.Rendering.Sprites;
using Bliss.CSharp.Logging;
using FontStashSharp;
using Veldrid;

namespace Bliss.CSharp.Fonts;

public class Font : Disposable {

    public byte[] FontData { get; private set; }

    private FontSystem _fontSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="Font"/> class, loading font data from the specified file path.
    /// </summary>
    /// <param name="path">The file path to the font data.</param>
    public Font(string path) {
        this.FontData = this.LoadFontData(path);
        this._fontSystem = this.CreateFontSystem();
    }

    /// <summary>
    /// Draws text using the specified parameters.
    /// </summary>
    /// <param name="batch">The sprite batch used for rendering.</param>
    /// <param name="text">The text to be drawn.</param>
    /// <param name="position">The position where the text should be drawn.</param>
    /// <param name="size">The size of the font.</param>
    /// <param name="characterSpacing">The spacing between characters. Default is 0.0F.</param>
    /// <param name="lineSpacing">The spacing between lines. Default is 0.0F.</param>
    /// <param name="scale">The scaling factor for the text. Default is null.</param>
    /// <param name="origin">The origin for rotation and scaling. Default is null.</param>
    /// <param name="rotation">The rotation angle for the text. Default is 0.0F.</param>
    /// <param name="color">The color of the text. Default is white.</param>
    /// <param name="style">The style of the text. Default is none.</param>
    /// <param name="effect">The effect to apply to the text. Default is none.</param>
    /// <param name="effectAmount">The intensity of the applied effect. Default is 0.</param>
    public void Draw(SpriteBatch batch, string text, Vector2 position, int size, float characterSpacing = 0.0F, float lineSpacing = 0.0F, Vector2? scale = null, Vector2? origin = null, float rotation = 0.0F, Color? color = null, TextStyle style = TextStyle.None, FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0) {
        Color finalColor = color ?? Color.White;
        Vector2 textSize = this.MeasureText(text, size);
        Vector2 finalOrigin = origin ?? new Vector2(position.X / 2 - (textSize.X / 2), position.Y / 2 - (textSize.Y / 2));
        
        this._fontSystem.GetFont(size).DrawText(batch.FontStashAdapter, text, position, new FSColor(finalColor.R, finalColor.G, finalColor.B, finalColor.A), rotation, finalOrigin, scale, default, characterSpacing, lineSpacing, style, effect, effectAmount);
    }

    /// <summary>
    /// Measures the dimensions of the specified text using the given font size.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="size">The font size to use for measurement.</param>
    /// <returns>A Vector2 representing the width and height of the text.</returns>
    public Vector2 MeasureText(string text, int size) {
        DynamicSpriteFont font = this._fontSystem.GetFont(size);

        Bounds bounds = font.TextBounds(text, Vector2.Zero);
        return new Vector2(bounds.X2, bounds.Y2);
    }

    /// <summary>
    /// Measures the dimensions of the specified trimmed text using the given font size.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="size">The size of the font.</param>
    /// <returns>A <see cref="Vector2"/> representing the width and height of the trimmed text.</returns>
    public Vector2 MeasureTextTrimmed(string text, int size) {
        DynamicSpriteFont font = this._fontSystem.GetFont(size);

        Bounds bounds = font.TextBounds(text, Vector2.Zero);
        return new Vector2(bounds.X2 - bounds.X, bounds.Y2 - bounds.Y);
    }

    /// <summary>
    /// Measures the rectangular bounds of the given text with specified font size.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="size">The size of the font.</param>
    /// <return>The bounds of the text as a Rectangle.</return>
    public Rectangle MeasureTextRect(string text, int size) {
        DynamicSpriteFont font = this._fontSystem.GetFont(size);

        Bounds bounds = font.TextBounds(text, Vector2.Zero);
        return new Rectangle((int) bounds.X, (int) bounds.Y, (int) bounds.X2, (int) bounds.Y2);
    }

    /// <summary>
    /// Loads font data from a specified file path.
    /// </summary>
    /// <param name="path">The path to the font file (.ttf) to be loaded.</param>
    /// <returns>A byte array containing the font data.</returns>
    /// <exception cref="ApplicationException">Thrown if the file does not exist or is not a .ttf file.</exception>
    private byte[] LoadFontData(string path) {
        if (!File.Exists(path)) {
            throw new ApplicationException($"No font file found in the path: [{path}]");
        }
        
        Logger.Info($"Successfully loaded font data from the path: [{path}]");
        return File.ReadAllBytes(path);
    }

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="FontSystem"/> class using the current font data.
    /// </summary>
    /// <returns>A newly created <see cref="FontSystem"/> instance.</returns>
    private FontSystem CreateFontSystem() {
        FontSystem system = new FontSystem(new FontSystemSettings());
        system.AddFont(this.FontData);

        return system;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._fontSystem.Dispose();
        }
    }
}