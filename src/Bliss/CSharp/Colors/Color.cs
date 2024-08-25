using System.Numerics;
using Veldrid;

namespace Bliss.CSharp.Colors;

public struct Color : IEquatable<Color> {
    
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
    /// Determines whether two Color objects are equal.
    /// </summary>
    /// <param name="left">The first Color to compare.</param>
    /// <param name="right">The second Color to compare.</param>
    /// <returns>
    /// <c>true</c> if the specified Color objects are equal; otherwise, <c>false</c>.
    /// </returns>
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
    /// Converts the color to an RgbaFloat value.
    /// </summary>
    /// <returns>A new instance of the RgbaFloat struct representing the color.</returns>
    public readonly RgbaFloat ToRgbaFloat() {
        return new RgbaFloat(this.R / 255.0F, this.G / 255.0F, this.B / 255.0F, this.A / 255.0F);
    }

    /// <summary>
    /// Converts the color to a Vector4 representation.
    /// </summary>
    /// <returns>A Vector4 representing the color.</returns>
    public Vector4 ToVector4() {
        return new Vector4(this.R, this.G, this.B, this.A);
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