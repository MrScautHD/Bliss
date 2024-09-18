using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Bliss.CSharp.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Veldrid.OpenGL;
using Veldrid.Sdl2;
using Point = Veldrid.Point;
using Rectangle = Veldrid.Rectangle;

namespace Bliss.CSharp.Windowing;

public class Window {
    
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
    /// Occurs when the window is resized.
    /// </summary>
    public event Action Resized;

    /// <summary>
    /// Occurs when the window is about to close.
    /// </summary>
    public event Action Closing;

    /// <summary>
    /// Occurs after the window has closed.
    /// </summary>
    public event Action Closed;

    /// <summary>
    /// Occurs when the window gains focus.
    /// </summary>
    public event Action FocusGained;

    /// <summary>
    /// Occurs when the window loses focus.
    /// </summary>
    public event Action FocusLost;

    /// <summary>
    /// Occurs when the window is shown.
    /// </summary>
    public event Action Shown;

    /// <summary>
    /// Occurs when the window is hidden.
    /// </summary>
    public event Action Hidden;

    /// <summary>
    /// Occurs when the window is exposed (made visible or unhidden).
    /// </summary>
    public event Action Exposed;

    /// <summary>
    /// Occurs when the window is moved.
    /// </summary>
    public event Action<Point> Moved;

    /// <summary>
    /// Occurs when the mouse enters the window.
    /// </summary>
    public event Action MouseEntered;

    /// <summary>
    /// Occurs when the mouse leaves the window.
    /// </summary>
    public event Action MouseLeft;

    /// <summary>
    /// Occurs when the mouse wheel is scrolled.
    /// </summary>
    public event Action<MouseWheelEventArgs> MouseWheel;

    /// <summary>
    /// Occurs when the mouse is moved.
    /// </summary>
    public event Action<MouseMoveEventArgs> MouseMove;

    /// <summary>
    /// Occurs when a mouse button is pressed.
    /// </summary>
    public event Action<MouseEvent> MouseDown;

    /// <summary>
    /// Occurs when a mouse button is released.
    /// </summary>
    public event Action<MouseEvent> MouseUp;

    /// <summary>
    /// Occurs when a key is pressed.
    /// </summary>
    public event Action<KeyEvent> KeyDown;

    /// <summary>
    /// Occurs when a key is released.
    /// </summary>
    public event Action<KeyEvent> KeyUp;

    /// <summary>
    /// Occurs when a drag-and-drop operation is performed.
    /// </summary>
    public event Action<DragDropEvent> DragDrop;

    /// <summary>
    /// Stores the maximum supported OpenGL version as a tuple of major and minor version numbers.
    /// </summary>
    private (int, int)? _maxSupportedGlVersion;

    /// <summary>
    /// Stores the maximum supported OpenGL ES (GLES) version.
    /// The value is a tuple where the first item represents the major version, and the second item represents the minor version.
    /// </summary>
    private (int, int)? _maxSupportedGlEsVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="Window"/> class with the specified properties and creates a graphics device.
    /// </summary>
    /// <param name="width">The width of the window in pixels.</param>
    /// <param name="height">The height of the window in pixels.</param>
    /// <param name="title">The title of the window.</param>
    /// <param name="state">The state of the window.</param>
    /// <param name="options">Options for creating the graphics device.</param>
    /// <param name="preferredBackend">The graphics backend to use.</param>
    /// <param name="graphicsDevice">Outputs the created graphics device.</param>
    public Window(int width, int height, string title, WindowState state, GraphicsDeviceOptions options, GraphicsBackend preferredBackend, out GraphicsDevice graphicsDevice) {
        Sdl2Native.SDL_Init(SDLInitFlags.Video | SDLInitFlags.GameController | SDLInitFlags.Joystick);
        
        if (preferredBackend == GraphicsBackend.OpenGL || preferredBackend == GraphicsBackend.OpenGLES) {
            this.SetSdlGlContextAttributes(options, preferredBackend);
        }

        SDL_WindowFlags flags = SDL_WindowFlags.OpenGL | SDL_WindowFlags.Resizable | this.GetWindowFlags(state);
        
        if (state != WindowState.Hidden) {
            flags |= SDL_WindowFlags.Shown;
        }
        
        this.Sdl2Window = new Sdl2Window(
            title,
            Sdl2Native.SDL_WINDOWPOS_CENTERED,
            Sdl2Native.SDL_WINDOWPOS_CENTERED,
            width,
            height,
            flags,
            false
        );

        graphicsDevice = this.CreateGraphicsDevice(this.Sdl2Window, options, preferredBackend);

        this.Sdl2Window.Resized += this.OnResize;
        this.Sdl2Window.Closing += this.OnClosing;
        this.Sdl2Window.Closed += this.OnClosed;
        this.Sdl2Window.FocusGained += this.OnFocusGained;
        this.Sdl2Window.FocusLost += this.OnFocusLost;
        this.Sdl2Window.Shown += this.OnShowing;
        this.Sdl2Window.Hidden += this.OnHiding;
        this.Sdl2Window.MouseEntered += this.OnMouseEntered;
        this.Sdl2Window.MouseLeft += this.OnMouseLeft;
        this.Sdl2Window.Exposed += this.OnExposing;
        this.Sdl2Window.Moved += this.OnMoving;
        this.Sdl2Window.MouseWheel += this.OnMouseWheel;
        this.Sdl2Window.MouseMove += this.OnMouseMoving;
        this.Sdl2Window.MouseDown += this.OnMouseDown;
        this.Sdl2Window.MouseUp += this.OnMouseUp;
        this.Sdl2Window.KeyDown += this.OnKeyDown;
        this.Sdl2Window.KeyUp += this.OnKeyUp;
        this.Sdl2Window.DragDrop += this.OnDragDrop;
    }

    /// <summary>
    /// Determines the default graphics backend for the current platform.
    /// </summary>
    /// <returns>The default <see cref="GraphicsBackend"/> for the current platform.</returns>
    public static GraphicsBackend GetPlatformDefaultBackend() {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            return GraphicsBackend.Direct3D11;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            return GraphicsDevice.IsBackendSupported(GraphicsBackend.Metal) ? GraphicsBackend.Metal : GraphicsBackend.OpenGL;
        }
        else {
            return GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan) ? GraphicsBackend.Vulkan : GraphicsBackend.OpenGL;
        }
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
    /// Sets the icon for the window using the specified image.
    /// </summary>
    /// <param name="image">The image to use as the icon, represented as an <see cref="Image{Rgba32}"/>.</param>
    public void SetIcon(Image<Rgba32> image) {
        Sdl2Helper.SetWindowIcon(this.Sdl2Window, image);
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
    /// Retrieves the SDL window flags based on the specified window state.
    /// </summary>
    /// <param name="state">The state of the window for which to retrieve the flags.</param>
    /// <return>The SDL window flags corresponding to the given window state.</return>
    private SDL_WindowFlags GetWindowFlags(WindowState state) {
        switch (state) {
            case WindowState.Normal:
                return 0;
            case WindowState.FullScreen:
                return SDL_WindowFlags.Fullscreen;
            case WindowState.BorderlessFullScreen:
                return SDL_WindowFlags.FullScreenDesktop;
            case WindowState.Maximized:
                return SDL_WindowFlags.Maximized;
            case WindowState.Minimized:
                return SDL_WindowFlags.Minimized;
            case WindowState.Hidden:
                return SDL_WindowFlags.Hidden;
            default:
                throw new Exception($"Invalid WindowState: [{state}]");
        }
    }

    /// <summary>
    /// Creates a graphics device using the specified window, options, and preferred graphics backend.
    /// </summary>
    /// <param name="window">The SDL2 window used to create the graphics device.</param>
    /// <param name="options">Options for creating the graphics device.</param>
    /// <param name="preferredBackend">The preferred graphics backend to use.</param>
    /// <returns>The created <see cref="GraphicsDevice"/>.</returns>
    private GraphicsDevice CreateGraphicsDevice(Sdl2Window window, GraphicsDeviceOptions options, GraphicsBackend preferredBackend) {
        switch (preferredBackend) {
            case GraphicsBackend.Direct3D11:
#if !EXCLUDE_D3D11_BACKEND
                return CreateD3D11GraphicsDevice(window, options);
#else
                throw new VeldridException("Direct3D11 support has not been included in this configuration of Veldrid");
#endif
            case GraphicsBackend.Vulkan:
#if !EXCLUDE_VULKAN_BACKEND
                return CreateVulkanGraphicsDevice(window, options);
#else
                throw new VeldridException("Vulkan support has not been included in this configuration of Veldrid");
#endif
            case GraphicsBackend.Metal:
#if !EXCLUDE_METAL_BACKEND
                return CreateMetalGraphicsDevice(window, options);
#else
                throw new VeldridException("Metal support has not been included in this configuration of Veldrid");
#endif
            case GraphicsBackend.OpenGL:
#if !EXCLUDE_OPENGL_BACKEND
                return CreateOpenGlGraphicsDevice(window, options, preferredBackend);
#else
                throw new VeldridException("OpenGL support has not been included in this configuration of Veldrid");
#endif
            case GraphicsBackend.OpenGLES:
#if !EXCLUDE_OPENGL_BACKEND
                return CreateOpenGlGraphicsDevice(window, options, preferredBackend);
#else
                throw new VeldridException("OpenGL ES support has not been included in this configuration of Veldrid");
#endif
            default:
                throw new VeldridException($"Invalid GraphicsBackend: [{preferredBackend}]");
        }
    }

    /// <summary>
    /// Creates a <see cref="SwapchainSource"/> for the specified <see cref="Sdl2Window"/>
    /// based on the underlying window system.
    /// </summary>
    /// <param name="window">The SDL2 window for which to create the swapchain source.</param>
    /// <returns>A <see cref="SwapchainSource"/> appropriate for the window's platform.</returns>
    private unsafe SwapchainSource GetSwapchainSource(Sdl2Window window) {
        SDL_SysWMinfo sysWmInfo;
        nint sdlHandle = window.SdlWindowHandle;
        
        Sdl2Native.SDL_GetVersion(&sysWmInfo.version);
        Sdl2Native.SDL_GetWMWindowInfo(sdlHandle, &sysWmInfo);
        
        switch (sysWmInfo.subsystem) {
            case SysWMType.Windows:
                Win32WindowInfo w32Info = Unsafe.Read<Win32WindowInfo>(&sysWmInfo.info);
                return SwapchainSource.CreateWin32(w32Info.Sdl2Window, w32Info.hinstance);
            case SysWMType.X11:
                X11WindowInfo x11Info = Unsafe.Read<X11WindowInfo>(&sysWmInfo.info);
                return SwapchainSource.CreateXlib(x11Info.display, x11Info.Sdl2Window);
            case SysWMType.Wayland:
                WaylandWindowInfo wlInfo = Unsafe.Read<WaylandWindowInfo>(&sysWmInfo.info);
                return SwapchainSource.CreateWayland(wlInfo.display, wlInfo.surface);
            case SysWMType.Cocoa:
                CocoaWindowInfo cocoaInfo = Unsafe.Read<CocoaWindowInfo>(&sysWmInfo.info);
                return SwapchainSource.CreateNSWindow(cocoaInfo.Window);
            default:
                throw new PlatformNotSupportedException($"Cannot create a SwapchainSource for: [{sysWmInfo.subsystem}]!");
        }
    }

    /// <summary>
    /// Creates a Direct3D11 graphics device for a specified SDL2 window with the provided graphics device options.
    /// </summary>
    /// <param name="window">The SDL2 window for which to create the graphics device.</param>
    /// <param name="options">The options to use for creating the graphics device.</param>
    /// <returns>The created Direct3D11 graphics device.</returns>
    private GraphicsDevice CreateD3D11GraphicsDevice(Sdl2Window window, GraphicsDeviceOptions options) {
        SwapchainDescription description = new SwapchainDescription() {
            Source = this.GetSwapchainSource(window),
            Width = (uint) window.Width,
            Height = (uint) window.Height,
            DepthFormat = options.SwapchainDepthFormat,
            SyncToVerticalBlank = options.SyncToVerticalBlank,
            ColorSrgb = options.SwapchainSrgbFormat
        };
        
        return GraphicsDevice.CreateD3D11(options, description);
    }

    /// <summary>
    /// Creates a Vulkan graphics device for a specified SDL2 window with the provided graphics device options.
    /// </summary>
    /// <param name="window">The SDL2 window for which to create the graphics device.</param>
    /// <param name="options">The options to use for creating the graphics device.</param>
    /// <returns>The created Vulkan graphics device.</returns>
    private GraphicsDevice CreateVulkanGraphicsDevice(Sdl2Window window, GraphicsDeviceOptions options) {
        SwapchainDescription description = new SwapchainDescription() {
            Source = this.GetSwapchainSource(window),
            Width = (uint) window.Width,
            Height = (uint) window.Height,
            DepthFormat = options.SwapchainDepthFormat,
            SyncToVerticalBlank = options.SyncToVerticalBlank,
            ColorSrgb = options.SwapchainSrgbFormat
        };

        return GraphicsDevice.CreateVulkan(options, description);
    }
    
    /// <summary>
    /// Creates a Metal graphics device for a specified SDL2 window with the provided graphics device options.
    /// </summary>
    /// <param name="window">The SDL2 window for which to create the graphics device.</param>
    /// <param name="options">The options to use for creating the graphics device.</param>
    /// <returns>The created Metal graphics device.</returns>
    private GraphicsDevice CreateMetalGraphicsDevice(Sdl2Window window, GraphicsDeviceOptions options) {
        SwapchainDescription description = new SwapchainDescription() {
            Source = this.GetSwapchainSource(window),
            Width = (uint) window.Width,
            Height = (uint) window.Height,
            DepthFormat = options.SwapchainDepthFormat,
            SyncToVerticalBlank = options.SyncToVerticalBlank,
            ColorSrgb = options.SwapchainSrgbFormat
        };

        return GraphicsDevice.CreateMetal(options, description);
    }
    
    /// <summary>
    /// Creates an OpenGL graphics device for the provided SDL2 window with the specified options and backend.
    /// </summary>
    /// <param name="window">The SDL2 window for which to create the graphics device.</param>
    /// <param name="options">Options for creating the graphics device.</param>
    /// <param name="backend">The graphics backend to use.</param>
    /// <returns>The created OpenGL graphics device.</returns>
    private unsafe GraphicsDevice CreateOpenGlGraphicsDevice(Sdl2Window window, GraphicsDeviceOptions options, GraphicsBackend backend) {
        Sdl2Native.SDL_ClearError();
        nint sdlHandle = window.SdlWindowHandle;

        SDL_SysWMinfo sysWmInfo;
        Sdl2Native.SDL_GetVersion(&sysWmInfo.version);
        Sdl2Native.SDL_GetWMWindowInfo(sdlHandle, &sysWmInfo);

        this.SetSdlGlContextAttributes(options, backend);

        nint contextHandle = Sdl2Native.SDL_GL_CreateContext(sdlHandle);
        string error = Sdl2Helper.GetErrorMessage();
        
        if (error != string.Empty) {
            throw new VeldridException($"Unable to create OpenGL Context: \"{error}\". This may indicate that the system does not support the requested OpenGL profile, version, or Swapchain format.");
        }

        int actualDepthSize;
        int actualStencilSize;
        
        Sdl2Native.SDL_GL_GetAttribute(SDL_GLAttribute.DepthSize, &actualDepthSize);
        Sdl2Native.SDL_GL_GetAttribute(SDL_GLAttribute.StencilSize, &actualStencilSize);
        Sdl2Native.SDL_GL_SetSwapInterval(options.SyncToVerticalBlank ? 1 : 0);

        OpenGLPlatformInfo platformInfo = new OpenGLPlatformInfo(
            contextHandle,
            Sdl2Native.SDL_GL_GetProcAddress,
            context => Sdl2Native.SDL_GL_MakeCurrent(sdlHandle, context),
            Sdl2Native.SDL_GL_GetCurrentContext,
            () => Sdl2Native.SDL_GL_MakeCurrent(new SDL_Window(nint.Zero), nint.Zero),
            Sdl2Native.SDL_GL_DeleteContext,
            () => Sdl2Native.SDL_GL_SwapWindow(sdlHandle),
            sync => Sdl2Native.SDL_GL_SetSwapInterval(sync ? 1 : 0)
        );

        return GraphicsDevice.CreateOpenGL(options, platformInfo, (uint) window.Width, (uint) window.Height);
    }

    /// <summary>
    /// Configures the attributes for the SDL GL context based on the specified graphics device options and backend.
    /// </summary>
    /// <param name="options">The graphics device options including settings for debugging and vertical sync.</param>
    /// <param name="backend">The graphics backend, which must be either OpenGL or OpenGLES.</param>
    private void SetSdlGlContextAttributes(GraphicsDeviceOptions options, GraphicsBackend backend) {
       if (backend != GraphicsBackend.OpenGL && backend != GraphicsBackend.OpenGLES) {
           throw new Exception($"GraphicsBackend must be: [{nameof(GraphicsBackend.OpenGL)}] or [{nameof(GraphicsBackend.OpenGLES)}]!");
       }

       SDL_GLContextFlag contextFlags = options.Debug ? (SDL_GLContextFlag.Debug | SDL_GLContextFlag.ForwardCompatible) : SDL_GLContextFlag.ForwardCompatible;
       Sdl2Native.SDL_GL_SetAttribute(SDL_GLAttribute.ContextFlags, (int) contextFlags);

       (int major, int minor) = this.GetMaxGlVersion(backend == GraphicsBackend.OpenGLES);

       if (backend == GraphicsBackend.OpenGL) {
           Sdl2Native.SDL_GL_SetAttribute(SDL_GLAttribute.ContextProfileMask, (int) SDL_GLProfile.Core);
           Sdl2Native.SDL_GL_SetAttribute(SDL_GLAttribute.ContextMajorVersion, major);
           Sdl2Native.SDL_GL_SetAttribute(SDL_GLAttribute.ContextMinorVersion, minor);
       }
       else {
           Sdl2Native.SDL_GL_SetAttribute(SDL_GLAttribute.ContextProfileMask, (int) SDL_GLProfile.ES);
           Sdl2Native.SDL_GL_SetAttribute(SDL_GLAttribute.ContextMajorVersion, major);
           Sdl2Native.SDL_GL_SetAttribute(SDL_GLAttribute.ContextMinorVersion, minor);
       }

       int depthBits = 0;
       int stencilBits = 0;
       
       if (options.SwapchainDepthFormat.HasValue) {
           switch (options.SwapchainDepthFormat) {
               case PixelFormat.R16UNorm:
                   depthBits = 16;
                   break;
               case PixelFormat.D24UNormS8UInt:
                   depthBits = 24;
                   stencilBits = 8;
                   break;
               case PixelFormat.R32Float:
                   depthBits = 32;
                   break;
               case PixelFormat.D32FloatS8UInt:
                   depthBits = 32;
                   stencilBits = 8;
                   break;
               default:
                   throw new VeldridException($"Invalid depth format: [{options.SwapchainDepthFormat.Value}]!");
           }
       }

       Sdl2Native.SDL_GL_SetAttribute(SDL_GLAttribute.DepthSize, depthBits);
       Sdl2Native.SDL_GL_SetAttribute(SDL_GLAttribute.StencilSize, stencilBits);
       Sdl2Native.SDL_GL_SetAttribute(SDL_GLAttribute.FramebufferSrgbCapable, options.SwapchainSrgbFormat ? 1 : 0);
    }

    /// <summary>
    /// Retrieves the maximum OpenGL or OpenGL ES version supported by the system.
    /// </summary>
    /// <param name="openGlEs">Specifies whether to query for OpenGL ES (true) or OpenGL (false) version.</param>
    /// <return>Returns a tuple containing the major and minor version numbers of the supported OpenGL or OpenGL ES version.</return>
    private (int, int) GetMaxGlVersion(bool openGlEs) {
        object glVersionLock = new object();
        
        lock (glVersionLock) {
            (int, int)? maxVersion = openGlEs ? this._maxSupportedGlEsVersion : this._maxSupportedGlVersion;

            if (maxVersion == null) {
                maxVersion = this.TestMaxGlVersion(openGlEs);

                if (openGlEs) {
                    this._maxSupportedGlEsVersion = maxVersion;
                }
                else {
                    this._maxSupportedGlVersion = maxVersion;
                }
            }

            return maxVersion.Value;
        }
    }

    /// <summary>
    /// Tests the maximum supported OpenGL version for OpenGL or OpenGL ES.
    /// </summary>
    /// <param name="openGlEs">Indicates whether to test for OpenGL ES versions. If false, tests for standard OpenGL versions.</param>
    /// <returns>
    /// A tuple containing two integers: the major and minor versions of the maximum supported OpenGL (or OpenGL ES) version.
    /// If no supported version is found, returns (0, 0).
    /// </returns>
    private (int, int) TestMaxGlVersion(bool openGlEs) {
        (int, int)[] testVersions = openGlEs 
            ? [
                (3, 2),
                (3, 0)
            ]
            : [
                (4, 6),
                (4, 3),
                (4, 0),
                (3, 3),
                (3, 0)
            ];

        foreach ((int major, int minor) in testVersions) {
            if (this.TestIndividualGlVersion(openGlEs, major, minor)) {
                return (major, minor);
            }
        }

        return (0, 0);
    }

    /// <summary>
    /// Tests the creation of an OpenGL or OpenGL ES context with the specified major and minor version numbers.
    /// </summary>
    /// <param name="openGlEs">Specifies whether to create an OpenGL ES context. Otherwise, creates an OpenGL context.</param>
    /// <param name="major">The major version number of the context to create.</param>
    /// <param name="minor">The minor version number of the context to create.</param>
    /// <return>True if the context was successfully created; otherwise, false.</return>
    private unsafe bool TestIndividualGlVersion(bool openGlEs, int major, int minor) {
        SDL_GLProfile profileMask = openGlEs ? SDL_GLProfile.ES : SDL_GLProfile.Core;

        Sdl2Native.SDL_GL_SetAttribute(SDL_GLAttribute.ContextProfileMask, (int) profileMask);
        Sdl2Native.SDL_GL_SetAttribute(SDL_GLAttribute.ContextMajorVersion, major);
        Sdl2Native.SDL_GL_SetAttribute(SDL_GLAttribute.ContextMinorVersion, minor);

        SDL_Window window = Sdl2Native.SDL_CreateWindow(string.Empty, 0, 0, 1, 1, SDL_WindowFlags.Hidden | SDL_WindowFlags.OpenGL);
        string windowError = Sdl2Helper.GetErrorMessage();
        
        if (window.NativePointer == nint.Zero || windowError != string.Empty) {
            Sdl2Native.SDL_ClearError();
            Logger.Debug($"Unable to create version {major}.{minor} {profileMask} context.");
            return false;
        }

        nint context = Sdl2Native.SDL_GL_CreateContext(window);
        string contextError = Sdl2Helper.GetErrorMessage();

        if (contextError != string.Empty) {
            Sdl2Native.SDL_ClearError();
            Logger.Debug($"Unable to create version {major}.{minor} {profileMask} context.");
            Sdl2Native.SDL_DestroyWindow(window);
            return false;
        }

        Sdl2Native.SDL_GL_DeleteContext(context);
        Sdl2Native.SDL_DestroyWindow(window);
        return true;
    }

    /// <summary>
    /// Invokes the <see cref="Resized"/> event when the window is resized.
    /// </summary>
    private void OnResize() {
        this.Resized?.Invoke();
    }
    
    /// <summary>
    /// Invokes the <see cref="Closing"/> event when the window is about to close.
    /// </summary>
    private void OnClosing() {
        this.Closing?.Invoke();
    }
    
    /// <summary>
    /// Invokes the <see cref="Closed"/> event after the window has closed.
    /// </summary>
    private void OnClosed() {
        this.Closed?.Invoke();
    }
    
    /// <summary>
    /// Invokes the <see cref="FocusGained"/> event when the window gains focus.
    /// </summary>
    private void OnFocusGained() {
        this.FocusGained?.Invoke();
    }
    
    /// <summary>
    /// Invokes the <see cref="FocusLost"/> event when the window loses focus.
    /// </summary>
    private void OnFocusLost() {
        this.FocusLost?.Invoke();
    }
    
    /// <summary>
    /// Invokes the <see cref="Shown"/> event when the window is shown.
    /// </summary>
    private void OnShowing() {
        this.Shown?.Invoke();
    }
    
    /// <summary>
    /// Invokes the <see cref="Hidden"/> event when the window is hidden.
    /// </summary>
    private void OnHiding() {
        this.Hidden?.Invoke();
    }
    
    /// <summary>
    /// Invokes the <see cref="Exposed"/> event when the window is exposed.
    /// </summary>
    private void OnExposing() {
        this.Exposed?.Invoke();
    }
    
    /// <summary>
    /// Invokes the <see cref="Moved"/> event when the window is moved, passing the new position.
    /// </summary>
    /// <param name="point">The new position of the window.</param>
    private void OnMoving(Point point) {
        this.Moved?.Invoke(point);
    }
    
    /// <summary>
    /// Invokes the <see cref="MouseEntered"/> event when the mouse enters the window.
    /// </summary>
    private void OnMouseEntered() {
        this.MouseEntered?.Invoke();
    }
    
    /// <summary>
    /// Invokes the <see cref="MouseLeft"/> event when the mouse leaves the window.
    /// </summary>
    private void OnMouseLeft() {
        this.MouseLeft?.Invoke();
    }
    
    /// <summary>
    /// Invokes the <see cref="MouseWheel"/> event when the mouse wheel is scrolled.
    /// </summary>
    /// <param name="args">The mouse wheel event arguments.</param>
    private void OnMouseWheel(MouseWheelEventArgs args) {
        this.MouseWheel?.Invoke(args);
    }
    
    /// <summary>
    /// Invokes the <see cref="MouseMove"/> event when the mouse is moved.
    /// </summary>
    /// <param name="args">The mouse move event arguments.</param>
    private void OnMouseMoving(MouseMoveEventArgs args) {
        this.MouseMove?.Invoke(args);
    }
    
    /// <summary>
    /// Invokes the <see cref="MouseDown"/> event when a mouse button is pressed.
    /// </summary>
    /// <param name="mouseEvent">The mouse event arguments.</param>
    private void OnMouseDown(MouseEvent mouseEvent) {
        this.MouseDown?.Invoke(mouseEvent);
    }
    
    /// <summary>
    /// Invokes the <see cref="MouseUp"/> event when a mouse button is released.
    /// </summary>
    /// <param name="mouseEvent">The mouse event arguments.</param>
    private void OnMouseUp(MouseEvent mouseEvent) {
        this.MouseUp?.Invoke(mouseEvent);
    }
    
    /// <summary>
    /// Invokes the <see cref="KeyDown"/> event when a key is pressed.
    /// </summary>
    /// <param name="keyEvent">The key event arguments.</param>
    private void OnKeyDown(KeyEvent keyEvent) {
        this.KeyDown?.Invoke(keyEvent);
    }
    
    /// <summary>
    /// Invokes the <see cref="KeyUp"/> event when a key is released.
    /// </summary>
    /// <param name="keyEvent">The key event arguments.</param>
    private void OnKeyUp(KeyEvent keyEvent) {
        this.KeyUp?.Invoke(keyEvent);
    }
    
    /// <summary>
    /// Invokes the <see cref="DragDrop"/> event when a drag-and-drop operation is performed.
    /// </summary>
    /// <param name="dropEvent">The drag-and-drop event arguments.</param>
    private void OnDragDrop(DragDropEvent dropEvent) {
        this.DragDrop?.Invoke(dropEvent);
    }
    
    /// <summary>
    /// Closes the window.
    /// </summary>
    public void Close() {
        this.Sdl2Window.Resized -= this.OnResize;
        this.Sdl2Window.Closing -= this.OnClosing;
        this.Sdl2Window.Closed -= this.OnClosed;
        this.Sdl2Window.FocusGained -= this.OnFocusGained;
        this.Sdl2Window.FocusLost -= this.OnFocusLost;
        this.Sdl2Window.Shown -= this.OnShowing;
        this.Sdl2Window.Hidden -= this.OnHiding;
        this.Sdl2Window.MouseEntered -= this.OnMouseEntered;
        this.Sdl2Window.MouseLeft -= this.OnMouseLeft;
        this.Sdl2Window.Exposed -= this.OnExposing;
        this.Sdl2Window.Moved -= this.OnMoving;
        this.Sdl2Window.MouseWheel -= this.OnMouseWheel;
        this.Sdl2Window.MouseMove -= this.OnMouseMoving;
        this.Sdl2Window.MouseDown -= this.OnMouseDown;
        this.Sdl2Window.MouseUp -= this.OnMouseUp;
        this.Sdl2Window.KeyDown -= this.OnKeyDown;
        this.Sdl2Window.KeyUp -= this.OnKeyUp;
        this.Sdl2Window.DragDrop -= this.OnDragDrop;
        
        this.Sdl2Window.Close();
    }
}