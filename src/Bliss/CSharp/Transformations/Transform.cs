using System.Numerics;

namespace Bliss.CSharp.Transformations;

public struct Transform : IEquatable<Transform> {

    /// <summary>
    /// Event triggered whenever the transformation is updated (translation, rotation, or scale changes).
    /// </summary>
    public event Action<Transform>? OnUpdate;

    /// <summary>
    /// Stores the translation (position) vector of the transform.
    /// </summary>
    private Vector3 _translation;

    /// <summary>
    /// Stores the rotation of the transform as a quaternion.
    /// </summary>
    private Quaternion _rotation;

    /// <summary>
    /// Stores the scale of the transform.
    /// </summary>
    private Vector3 _scale;

    /// <summary>
    /// Initializes a new instance of the <see cref="Transform"/> class with default values.
    /// </summary>
    public Transform() {
        this._translation = Vector3.Zero;
        this._rotation = Quaternion.Identity;
        this._scale = Vector3.One;
    }

    /// <summary>
    /// Gets or sets the translation (position) component of the transform.
    /// Triggers the <see cref="OnUpdate"/> event when changed.
    /// </summary>
    public Vector3 Translation {
        get => this._translation;
        set {
            this._translation = value;
            this.OnUpdate?.Invoke(this);
        }
    }

    /// <summary>
    /// Gets or sets the rotation component of the transform as a quaternion.
    /// Triggers the <see cref="OnUpdate"/> event when changed.
    /// </summary>
    public Quaternion Rotation {
        get => this._rotation;
        set {
            this._rotation = value;
            this.OnUpdate?.Invoke(this);
        }
    }

    /// <summary>
    /// Gets or sets the scale component of the transform.
    /// Triggers the <see cref="OnUpdate"/> event when changed.
    /// </summary>
    public Vector3 Scale {
        get => this._scale;
        set {
            this._scale = value;
            this.OnUpdate?.Invoke(this);
        }
    }

    /// <summary>
    /// The Transform Forward Vector
    /// </summary>
    public Vector3 Forward => Vector3.Transform(new Vector3(0, 0, -1), this._rotation);
    
    /// <summary>
    /// The Transform Up Vector
    /// </summary>
    public Vector3 Up => Vector3.Transform(new Vector3(0, 1, 0), this._rotation);
  
    /// <summary>
    /// The Transform Right Vector
    /// </summary>
    public Vector3 Right => Vector3.Transform(new Vector3(-1, 0, 0), this._rotation);
    
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
    public Matrix4x4 GetTransform() {
        Matrix4x4 matScale = Matrix4x4.CreateScale(this._scale);
        Matrix4x4 matRotation = Matrix4x4.CreateFromQuaternion(this._rotation);
        Matrix4x4 matTranslation = Matrix4x4.CreateTranslation(this._translation);
        
        return matScale * matRotation * matTranslation;
    }

    /// <summary>
    /// Determines whether the current instance is equal to another instance of the <see cref="Transform"/> struct.
    /// </summary>
    /// <param name="other">The <see cref="Transform"/> to compare with the current instance.</param>
    /// <returns><c>true</c> if the current instance is equal to the specified <see cref="Transform"/>; otherwise, <c>false</c>.</returns>
    public bool Equals(Transform other) {
        return this._translation.Equals(other._translation) &&
               this._rotation.Equals(other._rotation) &&
               this._scale.Equals(other._scale);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="Transform"/> instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current <see cref="Transform"/>.</param>
    /// <returns><c>true</c> if the specified object is equal to the current <see cref="Transform"/>; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj) {
        return obj is Transform p && this.Equals(p);
    }

    /// <summary>
    /// Returns the hash code for the current instance of the <see cref="Transform"/> struct.
    /// </summary>
    /// <returns>An integer representing the hash code of the current <see cref="Transform"/> instance.</returns>
    public override int GetHashCode() {
        return HashCode.Combine(this._translation.GetHashCode(), this._rotation.GetHashCode(), this._scale.GetHashCode());
    }

    /// <summary>
    /// Returns a string representation of the current Transform object.
    /// </summary>
    /// <returns>A string that represents the values of Translation, Rotation, and Scale.</returns>
    public override string ToString() {
        return $"Translation:{this._translation} Rotation:{this._rotation} Scale:{this._scale}";
    }
}