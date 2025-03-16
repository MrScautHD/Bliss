using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Textures;
using Veldrid;

namespace Bliss.CSharp.Graphics.Rendering.Passes;

public class FullScreenRenderPass : Disposable {
    
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
    /// Initializes a new instance of the <see cref="FullScreenRenderPass"/> class, setting up the necessary buffers and pipeline for full-screen rendering.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for resource creation and rendering.</param>
    public FullScreenRenderPass(GraphicsDevice graphicsDevice) {
        this.GraphicsDevice = graphicsDevice;
        
        // Create vertex buffer.
        uint vertexBufferSize = (uint) (6 * Marshal.SizeOf<SpriteVertex2D>());
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        graphicsDevice.UpdateBuffer(this._vertexBuffer, 0, this.GetVertices(graphicsDevice.IsUvOriginTopLeft));
        
        // Create pipeline.
        this._pipelineDescription = this.CreatePipelineDescription();
    }

    /// <summary>
    /// Executes the draw call using the provided command list, render texture, effect, and optional sampler.
    /// </summary>
    /// <param name="commandList">The command list used to issue draw commands.</param>
    /// <param name="renderTexture">The render texture used as the target for rendering.</param>
    /// <param name="output">The output description specifying render target and depth-stencil formats.</param>
    /// <param name="effect">The effect (shader) to be used during the render pass. If null, the default effect associated with the render pass is used.</param>
    /// <param name="sampler">The optional sampler for texture sampling. If null, a default sampler is applied.</param>
    public void Draw(CommandList commandList, RenderTexture2D renderTexture, OutputDescription output, Effect? effect = null, Sampler? sampler = null) {
        Effect finalEffect = effect ?? GlobalResource.DefaultFullScreenRenderPassEffect;
        Sampler finalSampler = sampler ?? GraphicsHelper.GetSampler(this.GraphicsDevice, SamplerType.Point);

        // Update pipeline description.
        this._pipelineDescription.BufferLayouts = finalEffect.GetBufferLayouts();
        this._pipelineDescription.TextureLayouts = finalEffect.GetTextureLayouts();
        this._pipelineDescription.ShaderSet = finalEffect.ShaderSet;
        this._pipelineDescription.Outputs = output;
        
        // Set vertex and index buffer.
        commandList.SetVertexBuffer(0, this._vertexBuffer);
        
        // Set pipeline.
        commandList.SetPipeline(finalEffect.GetPipeline(this._pipelineDescription).Pipeline);
        
        // Set resourceSet of the texture.
        commandList.SetGraphicsResourceSet(0, renderTexture.GetResourceSet(finalSampler, finalEffect.GetTextureLayout("fTexture").Layout));
        
        // Apply effect.
        finalEffect.Apply();
        
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
                Position = new Vector2(-1.0F, -1.0F),
                TexCoords = new Vector2(0.0F, top),
                Color = color.ToRgbaFloatVec4()
            },
        
            new SpriteVertex2D() {
                Position = new Vector2(1.0F, -1.0F),
                TexCoords = new Vector2(1.0F, top),
                Color = color.ToRgbaFloatVec4()
            },
        
            new SpriteVertex2D() {
                Position = new Vector2(1.0F, 1.0F),
                TexCoords = new Vector2(1.0F, bottom),
                Color = color.ToRgbaFloatVec4()
            },
        
            new SpriteVertex2D() {
                Position = new Vector2(-1.0F, -1.0F),
                TexCoords = new Vector2(0.0F, top),
                Color = color.ToRgbaFloatVec4()
            },
            
            new SpriteVertex2D() {
                Position = new Vector2(1.0F, 1.0F),
                TexCoords = new Vector2(1.0F, bottom),
                Color = color.ToRgbaFloatVec4()
            },
            
            new SpriteVertex2D() {
                Position = new Vector2(-1.0F, 1.0F),
                TexCoords = new Vector2(0.0F, bottom),
                Color = color.ToRgbaFloatVec4()
            }
        ];
    }
    
    /// <summary>
    /// Creates and returns a <see cref="SimplePipelineDescription"/> configured for full-screen rendering.
    /// </summary>
    /// <returns>A configured <see cref="SimplePipelineDescription"/> object.</returns>
    private SimplePipelineDescription CreatePipelineDescription() {
        return new SimplePipelineDescription() {
            BlendState = BlendState.AlphaBlend.Description,
            DepthStencilState = new DepthStencilStateDescription(false, false, ComparisonKind.LessEqual),
            RasterizerState = new RasterizerStateDescription() {
                DepthClipEnabled = true,
                CullMode = FaceCullMode.None
            },
            PrimitiveTopology = PrimitiveTopology.TriangleList
        };
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._vertexBuffer.Dispose();
        }
    }
}