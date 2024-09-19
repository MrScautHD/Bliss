using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Veldrid.OpenGL;

namespace Bliss.CSharp.Windowing;

public interface IWindow : IDisposable {
    
    /// <summary>
    /// Gets the native handle of the window. This handle can be used to interact
    /// with low-level windowing operations directly through platform-specific
    /// APIs or libraries.
    /// </summary>
    public nint Handle { get; }
    
    /// <summary>
    /// Gets the unique identifier for the window. This identifier can be used to
    /// reference the window in various windowing and event handling operations.
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Gets the source of the swapchain tied to the window. This source provides
    /// the necessary information to create and manage swapchains for rendering
    /// graphics content within the window.
    /// </summary>
    SwapchainSource SwapchainSource { get; }

    /// <summary>
    /// Indicates whether the window currently exists.
    /// This property can be used to check if the window has not been closed or destroyed.
    /// </summary>
    bool Exists { get; }

    /// <summary>
    /// Indicates whether the window is currently focused. A window is considered focused
    /// when it has received input focus and is the active window receiving user input.
    /// This property can be used to check if the window is the foreground window.
    /// </summary>
    bool IsFocused { get; }

    /// <summary>
    /// Gets or sets the state of the window, which defines its appearance and behavior.
    /// The possible states include resizable, full screen, maximized, minimized,
    /// borderless full screen, and hidden.
    /// </summary>
    WindowState State { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the window is visible on the screen.
    /// If set to true, the window will be displayed; otherwise, it will be hidden.
    /// </summary>
    bool Visible { get; set; }

    /// <summary>
    /// Gets or sets the opacity level of the window. The value ranges from 0.0 (completely transparent)
    /// to 1.0 (completely opaque). Adjusting the opacity can be used for visual effects or overlay purposes.
    /// </summary>
    float Opacity { get; set; }

    /// <summary>
    /// Indicates whether the window can be resized by the user.
    /// If set to true, the window can be resized; otherwise, it cannot be resized.
    /// </summary>
    bool Resizable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the window border is visible. When set to true, the window
    /// will have a visible border, which typically includes the title bar and the window frame that allows
    /// users to resize and move the window. When set to false, the window will be borderless.
    /// </summary>
    bool BorderVisible { get; set; }

    /// <summary>
    /// Retrieves the current title of the window.
    /// </summary>
    /// <returns>The current window title as a string.</returns>
    string GetTitle();

    /// <summary>
    /// Sets the title of the window.
    /// </summary>
    /// <param name="title">The new title to set for the window.</param>
    void SetTitle(string title);

    /// <summary>
    /// Retrieves the current size of the window.
    /// </summary>
    /// <returns>A tuple containing the width and height of the window.</returns>
    (int, int) GetSize();

    /// <summary>
    /// Sets the size of the window to the specified width and height.
    /// </summary>
    /// <param name="width">The new width for the window.</param>
    /// <param name="height">The new height for the window.</param>
    void SetSize(int width, int height);

    /// <summary>
    /// Retrieves the current width of the window.
    /// </summary>
    /// <returns>The current window width as an integer.</returns>
    int GetWidth();

    /// <summary>
    /// Sets the width of the window to the specified value.
    /// </summary>
    /// <param name="width">The new width for the window.</param>
    void SetWidth(int width);

    /// <summary>
    /// Retrieves the current height of the window.
    /// </summary>
    /// <returns>The current window height as an integer.</returns>
    int GetHeight();

    /// <summary>
    /// Sets the height of the window to the specified value.
    /// </summary>
    /// <param name="height">The new height for the window.</param>
    void SetHeight(int height);

    /// <summary>
    /// Sets the icon of the window.
    /// </summary>
    /// <param name="image">The image to set as the window icon, represented as an Image of Rgba32 format.</param>
    void SetIcon(Image<Rgba32> image);
    
    /// <summary>
    /// Processes all pending window events.
    /// </summary>
    public void PumpEvents();

    /// <summary>
    /// Converts the specified client-area point to screen coordinates.
    /// </summary>
    /// <param name="point">The client-area point to be converted.</param>
    /// <returns>The converted point in screen coordinates.</returns>
    Point ClientToScreen(Point point); //TODO: DO A own point or use a diffrent type!

    /// <summary>
    /// Converts the specified screen coordinates to client-area coordinates.
    /// </summary>
    /// <param name="point">The screen coordinates to convert.</param>
    /// <returns>The converted client-area coordinates as a Point.</returns>
    Point ScreenToClient(Point point);

    /// <summary>
    /// Retrieves or creates the OpenGL platform information required for the specified graphics device options and backend.
    /// </summary>
    /// <param name="options">The graphics device options to use.</param>
    /// <param name="backend">The graphics backend to use.</param>
    /// <returns>The OpenGL platform information.</returns>
    OpenGLPlatformInfo GetOrCreateOpenGlPlatformInfo(GraphicsDeviceOptions options, GraphicsBackend backend);
}