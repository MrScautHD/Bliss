using System.Numerics;

namespace Bliss.CSharp.Geometry;

public struct BoundingBox : IEquatable<BoundingBox> {

    /// <summary>
    /// Minimum box-corner.
    /// </summary>
    public Vector3 Min;
    
    /// <summary>
    /// Maximum box-corner.
    /// </summary>
    public Vector3 Max;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoundingBox"/> struct with the specified minimum and maximum vectors.
    /// </summary>
    /// <param name="min">The minimum vector defining one corner of the bounding box.</param>
    /// <param name="max">The maximum vector defining the opposite corner of the bounding box.</param>
    public BoundingBox(Vector3 min, Vector3 max) {
        this.Min = min;
        this.Max = max;
    }
    
    /// <summary>
    /// Determines whether two specified bounding boxes are equal.
    /// </summary>
    /// <param name="left">The first bounding box to compare.</param>
    /// <param name="right">The second bounding box to compare.</param>
    /// <returns><c>true</c> if the bounding boxes are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(BoundingBox left, BoundingBox right) => left.Equals(right);
    
    /// <summary>
    /// Determines whether two specified bounding boxes are not equal.
    /// </summary>
    /// <param name="left">The first bounding box to compare.</param>
    /// <param name="right">The second bounding box to compare.</param>
    /// <returns><c>true</c> if the bounding boxes are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(BoundingBox left, BoundingBox right) => !left.Equals(right);
    
    /// <summary>
    /// Determines whether the current bounding box is equal to another bounding box.
    /// </summary>
    /// <param name="other">The bounding box to compare with the current bounding box.</param>
    /// <returns><c>true</c> if the current bounding box is equal to <paramref name="other"/>; otherwise, <c>false</c>.</returns>
    public bool Equals(BoundingBox other) {
        return this.Min.Equals(other.Min) && this.Max.Equals(other.Max);
    }
    
    /// <summary>
    /// Determines whether the specified object is equal to the current bounding box.
    /// </summary>
    /// <param name="obj">The object to compare with the current bounding box.</param>
    /// <returns><c>true</c> if the specified object is a <see cref="BoundingBox"/> equal to the current bounding box; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj) {
        return obj is BoundingBox other && this.Equals(other);
    }
    
    /// <summary>
    /// Returns the hash code for this bounding box.
    /// </summary>
    /// <returns>A hash code for the current bounding box.</returns>
    public override int GetHashCode() {
        return HashCode.Combine(this.Min.GetHashCode(), this.Max.GetHashCode());
    }
    
    /// <summary>
    /// Returns a string that represents the current bounding box.
    /// </summary>
    /// <returns>A string containing the minimum and maximum corners of the bounding box.</returns>
    public override string ToString() {
        return $"Min:{this.Min} Max:{this.Max}";
    }
}