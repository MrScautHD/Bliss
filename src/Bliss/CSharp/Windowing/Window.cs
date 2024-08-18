using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Bliss.CSharp.Windowing;

public class Window {
    
    // Todo: SDL2 Window events in this class!

    /// <summary>
    /// Gets the underlying SDL2 window.
    /// </summary>
    public readonly Sdl2Window Sdl2Window;

    /// <summary>
    /// Gets the native window handle.
    /// </summary>
    public nint Handle => this.Sdl2Window.Handle;

    /// <summary>
    /// Gets the SDL window handle.
    /// </summary>
    public nint SdlWindowHandle => this.Sdl2Window.SdlWindowHandle;

    /// <summary>
    /// Gets a value indicating whether the SDL window exists.
    /// </summary>
    public bool Exists => this.Sdl2Window.Exists;

    /// <summary>
    /// Gets the scale factor of the window.
    /// </summary>
    public Vector2 ScaleFactor => this.Sdl2Window.ScaleFactor;

    /// <summary>
    /// Gets the bounds of the window.
    /// </summary>
    public Rectangle Bounds => this.Sdl2Window.Bounds;

    /// <summary>
    /// Gets a value indicating whether the window is focused.
    /// </summary>
    public bool Focused => this.Sdl2Window.Focused;

    /// <summary>
    /// Gets the mouse movement delta.
    /// </summary>
    public Vector2 MouseDelta => this.Sdl2Window.MouseDelta;

    /// <summary>
    /// Initializes a new instance of the <see cref="Window"/> class with the specified properties and creates a graphics device.
    /// </summary>
    /// <param name="width">The width of the window in pixels.</param>
    /// <param name="height">The height of the window in pixels.</param>
    /// <param name="title">The title of the window.</param>
    /// <param name="options">Options for creating the graphics device.</param>
    /// <param name="backend">The graphics backend to use.</param>
    /// <param name="graphicsDevice">Outputs the created graphics device.</param>
    public Window(int width, int height, string title, GraphicsDeviceOptions options, GraphicsBackend backend, out GraphicsDevice graphicsDevice) {
        WindowCreateInfo info = new WindowCreateInfo() {
            X = Sdl2Native.SDL_WINDOWPOS_CENTERED,
            Y = Sdl2Native.SDL_WINDOWPOS_CENTERED,
            WindowWidth = width,
            WindowHeight = height,
            WindowTitle = title
        };
        
        VeldridStartup.CreateWindowAndGraphicsDevice(info, options, backend, out this.Sdl2Window, out graphicsDevice);
        Sdl2Native.SDL_Init(SDLInitFlags.GameController | SDLInitFlags.Joystick);
    }
    
    /// <summary>
    /// Gets or sets the X coordinate of the window.
    /// </summary>
    public int X {
        get => this.Sdl2Window.X;
        set => this.Sdl2Window.X = value;
    }
    
    /// <summary>
    /// Gets or sets the Y coordinate of the window.
    /// </summary>
    public int Y {
        get => this.Sdl2Window.Y;
        set => this.Sdl2Window.Y = value;
    }
    
    /// <summary>
    /// Gets or sets the width of the window.
    /// </summary>
    public int Width {
        get => this.Sdl2Window.Width;
        set => this.Sdl2Window.Width = value;
    }
    
    /// <summary>
    /// Gets or sets the height of the window.
    /// </summary>
    public int Height {
        get => this.Sdl2Window.Height;
        set => this.Sdl2Window.Height = value;
    }
    
    /// <summary>
    /// Gets or sets the title of the window.
    /// </summary>
    public string Title {
        get => this.Sdl2Window.Title;
        set => this.Sdl2Window.Title = value;
    }
    
    /// <summary>
    /// Gets or sets the state of the window (e.g., minimized, maximized).
    /// </summary>
    public WindowState State {
        get => this.Sdl2Window.WindowState;
        set => this.Sdl2Window.WindowState = value;
    }
    
    /// <summary>
    /// Gets or sets a value indicating whether the window is visible.
    /// </summary>
    public bool Visible {
        get => this.Sdl2Window.Visible;
        set => this.Sdl2Window.Visible = value;
    }
    
    /// <summary>
    /// Gets or sets a value indicating whether the cursor is visible.
    /// </summary>
    public bool CursorVisible {
        get => this.Sdl2Window.CursorVisible;
        set => this.Sdl2Window.CursorVisible = value;
    }
    
    /// <summary>
    /// Gets or sets the opacity of the window.
    /// </summary>
    public float Opacity {
        get => this.Sdl2Window.Opacity;
        set => this.Sdl2Window.Opacity = value;
    }
    
    /// <summary>
    /// Gets or sets a value indicating whether the window is resizable.
    /// </summary>
    public bool Resizable {
        get => this.Sdl2Window.Resizable;
        set => this.Sdl2Window.Resizable = value;
    }
    
    /// <summary>
    /// Gets or sets a value indicating whether the window border is visible.
    /// </summary>
    public bool BorderVisible {
        get => this.Sdl2Window.BorderVisible;
        set => this.Sdl2Window.BorderVisible = value;
    }

    /// <summary>
    /// Processes all pending events in the event queue for the window.
    /// </summary>
    public InputSnapshot PumpEvents() {
        return this.Sdl2Window.PumpEvents();
    }

    /// <summary>
    /// Processes any pending events in the event queue, dispatching them to the appropriate event handlers.
    /// </summary>
    public void PumpEvents(SDLEventHandler eventHandler) {
        this.Sdl2Window.PumpEvents(eventHandler);
    }

    /// <summary>
    /// Sets the handler that is called when the user requests to close the window.
    /// </summary>
    /// <param name="handler">The handler to be called when the user requests to close the window. It should return true if the close request is handled, false otherwise.</param>
    public void SetCloseRequestedHandler(Func<bool> handler) {
        this.Sdl2Window.SetCloseRequestedHandler(handler);
    }

    /// <summary>
    /// Sets the position of the mouse pointer within the client area of the window.
    /// </summary>
    /// <param name="position">The new position of the mouse pointer.</param>
    public void SetMousePosition(Vector2 position) {
        this.Sdl2Window.SetMousePosition(position);
    }

    /// <summary>
    /// Sets the position of the mouse cursor relative to the window. The X and Y coordinates are specified in pixels.
    /// </summary>
    /// <param name="x">The X coordinate of the mouse cursor.</param>
    /// <param name="y">The Y coordinate of the mouse cursor.</param>
    public void SetMousePosition(int x, int y) {
        this.Sdl2Window.SetMousePosition(x, y);
    }

    /// <summary>
    /// Converts the coordinates of a point from client-space coordinates to screen-space coordinates.
    /// </summary>
    /// <param name="point">The point to convert.</param>
    /// <returns>The converted point in screen-space coordinates.</returns>
    public Point ClientToScreen(Point point) {
        return this.Sdl2Window.ClientToScreen(point);
    }

    /// <summary>
    /// Converts the specified screen coordinates to client coordinates.
    /// </summary>
    /// <param name="point">The screen coordinates to convert.</param>
    /// <returns>The client coordinates equivalent to the specified screen coordinates.</returns>
    public Point ScreenToClient(Point point) {
        return this.Sdl2Window.ScreenToClient(point);
    }

    /// <summary>
    /// Closes the window.
    /// </summary>
    public void Close() {
        this.Sdl2Window.Close();
    }
}