/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

namespace Bliss.CSharp.Transformations;

public struct Point : IEquatable<Point> {

    /// <summary>
    /// The X-coordinate of the Point structure.
    /// Represents the horizontal component in a 2D space.
    /// </summary>
    public int X;

    /// <summary>
    /// The Y-coordinate of the Point structure.
    /// Represents the vertical component in a 2D space.
    /// </summary>
    public int Y;

    /// <summary>
    /// Initializes a new instance of the Point class with the specified X and Y coordinates.
    /// </summary>
    /// <param name="x">The X coordinate of the point.</param>
    /// <param name="y">The Y coordinate of the point.</param>
    public Point(int x, int y) {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Determines whether two specified Point instances have the same coordinates.
    /// </summary>
    /// <param name="left">The first Point to compare.</param>
    /// <param name="right">The second Point to compare.</param>
    /// <returns>true if both Point instances have the same X and Y coordinates; otherwise, false.</returns>
    public static bool operator ==(Point left, Point right) => left.Equals(right);

    /// <summary>
    /// Determines whether two specified Point instances have different coordinates.
    /// </summary>
    /// <param name="left">The first Point to compare.</param>
    /// <param name="right">The second Point to compare.</param>
    /// <returns>true if the Point instances do not have the same X and Y coordinates; otherwise, false.</returns>
    public static bool operator !=(Point left, Point right) => !left.Equals(right);

    /// <summary>
    /// Indicates whether this instance and a specified Point object represent the same point.
    /// </summary>
    /// <param name="other">The Point to compare with the current instance.</param>
    /// <returns>true if the specified Point is equal to the current Point; otherwise, false.</returns>
    public bool Equals(Point other) {
        return this.X.Equals(other.X) && this.Y.Equals(other.Y);
    }

    /// <summary>
    /// Determines whether this instance and a specified object represent the same point.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns>true if the specified object is a Point and is equal to the current Point; otherwise, false.</returns>
    public override bool Equals(object? obj) {
        return obj is Point p && this.Equals(p);
    }

    /// <summary>
    /// Returns the hash code for this Point instance.
    /// </summary>
    /// <returns>A hash code for the current Point, which is a unique identifier of the instance.</returns>
    public override int GetHashCode() {
        return HashCode.Combine(this.X.GetHashCode(), this.Y.GetHashCode());
    }
    
    /// <summary>
    /// Returns a string that represents the current Point instance.
    /// </summary>
    /// <returns>A string that contains the X and Y coordinates of the Point instance.</returns>
    public override string ToString() {
        return $"X:{this.X} Y:{this.Y}";
    }
}