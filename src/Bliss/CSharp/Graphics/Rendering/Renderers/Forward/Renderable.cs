using System.Numerics;
using Bliss.CSharp.Geometry.Meshes;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Materials.Data;
using Bliss.CSharp.Materials;
using Bliss.CSharp.Transformations;
using Veldrid;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward;

public class Renderable : Disposable {
    
    /// <summary>
    /// The mesh associated with this renderable object.
    /// </summary>
    public IMesh Mesh { get; private set; }
    
    /// <summary>
    /// Gets or sets the material used to render.
    /// </summary>
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
    
    /// <summary>
    /// Gets or sets a value indicating whether instanced rendering is enabled.
    /// </summary>
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
    
    /// <summary>
    /// Gets the number of instance transforms stored by this renderable.
    /// </summary>
    public uint InstanceCount => (uint) this._transforms.Length;
    
    /// <summary>
    /// Gets a value indicating whether this renderable has any bone matrices.
    /// </summary>
    public bool HasBones => this._boneMatrices?.Length > 0;
    
    /// <summary>
    /// Gets a value indicating whether the transform buffer needs to be updated.
    /// </summary>
    public bool IsTransformBufferDirty { get; private set; }
    
    /// <summary>
    /// Gets a value indicating whether the bone buffer needs to be updated.
    /// </summary>
    public bool IsBoneBufferDirty { get; private set; }
    
    /// <summary>
    /// Gets a value indicating whether the material buffer needs to be updated.
    /// </summary>
    public bool IsMaterialBufferDirty => this.Material.IsDirty || this._hasMaterialChanged;
    
    /// <summary>
    /// Indicates whether the material has changed since the last buffer update.
    /// </summary>
    private bool _hasMaterialChanged;
    
    /// <summary>
    /// Stores the transforms used by this renderable, including support for instancing.
    /// </summary>
    private Transform[] _transforms;
    
    /// <summary>
    /// Stores the bone matrices used for skeletal animation, if the mesh supports bones.
    /// </summary>
    private Matrix4x4[]? _boneMatrices;
    
    /// <summary>
    /// The uniform buffer that stores transform data for rendering.
    /// </summary>
    private SimpleUniformBuffer<Matrix4x4> _transformBuffer;
    
    /// <summary>
    /// The uniform buffer that stores bone matrices for skinned rendering.
    /// </summary>
    private SimpleUniformBuffer<Matrix4x4>? _boneBuffer;
    
    /// <summary>
    /// The uniform buffer that stores material data for rendering.
    /// </summary>
    private SimpleUniformBuffer<MaterialData> _materialDataBuffer;
    
    /// <summary>
    /// Initializes a new <see cref="Renderable"/> using a single transform and optionally a cloned mesh material.
    /// </summary>
    /// <param name="mesh">The mesh to render.</param>
    /// <param name="transform">The transform applied to the renderable.</param>
    /// <param name="copyMeshMaterial">If true, clones the mesh material for independent modification.</param>
    /// <param name="useInstancing">If true, enables GPU instancing even for a single transform.</param>
    public Renderable(IMesh mesh, Transform transform, bool copyMeshMaterial = false, bool useInstancing = false) : this(mesh, [transform], copyMeshMaterial, useInstancing) { }
    
    /// <summary>
    /// Initializes a new <see cref="Renderable"/> using one or more transforms and optionally a cloned mesh material.
    /// </summary>
    /// <param name="mesh">The mesh to render.</param>
    /// <param name="transforms">The transforms applied to the renderable, where providing more than one transform enables instanced rendering.</param>
    /// <param name="copyMeshMaterial">If true, clones the mesh material for independent modification.</param>
    /// <param name="useInstancing">If true, enables GPU instancing even for a single transform.</param>
    public Renderable(IMesh mesh, Transform[] transforms, bool copyMeshMaterial = false, bool useInstancing = false) : this(mesh, transforms, copyMeshMaterial ? (Material) mesh.Material.Clone() : mesh.Material, useInstancing) { }
    
    /// <summary>
    /// Initializes a new <see cref="Renderable"/> using a single transform and a specific material.
    /// </summary>
    /// <param name="mesh">The mesh to render.</param>
    /// <param name="transform">The transform applied to the renderable.</param>
    /// <param name="material">The material used for rendering.</param>
    /// <param name="useInstancing">If true, enables GPU instancing even for a single transform.</param>
    public Renderable(IMesh mesh, Transform transform, Material material, bool useInstancing = false) : this(mesh, [transform], material, useInstancing) { }
    
    /// <summary>
    /// Initializes a new <see cref="Renderable"/> using one or more transforms and a specific material.
    /// </summary>
    /// <param name="mesh">The mesh to render.</param>
    /// <param name="transforms">The transforms applied to the renderable, where providing more than one transform enables instanced rendering.</param>
    /// <param name="material">The material used for rendering.</param>
    /// <param name="useInstancing">If true, enables GPU instancing even for a single transform.</param>
    public Renderable(IMesh mesh, Transform[] transforms, Material material, bool useInstancing = false) {
        this.Mesh = mesh;
        this.UseInstancing = useInstancing;
        this._transforms = transforms;
        this._boneMatrices = mesh.IsSkinned ? Enumerable.Repeat(Matrix4x4.Identity, IMesh.MaxBoneCount).ToArray() : null;
        this.Material = material;
        
        // Create the transform buffer.
        this._transformBuffer = new SimpleUniformBuffer<Matrix4x4>(mesh.GraphicsDevice, 1, ShaderStages.Vertex);
        this._transformBuffer.DeviceBuffer.Name = "TransformBuffer";
        this.IsTransformBufferDirty = true;
        
        // Create the bone buffer.
        if (mesh.IsSkinned) {
            this._boneBuffer = new SimpleUniformBuffer<Matrix4x4>(mesh.GraphicsDevice, IMesh.MaxBoneCount, ShaderStages.Vertex);
            this._boneBuffer.DeviceBuffer.Name = "BoneBuffer";
            this.IsBoneBufferDirty = true;
        }
        
        // Create material data buffer.
        this._materialDataBuffer = new SimpleUniformBuffer<MaterialData>(mesh.GraphicsDevice, 1, ShaderStages.Fragment);
        this._materialDataBuffer.DeviceBuffer.Name = "MaterialBuffer";
        this._hasMaterialChanged = true;
    }
    
    /// <summary>
    /// Gets the transform buffer used for rendering.
    /// </summary>
    /// <returns>The transform uniform buffer.</returns>
    public SimpleUniformBuffer<Matrix4x4> GetTransformBuffer() {
        return this._transformBuffer;
    }
    
    /// <summary>
    /// Gets the stored transforms for this renderable.
    /// </summary>
    /// <returns>A read-only span of transforms.</returns>
    public ReadOnlySpan<Transform> GetTransforms() {
        return this._transforms;
    }
    
    /// <summary>
    /// Sets the transform at the specified index.
    /// </summary>
    /// <param name="index">The transform index to update.</param>
    /// <param name="transform">The new transform value.</param>
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
    
    /// <summary>
    /// Resizes the stored transform array.
    /// Existing transforms are preserved, and any new slots are filled with the default transform.
    /// </summary>
    /// <param name="newSize">The new number of transforms to store.</param>
    public void ResizeTransformArray(int newSize) {
        if (newSize < 1) {
            throw new ArgumentOutOfRangeException(nameof(newSize), "Transform array size must be at least 1.");
        }
        
        if (newSize == this._transforms.Length) {
            return;
        }
        
        Array.Resize(ref this._transforms, newSize);
        
        if (!this.UseInstancing) {
            this.IsTransformBufferDirty = true;
        }
    }
    
    /// <summary>
    /// Clears all stored transforms and marks the transform buffer as dirty.
    /// </summary>
    public void ClearTransform() {
        Array.Fill(this._transforms, new Transform());
        
        if (!this.UseInstancing) {
            this.IsTransformBufferDirty = true;
        }
    }
    
    /// <summary>
    /// Updates the transform buffer with the current transform data.
    /// </summary>
    /// <param name="commandList">The command list used to defer the GPU upload.</param>
    public void UpdateTransformBuffer(CommandList commandList) {
        this._transformBuffer.SetValue(0, this.UseInstancing ? Matrix4x4.Identity : this._transforms[0].GetTransform());
        this._transformBuffer.UpdateBufferDeferred(commandList);
        this.IsTransformBufferDirty = false;
    }
    
    /// <summary>
    /// Gets the bone buffer used for skinned rendering.
    /// </summary>
    /// <returns>The bone uniform buffer.</returns>
    public SimpleUniformBuffer<Matrix4x4>? GetBoneBuffer() {
        return this._boneBuffer;
    }
    
    /// <summary>
    /// Gets the stored bone matrices.
    /// </summary>
    /// <returns>A read-only span of bone matrices.</returns>
    public ReadOnlySpan<Matrix4x4> GetBoneMatrices() {
        return this._boneMatrices;
    }
    
    /// <summary>
    /// Sets a bone matrix at the specified index.
    /// </summary>
    /// <param name="index">The bone index to update.</param>
    /// <param name="value">The new bone matrix value.</param>
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
    
    /// <summary>
    /// Clears all bone matrices and marks the bone buffer as dirty.
    /// </summary>
    public void ClearBoneMatrices() {
        if (this._boneMatrices != null) {
            Array.Fill(this._boneMatrices, Matrix4x4.Identity);
            this.IsBoneBufferDirty = true;
        }
    }
    
    /// <summary>
    /// Updates the bone buffer with the current bone matrix data.
    /// </summary>
    /// <param name="commandList">The command list used to defer the GPU upload.</param>
    public void UpdateBoneBuffer(CommandList commandList) {
        if (this._boneMatrices == null || this._boneBuffer == null) {
            return;
        }
        
        for (int i = 0; i < IMesh.MaxBoneCount; i++) {
            this._boneBuffer.SetValue(i, this._boneMatrices[i]);
        }
        
        this._boneBuffer.UpdateBufferDeferred(commandList);
        this.IsBoneBufferDirty = false;
    }
    
    /// <summary>
    /// Gets the material data buffer used for rendering.
    /// </summary>
    /// <returns>The material data uniform buffer.</returns>
    public SimpleUniformBuffer<MaterialData> GetMaterialBuffer() {
        return this._materialDataBuffer;
    }
    
    /// <summary>
    /// Updates the material buffer with the current material data.
    /// </summary>
    /// <param name="commandList">The command list used to defer the GPU upload.</param>
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

    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._transformBuffer.Dispose();
            this._boneBuffer?.Dispose();
            this._materialDataBuffer.Dispose();
        }
    }
}