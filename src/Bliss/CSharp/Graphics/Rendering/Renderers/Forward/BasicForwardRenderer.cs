using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.Pipelines.Textures;
using Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Materials.Data;
using Bliss.CSharp.Materials;
using Veldrid;
using Mesh = Bliss.CSharp.Geometry.Mesh;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward;

public class BasicForwardRenderer : Disposable, IRenderer {
    
    /// <summary>
    /// The graphics device used for rendering.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// List of opaque renderables waiting to be drawn.
    /// </summary>
    private List<Renderable> _opaqueRenderables;
    
    /// <summary>
    /// List of translucent renderables waiting to be drawn.
    /// </summary>
    private List<Renderable> _translucentRenderables;
    
    /// <summary>
    /// Uniform buffer storing projection, view, and model matrices.
    /// </summary>
    private SimpleUniformBuffer<Matrix4x4> _matrixBuffer;
    
    /// <summary>
    /// Uniform buffer storing bone matrices for skinned meshes.
    /// </summary>
    private SimpleUniformBuffer<Matrix4x4> _boneBuffer;
    
    /// <summary>
    /// Uniform buffer storing material data.
    /// </summary>
    private SimpleUniformBuffer<MaterialData> _materialDataBuffer;
    
    /// <summary>
    /// A device buffer used to store instance-specific vertex data for rendering instanced objects.
    /// </summary>
    private DeviceBuffer? _instanceVertexBuffer;
    
    /// <summary>
    /// A temporary array of instance transforms used for buffering model transformation matrices during instance rendering.
    /// </summary>
    private Matrix4x4[]? _tempInstanceTransforms;
    
    /// <summary>
    /// The capacity of the instance model buffer, indicating the maximum number of instances that can be stored and rendered in the current buffer allocation.
    /// </summary>
    private uint _instanceCapacity;
    
    /// <summary>
    /// Description of the pipeline used for rendering.
    /// </summary>
    private SimplePipelineDescription _pipelineDescription;
    
    // TODO: Add MultiThread system.
    
    public BasicForwardRenderer(GraphicsDevice graphicsDevice) {
        this.GraphicsDevice = graphicsDevice;
        
        // Create lists for renderables.
        this._opaqueRenderables = new List<Renderable>();
        this._translucentRenderables = new List<Renderable>();
        
        // Create the matrix buffer.
        this._matrixBuffer = new SimpleUniformBuffer<Matrix4x4>(graphicsDevice, 3, ShaderStages.Vertex);
        
        // Create the bone buffer.
        this._boneBuffer = new SimpleUniformBuffer<Matrix4x4>(graphicsDevice, Mesh.MaxBoneCount, ShaderStages.Vertex);
        
        // Create material map buffer.
        this._materialDataBuffer = new SimpleUniformBuffer<MaterialData>(graphicsDevice, 1, ShaderStages.Fragment);
        
        // Create the main pipeline description.
        this._pipelineDescription = new SimplePipelineDescription() {
            DepthStencilState = DepthStencilStateDescription.DEPTH_ONLY_LESS_EQUAL,
            PrimitiveTopology = PrimitiveTopology.TriangleList
        };
    }
    
    /// <summary>
    /// Queues a renderable for drawing.
    /// </summary>
    /// <param name="renderable">The renderable to draw.</param>
    public void DrawRenderable(Renderable renderable) {
        if (renderable.Material.RenderMode == RenderMode.Translucent) {
            this._translucentRenderables.Add(renderable);
        }
        else {
            this._opaqueRenderables.Add(renderable);
        }
    }
    
    /// <summary>
    /// Executes the rendering process for drawing opaque and translucent renderable objects.
    /// </summary>
    /// <param name="commandList">The command list used to execute rendering commands.</param>
    /// <param name="output">The output description specifying the rendering target configuration.</param>
    public void Draw(CommandList commandList, OutputDescription output) {
        Cam3D? cam3D = Cam3D.ActiveCamera;
        
        if (cam3D == null) {
            return;
        }
        
        // Order renderables.
        this._opaqueRenderables.Sort((a, b) => Vector3.DistanceSquared(a.Transforms[0].Translation, cam3D.Position).CompareTo(Vector3.DistanceSquared(b.Transforms[0].Translation, cam3D.Position)));
        this._translucentRenderables.Sort((a, b) => Vector3.DistanceSquared(b.Transforms[0].Translation, cam3D.Position).CompareTo(Vector3.DistanceSquared(a.Transforms[0].Translation, cam3D.Position)));
        
        // Set projection and view matrix to the buffer.
        this._matrixBuffer.SetValue(0, cam3D.GetProjection());
        this._matrixBuffer.SetValue(1, cam3D.GetView());
        
        // Set the pipeline output.
        this._pipelineDescription.Outputs = output;
        
        // Draw opaques renderables.
        foreach (Renderable renderable in this._opaqueRenderables) {
            this.DrawPreparedRenderable(commandList, renderable);
        }
        
        // Draw translucent renderables.
        foreach (Renderable renderable in this._translucentRenderables) {
            this.DrawPreparedRenderable(commandList, renderable);
        }
        
        // Clean up.
        this._opaqueRenderables.Clear();
        this._translucentRenderables.Clear();
    }
    
    /// <summary>
    /// Draws a prepared renderable object, setting up necessary buffers, pipeline parameters, and managing the drawing process.
    /// </summary>
    /// <param name="commandList">The command list used to execute rendering commands.</param>
    /// <param name="renderable">The renderable object to be drawn.</param>
    private void DrawPreparedRenderable(CommandList commandList, Renderable renderable) {
        
        // Update bone buffer.
        if (renderable.BoneMatrices != null) {
            for (int i = 0; i < Mesh.MaxBoneCount; i++) {
                this._boneBuffer.SetValue(i, renderable.BoneMatrices[i]);
            }
            
            this._boneBuffer.UpdateBufferDeferred(commandList);
        }
        
        // Update material buffer.
        MaterialData materialData = new MaterialData {
            RenderMode = renderable.Material.RenderMode
        };
        
        foreach (MaterialMapType mapType in renderable.Material.GetMaterialMapTypes()) {
            MaterialMap? map = renderable.Material.GetMaterialMap(mapType);
            
            if (map != null) {
                materialData[(int) mapType] = new MaterialMapData() {
                    Color = map.Color?.ToRgbaFloatVec4() ?? Vector4.Zero,
                    Value = map.Value
                };
            }
        }
        
        this._materialDataBuffer.SetValueDeferred(commandList, 0, ref materialData);
        
        // Set renderable transform (And updating matrix buffer).
        if (renderable.IsInstanced) {
            this._matrixBuffer.SetValue(2, Matrix4x4.Identity);
        }
        else {
            this._matrixBuffer.SetValue(2, renderable.Transforms[0].GetTransform());
        }
        
        this._matrixBuffer.UpdateBufferDeferred(commandList);
        
        // Set the main pipeline parameters.
        this._pipelineDescription.BlendState = renderable.Material.BlendState;
        this._pipelineDescription.RasterizerState = renderable.Material.RasterizerState;
        this._pipelineDescription.BufferLayouts = renderable.Material.Effect.GetBufferLayouts();
        this._pipelineDescription.TextureLayouts = renderable.Material.Effect.GetTextureLayouts();
        this._pipelineDescription.ShaderSet = renderable.Material.Effect.ShaderSet;
        
        // Set pipeline.
        commandList.SetPipeline(renderable.Material.Effect.GetPipeline(this._pipelineDescription).Pipeline);
        
        // Set matrix buffer.
        commandList.SetGraphicsResourceSet(renderable.Material.Effect.GetBufferLayoutSlot("MatrixBuffer"), this._matrixBuffer.GetResourceSet(renderable.Material.Effect.GetBufferLayout("MatrixBuffer")));
        
        // Set bone buffer.
        commandList.SetGraphicsResourceSet(renderable.Material.Effect.GetBufferLayoutSlot("BoneBuffer"), this._boneBuffer.GetResourceSet(renderable.Material.Effect.GetBufferLayout("BoneBuffer")));
        
        // Set material map buffer.
        commandList.SetGraphicsResourceSet(renderable.Material.Effect.GetBufferLayoutSlot("MaterialBuffer"), this._materialDataBuffer.GetResourceSet(renderable.Material.Effect.GetBufferLayout("MaterialBuffer")));
        
        // Set material texture.
        foreach (SimpleTextureLayout textureLayout in renderable.Material.Effect.GetTextureLayouts()) {
            foreach (MaterialMapType mapType in renderable.Material.GetMaterialMapTypes()) {
                if (textureLayout.Name == mapType.GetName()) {
                    string mapName = textureLayout.Name;
                    MaterialMap map = renderable.Material.GetMaterialMap(mapType)!;
                    ResourceSet? resourceSet = map.GetTextureResourceSet(map.Sampler ?? GraphicsHelper.GetSampler(this.GraphicsDevice, SamplerType.PointWrap), renderable.Material.Effect.GetTextureLayout(mapName));
                    
                    if (resourceSet != null) {
                        commandList.SetGraphicsResourceSet(renderable.Material.Effect.GetTextureLayoutSlot(mapName), resourceSet);
                    }
                }
            }
        }
        
        // Apply effect.
        renderable.Material.Effect.Apply(commandList, renderable.Material);
        
        // Ensure the instance-matrix vertex buffer is large enough for this draw call
        this.EnsureInstanceModelBufferCapacity(renderable.InstanceCount);
        
        // Draw renderable and set vertex/index buffers.
        if (renderable.Mesh.IndexCount > 0) {
            
            // Set vertex and index buffer.
            commandList.SetVertexBuffer(0, renderable.Mesh.VertexBuffer);
            commandList.SetIndexBuffer(renderable.Mesh.IndexBuffer, IndexFormat.UInt32);
            
            if (renderable.IsInstanced) {
                
                // Set the temp instance transformations.
                for (int i = 0; i < renderable.InstanceCount; i++) {
                    this._tempInstanceTransforms?[i] = renderable.Transforms[i].GetTransform();
                }
                
                // Set the instance buffer.
                commandList.UpdateBuffer(this._instanceVertexBuffer, 0, new ReadOnlySpan<Matrix4x4>(this._tempInstanceTransforms, 0, (int) renderable.InstanceCount));
                commandList.SetVertexBuffer(1, this._instanceVertexBuffer);
                
                // Draw.
                commandList.DrawIndexed(renderable.Mesh.IndexCount, renderable.InstanceCount, 0, 0, 0);
            }
            else {
                
                // Set the instance buffer. (Set to Matrix4x4.Identity to avoid braking the shader)
                commandList.UpdateBuffer(this._instanceVertexBuffer, 0, Matrix4x4.Identity);
                commandList.SetVertexBuffer(1, this._instanceVertexBuffer);
                
                // Draw.
                commandList.DrawIndexed(renderable.Mesh.IndexCount);
            }
        }
        else {
            
            // Set vertex buffer.
            commandList.SetVertexBuffer(0, renderable.Mesh.VertexBuffer);
            
            if (renderable.IsInstanced) {
                
                // Set the temp instance transformations.
                for (int i = 0; i < renderable.InstanceCount; i++) {
                    this._tempInstanceTransforms?[i] = renderable.Transforms[i].GetTransform();
                }
                
                // Set the instance buffer.
                commandList.UpdateBuffer(this._instanceVertexBuffer, 0, new ReadOnlySpan<Matrix4x4>(this._tempInstanceTransforms, 0, (int) renderable.InstanceCount));
                commandList.SetVertexBuffer(1, this._instanceVertexBuffer);
                
                // Draw.
                commandList.Draw(renderable.Mesh.VertexCount, renderable.InstanceCount, 0, 0);
            }
            else {
                
                // Set the instance buffer. (Set to Matrix4x4.Identity to avoid braking the shader)
                commandList.UpdateBuffer(this._instanceVertexBuffer, 0, Matrix4x4.Identity);
                commandList.SetVertexBuffer(1, this._instanceVertexBuffer);
                
                // Draw.
                commandList.Draw(renderable.Mesh.VertexCount);
            }
        }
        
        // Clear temp data.
        if (this._tempInstanceTransforms != null) {
            Array.Clear(this._tempInstanceTransforms);
        }
    }
    
    /// <summary>
    /// Ensures that the instance model buffer has enough capacity to accommodate the specified number of instances.
    /// Increases the buffer size if needed, growing to the next power of two to minimize frequent reallocations.
    /// </summary>
    /// <param name="requiredInstanceCount">The number of instances needed for rendering.</param>
    private void EnsureInstanceModelBufferCapacity(uint requiredInstanceCount) {
        
        // Nothing to allocate if we don't draw any instances.
        if (requiredInstanceCount == 0) {
            return;
        }
        
        // If the current buffer exists and is large enough, keep it.
        if (this._instanceVertexBuffer != null && this._instanceCapacity >= requiredInstanceCount) {
            return;
        }
        
        // Grow to the next power of two to avoid reallocating every frame.
        uint newCapacity = this._instanceCapacity == 0 ? 1 : this._instanceCapacity;
        
        while (newCapacity < requiredInstanceCount) {
            newCapacity <<= 1;
        }
        
        // Persist the new capacity.
        this._instanceCapacity = newCapacity;
        
        // Recreate the instance transform array.
        this._tempInstanceTransforms = new Matrix4x4[this._instanceCapacity];
        
        // Recreate buffer with the new size.
        this._instanceVertexBuffer?.Dispose();
        this._instanceVertexBuffer = this.GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(this._instanceCapacity * (uint) Marshal.SizeOf<Matrix4x4>(), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._matrixBuffer.Dispose();
            this._boneBuffer.Dispose();
            this._materialDataBuffer.Dispose();
            this._instanceVertexBuffer?.Dispose();
        }
    }
}