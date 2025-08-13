using System.Numerics;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Graphics.Rendering.Renderers.Batches.Sprites;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Transformations;
using FontStashSharp;

namespace Bliss.CSharp.Fonts;

public class Font : Disposable {
    
    /// <summary>
    /// Gets the byte array containing the raw font data.
    /// </summary>
    public byte[] FontData { get; private set; }

    /// <summary>
    /// The font system used to manage and render fonts for the <see cref="Font"/> class.
    /// </summary>
    public FontSystem FontSystem { get; private set; }

    /// <summary>
    /// Creates a new <see cref="Font"/> instance by loading font data from the specified file path.
    /// </summary>
    /// <param name="path">The file path to the font file to load.</param>
    /// <param name="settings">Settings used to configure the internal font system.</param>
    public Font(string path, FontSystemSettings? settings = null) : this(LoadFontData(path), settings) { }

    /// <summary>
    /// Creates a new <see cref="Font"/> instance using the provided font data in memory.
    /// </summary>
    /// <param name="data">A byte array containing the raw font data.</param>
    /// <param name="settings">Settings used to configure the internal font system.</param>
    public Font(byte[] data, FontSystemSettings? settings = null) {
        this.FontData = data;
        this.FontSystem = new FontSystem(settings ?? new FontSystemSettings());
        this.FontSystem.AddFont(this.FontData);
    }
    
    /// <summary>
    /// Loads font data from a specified file path.
    /// </summary>
    /// <param name="path">The path to the font file (.ttf) to be loaded.</param>
    /// <returns>A byte array containing the font data.</returns>
    /// <exception cref="ApplicationException">Thrown if the file does not exist or is not a .ttf file.</exception>
    private static byte[] LoadFontData(string path) {
        if (!File.Exists(path)) {
            throw new Exception($"No font file found in path: [{path}]");
        }

        if (Path.GetExtension(path) != ".ttf") {
            throw new Exception($"This font type is not supported: [{Path.GetExtension(path)}]");
        }
        
        Logger.Info($"Font data loaded successfully from path: [{path}]");
        return File.ReadAllBytes(path);
    }
    
    /// <summary>
    /// Retrieves a <see cref="DynamicSpriteFont"/> object for the specified font size.
    /// </summary>
    /// <param name="fontSize">The desired font size for the retrieved sprite font.</param>
    /// <returns>A <see cref="DynamicSpriteFont"/> instance corresponding to the specified font size.</returns>
    public DynamicSpriteFont GetSpriteFont(float fontSize) {
        return this.FontSystem.GetFont(fontSize);
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
    /// <param name="depth">The depth at which the text will be rendered. Default is 0.5F.</param>
    /// <param name="origin">The origin for rotation and scaling. Default is null.</param>
    /// <param name="rotation">The rotation angle for the text. Default is 0.0F.</param>
    /// <param name="color">The color of the text. Default is white.</param>
    /// <param name="style">The style of the text. Default is none.</param>
    /// <param name="effect">The effect to apply to the text. Default is none.</param>
    /// <param name="effectAmount">The intensity of the applied effect. Default is 0.</param>
    public void Draw(SpriteBatch batch, string text, Vector2 position, float size, float characterSpacing = 0.0F, float lineSpacing = 0.0F, Vector2? scale = null, float depth = 0.5F, Vector2? origin = null, float rotation = 0.0F, Color? color = null, TextStyle style = TextStyle.None, FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0) {
        Color finalColor = color ?? Color.White;
        Vector2 finalOrigin = origin ?? new Vector2(0.0F, 0.0F);
        
        // Draw font.
        this.GetSpriteFont(size).DrawText(batch.FontStashRenderer, text, position, new FSColor(finalColor.R, finalColor.G, finalColor.B, finalColor.A), float.DegreesToRadians(rotation), finalOrigin, scale, depth, characterSpacing, lineSpacing, style, effect, effectAmount);
    }

    /// <summary>
    /// Measures the dimensions of the specified text using the given font size and other parameters.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="size">The font size to use for measurement.</param>
    /// <param name="scale">The scale to apply to the text, if any.</param>
    /// <param name="characterSpacing">Spacing between characters, in pixels.</param>
    /// <param name="lineSpacing">Spacing between lines, in pixels.</param>
    /// <param name="effect">Special visual effect to apply when measuring text.</param>
    /// <param name="effectAmount">The intensity of the specified visual effect.</param>
    /// <returns>A Vector2 representing the width and height of the text.</returns>
    public Vector2 MeasureText(string text, float size, Vector2? scale = null, float characterSpacing = 0.0F, float lineSpacing = 0.0F, FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0) {
        Bounds bounds = this.GetSpriteFont(size).TextBounds(text, Vector2.Zero, scale, characterSpacing, lineSpacing, effect, effectAmount);
        return new Vector2(bounds.X2, bounds.Y2);
    }

    /// <summary>
    /// Measures the dimensions of the specified trimmed text using the provided font size.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="size">The size of the font.</param>
    /// <param name="scale">An optional scaling factor for the text. Defaults to null.</param>
    /// <param name="characterSpacing">The spacing between characters. Defaults to 0.0.</param>
    /// <param name="lineSpacing">The spacing between lines. Defaults to 0.0.</param>
    /// <param name="effect">The text rendering effect to apply. Defaults to <see cref="FontSystemEffect.None"/>.</param>
    /// <param name="effectAmount">The magnitude of the effect to apply. Defaults to 0.</param>
    /// <returns>A <see cref="Vector2"/> representing the width and height of the trimmed text.</returns>
    public Vector2 MeasureTextTrimmed(string text, float size, Vector2? scale = null, float characterSpacing = 0.0F, float lineSpacing = 0.0F, FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0) {
        Bounds bounds = this.GetSpriteFont(size).TextBounds(text, Vector2.Zero, scale, characterSpacing, lineSpacing, effect, effectAmount);
        return new Vector2(bounds.X2 - bounds.X, bounds.Y2 - bounds.Y);
    }

    /// <summary>
    /// Measures the rectangular bounds of the specified text with the given font size and additional text settings.
    /// </summary>
    /// <param name="text">The text to be measured.</param>
    /// <param name="size">The size of the font used for measurement.</param>
    /// <param name="scale">Optional scale to be applied to the text. Defaults to null for no scaling.</param>
    /// <param name="characterSpacing">Optional additional spacing between characters. Defaults to 0.0.</param>
    /// <param name="lineSpacing">Optional additional spacing between lines. Defaults to 0.0.</param>
    /// <param name="effect">Optional font effect to apply. Defaults to <see cref="FontSystemEffect.None"/>.</param>
    /// <param name="effectAmount">Optional intensity of the font effect. Defaults to 0.</param>
    /// <returns>A <see cref="RectangleF"/> representing the bounds of the measured text.</returns>
    public RectangleF MeasureTextRect(string text, float size, Vector2? scale = null, float characterSpacing = 0.0F, float lineSpacing = 0.0F, FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0) {
        Bounds bounds = this.GetSpriteFont(size).TextBounds(text, Vector2.Zero, scale, characterSpacing, lineSpacing, effect, effectAmount);
        return new RectangleF(bounds.X, bounds.Y, bounds.X2, bounds.Y2);
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.FontSystem.Dispose();
        }
    }
}