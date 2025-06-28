using System.Numerics;
using Veldrid;
using Bliss.CSharp.Colors.LAB;

namespace Bliss.CSharp.Colors;

public readonly struct Color : IEquatable<Color> {
    
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

    public static readonly Color LightPink = new Color(255, 192, 203, 255);
    public static readonly Color Pink = new Color(255, 182, 193, 255);
    public static readonly Color DarkPink = new Color(231, 84, 128, 255);

    public static readonly Color LightGray = new Color(166, 166, 166, 255);
    public static readonly Color Gray = new Color(128, 128, 128, 255);
    public static readonly Color DarkGray = new Color(64, 64, 64, 255);

    /// <summary>
    /// Represents the red component of the color in the range of 0 to 255.
    /// </summary>
    public readonly byte R;

    /// <summary>
    /// Represents the green component of the color in the range of 0 to 255.
    /// </summary>
    public readonly byte G;

    /// <summary>
    /// Represents the blue component of the color in the range of 0 to 255.
    /// </summary>
    public readonly byte B;

    /// <summary>
    /// Represents the alpha component of the color, indicating its transparency, in the range of 0 to 255.
    /// </summary>
    public readonly byte A;
    
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
    /// Represents a color with red, green, blue, and alpha components.
    /// </summary>
    public Color(RgbaFloat rgbaFloat) {
        this.R = (byte) (rgbaFloat.R * 255.0F);
        this.G = (byte) (rgbaFloat.G * 255.0F);
        this.B = (byte) (rgbaFloat.B * 255.0F);
        this.A = (byte) (rgbaFloat.A * 255.0F);
    }

    /// <summary>
    /// Determines whether two Color objects are equal.
    /// </summary>
    /// <param name="left">The first Color to compare.</param>
    /// <param name="right">The second Color to compare.</param>
    /// <returns><c>true</c> if the specified Color objects are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(Color left, Color right) {
        return left.Equals(right);
    }

    /// <summary>
    /// Represents the equality operator for comparing two colors for equality.
    /// </summary>
    /// <param name="left">The first color to compare.</param>
    /// <param name="right">The second color to compare.</param>
    /// <returns>true if the colors are equal; otherwise, false.</returns>
    public static bool operator !=(Color left, Color right) {
        return !left.Equals(right);
    }

    /// <summary>
    /// Returns a color result of piece-wise modulus by an integer value
    /// </summary>
    /// <param name="color"> The color to modulate.</param>
    /// <param name="mod"> The value to modulate the RGB values by.</param>
    /// <returns>A <see cref="Color"/> that is the result of piece-wise modulo on the color.</returns>
    public static Color operator %(Color color, int mod) {
        if (mod <= 0) {
            throw new ArgumentOutOfRangeException(nameof(mod), "Modulus must be greater than zero.");
        }
        
        byte r = (byte)(color.R % mod);
        byte g = (byte)(color.G % mod);
        byte b = (byte)(color.B % mod);
        byte a = color.A;
        return new Color(r, g, b, a);
    }
    
    /// <summary>
    /// Adds two colours together piece-wise
    /// </summary>
    /// <param name="left"> The first color to add.</param>
    /// <param name="right"> The second color to add.</param>
    /// <returns>A <see cref="Color"/> that is the result of adding the two colors together.</returns>
    public static Color operator +(Color left, Color right) {
        byte r = (byte)Math.Min(left.R + right.R, 255);
        byte g = (byte)Math.Min(left.G + right.G, 255);
        byte b = (byte)Math.Min(left.B + right.B, 255);
        byte a = (byte)Math.Min(left.A + right.A, 255);
        return new Color(r, g, b, a);
    }
    
    /// <summary>
    /// Subtracts two colours together piece-wise
    /// </summary>
    /// <param name="left"> The first color to subtract.</param>
    /// <param name="right"> The second color to subtract.</param>
    /// <returns>A <see cref="Color"/> that is the result of subtracting the two colors together.</returns>
    public static Color operator -(Color left, Color right) {
        byte r = (byte)Math.Max(left.R - right.R, 0);
        byte g = (byte)Math.Max(left.G - right.G, 0);
        byte b = (byte)Math.Max(left.B - right.B, 0);
        byte a = (byte)Math.Max(left.A - right.A, 0);
        return new Color(r, g, b, a);
    }

    /// <summary>
    /// Multiplies two colours together piece-wise
    /// </summary>
    /// <param name="left"> The first color to multiply.</param>
    /// <param name="multiplier"> The value to multiply the color by.</param>
    /// <returns>A <see cref="Color"/> that is the result of multiplying the color by the float value.</returns>
    public static Color operator *(Color left, float multiplier) {
        if (multiplier < 0.001F) {
            return new Color(0, 0, 0, 0);
        }
        byte r = (byte)Math.Min(((byte)(float)left.R * multiplier), 255);
        byte g = (byte)Math.Min(((byte)(float)left.G * multiplier), 255);
        byte b = (byte)Math.Min(((byte)(float)left.B * multiplier), 255);
        byte a = (byte)Math.Min(((byte)(float)left.A * multiplier), 255);
        return new Color(r, g, b, a);
    }
    
    /// <summary>
    /// Divides a color by another color piece-wise
    /// </summary>
    /// <param name="left"> Color Dividend (The color to divide.)</param>
    /// <param name="divisor"> The Divisor (The value to divide by.)</param>
    /// <returns>A <see cref="Color"/> that is the result of dividing the color by the float value.</returns>
    public static Color operator /(Color left, float divisor) {
        if (divisor < 0.001F) {
            return left;
        }
        byte r = (byte)Math.Max(divisor <= 0.001F ? 0 : left.R / divisor, 0);
        byte g = (byte)Math.Max(divisor <= 0.001F ? 0 : left.G / divisor, 0);
        byte b = (byte)Math.Max(divisor <= 0.001F ? 0 : left.B / divisor, 0);
        byte a = (byte)Math.Max(divisor <= 0.001F ? 0 : left.A / divisor, 0);
        return new Color(r, g, b, a);
    }

    /// <summary>
    /// Lab Color based interpolation
    /// </summary>
    /// <param name="left">The first color to blend.</param>
    /// <param name="right">The second color to blend.</param>
    /// <param name="t">The blend factor.
    /// 0.0 is 100% of the first color.
    /// 0.5 is 50% blend between color a and color b.
    /// 1.0 is 100% of the second color.</param>
    /// <returns></returns>
    public static Color Interpolate(Color left, Color right, float t) {
        return LabColor.Interpolate(left, right, t);
    }
    
    /// <summary>
    /// Converts the color to an <see cref="RgbaFloat"/> value.
    /// </summary>
    /// <returns>A new instance of the <see cref="RgbaFloat"/> struct representing the color.</returns>
    public RgbaFloat ToRgbaFloat() {
        return new RgbaFloat(this.R / 255.0F, this.G / 255.0F, this.B / 255.0F, this.A / 255.0F);
    }

    /// <summary>
    /// Converts the color to a <see cref="Vector4"/> representation with each component normalized to the range of 0 to 1.
    /// </summary>
    /// <returns>A <see cref="Vector4"/> object representing the normalized RGBA components of the color.</returns>
    public Vector4 ToRgbaFloatVec4() {
        return new Vector4(this.R / 255.0F, this.G / 255.0F, this.B / 255.0F, this.A / 255.0F);
    }

    /// <summary>
    /// Converts the color to a Vector4 representation.
    /// </summary>
    /// <returns>A Vector4 representing the color.</returns>
    public Vector4 ToVector4() {
        return new Vector4(this.R, this.G, this.B, this.A);
    }
    
    /// <summary>
    /// Inverts the color by subtracting each RGB component from 255.
    /// </summary>
    /// <returns>A <see cref="Color"/> with inverted RGB channels, alpha is unaffected.</returns>
    public Color Invert(bool keepAlpha = true) {
        byte r = (byte)(255 - this.R);
        byte g = (byte)(255 - this.G);
        byte b = (byte)(255 - this.B);
        byte a = keepAlpha ? this.A : (byte)(255 - this.A); // Keep alpha unchanged
        return new Color( r, g, b, a );
    }
    
    /// <summary>
    /// Builds a <see cref="Color"/> from Hue-Saturation-Value (HSV) color model.
    /// </summary>
    /// <param name="Hue"> The hue component, in degrees (0-360).</param>
    /// <param name="Saturation"> The saturation component, in the range of 0 to 1.</param>
    /// <param name="Value"> The value component, in the range of 0 to 1.</param>
    /// <returns>A <see cref="Color"/> made from HSV values.</returns>
    public static Color FromHsv(float Hue, float Saturation, float Value) {
        if (Saturation == 0) {
            return new Color((byte)(Value * 255), (byte)(Value * 255), (byte)(Value * 255), (byte)(255));
        }
        
        float h = Hue % 360;
        float c = Value * Saturation;
        float x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        float m = Value - c;

        float r, g, b;

        if (h < 60) {
            r = c; g = x; b = 0;
        } else if (h < 120) {
            r = x; g = c; b = 0;
        } else if (h < 180) {
            r = 0; g = c; b = x;
        } else if (h < 240) {
            r = 0; g = x; b = c;
        } else if (h < 300) {
            r = x; g = 0; b = c;
        } else {
            r = c; g = 0; b = x;
        }

        return new Color(
            (byte)((r + m) * 255),
            (byte)((g + m) * 255),
            (byte)((b + m) * 255),
            (byte)255
        );
    }
    
    /// <summary>
    /// Calculates the Hue Saturation Value components of a given <see cref="Color"/>.
    /// </summary>
    /// <returns> A tuple containing the Hue, Saturation, and Value of the <see cref="Color"/>.</returns>
    public (float Hue, float Saturation, float Value) ToHsv() {
        float r = this.R / 255.0F;
        float g = this.G / 255.0F;
        float b = this.B / 255.0F;

        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        float delta = max - min;

        float hue = 0;
        if (delta > 0) {
            if (max == r) {
                hue = (g - b) / delta + (g < b ? 6 : 0);
            } else if (max == g) {
                hue = (b - r) / delta + 2;
            } else {
                hue = (r - g) / delta + 4;
            }
            hue *= 60;
        }

        float saturation = max == 0 ? 0 : delta / max;
        float value = max;

        return (hue, saturation, value);
    }
    
    /// <summary>
    /// Desaturates the given <see cref="Color"/>.
    /// </summary>
    /// <param name="amount"> The amount to desaturate the color by, between 0 and 1.</param>
    /// <returns> The desaturated <see cref="Color"/>.</returns>
    public Color Desaturate(float amount) {
        if (amount < 0 || amount > 1) {
            throw new ArgumentOutOfRangeException(nameof(amount), "Desaturation amount must be between 0 and 1.");
        }

        var (hue, saturation, value) = this.ToHsv();
        saturation *= (1 - amount);
        return FromHsv(hue, saturation, value);
    }
    
    /// <summary>
    /// Determines whether the current color object is equal to another color object.
    /// </summary>
    /// <param name="other">The color to compare to.</param>
    /// <returns>
    /// True if the current color object is equal to the other color object; otherwise, false.
    /// </returns>
    public bool Equals(Color other) {
        return this.R.Equals(other.R) && this.G.Equals(other.G) && this.B.Equals(other.B) && this.A.Equals(other.A);
    }

    /// <summary>
    /// Determines whether the current color object is equal to another color object.
    /// </summary>
    /// <param name="obj">The color to compare to.</param>
    /// <returns>
    /// True if the current color object is equal to the other color object; otherwise, false.
    /// </returns>
    public override bool Equals(object? obj) {
        return obj is Color other && this.Equals(other);
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>The hash code for this instance.</returns>
    public override int GetHashCode() {
        return HashCode.Combine(this.R, this.G, this.B, this.A);
    }

    /// <summary>
    /// Returns a string that represents the current color.
    /// </summary>
    /// <returns>A string representation of the current color.</returns>
    public override string ToString() {
        return $"R:{this.R}, G:{this.G}, B:{this.B}, A:{this.A}";
    }
}