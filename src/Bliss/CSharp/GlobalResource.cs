using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Images;
using Bliss.CSharp.Materials;
using Bliss.CSharp.Textures;
using Veldrid;

namespace Bliss.CSharp;

public static class GlobalResource {
    
    /// <summary>
    /// Provides access to the global graphics device used for rendering operations.
    /// </summary>
    public static GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// A global point sampler using clamp addressing mode.
    /// </summary>
    public static Sampler PointClampSampler { get; private set; }
    
    /// <summary>
    /// A global linear sampler using clamp addressing mode.
    /// </summary>
    public static Sampler LinearClampSampler { get; private set; }
    
    /// <summary>
    /// A global 4x anisotropic sampler using clamp addressing mode.
    /// </summary>
    public static Sampler Aniso4XClampSampler { get; private set; }
    
    /// <summary>
    /// Gets the default <see cref="Effect"/> used for rendering sprites.
    /// </summary>
    public static Effect DefaultSpriteEffect { get; private set; }
    
    /// <summary>
    /// Gets the <see cref="Effect"/> used for rendering primitive shapes.
    /// </summary>
    public static Effect DefaultPrimitiveEffect { get; private set; }
    
    /// <summary>
    /// Gets the default <see cref="Effect"/> used for full-screen render passes.
    /// </summary>
    public static Effect DefaultFullScreenRenderPassEffect { get; private set; }
    
    /// <summary>
    /// Gets the default <see cref="Effect"/> used for immediate mode rendering operations.
    /// </summary>
    public static Effect DefaultImmediateRendererEffect { get; private set; }
    
    /// <summary>
    /// The default <see cref="Effect"/> used for rendering 3D models.
    /// </summary>
    public static Effect DefaultModelEffect { get; private set; }

    /// <summary>
    /// The default <see cref="Texture2D"/> used for immediate mode rendering.
    /// </summary>
    public static Texture2D DefaultImmediateRendererTexture { get; private set; }

    /// <summary>
    /// The default <see cref="Texture2D"/> used for rendering 3D models.
    /// </summary>
    public static Texture2D DefaultModelTexture { get; private set; }

    /// <summary>
    /// Initializes global resources.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device to be used for resource creation and rendering.</param>
    public static void Init(GraphicsDevice graphicsDevice) {
        GraphicsDevice = graphicsDevice;
        
        // Default Samplers.
        PointClampSampler = graphicsDevice.ResourceFactory.CreateSampler(new SamplerDescription {
            AddressModeU = SamplerAddressMode.Clamp,
            AddressModeV = SamplerAddressMode.Clamp,
            AddressModeW = SamplerAddressMode.Clamp,
            Filter = SamplerFilter.MinPointMagPointMipPoint,
            LodBias = 0,
            MinimumLod = 0,
            MaximumLod = uint.MaxValue,
            MaximumAnisotropy = 0
        });
        
        LinearClampSampler = graphicsDevice.ResourceFactory.CreateSampler(new SamplerDescription {
            AddressModeU = SamplerAddressMode.Clamp,
            AddressModeV = SamplerAddressMode.Clamp,
            AddressModeW = SamplerAddressMode.Clamp,
            Filter = SamplerFilter.MinLinearMagLinearMipLinear,
            LodBias = 0,
            MinimumLod = 0,
            MaximumLod = uint.MaxValue,
            MaximumAnisotropy = 0
        });
        
        Aniso4XClampSampler = graphicsDevice.ResourceFactory.CreateSampler(new SamplerDescription {
            AddressModeU = SamplerAddressMode.Clamp,
            AddressModeV = SamplerAddressMode.Clamp,
            AddressModeW = SamplerAddressMode.Clamp,
            Filter = SamplerFilter.Anisotropic,
            LodBias = 0,
            MinimumLod = 0,
            MaximumLod = uint.MaxValue,
            MaximumAnisotropy = 4
        });
        
        // Default sprite effect.
        DefaultSpriteEffect = new Effect(graphicsDevice, SpriteVertex2D.VertexLayout, "content/bliss/shaders/sprite.vert", "content/bliss/shaders/sprite.frag");
        DefaultSpriteEffect.AddBufferLayout("ProjectionViewBuffer", 0, SimpleBufferType.Uniform, ShaderStages.Vertex);
        DefaultSpriteEffect.AddTextureLayout("fTexture", 1);
        
        // Primitive effect.
        DefaultPrimitiveEffect = new Effect(graphicsDevice, PrimitiveVertex2D.VertexLayout, "content/bliss/shaders/primitive.vert", "content/bliss/shaders/primitive.frag");
        DefaultPrimitiveEffect.AddBufferLayout("ProjectionViewBuffer", 0, SimpleBufferType.Uniform, ShaderStages.Vertex);
        
        // FullScreenRenderPass effect.
        DefaultFullScreenRenderPassEffect = new Effect(graphicsDevice, SpriteVertex2D.VertexLayout, "content/bliss/shaders/full_screen_render_pass.vert", "content/bliss/shaders/full_screen_render_pass.frag");
        DefaultFullScreenRenderPassEffect.AddTextureLayout("fTexture", 0);
        
        // ImmediateRenderer effect.
        DefaultImmediateRendererEffect = new Effect(graphicsDevice, ImmediateVertex3D.VertexLayout, "content/bliss/shaders/immediate_renderer.vert", "content/bliss/shaders/immediate_renderer.frag");
        DefaultImmediateRendererEffect.AddBufferLayout("MatrixBuffer", 0, SimpleBufferType.Uniform, ShaderStages.Vertex);
        DefaultImmediateRendererEffect.AddTextureLayout("fTexture", 1);
        
        // Default model effect.
        DefaultModelEffect = new Effect(graphicsDevice, [Vertex3D.VertexLayout, Vertex3D.InstanceMatrixLayout], "content/bliss/shaders/default_model.vert", "content/bliss/shaders/default_model.frag");
        DefaultModelEffect.AddBufferLayout("MatrixBuffer", 0, SimpleBufferType.Uniform, ShaderStages.Vertex);
        DefaultModelEffect.AddBufferLayout("BoneBuffer", 1, SimpleBufferType.Uniform, ShaderStages.Vertex);
        DefaultModelEffect.AddBufferLayout("MaterialBuffer", 2, SimpleBufferType.Uniform, ShaderStages.Fragment);
        DefaultModelEffect.AddTextureLayout(MaterialMapType.Albedo.GetName(), 3);

        // Default immediate renderer texture.
        DefaultImmediateRendererTexture = new Texture2D(graphicsDevice, new Image(1, 1, Color.White));
        
        // Default model texture.
        DefaultModelTexture = new Texture2D(graphicsDevice, new Image(1, 1, Color.Gray));
    }
    
    /// <summary>
    /// Releases and disposes of all global resources.
    /// </summary>
    public static void Destroy() {
        PointClampSampler.Dispose();
        LinearClampSampler.Dispose();
        Aniso4XClampSampler.Dispose();
        DefaultSpriteEffect.Dispose();
        DefaultPrimitiveEffect.Dispose();
        DefaultFullScreenRenderPassEffect.Dispose();
        DefaultImmediateRendererEffect.Dispose();
        DefaultModelEffect.Dispose();
        DefaultImmediateRendererTexture.Dispose();
        DefaultModelTexture.Dispose();
    }
}