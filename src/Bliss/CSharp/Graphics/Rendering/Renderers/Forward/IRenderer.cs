using Veldrid;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward;

public interface IRenderer : IDisposable {
    
    /// <summary>
    /// Draws the specified <see cref="Renderable"/> object using the renderer’s internal pipeline and state.
    /// </summary>
    /// <param name="renderable">The renderable object to be drawn.</param>
    void DrawRenderable(Renderable renderable);
    
    /// <summary>
    /// Performs a rendering operation using the specified <see cref="CommandList"/> and <see cref="OutputDescription"/>.
    /// </summary>
    /// <param name="commandList">The command list that records GPU draw commands.</param>
    /// <param name="output">The output description that defines the render target format and depth configuration.</param>
    void Draw(CommandList commandList, OutputDescription output);
}