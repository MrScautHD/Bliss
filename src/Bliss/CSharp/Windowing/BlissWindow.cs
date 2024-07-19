using Silk.NET.Core;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Window = Silk.NET.Windowing.Window;

namespace Bliss.CSharp.Windowing;

public class BlissWindow : IDisposable {

    /// <inheritdoc cref="IWindow.Parent" />
    public IWindowHost? Parent => this._window.Parent;
    
    /// <inheritdoc cref="IWindow.Monitor" />
    public IMonitor? Monitor => this._window.Monitor;
    
    /// <inheritdoc cref="IWindow.IsClosing" />
    public bool IsClosing => this._window.IsClosing;
    
    /// <inheritdoc cref="IWindow.BorderSize" />
    public Rectangle<int> BorderSize => this._window.BorderSize;
    
    /// <inheritdoc cref="IWindow.Move" />
    public event Action<Vector2D<int>>? Move {
        add => this._window.Move += value;
        remove => this._window.Move -= value;
    }
    
    /// <inheritdoc cref="IWindow.StateChanged" />
    public event Action<WindowState>? StateChanged {
        add => this._window.StateChanged += value;
        remove => this._window.StateChanged -= value;
    }
    
    /// <inheritdoc cref="IWindow.FileDrop" />
    public event Action<string[]>? FileDrop {
        add => this._window.FileDrop += value;
        remove => this._window.FileDrop -= value;
    }
    
    private IWindow _window;

    /// <summary>
    /// Constructor for creating a BlissWindow object.
    /// </summary>
    /// <param name="size">The size of the window.</param>
    /// <param name="title">The title of the window.</param>
    public BlissWindow(Vector2D<int> size, string title) {
        this._window = Window.Create(WindowOptions.DefaultVulkan with {
            Size = size,
            Title = title
        });
    }

    /// <inheritdoc cref="IWindow.Initialize" />
    public void Init() {
        this._window.Initialize();

        if (this._window.VkSurface == null) {
            throw new PlatformNotSupportedException("Windowing platform doesn't support Vulkan.");
        }
        
        this._window.Run();
    }

    /// <inheritdoc cref="IWindow.SetWindowIcon" />
    public void SetWindowIcon(ReadOnlySpan<RawImage> icons) {
        this._window.SetWindowIcon(icons);
    }

    /// <inheritdoc cref="IWindow.Dispose" />
    public void Dispose() {
        this._window.Dispose();
    }
}