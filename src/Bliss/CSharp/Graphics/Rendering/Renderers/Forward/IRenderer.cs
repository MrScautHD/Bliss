using Veldrith;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward;

public interface IRenderer : IDisposable {
    
    /// <summary>
    /// Gets or sets the skybox used for rendering the background environment in a 3D scene.
    /// </summary>
    SkyBox? SkyBox { get; set; }
    
    /// <summary>
    /// Draws the specified <see cref="Renderable"/> object using the renderer’s internal pipeline and state.
    /// </summary>
    /// <param name="renderable">The renderable object to be drawn.</param>
    void DrawRenderable(Renderable renderable);
    
    /// <summary>
    /// Performs a rendering operation using the specified <see cref="CommandList"/> and <see cref="Framebuffer"/>.
    /// </summary>
    /// <param name="commandList">The command list that records GPU draw commands.</param>
    /// <param name="framebuffer">The framebuffer that defines the rendering target and its properties.</param>
    void Draw(CommandList commandList, Framebuffer framebuffer);
}