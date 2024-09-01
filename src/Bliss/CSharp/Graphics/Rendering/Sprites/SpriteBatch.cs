using System.Numerics;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Textures;
using Veldrid;

namespace Bliss.CSharp.Graphics.Rendering.Sprites;

public class SpriteBatch : Disposable {

    public const uint MaxSprites = 15360;

    private const uint VertexCount = 4;
    private const uint IndexCount = 6;
    
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    private CommandList _commandList;
    private Fence _fence;
    
    private List<Sprite> _sprites;

    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;

    private Effect? _effect;
    private ResourceLayout? _resourceLayout;
    private Pipeline _pipeline;
    private bool _isDefaultPipeline;

    private ResourceSet _resourceSet;
    
    private bool _begun;
    
    public Matrix4x4 _currentTransform;
    private Effect _currentEffect;
    private Texture2D _currentTexture;
    
    public SpriteBatch(GraphicsDevice graphicsDevice, SimplePipeline? pipeline = null) {
        this.GraphicsDevice = graphicsDevice;
        this._commandList = graphicsDevice.ResourceFactory.CreateCommandList();
        this._fence = graphicsDevice.ResourceFactory.CreateFence(false);
        
        this._sprites = new List<Sprite>();

        // Create Vertex and Index Buffer.
        uint vertexBufferSize = VertexCount * sizeof(float);
        uint indexBufferSize = IndexCount * sizeof(float);
        
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(MaxSprites * vertexBufferSize, BufferUsage.VertexBuffer));
        this._indexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(MaxSprites * indexBufferSize, BufferUsage.IndexBuffer));

        if (pipeline != null) {
            this._pipeline = pipeline.Pipeline;
        }
        else {
            // Load default sprite shader.
            this._effect = new Effect(graphicsDevice.ResourceFactory, new VertexLayoutDescription() {
                Elements = [ // TODO: UPDATE THE SHADER PARAMS
                    new VertexElementDescription("vertexPosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                    new VertexElementDescription("vertexTexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                    new VertexElementDescription("vertexColor", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4)
                ]
            }, "content/shaders/default_shader.vert", "content/shaders/default_shader.frag");
        
            // Create default resource layout.
            this._resourceLayout = graphicsDevice.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription( // TODO: UPDATE THE SHADER PARAMS
                    new ResourceLayoutElementDescription("MVP", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("Color", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("texture0", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)
                )
            );

            // Create default pipeline.
            this._pipeline = new SimplePipeline(this.GraphicsDevice, this._effect, this._resourceLayout, graphicsDevice.SwapchainFramebuffer.OutputDescription, BlendStateDescription.SingleOverrideBlend, FaceCullMode.Back).Pipeline;
            this._isDefaultPipeline = true;
        }
    }

    public void Begin(Matrix4x4? transform = null, Effect? effect = null, BlendStateDescription? blendState = null) {
        if (this._begun) {
            throw new Exception("SpriteBatch is already active.");
        }
        
        this._begun = true;

        Matrix4x4 trans = transform ?? Matrix4x4.Identity;
        
        this._commandList.Begin();
    }

    public CommandList End() {
        if (!this._begun) {
            throw new Exception("The SpriteBatch begin method get not called at first.");
        }
        
        // TODO: DO A IF CHECK IF THERE IS A INSTANCE if not do not run it!

        if (true) {
            this.ReallyDraw();
            
            this._commandList.End();
            this.GraphicsDevice.SubmitCommands(this._commandList, this._fence);
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

    private void ReallyDraw() {
        
    }

    private void Flush() {

        ResourceSetDescription resourceSetDescription = new ResourceSetDescription(this._resourceLayout, this._currentTexture.DeviceTexture, this.GraphicsDevice.PointSampler);
        // Update Resources
        this._resourceSet = this.GraphicsDevice.ResourceFactory.CreateResourceSet(ref resourceSetDescription);
        
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
            this._fence.Dispose();
            this._sprites.Clear();
            
            this._vertexBuffer.Dispose();
            this._indexBuffer.Dispose();
            
            this._effect?.Dispose();
            this._resourceLayout?.Dispose();

            if (this._isDefaultPipeline) {
                this._pipeline.Dispose();
            }
        }
    }
}