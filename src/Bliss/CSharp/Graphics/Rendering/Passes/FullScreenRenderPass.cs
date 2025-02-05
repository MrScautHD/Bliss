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
    /// The output description for the rendering pass.
    /// </summary>
    public OutputDescription Output { get; private set; }

    /// <summary>
    /// The shader effect used for rendering in the full-screen pass.
    /// </summary>
    private Effect _effect;
    
    /// <summary>
    /// The vertex buffer that stores the vertex data for rendering a full-screen quad.
    /// </summary>
    private DeviceBuffer _vertexBuffer;

    /// <summary>
    /// Represents the configuration details for creating and managing a simple graphics pipeline.
    /// </summary>
    private SimplePipelineDescription _pipelineDescription;
    
    // TODO: Check pipeline + effect to maybe allow post processing to!
    /// <summary>
    /// Initializes a new instance of the <see cref="FullScreenRenderPass"/> class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device to use for rendering.</param>
    /// <param name="output">The output description for the rendering pass.</param>
    public FullScreenRenderPass(GraphicsDevice graphicsDevice, OutputDescription output) {
        this.GraphicsDevice = graphicsDevice;
        this.Output = output;
        this._effect = GlobalResource.FullScreenRenderPassEffect;

        uint vertexBufferSize = (uint) (6 * Marshal.SizeOf<SpriteVertex2D>());
        
        // Create vertex buffer.
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        graphicsDevice.UpdateBuffer(this._vertexBuffer, 0, this.GetVertices(graphicsDevice.IsUvOriginTopLeft));
        
        // Create pipeline.
        this._pipelineDescription = new SimplePipelineDescription() {
            BlendState = BlendState.AlphaBlend.Description,
            DepthStencilState = new DepthStencilStateDescription(false, false, ComparisonKind.LessEqual),
            RasterizerState = new RasterizerStateDescription() {
                DepthClipEnabled = true,
                CullMode = FaceCullMode.None
            },
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            TextureLayouts = this._effect.GetTextureLayouts(),
            ShaderSet = new ShaderSetDescription() {
                VertexLayouts = [
                    SpriteVertex2D.VertexLayout
                ],
                Shaders = [
                    this._effect.Shader.Item1,
                    this._effect.Shader.Item2
                ]
            },
            Outputs = output
        };
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
                Color = color.ToRgbaFloat().ToVector4()
            },
        
            new SpriteVertex2D() {
                Position = new Vector2(1.0F, -1.0F),
                TexCoords = new Vector2(1.0F, top),
                Color = color.ToRgbaFloat().ToVector4()
            },
        
            new SpriteVertex2D() {
                Position = new Vector2(1.0F, 1.0F),
                TexCoords = new Vector2(1.0F, bottom),
                Color = color.ToRgbaFloat().ToVector4()
            },
        
            new SpriteVertex2D() {
                Position = new Vector2(-1.0F, -1.0F),
                TexCoords = new Vector2(0.0F, top),
                Color = color.ToRgbaFloat().ToVector4()
            },
            
            new SpriteVertex2D() {
                Position = new Vector2(1.0F, 1.0F),
                TexCoords = new Vector2(1.0F, bottom),
                Color = color.ToRgbaFloat().ToVector4()
            },
            
            new SpriteVertex2D() {
                Position = new Vector2(-1.0F, 1.0F),
                TexCoords = new Vector2(0.0F, bottom),
                Color = color.ToRgbaFloat().ToVector4()
            }
        ];
    }

    /// <summary>
    /// Renders a full-screen quad with the provided render texture and sampler type.
    /// </summary>
    /// <param name="commandList">The command list used for recording rendering commands.</param>
    /// <param name="renderTexture">The render texture to draw onto the full-screen quad.</param>
    /// <param name="samplerType">The type of sampler to use for texture sampling.</param>
    public void Draw(CommandList commandList, RenderTexture2D renderTexture, SamplerType samplerType) {
        commandList.SetPipeline(this._effect.GetPipeline(this._pipelineDescription).Pipeline);
        commandList.SetVertexBuffer(0, this._vertexBuffer);
        commandList.SetGraphicsResourceSet(0, renderTexture.GetResourceSet(GraphicsHelper.GetSampler(this.GraphicsDevice, samplerType), this._effect.GetTextureLayout("fTexture").Layout));
        commandList.Draw(6);
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._effect.Dispose();
            this._vertexBuffer.Dispose();
        }
    }
}