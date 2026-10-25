using System.Numerics;
using Bliss.CSharp.Colors;
using Veldrith;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Sky;

public interface ISkybox : IDisposable {
    
    /// <summary>
    /// Gets or sets the color tint applied to the skybox.
    /// </summary>
    Color Color { get; set; }
    
    /// <summary>
    /// Gets or sets the exposure applied to the skybox.
    /// </summary>
    float Exposure { get; set; }
    
    /// <summary>
    /// Renders the skybox using the specified projection and view matrices.
    /// </summary>
    /// <param name="commandList">The command list used to record the rendering commands.</param>
    /// <param name="framebuffer">The framebuffer to render the skybox into.</param>
    /// <param name="projection">The projection matrix used to transform the skybox geometry.</param>
    /// <param name="view">The view matrix used to determine the viewing orientation.</param>
    void Draw(CommandList commandList, Framebuffer framebuffer, Matrix4x4 projection, Matrix4x4 view);
}