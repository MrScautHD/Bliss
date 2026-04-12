using Veldrid;

namespace Bliss.CSharp.Camera;

public interface ICam {
    
    /// <summary>
    /// Updates the camera's state, recalculating its parameters as needed.
    /// </summary>
    void Update(double delta);
    
    /// <summary>
    /// Resizes the viewport and updates the aspect ratio based on the given width and height.
    /// </summary>
    /// <param name="width">The new width of the viewport.</param>
    /// <param name="height">The new height of the viewport.</param>
    void Resize(uint width, uint height);

    /// <summary>
    /// Prepares the camera for rendering in the current frame and assigns it as the active camera.
    /// </summary>
    /// <param name="commandList">The command list used to record rendering commands or update GPU resources for the current frame.</param>
    void Begin(CommandList commandList);

    /// <summary>
    /// Concludes the camera's operations for the current frame.
    /// </summary>
    void End();
}