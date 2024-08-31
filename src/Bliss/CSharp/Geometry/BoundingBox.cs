using System.Numerics;

namespace Bliss.CSharp.Geometry;

public struct BoundingBox {

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
}