namespace Bliss.CSharp.Camera;

public interface ICam {
    
    /// <summary>
    /// Updates the camera's state, recalculating its parameters as needed.
    /// </summary>
    void Update(double timeStep);
    
    /// <summary>
    /// Resizes the viewport and updates the aspect ratio based on the given width and height.
    /// </summary>
    /// <param name="width">The new width of the viewport.</param>
    /// <param name="height">The new height of the viewport.</param>
    void Resize(uint width, uint height);

    /// <summary>
    /// Initializes the camera's usage in the current frame and sets it as the active camera.
    /// </summary>
    void Begin();
    
    /// <summary>
    /// Concludes the camera's operations for the current frame.
    /// </summary>
    void End();
}