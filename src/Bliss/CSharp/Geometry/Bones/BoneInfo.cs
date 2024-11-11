using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bliss.CSharp.Geometry.Bones;

public struct BoneInfo {
    
    /// <summary>
    /// Represents an array of transformations for bones in a 3D model.
    /// Each Matrix4x4 in the array corresponds to a single bone's transformation matrix.
    /// </summary>
    public Matrix4x4[] BonesTransformations;

    /// <summary>
    /// Represents bone information in a 3D model, including an array of transformations for the bones.
    /// Each Matrix4x4 in the array corresponds to a single bone's transformation matrix.
    /// </summary>
    public BoneInfo(Matrix4x4[] boneTransformations) {
        this.BonesTransformations = boneTransformations;
    }

    /// <summary>
    /// Converts the bone transformations into a blittable structure for use in low-level operations.
    /// </summary>
    /// <returns>
    /// A Blittable struct containing the transformed bone data suitable for unmanaged operations.
    /// </returns>
    public unsafe Blittable GetBlittable() {
        Blittable b;
        
        fixed (Matrix4x4* ptr = this.BonesTransformations) {
            Unsafe.CopyBlock(&b, ptr, 64 * 128);
        }

        return b;
    }
}