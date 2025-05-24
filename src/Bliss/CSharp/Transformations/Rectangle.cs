using System.Numerics;

namespace Bliss.CSharp.Transformations;

public struct Rectangle : IEquatable<Rectangle> {

    /// <summary>
    /// The X-coordinate of the rectangle's top-left corner.
    /// </summary>
    public int X;
    
    /// <summary>
    /// The Y-coordinate of the rectangle's top-left corner.
    /// </summary>
    public int Y;
    
    /// <summary>
    /// The width of the rectangle.
    /// </summary>
    public int Width;
    
    /// <summary>
    /// The height of the rectangle.
    /// </summary>
    public int Height;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Rectangle"/> struct with the specified position and size.
    /// </summary>
    /// <param name="x">The X-coordinate of the rectangle's top-left corner.</param>
    /// <param name="y">The Y-coordinate of the rectangle's top-left corner.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    public Rectangle(int x, int y, int width, int height) {
        this.X = x;
        this.Y = y;
        this.Width = width;
        this.Height = height;
    }
    
    /// <summary>
    /// Determines whether two <see cref="Rectangle"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="Rectangle"/> to compare.</param>
    /// <param name="right">The second <see cref="Rectangle"/> to compare.</param>
    /// <returns>True if both rectangles are equal; otherwise, false.</returns>
    public static bool operator ==(Rectangle left, Rectangle right) => left.Equals(right);
    
    /// <summary>
    /// Determines whether two <see cref="Rectangle"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="Rectangle"/> to compare.</param>
    /// <param name="right">The second <see cref="Rectangle"/> to compare.</param>
    /// <returns>True if the rectangles are not equal; otherwise, false.</returns>
    public static bool operator !=(Rectangle left, Rectangle right) => !left.Equals(right);
    
    /// <summary>
    /// Gets or sets the position (X, Y) of the rectangle's top-left corner.
    /// </summary>
    public Vector2 Position {
        get => new Vector2(this.X, this.Y);
        set {
            this.X = (int) value.X;
            this.Y = (int) value.Y;
        }
    }
    
    /// <summary>
    /// Gets or sets the size (Width, Height) of the rectangle.
    /// </summary>
    public Vector2 Size {
        get => new Vector2(this.Width, this.Height);
        set {
            this.Width = (int) value.X;
            this.Height = (int) value.Y;
        }
    }
    
    /// <summary>
    /// Determines whether the specified point is contained within the rectangle.
    /// </summary>
    /// <param name="p">The point to check.</param>
    /// <returns>True if the point is inside the rectangle; otherwise, false.</returns>
    public bool Contains(Vector2 p) => this.Contains(p.X, p.Y);
    
    /// <summary>
    /// Determines whether the specified coordinates are contained within the rectangle.
    /// </summary>
    /// <param name="x">The X-coordinate of the point to check.</param>
    /// <param name="y">The Y-coordinate of the point to check.</param>
    /// <returns>True if the point is inside the rectangle; otherwise, false.</returns>
    public bool Contains(float x, float y) {
        return (this.X <= x && (this.X + this.Width) > x) && (this.Y <= y && (this.Y + this.Height) > y);
    }

    /// <summary>
    /// Determines whether the specified point, transformed by an origin and rotation, is contained within the rectangle.
    /// </summary>
    /// <param name="p">The point to check.</param>
    /// <param name="origin">The origin point for the transformation.</param>
    /// <param name="rotation">The rotation angle, in degrees, applied around the specified origin.</param>
    /// <returns>True if the transformed point is inside the rectangle; otherwise, false.</returns>
    public bool Contains(Vector2 p, Vector2 origin, float rotation) {
        Matrix3x2 rotationMatrix = Matrix3x2.CreateRotation(float.DegreesToRadians(rotation), p);
        Vector2 transform = Vector2.Transform(p - origin, rotationMatrix);
        return this.Contains(transform);
    }
    
    /// <summary>
    /// Determines whether the current rectangle is equal to another <see cref="Rectangle"/>.
    /// </summary>
    /// <param name="other">The rectangle to compare with the current rectangle.</param>
    /// <returns>True if the rectangles are equal; otherwise, false.</returns>
    public bool Equals(Rectangle other) {
        return this.X.Equals(other.X) && this.Y.Equals(other.Y) && this.Width.Equals(other.Width) && this.Height.Equals(other.Height);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current rectangle.
    /// </summary>
    /// <param name="obj">The object to compare with the current rectangle.</param>
    /// <returns>True if the object is a <see cref="Rectangle"/> and is equal to the current rectangle; otherwise, false.</returns>
    public override bool Equals(object? obj) {
        return obj is Rectangle other && this.Equals(other);
    }
    
    /// <summary>
    /// Returns a hash code for the current rectangle.
    /// </summary>
    /// <returns>A hash code for the current rectangle.</returns>
    public override int GetHashCode() {
        return HashCode.Combine(this.X, this.Y, this.Width, this.Height);
    }
    
    /// <summary>
    /// Returns a string that represents the current rectangle.
    /// </summary>
    /// <returns>A string that represents the rectangle's position and size.</returns>
    public override string ToString() {
        return $"X:{this.X} Y:{this.Y} Width:{this.Width} Height:{this.Height}";
    }
}