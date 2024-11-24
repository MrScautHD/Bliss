using System.Numerics;

namespace Bliss.CSharp.Geometry.Animations.Bones;

public class BoneInfo {

    public string Name;
    
    public uint Id;

    public BoneInfo? Parent;
    
    public Matrix4x4 Offset;

    public Matrix4x4 Transform;

    public BoneInfo(string name, uint id, BoneInfo? parent, Matrix4x4 offset, Matrix4x4 transform) {
        this.Name = name;
        this.Id = id;
        this.Parent = parent;
        this.Offset = offset;
        this.Transform = transform;
    }
}