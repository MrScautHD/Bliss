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
    /// The index of the parent bone in the skeleton. -1 if it is a root bone.
    /// </summary>
    public int ParentId { get; internal set; }
    
    /// <summary>
    /// The transformation matrix representing the bone's transformation in the skeleton hierarchy.
    /// </summary>
    public Matrix4x4 Transformation { get; private set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BoneInfo"/> class.
    /// </summary>
    /// <param name="name">The name of the bone.</param>
    /// <param name="id">The unique identifier of the bone.</param>
    /// <param name="transformation">The transformation matrix representing the bone's default pose.</param>
    /// <param name="parentId">The index of the parent bone in the skeleton hierarchy. Use <c>-1</c> if the bone is a root bone.</param>
    public BoneInfo(string name, uint id, Matrix4x4 transformation, int parentId = -1) {
        this.Name = name;
        this.Id = id;
        this.Transformation = transformation;
        this.ParentId = parentId;
    }
}