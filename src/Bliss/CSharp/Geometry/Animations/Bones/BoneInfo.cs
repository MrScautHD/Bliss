using System.Numerics;

namespace Bliss.CSharp.Geometry.Animations.Bones;

public struct BoneInfo {

    public string Name;
    
    public uint Id;
    
    public Matrix4x4 OffsetMatrix;

    public BoneInfo(string name, uint id, Matrix4x4 offsetMatrix) {
        this.Name = name;
        this.Id = id;
        this.OffsetMatrix = offsetMatrix;
    }
}