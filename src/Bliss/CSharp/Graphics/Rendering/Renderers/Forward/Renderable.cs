using System.Numerics;
using Bliss.CSharp.Geometry;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Materials.Data;
using Bliss.CSharp.Materials;
using Bliss.CSharp.Transformations;
using Veldrid;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward;

public class Renderable {
    
    /// <summary>
    /// The mesh associated with this renderable object.
    /// </summary>
    public Mesh Mesh { get; private set; }
    
    public Material Material {
        get;
        set {
            if (ReferenceEquals(field, value)) {
                return;
            }

            field = value;
            this._hasMaterialChanged = true;
        }
    }
    
    public bool UseInstancing {
        get;
        set {
            if (value == field) {
                return;
            }
            
            field = value;
            this.IsTransformBufferDirty = true;
        }
    }
    
    public uint InstanceCount => (uint) this._transforms.Length;
    
    public bool HasBones => this._boneMatrices?.Length > 0;
    
    public bool IsTransformBufferDirty { get; private set; }
    
    public bool IsBoneBufferDirty { get; private set; }

    public bool IsMaterialBufferDirty => this.Material.IsDirty || this._hasMaterialChanged;
    
    private bool _hasMaterialChanged;
    
    private Transform[] _transforms;
    
    private Matrix4x4[]? _boneMatrices;
    
    private SimpleUniformBuffer<Matrix4x4> _transformBuffer;
    
    private SimpleUniformBuffer<Matrix4x4> _boneBuffer; // Make it nullable and add a new shader macro for it so you have to use skinned mesh for using it yk?
    
    private SimpleUniformBuffer<MaterialData> _materialDataBuffer;
    
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
        this.UseInstancing = useInstancing;
        this._transforms = transforms;
        this._boneMatrices = mesh.HasBones ? Enumerable.Repeat(Matrix4x4.Identity, Mesh.MaxBoneCount).ToArray() : null;
        this.Material = material;
        
        // Create the transform buffer.
        this._transformBuffer = new SimpleUniformBuffer<Matrix4x4>(mesh.GraphicsDevice, 1, ShaderStages.Vertex);
        this._transformBuffer.DeviceBuffer.Name = "TransformBuffer";
        this.IsTransformBufferDirty = true;
        
        // Create the bone buffer.
        this._boneBuffer = new SimpleUniformBuffer<Matrix4x4>(mesh.GraphicsDevice, Mesh.MaxBoneCount, ShaderStages.Vertex);
        this._boneBuffer.DeviceBuffer.Name = "BoneBuffer";
        this.IsBoneBufferDirty = mesh.HasBones;
        
        // Create material data buffer.
        this._materialDataBuffer = new SimpleUniformBuffer<MaterialData>(mesh.GraphicsDevice, 1, ShaderStages.Fragment);
        this._materialDataBuffer.DeviceBuffer.Name = "MaterialBuffer";
        this._hasMaterialChanged = true;
    }
    
    public SimpleUniformBuffer<Matrix4x4> GetTransformBuffer() {
        return this._transformBuffer;
    }
    
    public ReadOnlySpan<Transform> GetTransforms() {
        return this._transforms;
    }
    
    public void SetTransform(int index, Transform transform) {
        if (index < 0 || index >= this._transforms.Length) {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        
        if (this._transforms[index] == transform) {
            return;
        }
        
        this._transforms[index] = transform;
        
        if (!this.UseInstancing && index == 0) {
            this.IsTransformBufferDirty = true;
        }
    }
    
    public void ClearTransform() {
        Array.Fill(this._transforms, new Transform());
        this.IsTransformBufferDirty = true;
    }

    public void UpdateTransformBuffer(CommandList commandList) {
        this._transformBuffer.SetValue(0, this.UseInstancing ? Matrix4x4.Identity : this._transforms[0].GetTransform());
        this._transformBuffer.UpdateBufferDeferred(commandList);
        this.IsTransformBufferDirty = false;
    }
    
    public SimpleUniformBuffer<Matrix4x4> GetBoneBuffer() {
        return this._boneBuffer;
    }
    
    public ReadOnlySpan<Matrix4x4> GetBoneMatrices() {
        return this._boneMatrices;
    }
    
    public void SetBoneMatrix(int index, Matrix4x4 value) {
        if (this._boneMatrices == null) {
            return;
        }
        
        if (index < 0 || index >= this._boneMatrices.Length) {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        
        if (this._boneMatrices[index] == value) {
            return;
        }
        
        this._boneMatrices[index] = value;
        this.IsBoneBufferDirty = true;
    }
    
    public void ClearBoneMatrices() {
        if (this._boneMatrices != null) {
            Array.Fill(this._boneMatrices, Matrix4x4.Identity);
            this.IsBoneBufferDirty = true;
        }
    }
    
    public void UpdateBoneBuffer(CommandList commandList) {
        if (this._boneMatrices == null) {
            return;
        }
        
        for (int i = 0; i < Mesh.MaxBoneCount; i++) {
            this._boneBuffer.SetValue(i, this._boneMatrices[i]);
        }
        
        this._boneBuffer.UpdateBufferDeferred(commandList);
        this.IsBoneBufferDirty = false;
    }
    
    public SimpleUniformBuffer<MaterialData> GetMaterialBuffer() {
        return this._materialDataBuffer;
    }
    
    public void UpdateMaterialBuffer(CommandList commandList) {
        MaterialData materialData = new MaterialData {
            RenderMode = this.Material.RenderMode
        };
        
        foreach (MaterialMapType mapType in this.Material.GetMaterialMapTypes()) {
            MaterialMap? map = this.Material.GetMaterialMap(mapType);
            
            if (map != null) {
                materialData[(int) mapType] = new MaterialMapData() {
                    Color = map.Color?.ToRgbaFloatVec4() ?? Vector4.Zero,
                    Value = map.Value
                };
            }
        }
        
        this._materialDataBuffer.SetValueDeferred(commandList, 0, ref materialData);
        this.Material.IsDirty = false;
        this._hasMaterialChanged = false;
    }
}