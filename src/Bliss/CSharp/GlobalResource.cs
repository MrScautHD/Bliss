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
    /// Stores a collection of globally accessible buffer layouts used in rendering pipelines.
    /// </summary>
    public static List<SimpleBufferLayout> BufferLayouts { get; private set; }

    /// <summary>
    /// Maintains a collection of texture layouts used for configuring and managing texture bindings in graphics rendering operations.
    /// </summary>
    public static List<SimpleTextureLayout> TextureLayouts { get; private set; }
    
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
        BufferLayouts = new List<SimpleBufferLayout>();
        TextureLayouts = new List<SimpleTextureLayout>();
        
        // Default sprite effect.
        DefaultSpriteEffect = new Effect(graphicsDevice, SpriteVertex2D.VertexLayout, "content/shaders/sprite.vert", "content/shaders/sprite.frag");
        DefaultSpriteEffect.AddBufferLayout(CreateBufferLayout("ProjectionViewBuffer", SimpleBufferType.Uniform, ShaderStages.Vertex));
        DefaultSpriteEffect.AddTextureLayout(CreateTextureLayout("fTexture"));
        
        // Primitive effect.
        DefaultPrimitiveEffect = new Effect(graphicsDevice, PrimitiveVertex2D.VertexLayout, "content/shaders/primitive.vert", "content/shaders/primitive.frag");
        DefaultPrimitiveEffect.AddBufferLayout(CreateBufferLayout("ProjectionViewBuffer", SimpleBufferType.Uniform, ShaderStages.Vertex));
        
        // FullScreenRenderPass effect.
        DefaultFullScreenRenderPassEffect = new Effect(graphicsDevice, SpriteVertex2D.VertexLayout, "content/shaders/full_screen_render_pass.vert", "content/shaders/full_screen_render_pass.frag");
        DefaultFullScreenRenderPassEffect.AddTextureLayout(CreateTextureLayout("fTexture"));
        
        // ImmediateRenderer effect.
        DefaultImmediateRendererEffect = new Effect(graphicsDevice, ImmediateVertex3D.VertexLayout, "content/shaders/immediate_renderer.vert", "content/shaders/immediate_renderer.frag");
        DefaultImmediateRendererEffect.AddBufferLayout(CreateBufferLayout("MatrixBuffer", SimpleBufferType.Uniform, ShaderStages.Vertex));
        DefaultImmediateRendererEffect.AddTextureLayout(CreateTextureLayout("fTexture"));
        
        // Default model effect.
        DefaultModelEffect = new Effect(graphicsDevice, Vertex3D.VertexLayout, "content/shaders/default_model.vert", "content/shaders/default_model.frag");
        DefaultModelEffect.AddBufferLayout(CreateBufferLayout("MatrixBuffer", SimpleBufferType.Uniform, ShaderStages.Vertex));
        DefaultModelEffect.AddBufferLayout(CreateBufferLayout("BoneBuffer", SimpleBufferType.Uniform, ShaderStages.Vertex));
        DefaultModelEffect.AddBufferLayout(CreateBufferLayout("ColorBuffer", SimpleBufferType.Uniform, ShaderStages.Fragment));
        DefaultModelEffect.AddBufferLayout(CreateBufferLayout("ValueBuffer", SimpleBufferType.Uniform, ShaderStages.Fragment));
        DefaultModelEffect.AddTextureLayout(CreateTextureLayout(MaterialMapType.Albedo.GetName()));

        // Default immediate renderer texture.
        DefaultImmediateRendererTexture = new Texture2D(graphicsDevice, new Image(1, 1, Color.White));
        
        // Default model texture.
        DefaultModelTexture = new Texture2D(graphicsDevice, new Image(1, 1, Color.Gray));
    }

    /// <summary>
    /// Creates a new buffer layout and adds it to the global list of buffer layouts.
    /// </summary>
    /// <param name="name">The name of the buffer layout to create.</param>
    /// <param name="bufferType">The type of buffer being created, such as uniform or structured.</param>
    /// <param name="stages">The shader stages where the buffer will be used.</param>
    /// <returns>The created <c>SimpleBufferLayout</c>.</returns>
    public static SimpleBufferLayout CreateBufferLayout(string name, SimpleBufferType bufferType, ShaderStages stages) {
        SimpleBufferLayout bufferLayout = new SimpleBufferLayout(GraphicsDevice, name, bufferType, stages);
        BufferLayouts.Add(bufferLayout);
        return bufferLayout;
    }

    /// <summary>
    /// Creates a new texture layout and adds it to the global collection of texture layouts.
    /// </summary>
    /// <param name="name">The name of the texture layout.</param>
    /// <returns>A new <c>SimpleTextureLayout</c> instance initialized with the specified name.</returns>
    public static SimpleTextureLayout CreateTextureLayout(string name) {
        SimpleTextureLayout textureLayout = new SimpleTextureLayout(GraphicsDevice, name);
        TextureLayouts.Add(textureLayout);
        return textureLayout;
    }

    /// <summary>
    /// Releases and disposes of all global resources.
    /// </summary>
    public static void Destroy() {
        DefaultSpriteEffect.Dispose();
        DefaultPrimitiveEffect.Dispose();
        DefaultModelEffect.Dispose();
        DefaultModelTexture.Dispose();

        foreach (SimpleBufferLayout bufferLayout in BufferLayouts) {
            bufferLayout.Dispose();
        }
        
        BufferLayouts.Clear();
        
        foreach (SimpleTextureLayout textureLayout in TextureLayouts) {
            textureLayout.Dispose();
        }
        
        TextureLayouts.Clear();
    }
}