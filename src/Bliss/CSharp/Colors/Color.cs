using System.Numerics;
using Veldrid;

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
    /// Computes the component-wise remainder (modulo) of two colors, treating each channel separately.
    /// </summary>
    /// <param name="left">The left operand representing the dividend color.</param>
    /// <param name="right">The right operand representing the divisor color.</param>
    /// <returns>A new <see cref="Color"/> representing the component-wise remainder.</returns>
    public static Color operator %(Color left, Color right) {
        byte r = right.R == 0 ? (byte) 255.0F : (byte) (left.R % right.R);
        byte g = right.G == 0 ? (byte) 255.0F : (byte) (left.G % right.G);
        byte b = right.B == 0 ? (byte) 255.0F : (byte) (left.B % right.B);
        byte a = right.A == 0 ? (byte) 255.0F : (byte) (left.A % right.A);
        
        return new Color(r, g, b, a);
    }

    /// <summary>
    /// Adds two colours together piece-wise.
    /// </summary>
    /// <param name="left"> The first color to add.</param>
    /// <param name="right"> The second color to add.</param>
    /// <returns>A <see cref="Color"/> that is the result of adding the two colors together.</returns>
    public static Color operator +(Color left, Color right) {
        byte r = (byte) Math.Min(left.R + right.R, 255.0F);
        byte g = (byte) Math.Min(left.G + right.G, 255.0F);
        byte b = (byte) Math.Min(left.B + right.B, 255.0F);
        byte a = (byte) Math.Min(left.A + right.A, 255.0F);
        
        return new Color(r, g, b, a);
    }

    /// <summary>
    /// Subtracts two colours together piece-wise.
    /// </summary>
    /// <param name="left"> The first color to subtract.</param>
    /// <param name="right"> The second color to subtract.</param>
    /// <returns>A <see cref="Color"/> that is the result of subtracting the two colors together.</returns>
    public static Color operator -(Color left, Color right) {
        byte r = (byte) Math.Max(left.R - right.R, 0.0F);
        byte g = (byte) Math.Max(left.G - right.G, 0.0F);
        byte b = (byte) Math.Max(left.B - right.B, 0.0F);
        byte a = (byte) Math.Max(left.A - right.A, 0.0F);
        
        return new Color(r, g, b, a);
    }

    /// <summary>
    /// Multiplies the color components of two <see cref="Color"/> instances component-wise.
    /// </summary>
    /// <param name="left">The first <see cref="Color"/> instance.</param>
    /// <param name="right">The second <see cref="Color"/> instance.</param>
    /// <returns>A new <see cref="Color"/> resulting from the component-wise multiplication of the two input colors.</returns>
    public static Color operator *(Color left, Color right) {
        byte r = (byte) Math.Clamp((left.R / 255.0F) * (right.R / 255.0F) * 255.0F, 0.0F, 255.0F);
        byte g = (byte) Math.Clamp((left.G / 255.0F) * (right.G / 255.0F) * 255.0F, 0.0F, 255.0F);
        byte b = (byte) Math.Clamp((left.B / 255.0F) * (right.B / 255.0F) * 255.0F, 0.0F, 255.0F);
        byte a = (byte) Math.Clamp((left.A / 255.0F) * (right.A / 255.0F) * 255.0F, 0.0F, 255.0F);
        
        return new Color(r, g, b, a);
    }

    /// <summary>
    /// Implements the division operator for the <see cref="Color"/> structure.
    /// </summary>
    /// <param name="left">The <see cref="Color"/> instance acting as the dividend.</param>
    /// <param name="right">The <see cref="Color"/> instance acting as the divisor.</param>
    /// <returns>A new <see cref="Color"/> resulting from component-wise division of the two colors. If a component in the divisor is zero, the resulting component is clamped to 255.</returns>
    public static Color operator /(Color left, Color right) {
        byte r = right.R == 0 ? (byte) 255.0F : (byte) Math.Clamp((left.R / (float) right.R) * 255.0F, 0.0F, 255.0F);
        byte g = right.G == 0 ? (byte) 255.0F : (byte) Math.Clamp((left.G / (float) right.G) * 255.0F, 0.0F, 255.0F);
        byte b = right.B == 0 ? (byte) 255.0F : (byte) Math.Clamp((left.B / (float) right.B) * 255.0F, 0.0F, 255.0F);
        byte a = right.A == 0 ? (byte) 255.0F : (byte) Math.Clamp((left.A / (float) right.A) * 255.0F, 0.0F, 255.0F);
        
        return new Color(r, g, b, a);
    }
    
    /// <summary>
    /// Creates a new <see cref="Color"/> instance from the specified HSV (Hue, Saturation, Value) model values.
    /// </summary>
    /// <param name="hue">The hue component of the color, in degrees (0-360).</param>
    /// <param name="saturation">The saturation component of the color, as a value between 0 and 1.</param>
    /// <param name="value">The value (brightness) component of the color, as a value between 0 and 1.</param>
    /// <returns>A new <see cref="Color"/> object representing the specified HSV color.</returns>
    public static Color FromHsv(float hue, float saturation, float value) {
        if (saturation == 0.0F) {
            byte gray = (byte) (value * 255.0F);
            return new Color(gray, gray, gray, 255);
        }
        
        float h = hue / 60.0F;
        int i = (int) Math.Floor(h);
        float f = h - i;
        
        float p = value * (1.0F - saturation);
        float q = value * (1.0F - saturation * f);
        float t = value * (1.0F - saturation * (1.0F - f));
        
        float r;
        float g;
        float b;
        
        switch (i % 6) {
            case 0:
                r = value;
                g = t;
                b = p;
                break;
            case 1:
                r = q;
                g = value;
                b = p;
                break;
            case 2:
                r = p;
                g = value;
                b = t;
                break;
            case 3:
                r = p;
                g = q;
                b = value;
                break;
            case 4:
                r = t;
                g = p;
                b = value;
                break;
            default:
                r = value;
                g = p;
                b = q;
                break;
        }
        
        return new Color((byte) (r * 255.0F), (byte) (g * 255.0F), (byte) (b * 255.0F), 255);
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
    /// Converts the current color to its HSV (Hue, Saturation, Value) representation.
    /// </summary>
    /// <returns>A tuple containing the Hue, Saturation, and Value components of the color.</returns>
    public (float Hue, float Saturation, float Value) ToHsv() {
        float r = this.R / 255.0F;
        float g = this.G / 255.0F;
        float b = this.B / 255.0F;
        
        float min = Math.Min(r, Math.Min(g, b));
        float max = Math.Max(r, Math.Max(g, b));
        float delta = max - min;
        
        float hue;
        float saturation;
        float value = max;
        
        const float epsilon = 0.00001F;
        
        if (delta < epsilon) {
            saturation = 0.0F;
            hue = 0.0F;
        }
        else {
            saturation = (max > 0.0F) ? (delta / max) : 0.0F;
            
            if (r >= max) {
                hue = (g - b) / delta;
            }
            else if (g >= max) {
                hue = 2.0F + (b - r) / delta;
            }
            else {
                hue = 4.0F + (r - g) / delta;
            }
            
            hue *= 60.0F;
            
            if (hue < 0.0F) {
                hue += 360.0F;
            }
        }
        
        return (hue, saturation, value);
    }

    /// <summary>
    /// Returns the inverted color, with an option to maintain the alpha component.
    /// </summary>
    /// <param name="keepAlpha">Specifies whether to retain the original alpha value. If set to false, the alpha value will also be inverted.</param>
    /// <returns>A new <see cref="Color"/> instance with the RGB components inverted, and the alpha component either retained or inverted based on the <paramref name="keepAlpha"/> value.</returns>
    public Color Invert(bool keepAlpha = true) {
        byte r = (byte) (255.0F - this.R);
        byte g = (byte) (255.0F - this.G);
        byte b = (byte) (255.0F - this.B);
        byte a = keepAlpha ? this.A : (byte) (255.0F - this.A);
        
        return new Color(r, g, b, a);
    }

    /// <summary>
    /// Adjusts the saturation of the current <see cref="Color"/>.
    /// </summary>
    /// <param name="adjustment">A value between -1 and 1. Negative values decrease saturation, and positive values increase it.</param>
    /// <returns>The <see cref="Color"/> with adjusted saturation.</returns>
    public Color AdjustSaturation(float adjustment) {
        float finalAdjustment = Math.Clamp(adjustment, -1.0F, 1.0F);
        
        // Convert the current color to HSV.
        (float hue, float saturation, float value) = this.ToHsv();
        
        if (finalAdjustment < 0.0F) {
            saturation *= (1.0F + finalAdjustment);
        } else {
            saturation += (1.0F - saturation) * finalAdjustment;
        }
        
        // Return the modified color.
        return FromHsv(hue, Math.Clamp(saturation, 0.0F, 1.0F), value);
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
