using System.Numerics;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.Pipelines.Textures;
using Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Lighting.Data;
using Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Lighting.Handlers;
using Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Lighting.Shadowing;
using Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Materials.Data;
using Bliss.CSharp.Materials;
using Veldrid;
using Mesh = Bliss.CSharp.Geometry.Mesh;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward;

public class ForwardRenderer<T> : Disposable, IRenderer where T : unmanaged {
    
    /// <summary>
    /// The graphics device used for rendering.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// The light handler responsible for managing and updating light data for rendering operations.
    /// </summary>
    public ILightHandler<T>? LightHandler { get; private set; }
    
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
    /// Uniform buffer storing shadow data.
    /// </summary>
    private SimpleUniformBuffer<ShadowData>? _shadowDataBuffer; // TODO: Not needed for shadow map shader but maybe for the standard shader...
    
    /// <summary>
    /// Uniform buffer storing light data.
    /// </summary>
    private ISimpleBuffer? _lightDataBuffer;
    
    /// <summary>
    /// Description of the pipeline used for rendering.
    /// </summary>
    private SimplePipelineDescription _mainPipelineDescription;
    
    /// <summary>
    /// Description of the shadow pipeline used for rendering the shadows.
    /// </summary>
    private SimplePipelineDescription _shadowPipelineDescription;
    
    // TODO: Add MultiThread system.
    
    public ForwardRenderer(GraphicsDevice graphicsDevice, ILightHandler<T>? lightHandler = null) {
        this.GraphicsDevice = graphicsDevice;
        this.LightHandler = lightHandler;
        
        // Create lists for renderables.
        this._opaqueRenderables = new List<Renderable>();
        this._translucentRenderables = new List<Renderable>();
        
        // Create the matrix buffer.
        this._matrixBuffer = new SimpleUniformBuffer<Matrix4x4>(graphicsDevice, 3, ShaderStages.Vertex);
        
        // Create the bone buffer.
        this._boneBuffer = new SimpleUniformBuffer<Matrix4x4>(graphicsDevice, Mesh.MaxBoneCount, ShaderStages.Vertex);
        
        // Create material map buffer.
        this._materialDataBuffer = new SimpleUniformBuffer<MaterialData>(graphicsDevice, 1, ShaderStages.Fragment);
        
        // Create buffers for the light/shadow system.
        if (lightHandler != null) {
            
            // Create the shadow buffer.
            if (lightHandler.ShadowEffect != null) {
                this._shadowDataBuffer = new SimpleUniformBuffer<ShadowData>(graphicsDevice, 1, ShaderStages.Fragment);
            }
            
            // Create the light buffer.
            if (!lightHandler.UseStorageBuffer) {
                this._lightDataBuffer = new SimpleUniformBuffer<T>(graphicsDevice, 1, ShaderStages.Fragment);
            }
            else {
                this._lightDataBuffer = new SimpleStructuredBuffer<T, Light>(graphicsDevice, 1, (uint) lightHandler.LightCapacity, ShaderStages.Fragment);
            }
        }
        
        // Create the main pipeline description.
        this._mainPipelineDescription = new SimplePipelineDescription() {
            DepthStencilState = DepthStencilStateDescription.DEPTH_ONLY_LESS_EQUAL,
            PrimitiveTopology = PrimitiveTopology.TriangleList
        };
        
        // Create the shadow pipeline description.
        if (lightHandler?.ShadowEffect != null) {
            this._shadowPipelineDescription = new SimplePipelineDescription() {
                BlendState = BlendStateDescription.EMPTY,
                DepthStencilState = DepthStencilStateDescription.DEPTH_ONLY_LESS_EQUAL,
                RasterizerState = RasterizerStateDescription.DEFAULT,
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                BufferLayouts = lightHandler.ShadowEffect.GetBufferLayouts(),
                TextureLayouts = lightHandler.ShadowEffect.GetTextureLayouts(),
                ShaderSet = lightHandler.ShadowEffect.ShaderSet
            };
        }
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
    public void Draw(CommandList commandList, Framebuffer framebuffer) {
        Cam3D? cam3D = Cam3D.ActiveCamera;
        
        if (cam3D == null) {
            return;
        }
        
        // Order renderables.
        this._opaqueRenderables.Sort((a, b) => Vector3.DistanceSquared(a.Transform.Translation, cam3D.Position).CompareTo(Vector3.DistanceSquared(b.Transform.Translation, cam3D.Position)));
        this._translucentRenderables.Sort((a, b) => Vector3.DistanceSquared(b.Transform.Translation, cam3D.Position).CompareTo(Vector3.DistanceSquared(a.Transform.Translation, cam3D.Position)));
        
        // Draw.
        this.DrawShadowScene(commandList, framebuffer, cam3D);
        this.DrawScene(commandList, framebuffer.OutputDescription, cam3D);
        
        // Clean up.
        this._opaqueRenderables.Clear();
        this._translucentRenderables.Clear();
    }

    private void DrawScene(CommandList commandList, OutputDescription output, Cam3D cam) {
        
        // Set projection and view matrix to the buffer.
        this._matrixBuffer.SetValue(0, cam.GetProjection());
        this._matrixBuffer.SetValue(1, cam.GetView());
        
        // Update light buffer.
        if (this.LightHandler != null && this._lightDataBuffer != null) {
            if (!this.LightHandler.UseStorageBuffer) {
                ((SimpleUniformBuffer<T>) this._lightDataBuffer).SetValueImmediate(0, this.LightHandler.LightData);
            }
            else {
                SimpleStructuredBuffer<T, Light> buffer = (SimpleStructuredBuffer<T, Light>) this._lightDataBuffer;
                buffer.SetHeaderValue(0, this.LightHandler.LightData);
                
                for (int i = 0; i < this.LightHandler.GetNumOfLights(); i++) {
                    buffer.SetElementValue(i, this.LightHandler.GetLights()[i]);
                }
                
                buffer.UpdateBufferImmediate();
            }
        }
        
        // Set the main pipeline output.
        this._mainPipelineDescription.Outputs = output;
        
        // Draw opaques renderables.
        foreach (Renderable renderable in this._opaqueRenderables) {
            this.DrawPreparedRenderable(commandList, renderable);
        }
        
        // Draw translucent renderables.
        foreach (Renderable renderable in this._translucentRenderables) {
            this.DrawPreparedRenderable(commandList, renderable);
        }
    }

    public void DrawShadowScene(CommandList commandList, Framebuffer framebuffer, Cam3D cam) {
        if (this.LightHandler?.ShadowEffect == null) {
            return;
        }
        
        foreach (Light light in this.LightHandler.GetLights()) {
            ShadowMap? shadowMap = this.LightHandler.GetShadowMapByLightId(light.Id);
            
            if (shadowMap != null) {
                
                commandList.SetFramebuffer(shadowMap.Framebuffer);
                commandList.ClearDepthStencil(1f);
                commandList.SetViewport(0, new Viewport(0, 0, shadowMap.Resolution, shadowMap.Resolution, 0f, 1f));
                commandList.SetScissorRect(0, 0, 0, framebuffer.Width, framebuffer.Height);
                
                // Set shadow pipeline output.
                this._shadowPipelineDescription.Outputs = shadowMap.Framebuffer.OutputDescription;
                
                // Set projection and view matrix to the buffer.
                this._matrixBuffer.SetValue(0, light.GetProjection());
                this._matrixBuffer.SetValue(1, light.GetView());
                
                // Draw opaques renderables.
                foreach (Renderable renderable in this._opaqueRenderables) {
                    if (renderable.Material.Effect.GetBufferLayouts().Any(b => b.Name == "LightBuffer")) {
                        this.DrawPreparedShadowedRenderable(commandList, renderable, this.LightHandler.ShadowEffect);
                    }
                }
                
                // Draw translucent renderables.
                foreach (Renderable renderable in this._translucentRenderables) {
                    if (renderable.Material.Effect.GetBufferLayouts().Any(b => b.Name == "LightBuffer")) {
                        this.DrawPreparedShadowedRenderable(commandList, renderable, this.LightHandler.ShadowEffect);
                    }
                }
            }
        }
        
        commandList.SetFramebuffer(framebuffer);
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
        this._matrixBuffer.SetValue(2, renderable.Transform.GetTransform());
        this._matrixBuffer.UpdateBufferDeferred(commandList);
        
        // Set the main pipeline parameters.
        this._mainPipelineDescription.BlendState = renderable.Material.BlendState;
        this._mainPipelineDescription.RasterizerState = renderable.Material.RasterizerState;
        this._mainPipelineDescription.BufferLayouts = renderable.Material.Effect.GetBufferLayouts();
        this._mainPipelineDescription.TextureLayouts = renderable.Material.Effect.GetTextureLayouts();
        this._mainPipelineDescription.ShaderSet = renderable.Material.Effect.ShaderSet;
        
        if (renderable.Mesh.IndexCount > 0) {
            
            // Set vertex and index buffer.
            commandList.SetVertexBuffer(0, renderable.Mesh.VertexBuffer);
            commandList.SetIndexBuffer(renderable.Mesh.IndexBuffer, IndexFormat.UInt32);
            
            // Set pipeline.
            commandList.SetPipeline(renderable.Material.Effect.GetPipeline(this._mainPipelineDescription).Pipeline);
            
            // Set matrix buffer.
            commandList.SetGraphicsResourceSet(renderable.Material.Effect.GetBufferLayoutSlot("MatrixBuffer"), this._matrixBuffer.GetResourceSet(renderable.Material.Effect.GetBufferLayout("MatrixBuffer")));
            
            // Set bone buffer.
            commandList.SetGraphicsResourceSet(renderable.Material.Effect.GetBufferLayoutSlot("BoneBuffer"), this._boneBuffer.GetResourceSet(renderable.Material.Effect.GetBufferLayout("BoneBuffer")));
            
            // Set material map buffer.
            commandList.SetGraphicsResourceSet(renderable.Material.Effect.GetBufferLayoutSlot("MaterialBuffer"), this._materialDataBuffer.GetResourceSet(renderable.Material.Effect.GetBufferLayout("MaterialBuffer")));
            
            // Set light buffer.
            if (this._lightDataBuffer != null && renderable.Material.Effect.GetBufferLayouts().Any(b => b.Name == "LightBuffer")) {
                commandList.SetGraphicsResourceSet(renderable.Material.Effect.GetBufferLayoutSlot("LightBuffer"), this._lightDataBuffer.GetResourceSet(renderable.Material.Effect.GetBufferLayout("LightBuffer")));
            }
            
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
            
            // Draw.
            commandList.DrawIndexed(renderable.Mesh.IndexCount);
        }
        else {
                        
            // Set vertex buffer.
            commandList.SetVertexBuffer(0, renderable.Mesh.VertexBuffer);
            
            // Set pipeline.
            commandList.SetPipeline(renderable.Material.Effect.GetPipeline(this._mainPipelineDescription).Pipeline);
            
            // Set matrix buffer.
            commandList.SetGraphicsResourceSet(renderable.Material.Effect.GetBufferLayoutSlot("MatrixBuffer"), this._matrixBuffer.GetResourceSet(renderable.Material.Effect.GetBufferLayout("MatrixBuffer")));
            
            // Set bone buffer.
            commandList.SetGraphicsResourceSet(renderable.Material.Effect.GetBufferLayoutSlot("BoneBuffer"), this._boneBuffer.GetResourceSet(renderable.Material.Effect.GetBufferLayout("BoneBuffer")));
            
            // Set material map buffer.
            commandList.SetGraphicsResourceSet(renderable.Material.Effect.GetBufferLayoutSlot("MaterialBuffer"), this._materialDataBuffer.GetResourceSet(renderable.Material.Effect.GetBufferLayout("MaterialBuffer")));
            
            // Set light buffer.
            if (this.LightHandler != null && this._lightDataBuffer != null && renderable.Material.Effect.GetBufferLayouts().Any(b => b.Name == "LightBuffer")) {
                commandList.SetGraphicsResourceSet(renderable.Material.Effect.GetBufferLayoutSlot("LightBuffer"), this._lightDataBuffer.GetResourceSet(renderable.Material.Effect.GetBufferLayout("LightBuffer")));
            }
            
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
            
            // Draw.
            commandList.Draw(renderable.Mesh.VertexCount);
        }
    }
    
    /// <summary>
    /// Draws a prepared renderable object with shadowing effects applied.
    /// </summary>
    /// <param name="commandList">The command list to record drawing commands.</param>
    /// <param name="renderable">The renderable object to be drawn.</param>
    /// <param name="shadowEffect">The effect used to apply shadowing to the renderable.</param>
    private void DrawPreparedShadowedRenderable(CommandList commandList, Renderable renderable, Effect shadowEffect) {
        
        // Update bone buffer.
        if (renderable.BoneMatrices != null) {
            for (int i = 0; i < Mesh.MaxBoneCount; i++) {
                this._boneBuffer.SetValue(i, renderable.BoneMatrices[i]);
            }
            
            this._boneBuffer.UpdateBufferDeferred(commandList);
        }
        
        // Set renderable transform (And updating matrix buffer).
        this._matrixBuffer.SetValue(2, renderable.Transform.GetTransform());
        this._matrixBuffer.UpdateBufferDeferred(commandList);
        
        if (renderable.Mesh.IndexCount > 0) {
            
            // Set vertex and index buffer.
            commandList.SetVertexBuffer(0, renderable.Mesh.VertexBuffer);
            commandList.SetIndexBuffer(renderable.Mesh.IndexBuffer, IndexFormat.UInt32);
            
            // Set pipeline.
            commandList.SetPipeline(shadowEffect.GetPipeline(this._shadowPipelineDescription).Pipeline);
            
            // Set matrix buffer.
            commandList.SetGraphicsResourceSet(shadowEffect.GetBufferLayoutSlot("MatrixBuffer"), this._matrixBuffer.GetResourceSet(shadowEffect.GetBufferLayout("MatrixBuffer")));
            
            // Set bone buffer.
            commandList.SetGraphicsResourceSet(shadowEffect.GetBufferLayoutSlot("BoneBuffer"), this._boneBuffer.GetResourceSet(shadowEffect.GetBufferLayout("BoneBuffer")));
            
            // Apply effect.
            shadowEffect.Apply(commandList, renderable.Material);
            
            // Draw.
            commandList.DrawIndexed(renderable.Mesh.IndexCount);
        }
        else {
            
            // Set vertex buffer.
            commandList.SetVertexBuffer(0, renderable.Mesh.VertexBuffer);
            
            // Set pipeline.
            commandList.SetPipeline(shadowEffect.GetPipeline(this._shadowPipelineDescription).Pipeline);
            
            // Set matrix buffer.
            commandList.SetGraphicsResourceSet(shadowEffect.GetBufferLayoutSlot("MatrixBuffer"), this._matrixBuffer.GetResourceSet(shadowEffect.GetBufferLayout("MatrixBuffer")));
            
            // Set bone buffer.
            commandList.SetGraphicsResourceSet(shadowEffect.GetBufferLayoutSlot("BoneBuffer"), this._boneBuffer.GetResourceSet(shadowEffect.GetBufferLayout("BoneBuffer")));
            
            // Apply effect.
            shadowEffect.Apply(commandList, renderable.Material);
            
            // Draw.
            commandList.Draw(renderable.Mesh.VertexCount);
        }
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._matrixBuffer.Dispose();
            this._boneBuffer.Dispose();
            this._materialDataBuffer.Dispose();
            this._lightDataBuffer?.Dispose();
        }
    }
}