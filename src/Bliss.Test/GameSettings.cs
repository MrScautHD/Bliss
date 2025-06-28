using System.Reflection;
using Bliss.CSharp.Windowing;
using Veldrid;

namespace Bliss.Test;

public struct GameSettings {
    
    /// <summary>
    /// The title of the game window.
    /// </summary>
    public string Title { get; init; }
    
    /// <summary>
    /// The width of the game window in pixels.
    /// </summary>
    public int Width { get; init; }
    
    /// <summary>
    /// The height of the game window in pixels.
    /// </summary>
    public int Height { get; init; }
    
    /// <summary>
    /// The file path to the window icon image.
    /// </summary>
    public string IconPath { get; private set; }
    
    /// <summary>
    /// The target frames per second (FPS) the game aims to achieve for rendering updates.
    /// </summary>
    public int TargetFps { get; init; }
    
    /// <summary>
    /// The fixed timestep duration in seconds, used for fixed update.
    /// </summary>
    public double FixedTimeStep { get; init; }
    
    /// <summary>
    /// Flags that determine window behaviors such as resizable or fullscreen.
    /// </summary>
    public WindowState WindowFlags { get; init; }
    
    /// <summary>
    /// The graphics backend (e.g., Vulkan, Direct3D, OpenGL) to be used for rendering.
    /// </summary>
    public GraphicsBackend Backend { get; init; }
    
    /// <summary>
    /// Indicates whether vertical synchronization (VSync) is enabled to prevent screen tearing.
    /// </summary>
    public bool VSync { get; init; }
    
    /// <summary>
    /// The level of multisample anti-aliasing (MSAA) to use for rendering.
    /// </summary>
    public TextureSampleCount SampleCount { get; init; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="GameSettings"/> struct.
    /// </summary>
    public GameSettings() {
        this.Title = Assembly.GetEntryAssembly()?.GetName().Name ?? "Bliss";
        this.Width = 1280;
        this.Height = 720;
        this.IconPath = string.Empty;
        this.TargetFps = 0;
        this.FixedTimeStep = 1.0F / 60.0F;
        this.WindowFlags = WindowState.Resizable;
        this.Backend = Window.GetPlatformDefaultBackend();
        this.VSync = true;
        this.SampleCount = TextureSampleCount.Count1;
    }
}