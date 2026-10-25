using System.Numerics;
using Bliss.CSharp.Images;
using Bliss.CSharp.Textures.Cubemaps;
using Veldrith;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Sky.Environment;

public class UniversalEnvironment : Disposable, IEnvironmentProvider {
    
    /// <summary>
    /// Gets the graphics device used to render and store the environment.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// Gets the skybox used to generate the environment.
    /// </summary>
    public ISkybox Skybox { get; private set; }
    
    /// <summary>
    /// Gets the width and height of each cubemap face in pixels.
    /// </summary>
    public uint Size { get; private set; }
    
    /// <summary>
    /// Gets the cubemap texture containing the baked environment.
    /// </summary>
    public Cubemap SourceEnvironmentCubemap => this._cubemapTexture!;
    
    /// <summary>
    /// Gets the environment color with the skybox exposure applied to its alpha component.
    /// </summary>
    public Vector4 EnvironmentColor => new Vector4(1.0F, 1.0F, 1.0F, this.Skybox.Exposure);
    
    /// <summary>
    /// The cubemap texture containing the rendered environment.
    /// </summary>
    private Cubemap _cubemapTexture;
    
    /// <summary>
    /// The color textures used as render targets for the six cubemap faces.
    /// </summary>
    private Texture[] _colorTextures;
    
    /// <summary>
    /// The depth textures used for depth testing while rendering the cubemap faces.
    /// </summary>
    private Texture[] _depthTextures;
    
    /// <summary>
    /// The framebuffers used to render the six cubemap faces.
    /// </summary>
    private Framebuffer[] _framebuffers;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UniversalEnvironment"/> class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create and render the environment.</param>
    /// <param name="skybox">The skybox used to generate the environment.</param>
    /// <param name="size">The width and height of each cubemap face in pixels.</param>
    /// <param name="mipmap">Whether to generate mipmaps for the cubemap texture.</param>
    /// <param name="srgb">Whether to use an sRGB pixel format for the cubemap and render targets.</param>
    public UniversalEnvironment(GraphicsDevice graphicsDevice, ISkybox skybox, uint size = 256, bool mipmap = true, bool srgb = false) {
        this.GraphicsDevice = graphicsDevice;
        this.Skybox = skybox;
        this.Size = size;
        
        // Create cubemap.
        Image[] faces = new Image[6];
        for (int i = 0; i < 6; i++) {
            faces[i] = new Image((int) size, (int) size);
        }
        
        this._cubemapTexture = new Cubemap(graphicsDevice, faces, mipmap, srgb);
        
        // Create framebuffers.
        this._framebuffers = this.CreateFramebuffers(size, srgb ? PixelFormat.R8G8B8A8UNormSRgb : PixelFormat.R8G8B8A8UNorm, out this._colorTextures, out this._depthTextures);
        
        // Bake environment.
        this.BakeEnvironment();
    }
    
    /// <summary>
    /// Bakes the current skybox into the environment cubemap.
    /// </summary>
    public void BakeEnvironment() {
        using CommandList commandList = this.GraphicsDevice.ResourceFactory.CreateCommandList();
        
        commandList.Begin();
        this.BakeEnvironment(commandList);
        commandList.End();
        
        this.GraphicsDevice.SubmitCommands(commandList);
        this.GraphicsDevice.WaitForIdle();
    }
    
    /// <summary>
    /// Records the commands required to bake the skybox into the environment cubemap.
    /// </summary>
    /// <param name="commandList">The command list used to record the rendering and texture-copy commands.</param>
    public void BakeEnvironment(CommandList commandList) {
        
        // Draw skybox environment.
        this.DrawSkyboxEnvironment(commandList);
        
        // Copy the rendered faces into the cubemap texture.
        for (uint face = 0; face < 6; face++) {
            commandList.CopyTexture(this._colorTextures[face], 0, 0, 0, 0, 0, this._cubemapTexture.DeviceTexture, 0, 0, 0, 0, face, this._cubemapTexture.Width, this._cubemapTexture.Height, 1, 1);
        }
        
        // Regenerate mipmaps.
        if (this._cubemapTexture.MipLevels > 1) {
            commandList.GenerateMipmaps(this._cubemapTexture.DeviceTexture);
        }
    }
    
    /// <summary>
    /// Renders the skybox into the six cubemap face framebuffers.
    /// </summary>
    /// <param name="commandList">The command list used to record the rendering commands.</param>
    private void DrawSkyboxEnvironment(CommandList commandList) {
        
        // Create projection.
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2.0F, 1.0F, 0.1F, 10.0F);
        
        // Create views.
        Span<Matrix4x4> views = stackalloc Matrix4x4[6];
        this.GetCubemapViews(views);
        
        for (int face = 0; face < 6; face++) {
            
            // Set framebuffer.
            commandList.SetFramebuffer(this._framebuffers![face]);
            commandList.ClearColorTarget(0, RgbaFloat.BLACK);
            commandList.ClearDepthStencil(1.0F);
            
            // Draw.
            this.Skybox.Draw(commandList, this._framebuffers[face], projection, views[face]);
        }
    }
    
    /// <summary>
    /// Creates the framebuffers and associated color and depth textures for all six cubemap faces.
    /// </summary>
    /// <param name="size">The width and height of each framebuffer in pixels.</param>
    /// <param name="format">The pixel format used for the color textures.</param>
    /// <param name="colorTextures">Receives the color textures created for the framebuffers.</param>
    /// <param name="depthTextures">Receives the depth textures created for the framebuffers.</param>
    /// <returns>The six framebuffers used to render the cubemap faces.</returns>
    private Framebuffer[] CreateFramebuffers(uint size, PixelFormat format, out Texture[] colorTextures, out Texture[] depthTextures) {
        Framebuffer[] framebuffers = new Framebuffer[6];
        colorTextures = new Texture[6];
        depthTextures = new Texture[6];
        
        for (uint face = 0; face < 6; face++) {
            Texture colorTexture = this.GraphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(size, size, 1, 1, format, TextureUsage.RenderTarget | TextureUsage.Sampled));
            Texture depthTexture = this.GraphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(size, size, 1, 1, PixelFormat.D32FloatS8UInt, TextureUsage.DepthStencil));
            
            colorTextures[face] = colorTexture;
            depthTextures[face] = depthTexture;
            framebuffers[face] = this.GraphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription(new FramebufferAttachmentDescription(depthTexture, 0), [
                    new FramebufferAttachmentDescription(colorTexture, 0)
                ]
            ));
        }
        
        return framebuffers;
    }
    
    /// <summary>
    /// Creates the six view matrices that look from the origin towards each cubemap face.
    /// </summary>
    /// <param name="views">The destination span for the six cubemap view matrices.</param>
    private void GetCubemapViews(Span<Matrix4x4> views) {
        if (views.Length < 6) {
            throw new ArgumentException("The span must contain at least six elements.", nameof(views));
        }
        
        Matrix4x4 flipY = Matrix4x4.CreateScale(1, -1, 1);
        
        views[0] = Matrix4x4.CreateLookAt(Vector3.Zero, Vector3.UnitX, -Vector3.UnitY) * flipY;
        views[1] = Matrix4x4.CreateLookAt(Vector3.Zero, -Vector3.UnitX, -Vector3.UnitY) * flipY;
        views[2] = Matrix4x4.CreateLookAt(Vector3.Zero, Vector3.UnitY, Vector3.UnitZ) * flipY;
        views[3] = Matrix4x4.CreateLookAt(Vector3.Zero, -Vector3.UnitY, -Vector3.UnitZ) * flipY;
        views[4] = Matrix4x4.CreateLookAt(Vector3.Zero, Vector3.UnitZ, -Vector3.UnitY) * flipY;
        views[5] = Matrix4x4.CreateLookAt(Vector3.Zero, -Vector3.UnitZ, -Vector3.UnitY) * flipY;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._cubemapTexture.Dispose();
            
            foreach (Framebuffer framebuffer in this._framebuffers) {
                framebuffer.Dispose();
            }
            
            foreach (Texture colorTexture in this._colorTextures) {
                colorTexture.Dispose();
            }
            
            foreach (Texture depthTexture in this._depthTextures) {
                depthTexture.Dispose();
            }
        }
    }
}