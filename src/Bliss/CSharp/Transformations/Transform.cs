using System.Numerics;
using Bliss.CSharp.Mathematics;

namespace Bliss.CSharp.Transformations;

public struct Transform : IEquatable<Transform> {
    
    /// <summary>
    /// Stores the translation (position) vector of the transform.
    /// </summary>
    public Vector3 Translation;
    
    /// <summary>
    /// The rotation as a quaternion. Setting it updates <see cref="EulerAngles"/>.
    /// </summary>
    public Quaternion Rotation {
        readonly get => this._rotation;
        set {
            this._rotation = value;
            this._eulerAngles = Quaternion.ToEulerAnglesInDegrees(value, this._eulerAngles);
        }
    }
    
    /// <summary>
    /// The rotation as Euler angles in degrees. Setting it updates <see cref="Rotation"/>.
    /// </summary>
    public Vector3 EulerAngles {
        readonly get => this._eulerAngles;
        set {
            this._eulerAngles = value;
            this._rotation = Quaternion.CreateFromYawPitchRollInDegrees(value.Y, value.X, value.Z);
        }
    }
    
    /// <summary>
    /// Stores the scale of the transform.
    /// </summary>
    public Vector3 Scale;
    
    /// <summary>
    /// The forward Vector.
    /// </summary>
    public Vector3 Forward => Vector3.Transform(-Vector3.UnitZ, this.Rotation);
    
    /// <summary>
    /// The up vector.
    /// </summary>
    public Vector3 Up => Vector3.Transform(Vector3.UnitY, this.Rotation);
    
    /// <summary>
    /// The right vector.
    /// </summary>
    public Vector3 Right => Vector3.Transform(Vector3.UnitX, this.Rotation);
    
    /// <summary>
    /// Backing field for <see cref="Rotation"/>.
    /// </summary>
    private Quaternion _rotation;
    
    /// <summary>
    /// Backing field for <see cref="EulerAngles"/>, stored in degrees.
    /// </summary>
    private Vector3 _eulerAngles;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Transform"/> class with default values.
    /// </summary>
    public Transform() {
        this.Translation = Vector3.Zero;
        this._rotation = Quaternion.Identity;
        this._eulerAngles = Vector3.Zero;
        this.Scale = Vector3.One;
    }
    
    /// <summary>
    /// Determines whether two instances of the <see cref="Transform"/> struct are equal.
    /// </summary>
    /// <param name="left">The first instance of <see cref="Transform"/> to compare.</param>
    /// <param name="right">The second instance of <see cref="Transform"/> to compare.</param>
    /// <returns><c>true</c> if the two instances are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(Transform left, Transform right) => left.Equals(right);

    /// <summary>
    /// Determines whether two instances of the <see cref="Transform"/> struct are not equal.
    /// </summary>
    /// <param name="left">The first instance of <see cref="Transform"/> to compare.</param>
    /// <param name="right">The second instance of <see cref="Transform"/> to compare.</param>
    /// <returns><c>true</c> if the two instances are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(Transform left, Transform right) => !left.Equals(right);
    
    /// <summary>
    /// Returns the transformation matrix for the current Transform object.
    /// </summary>
    /// <returns>The transformation matrix.</returns>
    public readonly Matrix4x4 GetMatrix() {
        return Matrix4x4.CreateScale(this.Scale)
               * Matrix4x4.CreateFromQuaternion(this.Rotation)
               * Matrix4x4.CreateTranslation(this.Translation);
    }
    
    /// <summary>
    /// Determines whether the current instance is equal to another instance of the <see cref="Transform"/> struct.
    /// </summary>
    /// <param name="other">The <see cref="Transform"/> to compare with the current instance.</param>
    /// <returns><c>true</c> if the current instance is equal to the specified <see cref="Transform"/>; otherwise, <c>false</c>.</returns>
    public bool Equals(Transform other) {
        return this.Translation.Equals(other.Translation) &&
               this.Rotation.Equals(other.Rotation) &&
               this.Scale.Equals(other.Scale);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="Transform"/> instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current <see cref="Transform"/>.</param>
    /// <returns><c>true</c> if the specified object is equal to the current <see cref="Transform"/>; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj) {
        return obj is Transform other && this.Equals(other);
    }

    /// <summary>
    /// Returns the hash code for the current instance of the <see cref="Transform"/> struct.
    /// </summary>
    /// <returns>An integer representing the hash code of the current <see cref="Transform"/> instance.</returns>
    public override int GetHashCode() {
        return HashCode.Combine(this.Translation.GetHashCode(), this.Rotation.GetHashCode(), this.Scale.GetHashCode());
    }

    /// <summary>
    /// Returns a string representation of the current Transform object.
    /// </summary>
    /// <returns>A string that represents the values of Translation, Rotation, and Scale.</returns>
    public override string ToString() {
        return $"Translation:{this.Translation} Rotation:{this.Rotation} Scale:{this.Scale}";
    }
}
