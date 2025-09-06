using System.Numerics;

namespace Bliss.CSharp.Geometry.Animation;

public class BoneInfo {

    /// <summary>
    /// The name of the bone.
    /// </summary>
    public string Name { get; private set; }
    
    /// <summary>
    /// The unique identifier of the bone.
    /// </summary>
    public uint Id { get; private set; }
    
    /// <summary>
    /// The transformation matrix representing the bone's transformation in the skeleton hierarchy.
    /// </summary>
    public Matrix4x4 Transformation { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoneInfo"/> class with the specified name, ID, and transformation.
    /// </summary>
    /// <param name="name">The name of the bone.</param>
    /// <param name="id">The unique identifier of the bone.</param>
    /// <param name="transformation">The transformation matrix for the bone.</param>
    public BoneInfo(string name, uint id, Matrix4x4 transformation) {
        this.Name = name;
        this.Id = id;
        this.Transformation = transformation;
    }
}