using System.Numerics;

namespace Bliss.CSharp.Rendering.Systems;

public struct SimplePushConstantData {
    
    public Matrix4x4 ModelMatrix;
    public Matrix4x4 NormalMatrix;

    /// <summary>
    /// Represents a container class for simple push constant data used in rendering systems.
    /// </summary>
    public SimplePushConstantData() {
        this.ModelMatrix = Matrix4x4.Identity;
        this.NormalMatrix = Matrix4x4.Identity;
    }
}