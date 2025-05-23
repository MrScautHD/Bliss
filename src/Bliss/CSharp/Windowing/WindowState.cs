namespace Bliss.CSharp.Windowing;

[Flags]
public enum WindowState {
    
    /// <summary>
    /// Indicates that the window has no specific state.
    /// </summary>
    None,
    
    /// <summary>
    /// The window is resizable, allowing the user to adjust its size.
    /// </summary>
    Resizable,

    /// <summary>
    /// The window is in full-screen mode, occupying the entire screen.
    /// </summary>
    FullScreen,

    /// <summary>
    /// The window is borderless, removing the title bar and window borders.
    /// </summary>
    Borderless,

    /// <summary>
    /// The window is maximized, taking up the largest possible area on the screen.
    /// </summary>
    Maximized,

    /// <summary>
    /// The window is minimized, reducing it to an icon or taskbar entry.
    /// </summary>
    Minimized,

    /// <summary>
    /// The window is hidden and not visible to the user.
    /// </summary>
    Hidden,

    /// <summary>
    /// The window captures the mouse, confining its movement to the window area.
    /// </summary>
    CaptureMouse,

    /// <summary>
    /// The window is always on top of other windows, maintaining its topmost position.
    /// </summary>
    AlwaysOnTop,
    
    /// <summary>
    /// The window is transparent, allowing content behind it to be partially visible.
    /// </summary>
    Transparent,
    
    /// <summary>
    /// Enables rendering optimized for high-DPI (high pixel density) displays.
    /// </summary>
    HighPixelDensity
}