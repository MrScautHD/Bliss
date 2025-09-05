using System.Numerics;
using Bliss.CSharp.Geometry;
using Bliss.CSharp.Materials;
using Bliss.CSharp.Transformations;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Renderables;

public class Renderable {
    
    public Mesh Mesh { get; private set; }
    
    public Material Material;
    
    public Matrix4x4[]? BoneMatrices;
    
    public Transform Transform;
    
    public Renderable(Mesh mesh, Transform transform) {
        this.Mesh = mesh;
        this.Material = mesh.Material; // Make it cloneable.
        this.BoneMatrices = mesh.BoneInfos != null ? Enumerable.Repeat(Matrix4x4.Identity, Mesh.MaxBoneCount).ToArray() : null;;
        this.Transform = transform;
    }
}