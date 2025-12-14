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
    /// The transforms applied to the renderable, where providing more than one transform enables instanced rendering.
    /// </summary>
    public Transform[] Transforms;
    
    /// <summary>
    /// True if this renderable should be drawn multiple times with different transforms.
    /// </summary>
    public bool IsInstanced => this.InstanceCount > 1;
    
    /// <summary>
    /// Number of instances (transforms) to draw.
    /// </summary>
    public uint InstanceCount => (uint) this.Transforms.Length;
    
    /// <summary>
    /// The material used for rendering.
    /// </summary>
    public Material Material;
    
    /// <summary>
    /// Optional array of bone matrices for skinned meshes.
    /// </summary>
    public Matrix4x4[]? BoneMatrices { get; private set; }
    
    /// <summary>
    /// Initializes a new <see cref="Renderable"/> using a single transform and optionally a cloned mesh material.
    /// </summary>
    /// <param name="mesh">The mesh to render.</param>
    /// <param name="transform">The transform applied to the renderable.</param>
    /// <param name="copyMeshMaterial">If true, clones the mesh material for independent modification.</param>
    public Renderable(Mesh mesh, Transform transform, bool copyMeshMaterial = false) : this(mesh, [transform], copyMeshMaterial) { }
    
    /// <summary>
    /// Initializes a new <see cref="Renderable"/> using one or more transforms and optionally a cloned mesh material.
    /// </summary>
    /// <param name="mesh">The mesh to render.</param>
    /// <param name="transforms">The transforms applied to the renderable, where providing more than one transform enables instanced rendering.</param>
    /// <param name="copyMeshMaterial">If true, clones the mesh material for independent modification.</param>
    public Renderable(Mesh mesh, Transform[] transforms, bool copyMeshMaterial = false) : this(mesh, transforms, copyMeshMaterial ? (Material) mesh.Material.Clone() : mesh.Material) { }
    
    /// <summary>
    /// Initializes a new <see cref="Renderable"/> using a single transform and a specific material.
    /// </summary>
    /// <param name="mesh">The mesh to render.</param>
    /// <param name="transform">The transform applied to the renderable.</param>
    /// <param name="material">The material used for rendering.</param>
    public Renderable(Mesh mesh, Transform transform, Material material) : this(mesh, [transform], material) { }
    
    /// <summary>
    /// Initializes a new <see cref="Renderable"/> using one or more transforms and a specific material.
    /// </summary>
    /// <param name="mesh">The mesh to render.</param>
    /// <param name="transforms">The transforms applied to the renderable, where providing more than one transform enables instanced rendering.</param>
    /// <param name="material">The material used for rendering.</param>
    public Renderable(Mesh mesh, Transform[] transforms, Material material) {
        this.Mesh = mesh;
        this.Material = material;
        this.BoneMatrices = mesh.HasBones ? Enumerable.Repeat(Matrix4x4.Identity, Mesh.MaxBoneCount).ToArray() : null;
        this.Transforms = transforms;
    }
}