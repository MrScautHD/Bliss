using Bliss.CSharp.Logging;
using SDL;
using Veldrid;
using Veldrid.OpenGL;

namespace Bliss.CSharp.Windowing;

public static class Window {
    
    /// <summary>
    /// Stores the maximum supported OpenGL version as a tuple of major and minor version numbers.
    /// </summary>
    private static (int, int)? _maxSupportedGlVersion;

    /// <summary>
    /// Stores the maximum supported OpenGL ES (GLES) version.
    /// The value is a tuple where the first item represents the major version, and the second item represents the minor version.
    /// </summary>
    private static (int, int)? _maxSupportedGlEsVersion;
    
    /// <summary>
    /// Creates a new window based on the specified parameters.
    /// </summary>
    /// <param name="type">The type of window to create.</param>
    /// <param name="width">The width of the window.</param>
    /// <param name="height">The height of the window.</param>
    /// <param name="title">The title of the window.</param>
    /// <param name="state">The state of the window (e.g., maximized, minimized).</param>
    /// <param name="options">Options for configuring the graphics device.</param>
    /// <param name="preferredBackend">The preferred graphics backend to use.</param>
    /// <param name="graphicsDevice">An output parameter that will hold the created graphics device.</param>
    /// <returns>An implementation of <see cref="IWindow"/> corresponding to the specified parameters.</returns>
    public static IWindow CreateWindow(WindowType type, int width, int height, string title, WindowState state, GraphicsDeviceOptions options, GraphicsBackend preferredBackend, out GraphicsDevice graphicsDevice) {
        switch (type) {
            case WindowType.Sdl3:
                Sdl3Window window = new Sdl3Window(width, height, title, GetSdl3WindowStates(state) | SDL_WindowFlags.SDL_WINDOW_OPENGL);

                if (preferredBackend == GraphicsBackend.OpenGL | preferredBackend == GraphicsBackend.OpenGLES) {
                    SetSdlGlContextAttributes(options, preferredBackend);
                }
                
                graphicsDevice = CreateGraphicsDevice(window, options, preferredBackend);
                return window;
            default:
                throw new Exception($"The window type: [{type}] is not supported!");
        }
    }

    /// <summary>
    /// Converts the specified <see cref="WindowState"/> to the corresponding SDL window flags.
    /// </summary>
    /// <param name="state">The state of the window (e.g., normal, fullscreen, maximized).</param>
    /// <returns>The SDL window flags that correspond to the specified window state.</returns>
    private static SDL_WindowFlags GetSdl3WindowStates(WindowState state) {
        switch (state) {
            case WindowState.Resizable:
                return SDL_WindowFlags.SDL_WINDOW_RESIZABLE;
            case WindowState.FullScreen:
                return SDL_WindowFlags.SDL_WINDOW_FULLSCREEN;
            case WindowState.BorderlessFullScreen:
                return SDL_WindowFlags.SDL_WINDOW_BORDERLESS;
            case WindowState.Maximized:
                return SDL_WindowFlags.SDL_WINDOW_MAXIMIZED;
            case WindowState.Minimized:
                return SDL_WindowFlags.SDL_WINDOW_MINIMIZED;
            case WindowState.Hidden:
                return SDL_WindowFlags.SDL_WINDOW_HIDDEN;
            default:
                throw new Exception($"Invalid WindowState: [{state}]");
        }
    }
    
    private static GraphicsDevice CreateGraphicsDevice(IWindow window, GraphicsDeviceOptions options, GraphicsBackend preferredBackend) {
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
    /// Creates a Direct3D11 graphics device for a specified SDL2 window with the provided graphics device options.
    /// </summary>
    /// <param name="window">The SDL2 window for which to create the graphics device.</param>
    /// <param name="options">The options to use for creating the graphics device.</param>
    /// <returns>The created Direct3D11 graphics device.</returns>
    private static GraphicsDevice CreateD3D11GraphicsDevice(IWindow window, GraphicsDeviceOptions options) {
        SwapchainDescription description = new SwapchainDescription() {
            Source = window.SwapchainSource,
            Width = (uint) window.GetWidth(),
            Height = (uint) window.GetHeight(),
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
    private static GraphicsDevice CreateVulkanGraphicsDevice(IWindow window, GraphicsDeviceOptions options) {
        SwapchainDescription description = new SwapchainDescription() {
            Source = window.SwapchainSource,
            Width = (uint) window.GetWidth(),
            Height = (uint) window.GetHeight(),
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
    private static GraphicsDevice CreateMetalGraphicsDevice(IWindow window, GraphicsDeviceOptions options) {
        SwapchainDescription description = new SwapchainDescription() {
            Source = window.SwapchainSource,
            Width = (uint) window.GetWidth(),
            Height = (uint) window.GetHeight(),
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
    private static unsafe GraphicsDevice CreateOpenGlGraphicsDevice(IWindow window, GraphicsDeviceOptions options, GraphicsBackend backend) {
        SDL3.SDL_ClearError();
        nint sdlHandle = window.Handle;

        SetSdlGlContextAttributes(options, backend);

        SDL_GLContextState* contextHandle = SDL3.SDL_GL_CreateContext((SDL_Window*) sdlHandle);
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
            proc => SDL3.SDL_GL_GetProcAddress(proc), //TODO: IM NOT SURE...
            context => SDL3.SDL_GL_MakeCurrent((SDL_Window*) sdlHandle, (SDL_GLContextState*) context),
            () => (nint) SDL3.SDL_GL_GetCurrentContext(),
            () => SDL3.SDL_GL_MakeCurrent((SDL_Window*) sdlHandle, (SDL_GLContextState*) nint.Zero),
            context => SDL3.SDL_GL_DestroyContext((SDL_GLContextState*) context),
            () => SDL3.SDL_GL_SwapWindow((SDL_Window*) sdlHandle),
            sync => SDL3.SDL_GL_SetSwapInterval(sync ? 1 : 0)
        );

        return GraphicsDevice.CreateOpenGL(options, platformInfo, (uint) window.GetWidth(), (uint) window.GetHeight());
    }

    /// <summary>
    /// Configures the attributes for the SDL GL context based on the specified graphics device options and backend.
    /// </summary>
    /// <param name="options">The graphics device options including settings for debugging and vertical sync.</param>
    /// <param name="backend">The graphics backend, which must be either OpenGL or OpenGLES.</param>
    private static void SetSdlGlContextAttributes(GraphicsDeviceOptions options, GraphicsBackend backend) {
       if (backend != GraphicsBackend.OpenGL && backend != GraphicsBackend.OpenGLES) {
           throw new Exception($"GraphicsBackend must be: [{nameof(GraphicsBackend.OpenGL)}] or [{nameof(GraphicsBackend.OpenGLES)}]!");
       }

       SDL_GLcontextFlag contextFlags = options.Debug ? (SDL_GLcontextFlag.SDL_GL_CONTEXT_DEBUG_FLAG | SDL_GLcontextFlag.SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG) : SDL_GLcontextFlag.SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG;
       SDL3.SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_FLAGS, (int) contextFlags);

       (int major, int minor) = GetMaxGlVersion(backend == GraphicsBackend.OpenGLES);

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
    private static (int, int) GetMaxGlVersion(bool openGlEs) {
        object glVersionLock = new object();
        
        lock (glVersionLock) {
            (int, int)? maxVersion = openGlEs ? _maxSupportedGlEsVersion : _maxSupportedGlVersion;

            if (maxVersion == null) {
                maxVersion = TestMaxGlVersion(openGlEs);

                if (openGlEs) {
                    _maxSupportedGlEsVersion = maxVersion;
                }
                else {
                    _maxSupportedGlVersion = maxVersion;
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
    private static (int, int) TestMaxGlVersion(bool openGlEs) {
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
            if (TestIndividualGlVersion(openGlEs, major, minor)) {
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
    private static unsafe bool TestIndividualGlVersion(bool openGlEs, int major, int minor) {
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
}