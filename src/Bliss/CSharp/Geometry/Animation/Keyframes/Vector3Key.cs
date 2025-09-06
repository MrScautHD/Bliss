using System.Numerics;

namespace Bliss.CSharp.Geometry.Animation.Keyframes;

public struct Vector3Key : IEquatable<Vector3Key> {

    /// <summary>
    /// The time at which this keyframe occurs.
    /// </summary>
    public double Time;
    
    /// <summary>
    /// The 3D vector value (e.g., position, rotation) at the specified time.
    /// </summary>
    public Vector3 Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Vector3Key"/> struct with the specified time and value.
    /// </summary>
    /// <param name="time">The time of the keyframe.</param>
    /// <param name="value">The 3D vector value at the specified time.</param>
    public Vector3Key(double time, Vector3 value) {
        this.Time = time;
        this.Value = value;
    }
    
    /// <summary>
    /// Compares two <see cref="Vector3Key"/> instances for equality based on their value.
    /// </summary>
    /// <param name="left">The first <see cref="Vector3Key"/> to compare.</param>
    /// <param name="right">The second <see cref="Vector3Key"/> to compare.</param>
    /// <returns>Returns true if the value of both keys are equal, otherwise false.</returns>
    public static bool operator ==(Vector3Key left, Vector3Key right) => left.Value == right.Value;

    /// <summary>
    /// Compares two <see cref="Vector3Key"/> instances for inequality based on their value.
    /// </summary>
    /// <param name="left">The first <see cref="Vector3Key"/> to compare.</param>
    /// <param name="right">The second <see cref="Vector3Key"/> to compare.</param>
    /// <returns>Returns true if the value of both keys are not equal, otherwise false.</returns>
    public static bool operator !=(Vector3Key left, Vector3Key right) => left.Value != right.Value;

    /// <summary>
    /// Compares two <see cref="Vector3Key"/> instances to see if the first occurs before the second based on their time.
    /// </summary>
    /// <param name="left">The first <see cref="Vector3Key"/> to compare.</param>
    /// <param name="right">The second <see cref="Vector3Key"/> to compare.</param>
    /// <returns>Returns true if the first key occurs before the second key in time.</returns>
    public static bool operator <(Vector3Key left, Vector3Key right) => left.Time < right.Time;
    
    /// <summary>
    /// Compares two <see cref="Vector3Key"/> instances to see if the first occurs after the second based on their time.
    /// </summary>
    /// <param name="left">The first <see cref="Vector3Key"/> to compare.</param>
    /// <param name="right">The second <see cref="Vector3Key"/> to compare.</param>
    /// <returns>Returns true if the first key occurs after the second key in time.</returns>
    public static bool operator >(Vector3Key left, Vector3Key right) => left.Time > right.Time;
    
    /// <summary>
    /// Compares this <see cref="Vector3Key"/> to another for equality based on their value.
    /// </summary>
    /// <param name="other">The other <see cref="Vector3Key"/> to compare.</param>
    /// <returns>Returns true if the value of both keys are equal, otherwise false.</returns>
    public bool Equals(Vector3Key other) {
        return this.Value == other.Value;
    }

    /// <summary>
    /// Compares this <see cref="Vector3Key"/> to another object for equality.
    /// </summary>
    /// <param name="obj">The object to compare to.</param>
    /// <returns>Returns true if the object is a <see cref="Vector3Key"/> and has the same value.</returns>
    public override bool Equals(object? obj) {
        return obj is Vector3Key other && this.Equals(other);
    }
    
    /// <summary>
    /// Generates a hash code based on the value of the <see cref="Vector3Key"/>.
    /// </summary>
    /// <returns>The hash code for this key.</returns>
    public override int GetHashCode() {
        return this.Value.GetHashCode();
    }
    
    /// <summary>
    /// Returns a string representation of the <see cref="Vector3Key"/> including its time and value.
    /// </summary>
    /// <returns>A string representing the <see cref="Vector3Key"/>.</returns>
    public override string ToString() {
        return $"Time: {this.Time} Value: {this.Value}";
    }
}