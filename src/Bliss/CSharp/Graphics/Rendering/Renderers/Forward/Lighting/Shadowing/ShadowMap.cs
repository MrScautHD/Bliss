using Veldrid;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Lighting.Shadowing;

public class ShadowMap : Disposable {
    
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    public int Resolution { get; private set; }
    
    public Texture DepthTexture { get; private set; }
    
    public Framebuffer Framebuffer { get; private set; }
    
    public ShadowMap(GraphicsDevice graphicsDevice, int resolution = 1024) {
        this.GraphicsDevice = graphicsDevice;
        this.Resolution = resolution;
        this.CreateFrameBuffer();
    }
    
    public void CreateFrameBuffer() {
        this.DepthTexture = this.GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription((uint) this.Resolution, (uint) this.Resolution, 1, 1, 1, PixelFormat.D32FloatS8UInt, TextureUsage.DepthStencil | TextureUsage.Sampled, TextureType.Texture2D));
        this.Framebuffer = this.GraphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription(this.DepthTexture));
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.DepthTexture.Dispose();
            this.Framebuffer.Dispose();
        }
    }
}