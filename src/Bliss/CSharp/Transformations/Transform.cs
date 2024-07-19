using System.Numerics;

namespace Bliss.CSharp.Transformations;

public struct Transform {
    
    /// <summary>
    /// Represents the position of an object in 3D space.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// Represents the rotation of an object in 3D space.
    /// </summary>
    public Quaternion Rotation;

    /// <summary>
    /// Represents the scale of an object in 3D space.
    /// </summary>
    public Vector3 Scale;
}