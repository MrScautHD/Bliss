using Veldrith;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Sky;

public interface ISkybox : IDisposable {
    
    /// <summary>
    /// Renders the skybox to the specified framebuffer using the provided command list.
    /// </summary>
    /// <param name="commandList">The command list that manages GPU commands for this rendering operation.</param>
    /// <param name="framebuffer">The framebuffer where the skybox will be rendered.</param>
    void Draw(CommandList commandList, Framebuffer framebuffer);
}