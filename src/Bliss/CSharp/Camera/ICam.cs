using System.Numerics;

namespace Bliss.CSharp.Camera;

public interface ICam {
    
    /// <summary>
    /// Resizes the viewport and updates the aspect ratio based on the given width and height.
    /// </summary>
    /// <param name="width">The new width of the viewport.</param>
    /// <param name="height">The new height of the viewport.</param>
    void Resize(int width, int height);

    /// <summary>
    /// Generates the projection matrix based on the current camera settings, such as projection type, field of view (FOV), aspect ratio, near plane, and far plane distances.
    /// </summary>
    /// <returns>The calculated projection matrix.</returns>
    Matrix4x4 GetProjection();

    /// <summary>
    /// Constructs and returns the view matrix for the camera based on its position, target, and up vector.
    /// </summary>
    /// <returns>The view matrix of the camera.</returns>
    Matrix4x4 GetView();

    /// <summary>
    /// Initializes the camera for rendering or capturing by performing any necessary setup operations.
    /// </summary>
    void Begin();

    /// <summary>
    /// Finalizes the camera operations, ensuring any necessary cleanup or finalization steps are performed.
    /// </summary>
    void End();
}