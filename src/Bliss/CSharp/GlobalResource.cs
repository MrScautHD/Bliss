using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.Pipelines.Textures;
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
    public static Effect LitModelEffect { get; private set; }
    /// <summary>
    /// The default <see cref="Effect"/> used for rendering 3D models.
    /// </summary>
    public static Effect UnlitModelEffect { get; private set; }

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
        DefaultSpriteEffect = new Effect(graphicsDevice, SpriteVertex2D.VertexLayout, "core/shaders/sprite.vert", "core/shaders/sprite.frag");
        DefaultSpriteEffect.AddBufferLayout("ProjectionViewBuffer", SimpleBufferType.Uniform, ShaderStages.Vertex);
        DefaultSpriteEffect.AddTextureLayout("fTexture");
        
        // Primitive effect.
        DefaultPrimitiveEffect = new Effect(graphicsDevice, PrimitiveVertex2D.VertexLayout, "core/shaders/primitive.vert", "core/shaders/primitive.frag");
        DefaultPrimitiveEffect.AddBufferLayout("ProjectionViewBuffer", SimpleBufferType.Uniform, ShaderStages.Vertex);
        
        // FullScreenRenderPass effect.
        DefaultFullScreenRenderPassEffect = new Effect(graphicsDevice, SpriteVertex2D.VertexLayout, "core/shaders/full_screen_render_pass.vert", "core/shaders/full_screen_render_pass.frag");
        DefaultFullScreenRenderPassEffect.AddTextureLayout("fTexture");
        
        // ImmediateRenderer effect.
        DefaultImmediateRendererEffect = new Effect(graphicsDevice, ImmediateVertex3D.VertexLayout, "core/shaders/immediate_renderer.vert", "core/shaders/immediate_renderer.frag");
        DefaultImmediateRendererEffect.AddBufferLayout("MatrixBuffer", SimpleBufferType.Uniform, ShaderStages.Vertex);
        DefaultImmediateRendererEffect.AddTextureLayout("fTexture");
        
        // Default model effect.
        LitModelEffect = new Effect(graphicsDevice, Vertex3D.VertexLayout, "core/shaders/msh_generic.vert", "core/shaders/msh_lit.frag");
        LitModelEffect.AddBufferLayout("MatrixBuffer", SimpleBufferType.Uniform, ShaderStages.Vertex);
        LitModelEffect.AddBufferLayout("ColorBuffer", SimpleBufferType.Uniform, ShaderStages.Fragment);
        LitModelEffect.AddTextureLayout("fAlbedo");
        LitModelEffect.AddTextureLayout("fRough");
        LitModelEffect.AddTextureLayout("fMetal");
        LitModelEffect.AddTextureLayout("fNormal");
        
        // Default model effect.
        UnlitModelEffect = new Effect(graphicsDevice, Vertex3D.VertexLayout, "core/shaders/msh_generic.vert", "core/shaders/msh_unlit.frag");
        UnlitModelEffect.AddBufferLayout("MatrixBuffer", SimpleBufferType.Uniform, ShaderStages.Vertex);
        UnlitModelEffect.AddBufferLayout("ColorBuffer", SimpleBufferType.Uniform, ShaderStages.Fragment);
        UnlitModelEffect.AddTextureLayout("fAlbedo");
        
        // Default immediate renderer texture.
        DefaultImmediateRendererTexture = new Texture2D(graphicsDevice, new Image(1, 1, Color.White));
        
        // Default model texture.
        DefaultModelTexture = new Texture2D(graphicsDevice, new Image(1, 1, Color.White));
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
        UnlitModelEffect.Dispose();
        LitModelEffect.Dispose();
        DefaultImmediateRendererTexture.Dispose();
        DefaultModelTexture.Dispose();
    }
}