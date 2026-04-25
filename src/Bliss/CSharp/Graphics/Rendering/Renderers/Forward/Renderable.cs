using System.Numerics;
using System.Runtime.InteropServices;
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
    /// Gets a value indicating whether instanced rendering is enabled.
    /// </summary>
    public bool UseInstancing { get; private set; }
    
    /// <summary>
    /// Gets the number of instance transforms stored by this renderable.
    /// </summary>
    public uint InstanceCount => (uint) this._transformCount;
    
    /// <summary>
    /// Gets a value indicating whether this renderable has any bone matrices.
    /// </summary>
    public bool HasBones => this._boneMatrices?.Length > 0;
    
    /// <summary>
    /// Gets a value indicating whether the transform buffer needs to be updated.
    /// </summary>
    public bool IsTransformBufferDirty { get; private set; }
    
    /// <summary>
    /// Gets a value indicating whether the instance vertex buffer needs to be updated.
    /// </summary>
    public bool IsInstanceVertexBufferDirty { get; private set; }
    
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
    /// The number of logically active transforms. This is the slice of
    /// <see cref="_transforms"/> that is meaningful and will be sent to the GPU.
    /// </summary>
    private int _transformCount;
    
    /// <summary>
    /// The number of slots currently allocated in <see cref="_transforms"/> and
    /// in <see cref="_instanceVertexBuffer"/>. Grows in powers-of-two (min 8).
    /// Never shrinks so that repeated add/remove cycles don't thrash the GPU allocator.
    /// </summary>
    private uint _transformCapacity;
    
    /// <summary>
    /// Reusable scratch buffer for converting <see cref="_transforms"/> to matrices before uploading
    /// </summary>
    private Matrix4x4[] _tempInstanceTransforms;
    
    /// <summary>
    /// Stores the bone matrices used for skeletal animation, if the mesh supports bones.
    /// </summary>
    private Matrix4x4[]? _boneMatrices;
    
    /// <summary>
    /// The uniform buffer that stores transform data for rendering.
    /// </summary>
    private SimpleUniformBuffer<Matrix4x4> _transformBuffer;
    
    /// <summary>
    /// The uniform buffer used to store instance-specific vertex data for rendering instanced objects.
    /// </summary>
    private DeviceBuffer? _instanceVertexBuffer;
    
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
        this.Material = material;
        this._boneMatrices = mesh.IsSkinned ? Enumerable.Repeat(Matrix4x4.Identity, IMesh.MaxBoneCount).ToArray() : null;
        
        // Allocate the backing transform array.
        this._transformCount = useInstancing ? transforms.Length : 1;
        this._transformCapacity = useInstancing ? this.NextPowerOfTwo((uint) Math.Max(transforms.Length, 8)) : 1;
        this._transforms = new Transform[this._transformCapacity];
        Array.Copy(transforms, this._transforms, transforms.Length);
        
        // Create the transform buffer.
        this._transformBuffer = new SimpleUniformBuffer<Matrix4x4>(mesh.GraphicsDevice, 1, ShaderStages.Vertex);
        this._transformBuffer.DeviceBuffer.Name = "TransformBuffer";
        this.IsTransformBufferDirty = true;
        
        // Create the instance vertex buffer.
        if (useInstancing) {
            this._instanceVertexBuffer = this.CreateInstanceVertexBuffer(this._transformCapacity);
            this._tempInstanceTransforms = new Matrix4x4[this._transformCapacity];
            this.IsInstanceVertexBufferDirty = true;
        }
        
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
        if (index < 0 || index >= this._transformCount) {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        
        if (this._transforms[index] == transform) {
            return;
        }
        
        this._transforms[index] = transform;

        if (this.UseInstancing) {
            this.IsInstanceVertexBufferDirty = true;
        }
        else {
            this.IsTransformBufferDirty = true;
        }
    }
    
    /// <summary>
    /// Resizes the stored transform array.
    /// Existing transforms are preserved, and any new slots are filled with the default transform.
    /// </summary>
    /// <param name="newCount">The new number of transforms to store.</param>
    public void ResizeTransformArray(uint newCount) {
        if (!this.UseInstancing) {
            throw new InvalidOperationException("Cannot resize transform array because instancing is disabled.");
        }
        
        if (newCount < 1) {
            throw new ArgumentOutOfRangeException(nameof(newCount), "Transform array size must be at least 1.");
        }
        
        if (newCount == this._transformCount) {
            return;
        }
        
        this._transformCount = (int) newCount;
        
        // Grow capacity if needed. (Never shrink)
        if (newCount > this._transformCapacity) {
            uint newCapacity = this._transformCapacity;
            
            while (newCapacity < newCount) {
                newCapacity *= 2;
            }
            
            this._transformCapacity = newCapacity;
            
            // Reallocate CPU arrays.
            Array.Resize(ref this._transforms, (int) newCapacity);
            this._tempInstanceTransforms = new Matrix4x4[newCapacity];
            
            // Reallocate GPU buffer.
            this._instanceVertexBuffer?.Dispose();
            this._instanceVertexBuffer = this.CreateInstanceVertexBuffer(newCapacity);
        }
        
        this.IsInstanceVertexBufferDirty = true;
    }
    
    /// <summary>
    /// Clears all stored transforms and marks the transform buffer as dirty.
    /// </summary>
    public void ClearTransforms() {
        Array.Fill(this._transforms, new Transform(), 0, this._transformCount);
        
        if (this.UseInstancing) {
            this.IsInstanceVertexBufferDirty = true;
        }
        else {
            this.IsTransformBufferDirty = true;
        }
    }
    
    /// <summary>
    /// Updates the transform buffer with the current transform data.
    /// </summary>
    /// <param name="commandList">The command list used to defer the GPU upload.</param>
    public void UpdateTransformBuffer(CommandList commandList) {
        this._transformBuffer.SetValue(0, this.UseInstancing ? Matrix4x4.Identity : this._transforms[0].GetMatrix());
        this._transformBuffer.UpdateBufferDeferred(commandList);
        this.IsTransformBufferDirty = false;
    }
    
    /// <summary>
    /// Returns the GPU vertex buffer that holds per-instance transform matrices, or
    /// <see langword="null"/> when instancing is disabled.
    /// </summary>
    public DeviceBuffer? GetInstanceVertexBuffer() {
        return this._instanceVertexBuffer;
    }
    
    /// <summary>
    /// Converts the current transforms to matrices and uploads them to the instance vertex buffer.
    /// </summary>
    /// <param name="commandList">The command list used to issue the buffer update.</param>
    public void UpdateInstanceVertexBuffer(CommandList commandList) {
        if (!this.UseInstancing) {
            return;
        }
        
        for (int i = 0; i < this._transforms.Length; i++) {
            this._tempInstanceTransforms[i] = this._transforms[i].GetMatrix();
        }
        
        commandList.UpdateBuffer(this._instanceVertexBuffer, 0, new ReadOnlySpan<Matrix4x4>(this._tempInstanceTransforms, 0, this._transformCount));
        this.IsInstanceVertexBufferDirty = false;
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
        
        if (index < 0 || index >= this.Mesh.BoneCount) {
            throw new ArgumentOutOfRangeException($"Index is out of range. Max bone count for this mesh: {this.Mesh.BoneCount}.");
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
            Array.Fill(this._boneMatrices, Matrix4x4.Identity, 0, (int) this.Mesh.BoneCount);
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
        
        for (int i = 0; i < this.Mesh.BoneCount; i++) {
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
        
        foreach (MaterialMapKey mapKey in this.Material.GetMaterialMapKeys()) {
            MaterialMap? map = this.Material.GetMaterialMap(mapKey);
            int slot = this.Material.GetMaterialMapSlot(mapKey);
            
            if (map == null || slot < 0) {
                continue;
            }
            
            materialData[slot] = new MaterialMapData() {
                Color = map.Color?.ToRgbaFloatVec4() ?? Vector4.Zero,
                Value = map.Value
            };
        }
        
        this._materialDataBuffer.SetValueDeferred(commandList, 0, ref materialData);
        this.Material.IsDirty = false;
        this._hasMaterialChanged = false;
    }
    
    /// <summary>
    /// Allocates a new dynamic vertex buffer large enough for <paramref name="capacity"/> transform matrices.
    /// </summary>
    /// <param name="capacity">The number of instances the buffer must accommodate.</param>
    private DeviceBuffer CreateInstanceVertexBuffer(uint capacity) {
        DeviceBuffer buffer = this.Mesh.GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(capacity * (uint) Marshal.SizeOf<Matrix4x4>(), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        buffer.Name = "InstanceVertexBuffer";
        return buffer;
    }
    
    /// <summary>
    /// Returns the smallest power of two that is ≥ <paramref name="value"/>.
    /// </summary>
    private uint NextPowerOfTwo(uint value) {
        if (value == 0) {
            return 1;
        }
        
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._transformBuffer.Dispose();
            this._instanceVertexBuffer?.Dispose();
            this._boneBuffer?.Dispose();
            this._materialDataBuffer.Dispose();
        }
    }
}