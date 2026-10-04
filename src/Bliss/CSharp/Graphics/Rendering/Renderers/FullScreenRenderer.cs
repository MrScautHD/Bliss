using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Textures;
using Veldrith;

namespace Bliss.CSharp.Graphics.Rendering.Renderers;

public class FullScreenRenderer : Disposable {
    
    /// <summary>
    /// The graphics device used for rendering.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// The vertex buffer that stores the vertex data for rendering a full-screen quad.
    /// </summary>
    private DeviceBuffer _vertexBuffer;

    /// <summary>
    /// Represents the configuration details for creating and managing a simple graphics pipeline.
    /// </summary>
    private SimplePipelineDescription _pipelineDescription;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="FullScreenRenderer"/> class, setting up the necessary buffers and pipeline for full-screen rendering.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for resource creation and rendering.</param>
    public FullScreenRenderer(GraphicsDevice graphicsDevice) {
        this.GraphicsDevice = graphicsDevice;
        
        // Create vertex buffer.
        uint vertexBufferSize = (uint) (6 * Marshal.SizeOf<SpriteVertex2D>());
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer));
        this._vertexBuffer.Name = "VertexBuffer";
        graphicsDevice.UpdateBuffer(this._vertexBuffer, 0, this.GetVertices(graphicsDevice.IsUvOriginTopLeft));
        
        // Create pipeline.
        this._pipelineDescription = new SimplePipelineDescription() {
            PrimitiveTopology = PrimitiveTopology.TriangleList
        };
    }
    
    /// <summary>
    /// Performs a full-screen rendering operation using the specified parameters for configuration.
    /// </summary>
    /// <param name="commandList">The command list used to record rendering commands.</param>
    /// <param name="texture">The 2D texture to be rendered to the full-screen quad.</param>
    /// <param name="framebuffer">The framebuffer used as the render target output.</param>
    /// <param name="effect">The effect to apply during rendering. If null, a default effect is used.</param>
    /// <param name="sampler">The sampler to use for texture sampling. If null, a default point-clamp sampler is used.</param>
    /// <param name="blendState">The blend state description used for blending. If null, a default single alpha blend state is used.</param>
    /// <param name="depthStencilState">The depth-stencil state description for depth and stencil testing. If null, depth-stencil testing is disabled.</param>
    /// <param name="rasterizerState">The rasterizer state description for configuring rasterization. If null, a default state with no culling is used.</param>
    public void Draw(CommandList commandList, Texture2D texture, Framebuffer framebuffer, Effect? effect = null, Sampler? sampler = null, BlendStateDescription? blendState = null, DepthStencilStateDescription? depthStencilState = null, RasterizerStateDescription? rasterizerState = null) {
        Effect finalEffect = effect ?? GlobalResource.DefaultFullScreenRenderPassEffect;
        Sampler finalSampler = sampler ?? GraphicsHelper.GetSampler(this.GraphicsDevice, SamplerType.PointClamp);
        BlendStateDescription finalBlendState = blendState ?? BlendStateDescription.SINGLE_ALPHA_BLEND;
        DepthStencilStateDescription finalDepthStencilState = depthStencilState ?? DepthStencilStateDescription.DISABLED;
        RasterizerStateDescription finalRasterizerState = rasterizerState ?? RasterizerStateDescription.CULL_NONE;
        
        // Update pipeline description.
        this._pipelineDescription.BlendState = finalBlendState;
        this._pipelineDescription.DepthStencilState = finalDepthStencilState;
        this._pipelineDescription.RasterizerState = finalRasterizerState;
        this._pipelineDescription.BufferLayouts = finalEffect.GetBufferLayouts();
        this._pipelineDescription.TextureLayouts = finalEffect.GetTextureLayouts();
        this._pipelineDescription.ShaderSet = new ShaderSetDescription(SpriteVertex2D.VertexLayout.Layouts, finalEffect.Shaders);
        this._pipelineDescription.Outputs = framebuffer.OutputDescription;
        
        // Set vertex buffer.
        commandList.SetVertexBuffer(0, this._vertexBuffer);
        
        // Set pipeline.
        commandList.SetPipeline(finalEffect.GetPipeline(this._pipelineDescription).Pipeline);
        
        // Set resourceSet of the texture.
        commandList.SetGraphicsResourceSet(finalEffect.GetTextureLayoutSlot("fTexture"), texture.GetResourceSet(finalSampler, finalEffect.GetTextureLayout("fTexture")));
        
        // Apply effect.
        finalEffect.Apply(commandList);
        
        // Draw.
        commandList.Draw(6);
    }
    
    /// <summary>
    /// Generates the vertices for a full-screen quad, which can be used for rendering textures or post-processing effects.
    /// </summary>
    /// <param name="isUvOriginTopLeft">Determines if the UV origin is at the top-left corner of the screen.</param>
    /// <returns>An array of Vector4 structures representing the vertices of the quad.</returns>
    private SpriteVertex2D[] GetVertices(bool isUvOriginTopLeft) {
        float top = isUvOriginTopLeft ? 1.0F : 0.0F;
        float bottom = isUvOriginTopLeft ? 0.0F : 1.0F;
        Color color = Color.White;
        
        return [
            new SpriteVertex2D() {
                Position = new Vector3(-1.0F, -1.0F, 0.0F),
                TexCoords = new Vector2(0.0F, top),
                Color = color.ToRgbaFloatVec4()
            },
            
            new SpriteVertex2D() {
                Position = new Vector3(1.0F, -1.0F, 0.0F),
                TexCoords = new Vector2(1.0F, top),
                Color = color.ToRgbaFloatVec4()
            },
            
            new SpriteVertex2D() {
                Position = new Vector3(1.0F, 1.0F, 0.0F),
                TexCoords = new Vector2(1.0F, bottom),
                Color = color.ToRgbaFloatVec4()
            },
            
            new SpriteVertex2D() {
                Position = new Vector3(-1.0F, -1.0F, 0.0F),
                TexCoords = new Vector2(0.0F, top),
                Color = color.ToRgbaFloatVec4()
            },
            
            new SpriteVertex2D() {
                Position = new Vector3(1.0F, 1.0F, 0.0F),
                TexCoords = new Vector2(1.0F, bottom),
                Color = color.ToRgbaFloatVec4()
            },
            
            new SpriteVertex2D() {
                Position = new Vector3(-1.0F, 1.0F, 0.0F),
                TexCoords = new Vector2(0.0F, bottom),
                Color = color.ToRgbaFloatVec4()
            }
        ];
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._vertexBuffer.Dispose();
        }
    }
}