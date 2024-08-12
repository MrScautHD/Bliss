using System.Numerics;

namespace Bliss.CSharp.Transformations;

public struct Transform {
    
    /// <summary>
    /// Represents the position of an object in 3D space.
    /// </summary>
    public Vector3 Translation;

    /// <summary>
    /// Represents the rotation of an object in 3D space.
    /// </summary>
    public Quaternion Rotation;

    /// <summary>
    /// Represents the scale of an object in 3D space.
    /// </summary>
    public Vector3 Scale;

    /// <summary>
    /// Initializes a new instance of the <see cref="Transform"/> class with default values.
    /// </summary>
    public Transform() {
        this.Translation = Vector3.Zero;
        this.Rotation = Quaternion.Identity;
        this.Scale = Vector3.One;
    }

    /// <summary>
    /// Returns the transformation matrix for the current Transform object.
    /// </summary>
    /// <returns>The transformation matrix.</returns>
    public Matrix4x4 GetMatrix() {
        Matrix4x4 matTranslate = Matrix4x4.CreateTranslation(this.Translation);
        Matrix4x4 matRot = Matrix4x4.CreateFromQuaternion(this.Rotation);
        Matrix4x4 matScale = Matrix4x4.CreateScale(this.Scale);
        
        return matTranslate * matRot * matScale;
    }

    /// <summary>
    /// Returns the normal transformation matrix for the current Transform object.
    /// </summary>
    /// <returns>The normal transformation matrix.</returns>
    public Matrix4x4 GetNormalMatrix() {
        Vector3 invScale = new Vector3(1.0F / this.Scale.X, 1.0F / this.Scale.Y, 1.0F / this.Scale.Z);
        
        Matrix4x4 matScale = Matrix4x4.CreateScale(invScale);
        Matrix4x4 matRot = Matrix4x4.CreateFromQuaternion(this.Rotation);
        
        return matScale * matRot;
    }
}