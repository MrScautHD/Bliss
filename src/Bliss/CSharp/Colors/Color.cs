namespace Bliss.CSharp.Colors;

public struct Color {
    
    public static readonly Color White = new Color(255, 255, 255, 255);
    public static readonly Color Black = new Color(0, 0, 0, 255);
    
    public static readonly Color LightRed = new Color(255, 102, 102, 255);
    public static readonly Color Red = new Color(255, 0, 0, 255);
    public static readonly Color DarkRed = new Color(139, 0, 0, 255);

    public static readonly Color LightGreen = new Color(144, 238, 144, 255);
    public static readonly Color Green = new Color(0, 255, 0, 255);
    public static readonly Color DarkGreen = new Color(0, 100, 0, 255);

    public static readonly Color LightBlue = new Color(173, 216, 230, 255);
    public static readonly Color Blue = new Color(0, 0, 255, 255);
    public static readonly Color DarkBlue = new Color(0, 0, 139, 255);

    public static readonly Color LightYellow = new Color(255, 255, 153, 255);
    public static readonly Color Yellow = new Color(255, 255, 0, 255);
    public static readonly Color DarkYellow = new Color(204, 204, 0, 255);

    public static readonly Color LightCyan = new Color(224, 255, 255, 255);
    public static readonly Color Cyan = new Color(0, 255, 255, 255);
    public static readonly Color DarkCyan = new Color(0, 139, 139, 255);

    public static readonly Color LightMagenta = new Color(255, 153, 255, 255);
    public static readonly Color Magenta = new Color(255, 0, 255, 255);
    public static readonly Color DarkMagenta = new Color(139, 0, 139, 255);

    public static readonly Color LightOrange = new Color(255, 200, 0, 255);
    public static readonly Color Orange = new Color(255, 165, 0, 255);
    public static readonly Color DarkOrange = new Color(255, 140, 0, 255);

    public static readonly Color LightBrown = new Color(205, 133, 63, 255);
    public static readonly Color Brown = new Color(165, 42, 42, 255);
    public static readonly Color DarkBrown = new Color(101, 67, 33, 255);

    public static readonly Color LightPurple = new Color(147, 112, 219, 255);
    public static readonly Color Purple = new Color(128, 0, 128, 255);
    public static readonly Color DarkPurple = new Color(75, 0, 130, 255);

    public static readonly Color LightPink = new Color(255, 182, 193, 255);
    public static readonly Color Pink = new Color(255, 192, 203, 255);
    public static readonly Color DarkPink = new Color(231, 84, 128, 255);

    public static readonly Color LightGray = new Color(211, 211, 211, 255);
    public static readonly Color Gray = new Color(128, 128, 128, 255);
    public static readonly Color DarkGray = new Color(169, 169, 169, 255);
    
    public float R;
    public float G;
    public float B;
    public float A;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Color"/> class.
    /// </summary>
    /// <param name="r">The red component.</param>
    /// <param name="g">The green component.</param>
    /// <param name="b">The blue component.</param>
    /// <param name="a">The alpha component.</param>
    public Color(byte r, byte g, byte b, byte a) {
        this.R = r;
        this.G = g;
        this.B = b;
        this.A = a;
    }
}