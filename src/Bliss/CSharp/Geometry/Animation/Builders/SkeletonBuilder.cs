using System.Numerics;
using Assimp;
using AMesh = Assimp.Mesh;

namespace Bliss.CSharp.Geometry.Animation.Builders;

public class SkeletonBuilder {
    
    /// <summary>
    /// The source scene containing meshes and bones used for building the skeleton.
    /// </summary>
    private readonly Scene _scene;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SkeletonBuilder"/> class.
    /// </summary>
    /// <param name="scene">The Assimp scene that contains the bone and mesh data.</param>
    public SkeletonBuilder(Scene scene) {
        this._scene = scene;
    }
    
    /// <summary>
    /// Builds a <see cref="Skeleton"/> by collecting unique bones from the scene’s meshes.
    /// </summary>
    /// <returns> A <see cref="Skeleton"/> containing the collected bones and their offset matrices. </returns>
    public Skeleton Build() {
        List<BoneInfo> bones = new List<BoneInfo>();
        
        foreach (AMesh mesh in this._scene.Meshes) {
            foreach (Bone bone in mesh.Bones) {
                if (bones.All(b => b.Name != bone.Name)) {
                    bones.Add(new BoneInfo(bone.Name, (uint) bones.Count, Matrix4x4.Transpose(bone.OffsetMatrix)));
                }
            }
        }
        
        return new Skeleton(bones.AsReadOnly());
    }
}