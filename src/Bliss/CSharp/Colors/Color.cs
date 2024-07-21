using System.Numerics;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="Color"/> class using HSV values.
    /// </summary>
    /// <param name="hue">The hue component, in degrees (0-360).</param>
    /// <param name="saturation">The saturation component, as a percentage (0-100).</param>
    /// <param name="value">The value (brightness) component, as a percentage (0-100).</param>
    public Color(float hue, float saturation, float value) {
        hue /= 360;
        saturation /= 100;
        value /= 100;
    
        float chroma = value * saturation;
        float hue2 = hue * 6f;
        float x = chroma * (1 - Math.Abs( (hue2 % 2) - 1));
        float r = 0, g = 0, b = 0;
    
        if( 0 <= hue2 && hue2 < 1) {
            r = chroma;
            g = x;
            b = 0;
        }
        else if( 1 <= hue2 && hue2 < 2) {
            r = x;
            g = chroma;
            b = 0;
        }
        else if( 2 <= hue2 && hue2 < 3) {
            r = 0;
            g = chroma;
            b = x;
        }    
        else if( 3 <= hue2 && hue2 < 4) {
            r = 0;
            g = x;
            b = chroma;
        }    
        else if( 4 <= hue2 && hue2 < 5) {
            r = x;
            g = 0;
            b = chroma;
        }       
        else if( 5 <= hue2 && hue2 < 6) {
            r = chroma;
            g = 0;
            b = x;
        }   
    
        float m = value - chroma;
        this.R = (r + m) * 255;
        this.G = (g + m) * 255;
        this.B = (b + m) * 255;
        this.A = 255;
    }

    /// <summary>
    /// Converts the color from RGB to HSV (Hue, Saturation, Value) format.
    /// </summary>
    /// <returns>A <see cref="Vector3"/> representing the HSV values, where:
    /// - X is Hue (0-360 degrees),
    /// - Y is Saturation (0-100 percent),
    /// - Z is Value (0-100 percent).</returns>
    public Vector3 GetHsv() {
        float r = this.R / 255.0f;
        float g = this.G / 255.0f;
        float b = this.B / 255.0f;

        float max = Math.Max(Math.Max(r, g), b);
        float min = Math.Min(Math.Min(r, g), b);
        
        float h = max;
        float s = max;
        float v = max;
        
        float diff = max - min;

        s = max == 0.0 ? 0 : diff / max;

        if (max == min) {
            h = 0;
        }
        else {
            if (max == r) {
                h = (g - b) / diff + (g < b ? 6 : 0);
            }
            else if (max == g) {
                h = (b - r) / diff + 2;
            }
            else if (max == b) {
                h = (r - g) / diff + 4;
            }
            
            h /= 6.0f;
        }
        
        return new Vector3(h * 360, s * 100, v * 100);
    }
}