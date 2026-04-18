using System.Runtime.InteropServices;

namespace Bliss.CSharp.Transformations;

[StructLayout(LayoutKind.Sequential)]
public struct UInt4 : IEquatable<UInt4> {
    
    /// <summary>
    /// The X component of the vector.
    /// </summary>
    public uint X;
    
    /// <summary>
    /// The Y component of the vector.
    /// </summary>
    public uint Y;
    
    /// <summary>
    /// The Z component of the vector.
    /// </summary>
    public uint Z;
    
    /// <summary>
    /// The W component of the vector.
    /// </summary>
    public uint W;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UInt4"/> struct with the specified component values.
    /// </summary>
    /// <param name="x">The X component value.</param>
    /// <param name="y">The Y component value.</param>
    /// <param name="z">The Z component value.</param>
    /// <param name="w">The W component value.</param>
    public UInt4(uint x, uint y, uint z, uint w) {
        this.X = x;
        this.Y = y;
        this.Z = z;
        this.W = w;
    }
    
    /// <summary>
    /// Determines whether two <see cref="UInt4"/> instances are equal.
    /// </summary>
    /// <param name="left">The first vector to compare.</param>
    /// <param name="right">The second vector to compare.</param>
    /// <returns><c>true</c> if the vectors are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(UInt4 left, UInt4 right) => left.Equals(right);
    
    /// <summary>
    /// Determines whether two <see cref="UInt4"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first vector to compare.</param>
    /// <param name="right">The second vector to compare.</param>
    /// <returns><c>true</c> if the vectors are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(UInt4 left, UInt4 right) => !left.Equals(right);
    
    /// <summary>
    /// Indicates whether the current instance is equal to another <see cref="UInt4"/> instance.
    /// </summary>
    /// <param name="other">The vector to compare with the current instance.</param>
    /// <returns><c>true</c> if all components are equal; otherwise, <c>false</c>.</returns>
    public bool Equals(UInt4 other) {
        return this.X.Equals(other.X) && this.Y.Equals(other.Y) && this.Z.Equals(other.Z) && this.W.Equals(other.W);
    }
    
    /// <summary>
    /// Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns><c>true</c> if <paramref name="obj"/> is a <see cref="UInt4"/> with matching component values; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj) {
        return obj is UInt4 other && this.Equals(other);
    }
    
    /// <summary>
    /// Returns a hash code for the current instance.
    /// </summary>
    /// <returns>The hash code of the vector.</returns>
    public override int GetHashCode() {
        return HashCode.Combine(this.X.GetHashCode(), this.Y.GetHashCode(), this.Z.GetHashCode(), this.W.GetHashCode());
    }
}