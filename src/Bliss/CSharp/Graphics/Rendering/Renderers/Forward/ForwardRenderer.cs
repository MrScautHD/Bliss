using System.Numerics;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.Pipelines.Textures;
using Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Renderables;
using Bliss.CSharp.Materials;
using Veldrid;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward;

public class ForwardRenderer : Disposable {
    
    // TODO:
    // - Bone support
    // - RenderMode (DONE)
    // - Light support
    // - Shadow map support
    
    private const int MaxBoneCount = 72;
    
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    private List<Renderable> _opaqueRenderables;
    private List<Renderable> _translucentRenderables;
    
    private SimpleBuffer<Matrix4x4> _matrixBuffer;
    private SimpleBuffer<Matrix4x4> _boneBuffer;
    private SimpleBuffer<MaterialData> _materialDataBuffer;
    
    private SimplePipelineDescription _pipelineDescription;
    
    public ForwardRenderer(GraphicsDevice graphicsDevice) {
        this.GraphicsDevice = graphicsDevice;
        
        // Create list for renderables.
        this._opaqueRenderables = new List<Renderable>();
        this._translucentRenderables = new List<Renderable>();
        
        // Create matrix buffer.
        this._matrixBuffer = new SimpleBuffer<Matrix4x4>(graphicsDevice, 3, SimpleBufferType.Uniform, ShaderStages.Vertex);
        
        // Create bone buffer.
        this._boneBuffer = new SimpleBuffer<Matrix4x4>(graphicsDevice, MaxBoneCount, SimpleBufferType.Uniform, ShaderStages.Vertex);
        
        for (int i = 0; i < MaxBoneCount; i++) {
            this._boneBuffer.SetValue(i, Matrix4x4.Identity);
        }
        
        this._boneBuffer.UpdateBufferImmediate();
        
        // Create material map buffer.
        this._materialDataBuffer = new SimpleBuffer<MaterialData>(graphicsDevice, 1, SimpleBufferType.Uniform, ShaderStages.Fragment);
        
        // Create pipeline description.
        this._pipelineDescription = new SimplePipelineDescription() {
            DepthStencilState = DepthStencilStateDescription.DEPTH_ONLY_LESS_EQUAL,
            PrimitiveTopology = PrimitiveTopology.TriangleList
        };
    }
    
    public void AddRenderable(Renderable renderable) {
        if (renderable.Mesh.Material.RenderMode == RenderMode.Translucent) {
            this._translucentRenderables.Add(renderable);
        }
        else {
            this._opaqueRenderables.Add(renderable);
        }
    }
    
    public void Draw(CommandList commandList, OutputDescription output) {
        Cam3D? cam3D = Cam3D.ActiveCamera;
        
        if (cam3D == null) {
            return;
        }

        // Order renderables.
        IOrderedEnumerable<Renderable> opaquesRenderables = this._opaqueRenderables.OrderBy(renderable => Vector3.Distance(renderable.Transform.Translation, cam3D.Position));
        IOrderedEnumerable<Renderable> translucentRenderables = this._translucentRenderables.OrderBy(renderable => -Vector3.Distance(renderable.Transform.Translation, cam3D.Position));
        
        // Set projection and view matrix to the buffer.
        this._matrixBuffer.SetValue(0, cam3D.GetProjection());
        this._matrixBuffer.SetValue(1, cam3D.GetView());
        
        // Set pipeline output.
        this._pipelineDescription.Outputs = output;
        
        // Draw opaques renderables.
        foreach (Renderable renderable in opaquesRenderables) {
            this.DrawRenderable(commandList, renderable);
        }
        
        // Draw translucent renderables.
        foreach (Renderable renderable in translucentRenderables) {
            this.DrawRenderable(commandList, renderable);
        }
        
        // Clean up.
        this._opaqueRenderables.Clear();
        this._translucentRenderables.Clear();
    }
    
    private void DrawRenderable(CommandList commandList, Renderable renderable) {
        
        // TODO: ADD BONE BUFFER
        
        // Update material buffer.
        MaterialData materialData = new MaterialData {
            RenderMode = (int) renderable.Mesh.Material.RenderMode
        };
        
        foreach (MaterialMapType mapType in renderable.Mesh.Material.GetMaterialMapTypes()) {
            MaterialMap? map = renderable.Mesh.Material.GetMaterialMap(mapType);
            
            if (map != null) {
                materialData.SetColor((uint) mapType, map.Color?.ToRgbaFloatVec4() ?? Vector4.Zero);
                materialData.SetValue((uint) mapType, map.Value);
            }
        }
        
        this._materialDataBuffer.SetValueDeferred(commandList, 0, materialData);
        
        // Set renderable transform (And updating matrix buffer).
        this._matrixBuffer.SetValue(2, renderable.Transform.GetTransform());
        this._matrixBuffer.UpdateBuffer(commandList);
        
        // Set pipeline parameters.
        this._pipelineDescription.BlendState = renderable.Mesh.Material.BlendState;
        this._pipelineDescription.RasterizerState = renderable.Mesh.Material.RasterizerState;
        this._pipelineDescription.BufferLayouts = renderable.Mesh.Material.Effect.GetBufferLayouts();
        this._pipelineDescription.TextureLayouts = renderable.Mesh.Material.Effect.GetTextureLayouts();
        this._pipelineDescription.ShaderSet = renderable.Mesh.Material.Effect.ShaderSet;
        
        if (renderable.Mesh.IndexCount > 0) {
            
            // Set vertex and index buffer.
            commandList.SetVertexBuffer(0, renderable.Mesh.VertexBuffer);
            commandList.SetIndexBuffer(renderable.Mesh.IndexBuffer, IndexFormat.UInt32);
            
            // Set pipeline.
            commandList.SetPipeline(renderable.Mesh.Material.Effect.GetPipeline(this._pipelineDescription).Pipeline);
            
            // Set matrix buffer.
            commandList.SetGraphicsResourceSet(renderable.Mesh.Material.Effect.GetBufferLayoutSlot("MatrixBuffer"), this._matrixBuffer.GetResourceSet(renderable.Mesh.Material.Effect.GetBufferLayout("MatrixBuffer")));
            
            // Set bone buffer.
            commandList.SetGraphicsResourceSet(renderable.Mesh.Material.Effect.GetBufferLayoutSlot("BoneBuffer"), this._boneBuffer.GetResourceSet(renderable.Mesh.Material.Effect.GetBufferLayout("BoneBuffer")));
            
            // Set material map buffer.
            commandList.SetGraphicsResourceSet(renderable.Mesh.Material.Effect.GetBufferLayoutSlot("MaterialBuffer"), this._materialDataBuffer.GetResourceSet(renderable.Mesh.Material.Effect.GetBufferLayout("MaterialBuffer")));
            
            // Set material texture.
            foreach (SimpleTextureLayout textureLayout in renderable.Mesh.Material.Effect.GetTextureLayouts()) {
                foreach (MaterialMapType mapType in renderable.Mesh.Material.GetMaterialMapTypes()) {
                    if (textureLayout.Name == mapType.GetName()) {
                        string mapName = textureLayout.Name;
                        MaterialMap map = renderable.Mesh.Material.GetMaterialMap(mapType)!;
                        ResourceSet? resourceSet = map.GetTextureResourceSet(map.Sampler ?? GraphicsHelper.GetSampler(this.GraphicsDevice, SamplerType.PointWrap), renderable.Mesh.Material.Effect.GetTextureLayout(mapName));
                        
                        if (resourceSet != null) {
                            commandList.SetGraphicsResourceSet(renderable.Mesh.Material.Effect.GetTextureLayoutSlot(mapName), resourceSet);
                        }
                    }
                }
            }
            
            // Apply effect.
            renderable.Mesh.Material.Effect.Apply(commandList, renderable.Mesh.Material);
            
            // Draw.
            commandList.DrawIndexed(renderable.Mesh.IndexCount);
        }
        else {
                        
            // Set vertex and index buffer.
            commandList.SetVertexBuffer(0, renderable.Mesh.VertexBuffer);
            
            // Set pipeline.
            commandList.SetPipeline(renderable.Mesh.Material.Effect.GetPipeline(this._pipelineDescription).Pipeline);
            
            // Set matrix buffer.
            commandList.SetGraphicsResourceSet(renderable.Mesh.Material.Effect.GetBufferLayoutSlot("MatrixBuffer"), this._matrixBuffer.GetResourceSet(renderable.Mesh.Material.Effect.GetBufferLayout("MatrixBuffer")));
            
            // Set bone buffer.
            commandList.SetGraphicsResourceSet(renderable.Mesh.Material.Effect.GetBufferLayoutSlot("BoneBuffer"), this._boneBuffer.GetResourceSet(renderable.Mesh.Material.Effect.GetBufferLayout("BoneBuffer")));
            
            // Set material map buffer.
            commandList.SetGraphicsResourceSet(renderable.Mesh.Material.Effect.GetBufferLayoutSlot("MaterialBuffer"), this._materialDataBuffer.GetResourceSet(renderable.Mesh.Material.Effect.GetBufferLayout("MaterialBuffer")));
            
            // Set material texture.
            foreach (SimpleTextureLayout textureLayout in renderable.Mesh.Material.Effect.GetTextureLayouts()) {
                foreach (MaterialMapType mapType in renderable.Mesh.Material.GetMaterialMapTypes()) {
                    if (textureLayout.Name == mapType.GetName()) {
                        string mapName = textureLayout.Name;
                        MaterialMap map = renderable.Mesh.Material.GetMaterialMap(mapType)!;
                        ResourceSet? resourceSet = map.GetTextureResourceSet(map.Sampler ?? GraphicsHelper.GetSampler(this.GraphicsDevice, SamplerType.PointWrap), renderable.Mesh.Material.Effect.GetTextureLayout(mapName));
                        
                        if (resourceSet != null) {
                            commandList.SetGraphicsResourceSet(renderable.Mesh.Material.Effect.GetTextureLayoutSlot(mapName), resourceSet);
                        }
                    }
                }
            }
            
            // Apply effect.
            renderable.Mesh.Material.Effect.Apply(commandList, renderable.Mesh.Material);
            
            // Draw.
            commandList.Draw(renderable.Mesh.VertexCount);
        }
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._matrixBuffer.Dispose();
            this._boneBuffer.Dispose();
            this._materialDataBuffer.Dispose();
        }
    }
}