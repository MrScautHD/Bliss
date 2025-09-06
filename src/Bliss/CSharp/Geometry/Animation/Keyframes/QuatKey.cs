using System.Numerics;

namespace Bliss.CSharp.Geometry.Animation.Keyframes;

public struct QuatKey : IEquatable<QuatKey> {
    
    /// <summary>
    /// The time at which this keyframe occurs.
    /// </summary>
    public double Time;
    
    /// <summary>
    /// The quaternion value (e.g., rotation) at the specified time.
    /// </summary>
    public Quaternion Value;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="QuatKey"/> struct with the specified time and value.
    /// </summary>
    /// <param name="time">The time of the keyframe.</param>
    /// <param name="value">The quaternion value (e.g., rotation) at the specified time.</param>
    public QuatKey(double time, Quaternion value) {
        this.Time = time;
        this.Value = value;
    }
    
    /// <summary>
    /// Compares two <see cref="QuatKey"/> instances for equality based on their quaternion value.
    /// </summary>
    /// <param name="left">The first <see cref="QuatKey"/> to compare.</param>
    /// <param name="right">The second <see cref="QuatKey"/> to compare.</param>
    /// <returns>Returns true if the quaternion values of both keys are equal, otherwise false.</returns>
    public static bool operator ==(QuatKey left, QuatKey right) => left.Value == right.Value;
    
    /// <summary>
    /// Compares two <see cref="QuatKey"/> instances for inequality based on their quaternion value.
    /// </summary>
    /// <param name="left">The first <see cref="QuatKey"/> to compare.</param>
    /// <param name="right">The second <see cref="QuatKey"/> to compare.</param>
    /// <returns>Returns true if the quaternion values of both keys are not equal, otherwise false.</returns>
    public static bool operator !=(QuatKey left, QuatKey right) => left.Value != right.Value;
    
    /// <summary>
    /// Compares two <see cref="QuatKey"/> instances to see if the first occurs before the second based on their time.
    /// </summary>
    /// <param name="left">The first <see cref="QuatKey"/> to compare.</param>
    /// <param name="right">The second <see cref="QuatKey"/> to compare.</param>
    /// <returns>Returns true if the first key occurs before the second key in time.</returns>
    public static bool operator <(QuatKey left, QuatKey right) => left.Time < right.Time;
    
    /// <summary>
    /// Compares two <see cref="QuatKey"/> instances to see if the first occurs after the second based on their time.
    /// </summary>
    /// <param name="left">The first <see cref="QuatKey"/> to compare.</param>
    /// <param name="right">The second <see cref="QuatKey"/> to compare.</param>
    /// <returns>Returns true if the first key occurs after the second key in time.</returns>
    public static bool operator >(QuatKey left, QuatKey right) => left.Time > right.Time;
    
    /// <summary>
    /// Compares this <see cref="QuatKey"/> to another for equality based on their quaternion value.
    /// </summary>
    /// <param name="other">The other <see cref="QuatKey"/> to compare.</param>
    /// <returns>Returns true if the quaternion values of both keys are equal, otherwise false.</returns>
    public bool Equals(QuatKey other) {
        return this.Value == other.Value;
    }

    /// <summary>
    /// Compares this <see cref="QuatKey"/> to another object for equality.
    /// </summary>
    /// <param name="obj">The object to compare to.</param>
    /// <returns>Returns true if the object is a <see cref="QuatKey"/> and has the same quaternion value.</returns>
    public override bool Equals(object? obj) {
        return obj is QuatKey other && this.Equals(other);
    }

    /// <summary>
    /// Generates a hash code based on the quaternion value of the <see cref="QuatKey"/>.
    /// </summary>
    /// <returns>The hash code for this key.</returns>
    public override int GetHashCode() {
        return this.Value.GetHashCode();
    }

    /// <summary>
    /// Returns a string representation of the <see cref="QuatKey"/> including its time and quaternion value.
    /// </summary>
    /// <returns>A string representing the <see cref="QuatKey"/>.</returns>
    public override string ToString() {
        return $"Time: {this.Time} Value: {this.Value}";
    }
}