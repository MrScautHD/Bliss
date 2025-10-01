using System.Numerics;
using Bliss.CSharp.Geometry;
using Bliss.CSharp.Materials;
using Bliss.CSharp.Transformations;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward;

public class Renderable {
    
    /// <summary>
    /// The mesh associated with this renderable object.
    /// </summary>
    public Mesh Mesh { get; private set; }
    
    /// <summary>
    /// The transformation (position, rotation, scale) applied to this renderable.
    /// </summary>
    public Transform Transform;
    
    /// <summary>
    /// The material used for rendering.
    /// </summary>
    public Material Material;
    
    /// <summary>
    /// Optional array of bone matrices for skinned meshes.
    /// </summary>
    public Matrix4x4[]? BoneMatrices { get; private set; }
    
    /// <summary>
    /// Initializes a new <see cref="Renderable"/> using the mesh's material or a cloned material.
    /// </summary>
    /// <param name="mesh">The mesh to render.</param>
    /// <param name="transform">The transform to apply.</param>
    /// <param name="copyMeshMaterial"> If true, creates a cloned copy of the mesh's material to allow independent modification. If false, uses the mesh's existing material. </param>
    public Renderable(Mesh mesh, Transform transform, bool copyMeshMaterial = false) : this(mesh, transform, copyMeshMaterial ? (Material) mesh.Material.Clone() : mesh.Material) { }
    
    /// <summary>
    /// Initializes a new <see cref="Renderable"/> with a specific material.
    /// </summary>
    /// <param name="mesh">The mesh to render.</param>
    /// <param name="transform">The transform to apply.</param>
    /// <param name="material">The material to use for this renderable.</param>
    public Renderable(Mesh mesh, Transform transform, Material material) {
        this.Mesh = mesh;
        this.Material = material;
        this.BoneMatrices = mesh.HasBones ? Enumerable.Repeat(Matrix4x4.Identity, Mesh.MaxBoneCount).ToArray() : null;
        this.Transform = transform;
    }
}