using System.Runtime.InteropServices;
using Bliss.CSharp.Logging;
using SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Veldrid.OpenGL;

namespace Bliss.CSharp.Windowing;

public class Sdl3Window : Disposable, IWindow {
    
    public nint Handle { get; private set; }
    public uint Id { get; private set; }
    
    public SwapchainSource SwapchainSource { get; }

    public bool Exists { get; }
    public bool IsFocused { get; }
    
    public WindowState State { get; set; }
    public bool Visible { get; set; }
    public float Opacity { get; set; }
    public bool Resizable { get; set; }
    public bool BorderVisible { get; set; }

    private OpenGLPlatformInfo? _openGlPlatformInfo;
    
    /// <summary>
    /// Stores the maximum supported OpenGL version as a tuple of major and minor version numbers.
    /// </summary>
    private (int, int)? _maxSupportedGlVersion;

    /// <summary>
    /// Stores the maximum supported OpenGL ES (GLES) version.
    /// The value is a tuple where the first item represents the major version, and the second item represents the minor version.
    /// </summary>
    private (int, int)? _maxSupportedGlEsVersion;
    
    public unsafe Sdl3Window(int width, int height, string title, SDL_WindowFlags flags) {
        this.Exists = true; // TODO: IS TEMP HERE!
        //SDL3.SDL_SetHint(SDL3.SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
        
        if (SDL3.SDL_InitSubSystem(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_GAMEPAD | SDL_InitFlags.SDL_INIT_JOYSTICK) == SDL_bool.SDL_FALSE) {
            throw new Exception($"Failed to initialise SDL! Error: {SDL3.SDL_GetError()}");
        }
        
        // ENABLE EVENTS.
        SDL3.SDL_SetGamepadEventsEnabled(SDL_bool.SDL_TRUE);
        
        this.Handle = (nint) SDL3.SDL_CreateWindow(title, width, height, flags);
        this.Id = (uint) SDL3.SDL_GetWindowID((SDL_Window*) this.Handle);
        // Do Window Events here like Sdl2WindowRegistry
        
        this.SwapchainSource = this.CreateSwapchainSource();
    }

    /// <summary>
    /// Retrieves a module handle for the specified module.
    /// </summary>
    /// <param name="lpModuleName">A pointer to a null-terminated string that specifies the name of the module. If this parameter is null, GetModuleHandleW returns a handle to the file used to create the calling process.</param>
    /// <returns>A handle to the specified module, or null if the module is not found.</returns>
    /// <exception cref="System.ComponentModel.Win32Exception">Thrown if an error occurs when retrieving the module handle.</exception>
    [DllImport("kernel32", ExactSpelling = true)]
    private static extern unsafe nint GetModuleHandleW(ushort* lpModuleName);
    
    public unsafe string GetTitle() {
        return SDL3.SDL_GetWindowTitle((SDL_Window*) this.Handle) ?? string.Empty;
    }
    
    public unsafe void SetTitle(string title) {
        if (SDL3.SDL_SetWindowTitle((SDL_Window*) this.Handle, title) == SDL_bool.SDL_FALSE) {
            Logger.Warn($"Failed to set the title of the window: [{this.Id}] Error: {SDL3.SDL_GetError()}");
        }
    }
    
    public unsafe (int, int) GetSize() {
        int width;
        int height;
        
        if (SDL3.SDL_GetWindowSizeInPixels((SDL_Window*) this.Handle, &width, &height) == SDL_bool.SDL_FALSE) {
            Logger.Warn($"Failed to get the size of the window: [{this.Id}] Error: {SDL3.SDL_GetError()}");
        }

        return (width, height);
    }

    public unsafe void SetSize(int width, int height) {
        if (SDL3.SDL_SetWindowSize((SDL_Window*) this.Handle, width, height) == SDL_bool.SDL_FALSE) {
            Logger.Warn($"Failed to set the size of the window: [{this.Id}] Error: {SDL3.SDL_GetError()}");
        }
    }
    
    public int GetWidth() {
        return this.GetSize().Item1;
    }
    
    public void SetWidth(int width) {
        this.SetSize(width, this.GetHeight());
    }
    
    public int GetHeight() {
        return this.GetSize().Item2;
    }
    
    public void SetHeight(int height) {
        this.SetSize(this.GetWidth(), height);
    }
    
    public unsafe void SetIcon(Image<Rgba32> image) {
        byte[] data = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(data);

        fixed (byte* dataPtr = data) {
            SDL_Surface* surface = SDL3.SDL_CreateSurfaceFrom(image.Width, image.Height, SDL_PixelFormat.SDL_PIXELFORMAT_RGB332, (nint) dataPtr, 1);

            if ((nint) surface == nint.Zero) {
                Logger.Error($"Failed to set Sdl3 window icon: {SDL3.SDL_GetError()}");
            }

            SDL3.SDL_SetWindowIcon((SDL_Window*) this.Handle, surface);
            SDL3.SDL_DestroySurface(surface);
        }
    }

    public void PumpEvents() {
        //throw new NotImplementedException();
    }

    public Point ClientToScreen(Point point) {
        //throw new NotImplementedException();
        return new Point();
    }

    public Point ScreenToClient(Point point) {
        //throw new NotImplementedException();
        return new Point();
    }

    public unsafe OpenGLPlatformInfo GetOrCreateOpenGlPlatformInfo(GraphicsDeviceOptions options, GraphicsBackend backend) {
        if (this._openGlPlatformInfo == null) {
            SDL3.SDL_ClearError();

            this.SetSdlGlContextAttributes(options, backend);

            SDL_GLContextState* contextHandle = SDL3.SDL_GL_CreateContext((SDL_Window*) this.Handle);
            string error = SDL3.SDL_GetError() ?? string.Empty;
        
            if (error != string.Empty) {
                throw new VeldridException($"Unable to create OpenGL Context: \"{error}\". This may indicate that the system does not support the requested OpenGL profile, version, or Swapchain format.");
            }

            int actualDepthSize;
            int actualStencilSize;

            SDL3.SDL_GL_GetAttribute(SDL_GLattr.SDL_GL_DEPTH_SIZE, &actualDepthSize);
            SDL3.SDL_GL_GetAttribute(SDL_GLattr.SDL_GL_STENCIL_SIZE, &actualStencilSize);
            SDL3.SDL_GL_SetSwapInterval(options.SyncToVerticalBlank ? 1 : 0);

            OpenGLPlatformInfo platformInfo = new OpenGLPlatformInfo(
                (nint) contextHandle,
                proc => SDL3.SDL_GL_GetProcAddress(proc),
                context => SDL3.SDL_GL_MakeCurrent((SDL_Window*) this.Handle, (SDL_GLContextState*) context),
                () => (nint) SDL3.SDL_GL_GetCurrentContext(),
                () => SDL3.SDL_GL_MakeCurrent((SDL_Window*) this.Handle, (SDL_GLContextState*) nint.Zero),
                context => SDL3.SDL_GL_DestroyContext((SDL_GLContextState*) context),
                () => SDL3.SDL_GL_SwapWindow((SDL_Window*) this.Handle),
                sync => SDL3.SDL_GL_SetSwapInterval(sync ? 1 : 0)
            );

            this._openGlPlatformInfo = platformInfo;
            return platformInfo;
        }
        else {
            return this._openGlPlatformInfo;
        }
    }

    /// <summary>
    /// Configures the SDL GL context attributes based on the provided graphics device options and backend.
    /// </summary>
    /// <param name="options">The options that specify various settings for the graphics device.</param>
    /// <param name="backend">The graphics backend in use (OpenGL or OpenGLES).</param>
    /// <exception cref="System.Exception">Thrown if the graphics backend is not OpenGL or OpenGLES.</exception>
    private void SetSdlGlContextAttributes(GraphicsDeviceOptions options, GraphicsBackend backend) {
       if (backend != GraphicsBackend.OpenGL && backend != GraphicsBackend.OpenGLES) {
           throw new Exception($"GraphicsBackend must be: [{nameof(GraphicsBackend.OpenGL)}] or [{nameof(GraphicsBackend.OpenGLES)}]!");
       }

       SDL_GLcontextFlag contextFlags = options.Debug ? (SDL_GLcontextFlag.SDL_GL_CONTEXT_DEBUG_FLAG | SDL_GLcontextFlag.SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG) : SDL_GLcontextFlag.SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG;
       SDL3.SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_FLAGS, (int) contextFlags);

       (int major, int minor) = this.GetMaxGlVersion(backend == GraphicsBackend.OpenGLES);

       if (backend == GraphicsBackend.OpenGL) {
           SDL3.SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int) SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);
           SDL3.SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, major);
           SDL3.SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, minor);
       }
       else {
           SDL3.SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int) SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_ES);
           SDL3.SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, major);
           SDL3.SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, minor);
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

       SDL3.SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_DEPTH_SIZE, depthBits);
       SDL3.SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_STENCIL_SIZE, stencilBits);
       SDL3.SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_FRAMEBUFFER_SRGB_CAPABLE, options.SwapchainSrgbFormat ? 1 : 0);
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
        SDL_GLprofile profileMask = openGlEs ? SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_ES : SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE;

        SDL3.SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int) profileMask);
        SDL3.SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, major);
        SDL3.SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, minor);

        SDL_Window* window = SDL3.SDL_CreateWindow(string.Empty, 1, 1, SDL_WindowFlags.SDL_WINDOW_HIDDEN | SDL_WindowFlags.SDL_WINDOW_OPENGL);
        string windowError = SDL3.SDL_GetError() ?? string.Empty;
        
        if ((nint) window == nint.Zero || windowError != string.Empty) {
            SDL3.SDL_ClearError();
            Logger.Debug($"Unable to create version {major}.{minor} {profileMask} context.");
            return false;
        }

        SDL_GLContextState* context = SDL3.SDL_GL_CreateContext(window);
        string contextError = SDL3.SDL_GetError() ?? string.Empty;

        if (contextError != string.Empty) {
            SDL3.SDL_ClearError();
            Logger.Debug($"Unable to create version {major}.{minor} {profileMask} context.");
            SDL3.SDL_DestroyWindow(window);
            return false;
        }

        SDL3.SDL_GL_DestroyContext(context);
        SDL3.SDL_DestroyWindow(window);
        return true;
    }

    /// <summary>
    /// Creates a SwapchainSource for use with the current operating system.
    /// </summary>
    /// <returns>A SwapchainSource configured for the underlying windowing system.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown if the current operating system is not supported.</exception>
    private unsafe SwapchainSource CreateSwapchainSource() {
        if (OperatingSystem.IsWindows()) {
            nint hwnd = SDL3.SDL_GetPointerProperty(SDL3.SDL_GetWindowProperties((SDL_Window*) this.Handle), SDL3.SDL_PROP_WINDOW_WIN32_HWND_POINTER, nint.Zero);
            nint hInstance = GetModuleHandleW(null);
            return SwapchainSource.CreateWin32(hwnd, hInstance);
        }
        else if (OperatingSystem.IsLinux()) {
            string driver = SDL3.SDL_GetCurrentVideoDriver() ?? string.Empty;
            
            if (driver == "wayland") {
                nint display = SDL3.SDL_GetPointerProperty(SDL3.SDL_GetWindowProperties((SDL_Window*) this.Handle), SDL3.SDL_PROP_WINDOW_WAYLAND_DISPLAY_POINTER, nint.Zero);
                nint surface = SDL3.SDL_GetPointerProperty(SDL3.SDL_GetWindowProperties((SDL_Window*) this.Handle), SDL3.SDL_PROP_WINDOW_WAYLAND_SURFACE_POINTER, nint.Zero);
                return SwapchainSource.CreateWayland(display, surface);
            }
            else {
                nint display = SDL3.SDL_GetPointerProperty(SDL3.SDL_GetWindowProperties((SDL_Window*) this.Handle), SDL3.SDL_PROP_WINDOW_X11_DISPLAY_POINTER, nint.Zero);
                nint surface = new IntPtr(SDL3.SDL_GetPointerProperty(SDL3.SDL_GetWindowProperties((SDL_Window*) this.Handle), SDL3.SDL_PROP_WINDOW_X11_WINDOW_NUMBER, 0));
                return SwapchainSource.CreateXlib(display, surface);
            }
        }
        else if (OperatingSystem.IsMacOS()) {
            nint surface = SDL3.SDL_GetPointerProperty(SDL3.SDL_GetWindowProperties((SDL_Window*) this.Handle), SDL3.SDL_PROP_WINDOW_COCOA_WINDOW_POINTER, nint.Zero);
            return SwapchainSource.CreateNSWindow(surface);
        }
        
        throw new PlatformNotSupportedException("Filed to create a SwapchainSource!");
    }

    protected override unsafe void Dispose(bool disposing) {
        if (disposing) {
            SDL3.SDL_DestroyWindow((SDL_Window*) this.Handle);
        }
    }
}