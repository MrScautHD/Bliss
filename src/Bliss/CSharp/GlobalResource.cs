using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Images;
using Bliss.CSharp.Materials;
using Bliss.CSharp.Textures;
using Veldrith;
using Veldrith.SPIRV;

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
    /// The skinned <see cref="Effect"/> used for rendering 3D models.
    /// </summary>
    public static Effect DefaultSkinnedModelEffect { get; private set; }
    
    /// <summary>
    /// Gets the <see cref="Effect"/> used for rendering cubemap skyboxes in 3D environments.
    /// </summary>
    public static Effect CubemapSkyboxEffect { get; private set; }
    
    /// <summary>
    /// Gets the <see cref="Effect"/> used for rendering procedural skyboxes in 3D environments.
    /// </summary>
    public static Effect ProceduralSkyboxEffect { get; private set; }
    
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
        DefaultSpriteEffect = new Effect(graphicsDevice, "content/bliss/shaders/batch/sprite.vert", "content/bliss/shaders/batch/sprite.frag", new CrossCompileOptions());
        DefaultSpriteEffect.AddBufferLayout("ProjectionViewBuffer", 0, SimpleBufferType.Uniform, ShaderStages.Vertex);
        DefaultSpriteEffect.AddTextureLayout("fTexture", 1);
        
        // Primitive effect.
        DefaultPrimitiveEffect = new Effect(graphicsDevice, "content/bliss/shaders/batch/primitive.vert", "content/bliss/shaders/batch/primitive.frag", new CrossCompileOptions());
        DefaultPrimitiveEffect.AddBufferLayout("ProjectionViewBuffer", 0, SimpleBufferType.Uniform, ShaderStages.Vertex);
        
        // FullScreenRenderPass effect.
        DefaultFullScreenRenderPassEffect = new Effect(graphicsDevice, "content/bliss/shaders/post/full_screen_render_pass.vert", "content/bliss/shaders/post/full_screen_render_pass.frag", new CrossCompileOptions());
        DefaultFullScreenRenderPassEffect.AddTextureLayout("fTexture", 0);
        
        // ImmediateRenderer effect.
        DefaultImmediateRendererEffect = new Effect(graphicsDevice, "content/bliss/shaders/immediate/immediate_renderer.vert", "content/bliss/shaders/immediate/immediate_renderer.frag", new CrossCompileOptions());
        DefaultImmediateRendererEffect.AddBufferLayout("MatrixBuffer", 0, SimpleBufferType.Uniform, ShaderStages.Vertex);
        DefaultImmediateRendererEffect.AddTextureLayout("fTexture", 1);
        
        // Default model effect.
        DefaultModelEffect = new Effect(graphicsDevice, Effect.LoadTextCodeFromFile("content/bliss/shaders/model/default_model.vert"), Effect.LoadTextCodeFromFile("content/bliss/shaders/model/default_model.frag"), new CrossCompileOptions(), [
            new MacroDefinition("MATRIX_BUFFER_SET", "0"),
            new MacroDefinition("TRANSFORMER_BUFFER_SET", "1"),
            new MacroDefinition("MATERIAL_BUFFER_SET", "2"),
            new MacroDefinition("ALBEDO_TEXTURE_SET", "3")
        ]);
        DefaultModelEffect.AddBufferLayout("MatrixBuffer", 0, SimpleBufferType.Uniform, ShaderStages.Vertex);
        DefaultModelEffect.AddBufferLayout("TransformBuffer", 1, SimpleBufferType.Uniform, ShaderStages.Vertex);
        DefaultModelEffect.AddBufferLayout("MaterialBuffer", 2, SimpleBufferType.Uniform, ShaderStages.Fragment);
        DefaultModelEffect.AddTextureLayout(MaterialMapType.Albedo.GetName(), 3);
        
        // Skinned model effect.
        DefaultSkinnedModelEffect = new Effect(graphicsDevice, Effect.LoadTextCodeFromFile("content/bliss/shaders/model/default_skinned_model.vert"), Effect.LoadTextCodeFromFile("content/bliss/shaders/model/default_model.frag"), new CrossCompileOptions(), [
            new MacroDefinition("MATRIX_BUFFER_SET", "0"),
            new MacroDefinition("TRANSFORMER_BUFFER_SET", "1"),
            new MacroDefinition("BONE_BUFFER_SET", "2"),
            new MacroDefinition("MATERIAL_BUFFER_SET", "3"),
            new MacroDefinition("ALBEDO_TEXTURE_SET", "4")
        ]);
        DefaultSkinnedModelEffect.AddBufferLayout("MatrixBuffer", 0, SimpleBufferType.Uniform, ShaderStages.Vertex);
        DefaultSkinnedModelEffect.AddBufferLayout("TransformBuffer", 1, SimpleBufferType.Uniform, ShaderStages.Vertex);
        DefaultSkinnedModelEffect.AddBufferLayout("BoneBuffer", 2, SimpleBufferType.Uniform, ShaderStages.Vertex);
        DefaultSkinnedModelEffect.AddBufferLayout("MaterialBuffer", 3, SimpleBufferType.Uniform, ShaderStages.Fragment);
        DefaultSkinnedModelEffect.AddTextureLayout(MaterialMapType.Albedo.GetName(), 4);
        
        // Cubemap skybox effect.
        CubemapSkyboxEffect = new Effect(GraphicsDevice, "content/bliss/shaders/environment/cubemap_skybox.vert", "content/bliss/shaders/environment/cubemap_skybox.frag", new CrossCompileOptions());
        CubemapSkyboxEffect.AddBufferLayout("MatrixBuffer", 0, SimpleBufferType.Uniform, ShaderStages.Vertex);
        CubemapSkyboxEffect.AddBufferLayout("SkyboxBuffer", 1, SimpleBufferType.Uniform, ShaderStages.Vertex | ShaderStages.Fragment);
        CubemapSkyboxEffect.AddTextureLayout("fCubemap", 2);
        
        // Procedural skybox effect.
        ProceduralSkyboxEffect = new Effect(GraphicsDevice, "content/bliss/shaders/environment/procedural_skybox.vert", "content/bliss/shaders/environment/procedural_skybox.frag", new CrossCompileOptions());
        ProceduralSkyboxEffect.AddBufferLayout("MatrixBuffer", 0, SimpleBufferType.Uniform, ShaderStages.Vertex);
        ProceduralSkyboxEffect.AddBufferLayout("SkyboxBuffer", 1, SimpleBufferType.Uniform, ShaderStages.Fragment);
        
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
        DefaultSkinnedModelEffect.Dispose();
        CubemapSkyboxEffect.Dispose();
        DefaultImmediateRendererTexture.Dispose();
        DefaultModelTexture.Dispose();
    }
}