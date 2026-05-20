using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Interact.Gamepads;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Interact.Mice;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Windowing.Events;
using SDL3;
using Veldrith;
using Image = Bliss.CSharp.Images.Image;
using Point = Bliss.CSharp.Transformations.Point;

namespace Bliss.CSharp.Windowing;

public class Sdl3Window : Disposable, IWindow {
    
    /// <summary>
    /// Specifies the initialization flags for SDL, which control which SDL subsystems are initialized when starting the window.
    /// </summary>
    private const SDL.InitFlags InitFlags = SDL.InitFlags.Video | SDL.InitFlags.Gamepad | SDL.InitFlags.Joystick;
    
    /// <summary>
    /// Sets the number of events that are processed in each loop iteration when handling window events.
    /// </summary>
    private const int EventsPerPeep = 64;

    /// <summary>
    /// Represents the native handle of the SDL window, which is used for low-level window operations and interactions.
    /// </summary>
    public nint Handle { get; private set; }

    /// <summary>
    /// Represents the unique identifier for the SDL window.
    /// This identifier can be used to reference or differentiate between multiple windows.
    /// </summary>
    public uint Id { get; private set; }

    /// <summary>
    /// Represents the source of the swapchain, which is used for managing the rendering surface and synchronization for the window.
    /// </summary>
    public SwapchainSource SwapchainSource { get; private set; }

    /// <summary>
    /// Indicates whether the window currently exists.
    /// </summary>
    public bool Exists { get; private set; }

    /// <summary>
    /// Indicates whether the window is currently focused.
    /// </summary>
    public bool IsFocused { get; private set; }
    
    /// <summary>
    /// Represents an event that is triggered whenever an SDL event occurs within the window.
    /// </summary>
    public event Action<SDL.Event>? SdlEvent; 
    
    /// <summary>
    /// Occurs when the window is resized.
    /// </summary>
    public event Action? Resized;
    
    /// <summary>
    /// Occurs after the window has closed.
    /// </summary>
    public event Action? Closed;

    /// <summary>
    /// Occurs when the window gains focus.
    /// </summary>
    public event Action? FocusGained;

    /// <summary>
    /// Occurs when the window loses focus.
    /// </summary>
    public event Action? FocusLost;

    /// <summary>
    /// Occurs when the window is shown.
    /// </summary>
    public event Action? Shown;

    /// <summary>
    /// Occurs when the window is hidden.
    /// </summary>
    public event Action? Hidden;

    /// <summary>
    /// Occurs when the window is exposed (made visible or unhidden).
    /// </summary>
    public event Action? Exposed;

    /// <summary>
    /// Occurs when the window is moved.
    /// </summary>
    public event Action<Point>? Moved;

    /// <summary>
    /// Occurs when the mouse enters the window.
    /// </summary>
    public event Action? MouseEntered;

    /// <summary>
    /// Occurs when the mouse leaves the window.
    /// </summary>
    public event Action? MouseLeft;

    /// <summary>
    /// Occurs when the mouse wheel is scrolled.
    /// </summary>
    public event Action<Vector2>? MouseWheel;

    /// <summary>
    /// Occurs when the mouse is moved.
    /// </summary>
    public event Action<Vector2>? MouseMove;

    /// <summary>
    /// Occurs when a mouse button is pressed.
    /// </summary>
    public event Action<MouseEvent>? MouseButtonDown;

    /// <summary>
    /// Occurs when a mouse button is released.
    /// </summary>
    public event Action<MouseEvent>? MouseButtonUp;

    /// <summary>
    /// Occurs when a key is pressed.
    /// </summary>
    public event Action<KeyEvent>? KeyDown;

    /// <summary>
    /// Occurs when a key is released.
    /// </summary>
    public event Action<KeyEvent>? KeyUp;

    /// <summary>
    /// Represents an event that triggers when text input is received.
    /// </summary>
    public event Action<string>? TextInput;

    /// <summary>
    /// Event triggered when a new gamepad is connected to the system.
    /// </summary>
    public event Action<uint>? GamepadAdded;

    /// <summary>
    /// Event triggered when a gamepad is removed from the system.
    /// </summary>
    public event Action<uint>? GamepadRemoved;

    /// <summary>
    /// Represents an event that is triggered when a gamepad axis is moved.
    /// </summary>
    public event Action<uint, GamepadAxis, short>? GamepadAxisMoved;

    /// <summary>
    /// Occurs when a button on the gamepad is pressed.
    /// </summary>
    public event Action<uint, GamepadButton>? GamepadButtonDown;

    /// <summary>
    /// Triggered when a gamepad button is released, providing the button identifier and related data.
    /// </summary>
    public event Action<uint, GamepadButton>? GamepadButtonUp;

    /// <summary>
    /// Occurs when a drag-and-drop operation is performed.
    /// </summary>
    public event Action<string>? DragDrop;

    /// <summary>
    /// Contains a collection of SDL_Event objects used for polling and handling SDL events.
    /// </summary>
    private readonly SDL.Event[] _events;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Sdl3Window"/> class with the specified width, height, title, and window state.
    /// </summary>
    /// <param name="width">The width of the window in pixels.</param>
    /// <param name="height">The height of the window in pixels.</param>
    /// <param name="title">The title of the window.</param>
    /// <param name="state">The initial state of the window, specified as a <see cref="WindowState"/> value.</param>
    /// <param name="backend">The graphics backend to use for rendering (e.g., Vulkan, OpenGL).</param>
    /// <exception cref="Exception">Thrown if SDL fails to initialize the subsystem required for creating the window.</exception>
    public Sdl3Window(int width, int height, string title, WindowState state, GraphicsBackend backend) {
        this.Exists = true;
        
        SDL.SetHint(SDL.Hints.WindowsCloseOnAltF4, "1");
        SDL.SetHint(SDL.Hints.MouseFocusClickthrough, "1");
        
        if (!SDL.InitSubSystem(InitFlags)) {
            throw new Exception($"Failed to initialise SDL! Error: {SDL.GetError()}");
        }

        // Setup window flags.
        SDL.WindowFlags flags = this.MapWindowState(state);

        if (backend == GraphicsBackend.Vulkan) {
            flags |= SDL.WindowFlags.Vulkan;
        }
        
        // Enable events.
        SDL.SetGamepadEventsEnabled(true);
        SDL.SetJoystickEventsEnabled(true);

        // Create window.
        this.Handle = SDL.CreateWindow(title, width, height, flags);
        
        if (this.Handle == nint.Zero) {
            throw new InvalidOperationException($"Failed to create window! Error: {SDL.GetError()}");
        }
        
        this.Id = SDL.GetWindowID(this.Handle);
        this.SwapchainSource = this.CreateSwapchainSource();
        this._events = new SDL.Event[EventsPerPeep];
    }
    
    /// <summary>
    /// Retrieves a module handle for the specified module.
    /// </summary>
    /// <param name="lpModuleName">A pointer to a null-terminated string that specifies the name of the module. If this parameter is null, GetModuleHandleW returns a handle to the file used to create the calling process.</param>
    /// <returns>A handle to the specified module, or null if the module is not found.</returns>
    /// <exception cref="System.ComponentModel.Win32Exception">Thrown if an error occurs when retrieving the module handle.</exception>
    [DllImport("kernel32", ExactSpelling = true)]
    private static extern unsafe nint GetModuleHandleW(ushort* lpModuleName);

    /// <summary>
    /// Retrieves the title of the window.
    /// </summary>
    /// <returns>The title of the window as a string.</returns>
    public string GetTitle() {
        return SDL.GetWindowTitle(this.Handle);
    }

    /// <summary>
    /// Sets the title of the window.
    /// </summary>
    /// <param name="title">The new title to set for the window.</param>
    /// <exception cref="System.InvalidOperationException">Thrown if the title could not be set due to an internal error.</exception>
    public void SetTitle(string title) {
        if (!SDL.SetWindowTitle(this.Handle, title)) {
            Logger.Warn($"Failed to set the title of the window: [{this.Id}] Error: {SDL.GetError()}");
        }
    }

    /// <summary>
    /// Retrieves the current width and height of the window in pixels.
    /// </summary>
    /// <returns>A tuple containing two integers representing the width and height of the window in pixels.</returns>
    public (int Width, int Height) GetSize() {
        if (!SDL.GetWindowSizeInPixels(this.Handle, out int width, out int height)) {
            Logger.Warn($"Failed to get the size of the window: [{this.Id}] Error: {SDL.GetError()}");
        }
        
        return (width, height);
    }

    /// <summary>
    /// Sets the size of the window to the specified width and height.
    /// </summary>
    /// <param name="width">The new width of the window.</param>
    /// <param name="height">The new height of the window.</param>
    public void SetSize(int width, int height) {
        if (!SDL.SetWindowSize(this.Handle, width, height)) {
            Logger.Warn($"Failed to set the size of the window: [{this.Id}] Error: {SDL.GetError()}");
        }
    }

    /// <summary>
    /// Gets the width of the window.
    /// </summary>
    /// <returns>The width of the window in pixels.</returns>
    public int GetWidth() {
        return this.GetSize().Width;
    }

    /// <summary>
    /// Sets the width of the window.
    /// </summary>
    /// <param name="width">The new width of the window.</param>
    public void SetWidth(int width) {
        this.SetSize(width, this.GetHeight());
    }

    /// <summary>
    /// Retrieves the height of the window.
    /// </summary>
    /// <returns>The height of the window in pixels.</returns>
    public int GetHeight() {
        return this.GetSize().Height;
    }
    
    /// <summary>
    /// Sets the height of the window to the specified value.
    /// </summary>
    /// <param name="height">The new height of the window.</param>
    public void SetHeight(int height) {
        this.SetSize(this.GetWidth(), height);
    }
    
    /// <summary>
    /// Retrieves the minimum allowed size of the window.
    /// </summary>
    /// <returns> A tuple containing the minimum width and height of the window.</returns>
    public (int Width, int Height) GetMinimumSize() {
        if (!SDL.GetWindowMinimumSize(this.Handle, out int width, out int height)) {
            Logger.Warn($"Failed to get the min size of the window: [{this.Id}] Error: {SDL.GetError()}");
        }
        
        return (width, height);
    }
    
    /// <summary>
    /// Sets the minimum allowed size of the window.
    /// </summary>
    /// <param name="width">The minimum width the window can be resized to.</param>
    /// <param name="height">The minimum height the window can be resized to.</param>
    public void SetMinimumSize(int width, int height) {
        if (!SDL.SetWindowMinimumSize(this.Handle, width, height)) {
            Logger.Warn($"Failed to set the min size of the window: [{this.Id}] Error: {SDL.GetError()}");
        }
    }
    
    /// <summary>
    /// Retrieves the minimum allowed width of the window.
    /// </summary>
    /// <returns>The minimum window width as an integer.</returns>
    public int GetMinimumWidth() {
        return this.GetMinimumSize().Width;
    }
    
    /// <summary>
    /// Sets the minimum allowed width of the window.
    /// </summary>
    /// <param name="width">The minimum width the window can be resized to.</param>
    public void SetMinimumWidth(int width) {
        this.SetMinimumSize(width, this.GetMinimumHeight());
    }
    
    /// <summary>
    /// Retrieves the minimum allowed height of the window.
    /// </summary>
    /// <returns>The minimum window height as an integer.</returns>
    public int GetMinimumHeight() {
        return this.GetMinimumSize().Height;
    }
    
    /// <summary>
    /// Sets the minimum allowed height of the window.
    /// </summary>
    /// <param name="height">The minimum height the window can be resized to.</param>
    public void SetMinimumHeight(int height) {
        this.SetMinimumSize(this.GetMinimumWidth(), height);
    }

    /// <summary>
    /// Retrieves the current position of the window.
    /// </summary>
    /// <returns>A tuple containing the x and y coordinates of the window's position.</returns>
    /// <exception cref="System.Exception">Thrown if there is an error retrieving the window's position.</exception>
    public (int X, int Y) GetPosition() {
        if (!SDL.GetWindowPosition(this.Handle, out int x, out int y)) {
            Logger.Warn($"Failed to set the position to the window: [{this.Id}] Error: {SDL.GetError()}");
        }
        
        return (x, y);
    }

    /// <summary>
    /// Sets the position of the window on the screen.
    /// </summary>
    /// <param name="x">The x-coordinate of the window position.</param>
    /// <param name="y">The y-coordinate of the window position.</param>
    public void SetPosition(int x, int y) {
        SDL.SetWindowPosition(this.Handle, x, y);
    }

    /// <summary>
    /// Retrieves the current X-coordinate position of the window.
    /// </summary>
    /// <returns>The X-coordinate of the window's position.</returns>
    public int GetX() {
        return this.GetPosition().X;
    }

    /// <summary>
    /// Sets the X coordinate of the window's position.
    /// </summary>
    /// <param name="x">The new X coordinate of the window.</param>
    public void SetX(int x) {
        this.SetPosition(x, this.GetY());
    }

    /// <summary>
    /// Retrieves the Y-coordinate of the window's position.
    /// </summary>
    /// <returns>The Y-coordinate of the window's position.</returns>
    public int GetY() {
        return this.GetPosition().Y;
    }

    /// <summary>
    /// Sets the Y-coordinate of the window's position.
    /// </summary>
    /// <param name="y">The new Y-coordinate.</param>
    public void SetY(int y) {
        this.SetPosition(this.GetX(), y);
    }
    
    /// <summary>
    /// Retrieves the current state of the window.
    /// </summary>
    /// <returns>The current state of the window represented by the <see cref="WindowState"/> enumeration.</returns>
    public WindowState GetState() {
        SDL.WindowFlags flags = SDL.GetWindowFlags(this.Handle);
        WindowState state = WindowState.None;
        
        if (flags.HasFlag(SDL.WindowFlags.Resizable)) {
            state |= WindowState.Resizable;
        }
        if (flags.HasFlag(SDL.WindowFlags.Fullscreen)) {
            state |= WindowState.FullScreen;
        }
        if (flags.HasFlag(SDL.WindowFlags.Borderless)) {
            state |= WindowState.Borderless;
        }
        if (flags.HasFlag(SDL.WindowFlags.Maximized)) {
            state |= WindowState.Maximized;
        }
        if (flags.HasFlag(SDL.WindowFlags.Minimized)) {
            state |= WindowState.Minimized;
        }
        if (flags.HasFlag(SDL.WindowFlags.Hidden)) {
            state |= WindowState.Hidden;
        }
        if (flags.HasFlag(SDL.WindowFlags.MouseCapture)) {
            state |= WindowState.CaptureMouse;
        }
        if (flags.HasFlag(SDL.WindowFlags.AlwaysOnTop)) {
            state |= WindowState.AlwaysOnTop;
        }
        if (flags.HasFlag(SDL.WindowFlags.Transparent)) {
            state |= WindowState.Transparent;
        }
        if (flags.HasFlag(SDL.WindowFlags.HighPixelDensity)) {
            state |= WindowState.HighPixelDensity;
        }
        
        // Remove "None" state if there is something.
        if (state != WindowState.None && state.HasFlag(WindowState.None)) {
            state &= ~WindowState.None;
        }
        
        return state;
    }
    
    /// <summary>
    /// Determines if the current window state matches the specified state.
    /// </summary>
    /// <param name="state">The window state to compare with the current state.</param>
    /// <returns>True if the current window state matches the specified state; otherwise, false.</returns>
    public bool HasState(WindowState state) {
        return this.GetState().HasFlag(state);
    }

    /// <summary>
    /// Specifies whether the window should be resizable and updates its resizable state accordingly.
    /// </summary>
    /// <param name="resizable">A boolean value indicating if the window should be resizable (true) or not resizable (false).</param>
    public void SetResizable(bool resizable) {
        SDL.SetWindowResizable(this.Handle, resizable);
    }

    /// <summary>
    /// Sets the fullscreen state of the window.
    /// </summary>
    /// <param name="fullscreen">A boolean value indicating whether the window should be fullscreen. Pass <c>true</c> to enable fullscreen mode, or <c>false</c> to disable it.</param>
    public void SetFullscreen(bool fullscreen) {
        SDL.SetWindowFullscreen(this.Handle, fullscreen);
    }

    /// <summary>
    /// Sets whether the window should have a border.
    /// </summary>
    /// <param name="bordered">A boolean indicating whether the window should be bordered. Pass true to enable the border, or false to remove it.</param>
    public void SetBordered(bool bordered) {
        SDL.SetWindowBordered(this.Handle, bordered);
    }

    /// <summary>
    /// Maximizes the window to occupy the entire screen space available within the current display's working area.
    /// </summary>
    public void Maximize() {
        SDL.MaximizeWindow(this.Handle);
    }

    /// <summary>
    /// Minimizes the current window.
    /// </summary>
    public void Minimize() {
        SDL.MinimizeWindow(this.Handle);
    }

    /// <summary>
    /// Hides the window, making it invisible to the user.
    /// </summary>
    public void Hide() {
        SDL.HideWindow(this.Handle);
    }

    /// <summary>
    /// Makes the window visible if it is currently hidden.
    /// </summary>
    public void Show() {
        SDL.ShowWindow(this.Handle);
    }

    /// <summary>
    /// Captures or releases the mouse input for the window.
    /// </summary>
    /// <param name="enabled">A boolean indicating whether to capture the mouse (true) or release it (false).</param>
    public void CaptureMouse(bool enabled) {
        SDL.CaptureMouse(enabled);
    }

    /// <summary>
    /// Sets whether the window should always be displayed on top of other windows.
    /// </summary>
    /// <param name="alwaysOnTop">A boolean indicating whether the window should be always on top. True to enable, false to disable.</param>
    public void SetWindowAlwaysOnTop(bool alwaysOnTop) {
        SDL.SetWindowAlwaysOnTop(this.Handle, alwaysOnTop);
    }
    
    /// <summary>
    /// Sets the icon for the SDL3 window using the provided image.
    /// </summary>
    /// <param name="image">The image to set as the window icon. It should be of type <see cref="Images.Image"/>.</param>
    /// <exception cref="Exception">Thrown if an error occurs while setting the window icon.</exception>
    public unsafe void SetIcon(Image image) {
        fixed (byte* dataPtr = image.Data) {
            nint surface = SDL.CreateSurfaceFrom(image.Width, image.Height, SDL.PixelFormat.ABGR8888, (nint) dataPtr, image.Width * 4);

            if (surface == nint.Zero) {
                Logger.Error($"Failed to set Sdl3 window icon: {SDL.GetError()}");
            }

            SDL.SetWindowIcon(this.Handle, surface);
            SDL.DestroySurface(surface);
        }
    }

    /// <summary>
    /// Processes all pending events for the window and invokes corresponding event handlers.
    /// </summary>
    /// <exception cref="System.ComponentModel.Win32Exception">Thrown if an error occurs when processing events.</exception>
    public void PumpEvents() {
        SDL.PumpEvents();
        int eventsRead;
        
        do {
            eventsRead = SDL.PeepEvents(this._events, this._events.Length, SDL.EventAction.GetEvent, (uint) SDL.EventType.First, (uint) SDL.EventType.Last);
            for (int i = 0; i < eventsRead; i++) {
                this.HandleEvent(this._events[i]);
            }
        } while (eventsRead == EventsPerPeep);
    }

    /// <summary>
    /// Converts a point from client-area coordinates to screen coordinates.
    /// </summary>
    /// <param name="point">The point in client-area coordinates to be converted.</param>
    /// <returns>The point in screen coordinates.</returns>
    public Point ClientToScreen(Point point) {
        return new Point(point.X + this.GetX(), point.Y + this.GetY());
    }

    /// <summary>
    /// Converts a point from screen coordinates to client coordinates.
    /// </summary>
    /// <param name="point">The point in screen coordinates to be converted.</param>
    /// <returns>The point in client coordinates.</returns>
    public Point ScreenToClient(Point point) {
        return new Point(point.X - this.GetX(), point.Y - this.GetY());
    }

    /// <summary>
    /// Creates a SwapchainSource for use with the current operating system.
    /// </summary>
    /// <returns>A SwapchainSource configured for the underlying windowing system.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown if the current operating system is not supported.</exception>
    private unsafe SwapchainSource CreateSwapchainSource() {
        if (OperatingSystem.IsWindows()) {
            nint hwnd = SDL.GetPointerProperty(SDL.GetWindowProperties(this.Handle), SDL.Props.WindowWin32HWNDPointer, nint.Zero);
            nint hInstance = GetModuleHandleW(null);
            return SwapchainSource.CreateWin32(hwnd, hInstance);
        }
        else if (OperatingSystem.IsLinux()) {
            if (SDL.GetCurrentVideoDriver() == "x11") {
                nint display = SDL.GetPointerProperty(SDL.GetWindowProperties(this.Handle), SDL.Props.WindowX11DisplayPointer, nint.Zero);
                long surface = SDL.GetNumberProperty(SDL.GetWindowProperties(this.Handle), SDL.Props.WindowX11WindowNumber, 0);
                return SwapchainSource.CreateXlib(display, (nint) surface);
            }
            else if (SDL.GetCurrentVideoDriver() == "wayland") {
                nint display = SDL.GetPointerProperty(SDL.GetWindowProperties(this.Handle), SDL.Props.WindowWaylandDisplayPointer, nint.Zero);
                nint surface = SDL.GetPointerProperty(SDL.GetWindowProperties(this.Handle), SDL.Props.WindowWaylandSurfacePointer, nint.Zero);
                return SwapchainSource.CreateWayland(display, surface);
            }
        }
        else if (OperatingSystem.IsMacOS()) {
            nint surface = SDL.GetPointerProperty(SDL.GetWindowProperties(this.Handle), SDL.Props.WindowCocoaWindowPointer, nint.Zero);
            return SwapchainSource.CreateNSWindow(surface);
        }
        
        throw new PlatformNotSupportedException("Failed to create a SwapchainSource!");
    }
    
    /// <summary>
    /// Maps a given <see cref="WindowState"/> to the corresponding <see cref="SDL.WindowFlags"/>.
    /// </summary>
    /// <param name="state">The state of the window to map, specified as <see cref="WindowState"/>.</param>
    /// <returns>The corresponding <see cref="SDL.WindowFlags"/> for the provided <paramref name="state"/>.</returns>
    /// <exception cref="Exception">Thrown when an invalid <see cref="WindowState"/> is provided.</exception>
    private SDL.WindowFlags MapWindowState(WindowState state) {
        switch (state) {
            case WindowState.Resizable:
                return SDL.WindowFlags.Resizable;
            case WindowState.FullScreen:
                return SDL.WindowFlags.Fullscreen;
            case WindowState.Borderless:
                return SDL.WindowFlags.Borderless;
            case WindowState.Maximized:
                return SDL.WindowFlags.Maximized;
            case WindowState.Minimized:
                return SDL.WindowFlags.Minimized;
            case WindowState.Hidden:
                return SDL.WindowFlags.Hidden;
            case WindowState.CaptureMouse:
                return SDL.WindowFlags.MouseCapture;
            case WindowState.AlwaysOnTop:
                return SDL.WindowFlags.AlwaysOnTop;
            case WindowState.Transparent:
                return SDL.WindowFlags.Transparent;
            default:
                throw new Exception($"Invalid WindowState: [{state}]");
        }
    }

    /// <summary>
    /// Handles a given SDL event and triggers the appropriate window event based on the type of the SDL event.
    /// </summary>
    /// <param name="sdlEvent">The SDL event to handle.</param>
    private void HandleEvent(SDL.Event sdlEvent) {
        this.SdlEvent?.Invoke(sdlEvent);
        
        switch ((SDL.EventType)sdlEvent.Type) {
            case SDL.EventType.Quit:
            case SDL.EventType.Terminating:
                this.Exists = false;
                this.Closed?.Invoke();
                break;

            case SDL.EventType.WindowResized:
            case SDL.EventType.WindowPixelSizeChanged:
            case SDL.EventType.WindowMinimized:
            case SDL.EventType.WindowMaximized:
            case SDL.EventType.WindowRestored:
                this.Resized?.Invoke();
                break;

            case SDL.EventType.WindowFocusGained:
                this.IsFocused = true;
                this.FocusGained?.Invoke();
                break;

            case SDL.EventType.WindowFocusLost:
                this.IsFocused = false;
                this.FocusLost?.Invoke();
                break;

            case SDL.EventType.WindowShown:
                this.Shown?.Invoke();
                break;

            case SDL.EventType.WindowHidden:
                this.Hidden?.Invoke();
                break;

            case SDL.EventType.WindowMouseEnter:
                this.MouseEntered?.Invoke();
                break;

            case SDL.EventType.WindowMouseLeave:
                this.MouseLeft?.Invoke();
                break;

            case SDL.EventType.WindowExposed:
                this.Exposed?.Invoke();
                break;

            case SDL.EventType.WindowMoved:
                this.Moved?.Invoke(new Point(sdlEvent.Window.Data1, sdlEvent.Window.Data2));
                break;

            case SDL.EventType.MouseWheel:
                this.MouseWheel?.Invoke(new Vector2(sdlEvent.Wheel.X, sdlEvent.Wheel.Y));
                break;

            case SDL.EventType.MouseMotion:
                this.MouseMove?.Invoke(new Vector2(sdlEvent.Motion.X, sdlEvent.Motion.Y));
                break;

            case SDL.EventType.MouseButtonDown:
                this.MouseButtonDown?.Invoke(new MouseEvent(this.MapMouseButton((SDL.MouseButtonFlags) sdlEvent.Button.Button), sdlEvent.Button.Down, sdlEvent.Button.Clicks == 2));
                break;

            case SDL.EventType.MouseButtonUp:
                this.MouseButtonUp?.Invoke(new MouseEvent(this.MapMouseButton((SDL.MouseButtonFlags) sdlEvent.Button.Button), sdlEvent.Button.Down, sdlEvent.Button.Clicks == 2));
                break;

            case SDL.EventType.KeyDown:
                this.KeyDown?.Invoke(new KeyEvent(this.MapKey(sdlEvent.Key.Scancode), sdlEvent.Key.Down, sdlEvent.Key.Repeat));
                break;

            case SDL.EventType.KeyUp:
                this.KeyUp?.Invoke(new KeyEvent(this.MapKey(sdlEvent.Key.Scancode), sdlEvent.Key.Down, sdlEvent.Key.Repeat));
                break;

            case SDL.EventType.TextInput:
                string? text = Marshal.PtrToStringUTF8(sdlEvent.Text.Text);
                if (text != null) {
                    this.TextInput?.Invoke(text);
                }

                break;

            case SDL.EventType.GamepadAdded:
                this.GamepadAdded?.Invoke(sdlEvent.GDevice.Which);
                break;

            case SDL.EventType.GamepadRemoved:
                this.GamepadRemoved?.Invoke(sdlEvent.GDevice.Which);
                break;

            case SDL.EventType.GamepadAxisMotion:
                this.GamepadAxisMoved?.Invoke(sdlEvent.GAxis.Which, this.MapGamepadAxis((SDL.GamepadAxis) sdlEvent.GAxis.Axis), sdlEvent.GAxis.Value);
                break;

            case SDL.EventType.GamepadButtonDown:
                this.GamepadButtonDown?.Invoke(sdlEvent.GButton.Which, this.MapGamepadButton((SDL.GamepadButton) sdlEvent.GButton.Button));
                break;

            case SDL.EventType.GamepadButtonUp:
                this.GamepadButtonUp?.Invoke(sdlEvent.GButton.Which, this.MapGamepadButton((SDL.GamepadButton) sdlEvent.GButton.Button));
                break;

            case SDL.EventType.DropFile:
                string? data = Marshal.PtrToStringUTF8(sdlEvent.Drop.Data);
                if (data != null) {
                    this.DragDrop?.Invoke(data);
                }
                break;
        }
    }

    /// <summary>
    /// Maps an SDL_Scancode to the corresponding KeyboardKey.
    /// </summary>
    /// <param name="scancode">The SDL_Scancode representing the key to be mapped.</param>
    /// <returns>The corresponding KeyboardKey for the given SDL_Scancode.</returns>
    private KeyboardKey MapKey(SDL.Scancode scancode) {
        return scancode switch {
            SDL.Scancode.A => KeyboardKey.A,
            SDL.Scancode.B => KeyboardKey.B,
            SDL.Scancode.C => KeyboardKey.C,
            SDL.Scancode.D => KeyboardKey.D,
            SDL.Scancode.E => KeyboardKey.E,
            SDL.Scancode.F => KeyboardKey.F,
            SDL.Scancode.G => KeyboardKey.G,
            SDL.Scancode.H => KeyboardKey.H,
            SDL.Scancode.I => KeyboardKey.I,
            SDL.Scancode.J => KeyboardKey.J,
            SDL.Scancode.K => KeyboardKey.K,
            SDL.Scancode.L => KeyboardKey.L,
            SDL.Scancode.M => KeyboardKey.M,
            SDL.Scancode.N => KeyboardKey.N,
            SDL.Scancode.O => KeyboardKey.O,
            SDL.Scancode.P => KeyboardKey.P,
            SDL.Scancode.Q => KeyboardKey.Q,
            SDL.Scancode.R => KeyboardKey.R,
            SDL.Scancode.S => KeyboardKey.S,
            SDL.Scancode.T => KeyboardKey.T,
            SDL.Scancode.U => KeyboardKey.U,
            SDL.Scancode.V => KeyboardKey.V,
            SDL.Scancode.W => KeyboardKey.W,
            SDL.Scancode.X => KeyboardKey.X,
            SDL.Scancode.Y => KeyboardKey.Y,
            SDL.Scancode.Z => KeyboardKey.Z,
            
            SDL.Scancode.Alpha1 => KeyboardKey.Number1,
            SDL.Scancode.Alpha2 => KeyboardKey.Number2,
            SDL.Scancode.Alpha3 => KeyboardKey.Number3,
            SDL.Scancode.Alpha4 => KeyboardKey.Number4,
            SDL.Scancode.Alpha5 => KeyboardKey.Number5,
            SDL.Scancode.Alpha6 => KeyboardKey.Number6,
            SDL.Scancode.Alpha7 => KeyboardKey.Number7,
            SDL.Scancode.Alpha8 => KeyboardKey.Number8,
            SDL.Scancode.Alpha9 => KeyboardKey.Number9,
            SDL.Scancode.Alpha0 => KeyboardKey.Number0,
            
            SDL.Scancode.Return => KeyboardKey.Enter,
            SDL.Scancode.Escape => KeyboardKey.Escape,
            SDL.Scancode.Backspace => KeyboardKey.BackSpace,
            SDL.Scancode.Tab => KeyboardKey.Tab,
            SDL.Scancode.Space => KeyboardKey.Space,
            
            SDL.Scancode.Minus => KeyboardKey.Minus,
            SDL.Scancode.Equals => KeyboardKey.Plus,
            SDL.Scancode.Leftbracket => KeyboardKey.BracketLeft,
            SDL.Scancode.Rightbracket => KeyboardKey.BracketRight,
            SDL.Scancode.Backslash => KeyboardKey.BackSlash,
            SDL.Scancode.Semicolon => KeyboardKey.Semicolon,
            SDL.Scancode.Apostrophe => KeyboardKey.Quote,
            SDL.Scancode.Grave => KeyboardKey.Grave,
            SDL.Scancode.Comma => KeyboardKey.Comma,
            SDL.Scancode.Period => KeyboardKey.Period,
            SDL.Scancode.Slash => KeyboardKey.Slash,
            
            SDL.Scancode.Capslock => KeyboardKey.CapsLock,
            
            SDL.Scancode.F1 => KeyboardKey.F1,
            SDL.Scancode.F2 => KeyboardKey.F2,
            SDL.Scancode.F3 => KeyboardKey.F3,
            SDL.Scancode.F4 => KeyboardKey.F4,
            SDL.Scancode.F5 => KeyboardKey.F5,
            SDL.Scancode.F6 => KeyboardKey.F6,
            SDL.Scancode.F7 => KeyboardKey.F7,
            SDL.Scancode.F8 => KeyboardKey.F8,
            SDL.Scancode.F9 => KeyboardKey.F9,
            SDL.Scancode.F10 => KeyboardKey.F10,
            SDL.Scancode.F11 => KeyboardKey.F11,
            SDL.Scancode.F12 => KeyboardKey.F12,
            
            SDL.Scancode.Printscreen => KeyboardKey.PrintScreen,
            SDL.Scancode.Scrolllock => KeyboardKey.ScrollLock,
            SDL.Scancode.Pause => KeyboardKey.Pause,
            
            SDL.Scancode.Insert => KeyboardKey.Insert,
            SDL.Scancode.Home => KeyboardKey.Home,
            SDL.Scancode.Pageup => KeyboardKey.PageUp,
            SDL.Scancode.Delete => KeyboardKey.Delete,
            SDL.Scancode.End => KeyboardKey.End,
            SDL.Scancode.Pagedown => KeyboardKey.PageDown,
            
            SDL.Scancode.Right => KeyboardKey.Right,
            SDL.Scancode.Left => KeyboardKey.Left,
            SDL.Scancode.Down => KeyboardKey.Down,
            SDL.Scancode.Up => KeyboardKey.Up,
            
            SDL.Scancode.NumLockClear => KeyboardKey.NumLock,
            SDL.Scancode.KpDivide => KeyboardKey.KeypadDivide,
            SDL.Scancode.KpMultiply => KeyboardKey.KeypadMultiply,
            SDL.Scancode.KpMinus => KeyboardKey.KeypadMinus,
            SDL.Scancode.KpPlus => KeyboardKey.KeypadPlus,
            SDL.Scancode.KpEnter => KeyboardKey.KeypadEnter,
            
            SDL.Scancode.Kp1 => KeyboardKey.Keypad1,
            SDL.Scancode.Kp2 => KeyboardKey.Keypad2,
            SDL.Scancode.Kp3 => KeyboardKey.Keypad3,
            SDL.Scancode.Kp4 => KeyboardKey.Keypad4,
            SDL.Scancode.Kp5 => KeyboardKey.Keypad5,
            SDL.Scancode.Kp6 => KeyboardKey.Keypad6,
            SDL.Scancode.Kp7 => KeyboardKey.Keypad7,
            SDL.Scancode.Kp8 => KeyboardKey.Keypad8,
            SDL.Scancode.Kp9 => KeyboardKey.Keypad9,
            SDL.Scancode.Kp0 => KeyboardKey.Keypad0,
            SDL.Scancode.KpPeriod => KeyboardKey.KeypadDecimal,
            
            SDL.Scancode.NonUsBackSlash => KeyboardKey.NonUsBackSlash,
            SDL.Scancode.KpEquals => KeyboardKey.KeypadPlus,
            
            SDL.Scancode.F13 => KeyboardKey.F13,
            SDL.Scancode.F14 => KeyboardKey.F14,
            SDL.Scancode.F15 => KeyboardKey.F15,
            SDL.Scancode.F16 => KeyboardKey.F16,
            SDL.Scancode.F17 => KeyboardKey.F17,
            SDL.Scancode.F18 => KeyboardKey.F18,
            SDL.Scancode.F19 => KeyboardKey.F19,
            SDL.Scancode.F20 => KeyboardKey.F20,
            SDL.Scancode.F21 => KeyboardKey.F21,
            SDL.Scancode.F22 => KeyboardKey.F22,
            SDL.Scancode.F23 => KeyboardKey.F23,
            SDL.Scancode.F24 => KeyboardKey.F24,
            
            SDL.Scancode.Menu => KeyboardKey.Menu,
            
            SDL.Scancode.LCtrl => KeyboardKey.ControlLeft,
            SDL.Scancode.LShift => KeyboardKey.ShiftLeft,
            SDL.Scancode.LAlt => KeyboardKey.AltLeft,
            SDL.Scancode.RCtrl => KeyboardKey.ControlRight,
            SDL.Scancode.RShift => KeyboardKey.ShiftRight,
            SDL.Scancode.RAlt => KeyboardKey.AltRight,
            SDL.Scancode.LGUI => KeyboardKey.WinLeft,
            SDL.Scancode.RGUI => KeyboardKey.WinRight,
            
            _ => KeyboardKey.Unknown
        };
    }

    /// <summary>
    /// Maps an SDL mouse button to a <see cref="MouseButton"/>.
    /// </summary>
    /// <param name="button">The SDL button to map.</param>
    /// <returns>The corresponding <see cref="MouseButton"/>.</returns>
    /// <exception cref="Exception">Thrown when the SDL button is not supported.</exception>
    private MouseButton MapMouseButton(SDL.MouseButtonFlags button) {
        return button switch {
            SDL.MouseButtonFlags.Left => MouseButton.Left,
            SDL.MouseButtonFlags.Middle => MouseButton.Middle,
            SDL.MouseButtonFlags.Right => MouseButton.Right,
            SDL.MouseButtonFlags.X1 => MouseButton.X1,
            SDL.MouseButtonFlags.X2 => MouseButton.X2,
            _ => throw new Exception("This type of mouse button is not supported!")
        };
    }

    /// <summary>
    /// Maps an SDL gamepad axis to the corresponding <see cref="GamepadAxis"/> value.
    /// </summary>
    /// <param name="gamepadAxis">The SDL gamepad axis to be mapped.</param>
    /// <returns>The corresponding <see cref="GamepadAxis"/> value, or <see cref="GamepadAxis.Invalid"/> if no match is found.</returns>
    private GamepadAxis MapGamepadAxis(SDL.GamepadAxis gamepadAxis) {
        return gamepadAxis switch {
            SDL.GamepadAxis.LeftX => GamepadAxis.LeftX,
            SDL.GamepadAxis.LeftY => GamepadAxis.LeftY,
            SDL.GamepadAxis.RightX => GamepadAxis.RightX,
            SDL.GamepadAxis.RightY => GamepadAxis.RightY,
            SDL.GamepadAxis.LeftTrigger => GamepadAxis.TriggerLeft,
            SDL.GamepadAxis.RightTrigger => GamepadAxis.TriggerRight,
            SDL.GamepadAxis.Count => GamepadAxis.TriggerRight,
            _ => GamepadAxis.Invalid
        };
    }

    /// <summary>
    /// Maps the specified SDL_GamepadButton to a corresponding GamepadButton value.
    /// </summary>
    /// <param name="gamepadButton">The SDL_GamepadButton value to be mapped.</param>
    /// <returns>The corresponding GamepadButton value.</returns>
    private GamepadButton MapGamepadButton(SDL.GamepadButton gamepadButton) {
        return gamepadButton switch {
            SDL.GamepadButton.Invalid => GamepadButton.Invalid,
            SDL.GamepadButton.South => GamepadButton.South,
            SDL.GamepadButton.East => GamepadButton.East,
            SDL.GamepadButton.West => GamepadButton.West,
            SDL.GamepadButton.North => GamepadButton.North,
            SDL.GamepadButton.Back => GamepadButton.Back,
            SDL.GamepadButton.Guide => GamepadButton.Guide,
            SDL.GamepadButton.Start => GamepadButton.Start,
            SDL.GamepadButton.LeftStick => GamepadButton.LeftStick,
            SDL.GamepadButton.RightStick => GamepadButton.RightStick,
            SDL.GamepadButton.LeftShoulder => GamepadButton.LeftShoulder,
            SDL.GamepadButton.RightShoulder => GamepadButton.RightShoulder,
            SDL.GamepadButton.DPadUp => GamepadButton.DpadUp,
            SDL.GamepadButton.DPadDown => GamepadButton.DpadDown,
            SDL.GamepadButton.DPadLeft => GamepadButton.DpadLeft,
            SDL.GamepadButton.DPadRight => GamepadButton.DpadRight,
            SDL.GamepadButton.Misc1 => GamepadButton.Misc1,
            SDL.GamepadButton.RightPaddle1 => GamepadButton.RightPaddle1,
            SDL.GamepadButton.LeftPaddle1 => GamepadButton.LeftPaddle1,
            SDL.GamepadButton.RightPaddle2 => GamepadButton.RightPaddle2,
            SDL.GamepadButton.LeftPaddle2 => GamepadButton.LeftPaddle2,
            SDL.GamepadButton.Touchpad => GamepadButton.Touchpad,
            SDL.GamepadButton.Misc2 => GamepadButton.Misc2,
            SDL.GamepadButton.Misc3 => GamepadButton.Misc3,
            SDL.GamepadButton.Misc4 => GamepadButton.Misc4,
            SDL.GamepadButton.Misc5 => GamepadButton.Misc5,
            SDL.GamepadButton.Misc6 => GamepadButton.Misc6,
            SDL.GamepadButton.Count => GamepadButton.Count,
            _ => GamepadButton.Invalid
        };
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            SDL.QuitSubSystem(InitFlags);
            SDL.DestroyWindow(this.Handle);
        }
    }
}