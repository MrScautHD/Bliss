using System.Numerics;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Textures;
using Veldrid;

namespace Bliss.CSharp.Rendering;

public class SpriteBatch : Disposable {

    public const uint MaxSprites = 15360;

    private const uint VertexCount = 4;
    private const uint IndexCount = 6;
    
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    private CommandList _commandList;

    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;

    private Effect _effect;
    private ResourceLayout _resourceLayout;
    private Pipeline _pipeline;

    private ResourceSet _resourceSet;
    
    private bool _begun;
    private Texture2D _currentTexture;
    
    public SpriteBatch(GraphicsDevice graphicsDevice) {
        this.GraphicsDevice = graphicsDevice;
        this._commandList = graphicsDevice.ResourceFactory.CreateCommandList();

        // Create Vertex and Index Buffer
        uint vertexBufferSize = VertexCount * sizeof(float);
        uint indexBufferSize = IndexCount * sizeof(float);
        
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(MaxSprites * vertexBufferSize, BufferUsage.VertexBuffer));
        this._indexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(MaxSprites * indexBufferSize, BufferUsage.IndexBuffer));
        
        // Load Shader.
        this._effect = new Effect(graphicsDevice.ResourceFactory, "content/shaders/default_shader.vert", "content/shaders/default_shader.frag");
        
        // Create Resource Layout.
        this._resourceLayout = graphicsDevice.ResourceFactory.CreateResourceLayout(
            new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("MVP", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("Color", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("texture0", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)
            )
        );
        
        // Create Pipeline
        this._pipeline = graphicsDevice.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription() {
            BlendState = BlendStateDescription.SingleOverrideBlend,
            DepthStencilState = new DepthStencilStateDescription() {
                DepthTestEnabled = true,
                DepthWriteEnabled = true,
                DepthComparison = ComparisonKind.LessEqual
            },
            RasterizerState = new RasterizerStateDescription() {
                CullMode = FaceCullMode.Back,
                FillMode = PolygonFillMode.Solid,
                FrontFace = FrontFace.Clockwise,
                DepthClipEnabled = true,
                ScissorTestEnabled = false
            },
            PrimitiveTopology = PrimitiveTopology.TriangleStrip,
            ResourceLayouts = [
                this._resourceLayout
            ],
            ShaderSet = new ShaderSetDescription() {
                VertexLayouts = [
                    new VertexLayoutDescription(
                        new VertexElementDescription("vertexPosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                        new VertexElementDescription("vertexTexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                        new VertexElementDescription("vertexColor", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4)
                    )
                ],
                Shaders = [
                    this._effect.Shader.Item1,
                    this._effect.Shader.Item2
                ]
            },
            Outputs = graphicsDevice.SwapchainFramebuffer.OutputDescription
        });
    }

    public void Begin(Matrix4x4 transform, Matrix4x4 projection, Effect effect, BlendStateDescription blendState) {
        if (this._begun) {
            throw new Exception("SpriteBatch is already active.");
        }

        this._begun = true;
        
        this._commandList.Begin();
    }

    public CommandList End() {
        if (!this._begun) {
            throw new Exception("The SpriteBatch begin method get not called at first.");
        }

        this._begun = false;

        return this._commandList;
    }

    public CommandList GetCommandList() {
        if (this._begun) {
            Logger.Error("Cannot call .GetCommandList() while begin is true. Call .End() first.");
            return null;
        }

        return this._commandList;
    }

    private void Flush() {
        
        // Update Resources
        this._resourceSet = this.GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(this._resourceLayout, this._currentTexture.DeviceTexture, this.GraphicsDevice.PointSampler));
        
        // Update Index Buffer
        this.GraphicsDevice.UpdateBuffer(this._vertexBuffer, 0, [
            (new Vector2())
        ]);
        
        // Update Index Buffer
        ushort[] quadIndices = [0, 1, 2, 3];
        this.GraphicsDevice.UpdateBuffer(this._indexBuffer, 0, quadIndices);
        
        // Setup Resources
        this._commandList.SetVertexBuffer(0, this._vertexBuffer);
        this._commandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt16);
        this._commandList.SetPipeline(this._pipeline);
        this._commandList.SetGraphicsResourceSet(0, this._resourceSet);
        
        // Draw
        this._commandList.DrawIndexed(4, 1, 0, 0, 0);
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            
        }
    }
}