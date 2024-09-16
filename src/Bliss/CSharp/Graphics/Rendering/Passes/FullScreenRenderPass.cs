using System.Numerics;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Textures;
using Bliss.CSharp.Textures;
using Veldrid;

namespace Bliss.CSharp.Graphics.Rendering.Passes;

public class FullScreenRenderPass : Disposable {
    
    public GraphicsDevice GraphicsDevice { get; private set; }
    public OutputDescription Output { get; private set; }

    private Effect _effect;
    private SimpleTextureLayout _textureLayout;
    private SimplePipeline _pipeline;
    private DeviceBuffer _vertexBuffer;

    public FullScreenRenderPass(GraphicsDevice graphicsDevice, OutputDescription output) {
        this.GraphicsDevice = graphicsDevice;
        this.Output = output;
        
        // Create effect.
        VertexLayoutDescription vertexLayouts = new VertexLayoutDescription(
            new VertexElementDescription("vPosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4)
        );
        
        this._effect = new Effect(graphicsDevice.ResourceFactory, vertexLayouts, "content/shaders/full_screen_render_pass.vert", "content/shaders/full_screen_render_pass.frag");
        
        // Create texture layout.
        this._textureLayout = new SimpleTextureLayout(this.GraphicsDevice, "fTexture");
        
        // Create pipeline.
        this._pipeline = new SimplePipeline(graphicsDevice, new SimplePipelineDescription() {
            BlendState = BlendState.AlphaBlend.Description,
            DepthStencilState = new DepthStencilStateDescription(true, true, ComparisonKind.LessEqual),
            RasterizerState = new RasterizerStateDescription() {
                DepthClipEnabled = true,
                CullMode = FaceCullMode.None
            },
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            TextureLayouts = [
                this._textureLayout
            ],
            ShaderSet = new ShaderSetDescription() {
                VertexLayouts = [
                    vertexLayouts
                ],
                Shaders = [
                    this._effect.Shader.Item1,
                    this._effect.Shader.Item2
                ]
            },
            Outputs = output
        });

        // Create vertex buffer.
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16 * 6, BufferUsage.VertexBuffer));
        graphicsDevice.UpdateBuffer(this._vertexBuffer, 0, this.GetQuadVertices(this.GraphicsDevice.IsUvOriginTopLeft));
    }

    /// <summary>
    /// Generates the vertices for a full-screen quad, which can be used for rendering textures or post-processing effects.
    /// </summary>
    /// <param name="isUvOriginTopLeft">Determines if the UV origin is at the top-left corner of the screen.</param>
    /// <returns>An array of Vector4 structures representing the vertices of the quad.</returns>
    private Vector4[] GetQuadVertices(bool isUvOriginTopLeft) {
        float top = isUvOriginTopLeft ? 1.0F : 0.0F;
        float bottom = isUvOriginTopLeft ? 0.0F : 1.0F;
        
        return [
            new Vector4(-1.0F, -1.0F, 0.0F, top),
            new Vector4( 1.0F, -1.0F, 1.0F, top),
            new Vector4( 1.0F,  1.0F, 1.0F, bottom),

            new Vector4(-1.0F, -1.0F, 0.0F, top),
            new Vector4( 1.0F,  1.0F, 1.0F, bottom),
            new Vector4(-1.0F,  1.0F, 0.0F, bottom)
        ];
    }

    /// <summary>
    /// Renders a full-screen quad with the provided render texture and sampler type.
    /// </summary>
    /// <param name="commandList">The command list used for recording rendering commands.</param>
    /// <param name="renderTexture">The render texture to draw onto the full-screen quad.</param>
    /// <param name="samplerType">The type of sampler to use for texture sampling.</param>
    public void Draw(CommandList commandList, RenderTexture2D renderTexture, SamplerType samplerType) {
        commandList.SetPipeline(this._pipeline.Pipeline);
        commandList.SetVertexBuffer(0, this._vertexBuffer);
        commandList.SetGraphicsResourceSet(0, renderTexture.GetResourceSet(GraphicsHelper.GetSampler(this.GraphicsDevice, samplerType), this._textureLayout.Layout));
        commandList.Draw(6);
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._effect.Dispose();
            this._textureLayout.Dispose();
            this._pipeline.Dispose();
            this._vertexBuffer.Dispose();
        }
    }
}