using System.Numerics;

namespace Bliss.CSharp.Geometry.Box;

public struct OrientedBoundingBox {
    
    /// <summary>
    /// Minimum box-corner.
    /// </summary>
    public Vector3 Min;
    
    /// <summary>
    /// Maximum box-corner.
    /// </summary>
    public Vector3 Max;

    /// <summary>
    /// Box rotation.
    /// </summary>
    public Quaternion Rotation;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedBoundingBox"/> struct with the specified minimum and maximum vectors and rotation.
    /// </summary>
    /// <param name="min">The minimum vector defining one corner of the bounding box.</param>
    /// <param name="max">The maximum vector defining the opposite corner of the bounding box.</param>
    /// <param name="rotation">The rotation of the bounding box as a quaternion.</param>
    public OrientedBoundingBox(Vector3 min, Vector3 max, Quaternion rotation) {
        this.Min = min;
        this.Max = max;
        this.Rotation = rotation;
    }
}