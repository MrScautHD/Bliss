namespace Bliss.CSharp.Geometry.Bones;

public struct Blittable {
    
    /// <summary>
    /// Represents an array of bone data used in the Bones geometry namespace.
    /// The array size is fixed to 16 * 128, allowing storage of multiple bone-related values.
    /// </summary>
    public unsafe fixed float BoneData[16 * 128];
}