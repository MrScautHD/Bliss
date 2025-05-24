using System.Numerics;

namespace Bliss.CSharp.Transformations;

public struct RectangleF : IEquatable<RectangleF> {

    /// <summary>
    /// The X-coordinate of the rectangle's top-left corner.
    /// </summary>
    public float X;
    
    /// <summary>
    /// The Y-coordinate of the rectangle's top-left corner.
    /// </summary>
    public float Y;
    
    /// <summary>
    /// The width of the rectangle.
    /// </summary>
    public float Width;
    
    /// <summary>
    /// The height of the rectangle.
    /// </summary>
    public float Height;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="RectangleF"/> struct with the specified position and size.
    /// </summary>
    /// <param name="x">The X-coordinate of the rectangle's top-left corner.</param>
    /// <param name="y">The Y-coordinate of the rectangle's top-left corner.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    public RectangleF(float x, float y, float width, float height) {
        this.X = x;
        this.Y = y;
        this.Width = width;
        this.Height = height;
    }
    
    /// <summary>
    /// Determines whether two <see cref="RectangleF"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="RectangleF"/> to compare.</param>
    /// <param name="right">The second <see cref="RectangleF"/> to compare.</param>
    /// <returns>True if both rectangles are equal; otherwise, false.</returns>
    public static bool operator ==(RectangleF left, RectangleF right) => left.Equals(right);
    
    /// <summary>
    /// Determines whether two <see cref="RectangleF"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="RectangleF"/> to compare.</param>
    /// <param name="right">The second <see cref="RectangleF"/> to compare.</param>
    /// <returns>True if the rectangles are not equal; otherwise, false.</returns>
    public static bool operator !=(RectangleF left, RectangleF right) => !left.Equals(right);
    
    /// <summary>
    /// Gets or sets the position (X, Y) of the rectangle's top-left corner.
    /// </summary>
    public Vector2 Position {
        get => new Vector2(this.X, this.Y);
        set {
            this.X = value.X;
            this.Y = value.Y;
        }
    }
    
    /// <summary>
    /// Gets or sets the size (Width, Height) of the rectangle.
    /// </summary>
    public Vector2 Size {
        get => new Vector2(this.Width, this.Height);
        set {
            this.Width = value.X;
            this.Height = value.Y;
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
    /// Determines whether a specified point is contained within a rotated rectangle relative to a given origin.
    /// </summary>
    /// <param name="p">The point that is being checked for containment within the rectangle.</param>
    /// <param name="origin">The origin point relative to which the rotation is applied.</param>
    /// <param name="rotation">The rotation angle, in degrees, applied counterclockwise to the rectangle.</param>
    /// <returns>True if the point is contained within the rotated rectangle; otherwise, false.</returns>
    public bool Contains(Vector2 p, Vector2 origin, float rotation) {
        Matrix4x4 rotationMatrix = Matrix4x4.CreateRotationZ(float.DegreesToRadians(-rotation));
        Vector2 localPoint = Vector2.Transform(p - this.Position, rotationMatrix) + origin;

        return localPoint.X >= 0 && localPoint.X <= this.Width && localPoint.Y >= 0 && localPoint.Y <= this.Height;
    }
    
    /// <summary>
    /// Determines whether the current rectangle is equal to another <see cref="RectangleF"/>.
    /// </summary>
    /// <param name="other">The rectangle to compare with the current rectangle.</param>
    /// <returns>True if the rectangles are equal; otherwise, false.</returns>
    public bool Equals(RectangleF other) {
        return this.X.Equals(other.X) && this.Y.Equals(other.Y) && this.Width.Equals(other.Width) && this.Height.Equals(other.Height);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current rectangle.
    /// </summary>
    /// <param name="obj">The object to compare with the current rectangle.</param>
    /// <returns>True if the object is a <see cref="RectangleF"/> and is equal to the current rectangle; otherwise, false.</returns>
    public override bool Equals(object? obj) {
        return obj is RectangleF other && this.Equals(other);
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