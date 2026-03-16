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
    /// The transforms applied to the renderable. 
    /// Multiple transforms can be used for instanced rendering, but instancing only occurs if <see cref="UseInstancing"/> is true.
    /// </summary>
    public Transform[] Transforms;
    
    /// <summary>
    /// If true, enables GPU instancing for all transforms.
    /// </summary>
    public bool UseInstancing;
    
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
    /// <param name="useInstancing">If true, enables GPU instancing even for a single transform.</param>
    public Renderable(Mesh mesh, Transform transform, bool copyMeshMaterial = false, bool useInstancing = false) : this(mesh, [transform], copyMeshMaterial, useInstancing) { }
    
    /// <summary>
    /// Initializes a new <see cref="Renderable"/> using one or more transforms and optionally a cloned mesh material.
    /// </summary>
    /// <param name="mesh">The mesh to render.</param>
    /// <param name="transforms">The transforms applied to the renderable, where providing more than one transform enables instanced rendering.</param>
    /// <param name="copyMeshMaterial">If true, clones the mesh material for independent modification.</param>
    /// <param name="useInstancing">If true, enables GPU instancing even for a single transform.</param>
    public Renderable(Mesh mesh, Transform[] transforms, bool copyMeshMaterial = false, bool useInstancing = false) : this(mesh, transforms, copyMeshMaterial ? (Material) mesh.Material.Clone() : mesh.Material, useInstancing) { }
    
    /// <summary>
    /// Initializes a new <see cref="Renderable"/> using a single transform and a specific material.
    /// </summary>
    /// <param name="mesh">The mesh to render.</param>
    /// <param name="transform">The transform applied to the renderable.</param>
    /// <param name="material">The material used for rendering.</param>
    /// <param name="useInstancing">If true, enables GPU instancing even for a single transform.</param>
    public Renderable(Mesh mesh, Transform transform, Material material, bool useInstancing = false) : this(mesh, [transform], material, useInstancing) { }
    
    /// <summary>
    /// Initializes a new <see cref="Renderable"/> using one or more transforms and a specific material.
    /// </summary>
    /// <param name="mesh">The mesh to render.</param>
    /// <param name="transforms">The transforms applied to the renderable, where providing more than one transform enables instanced rendering.</param>
    /// <param name="material">The material used for rendering.</param>
    /// <param name="useInstancing">If true, enables GPU instancing even for a single transform.</param>
    public Renderable(Mesh mesh, Transform[] transforms, Material material, bool useInstancing = false) {
        this.Mesh = mesh;
        this.Material = material;
        this.UseInstancing = useInstancing;
        this.BoneMatrices = mesh.HasBones ? Enumerable.Repeat(Matrix4x4.Identity, Mesh.MaxBoneCount).ToArray() : null;
        this.Transforms = transforms;
    }
}