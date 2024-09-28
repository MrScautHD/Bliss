using System.Runtime.InteropServices;
using Veldrid;

namespace Bliss.CSharp.Windowing;

public static class Window {
    
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
                Sdl3Window window = new Sdl3Window(width, height, title, state);
                graphicsDevice = CreateGraphicsDevice(window, options, preferredBackend);
                return window;
            default:
                throw new Exception($"The window type: [{type}] is not supported!");
        }
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
    /// Creates a graphics device for the specified window, based on the provided options and preferred backend.
    /// </summary>
    /// <param name="window">The window for which to create the graphics device.</param>
    /// <param name="options">Options for configuring the graphics device.</param>
    /// <param name="preferredBackend">The preferred graphics backend to use.</param>
    /// <returns>A graphics device configured according to the specified options and preferred backend.</returns>
    public static GraphicsDevice CreateGraphicsDevice(IWindow window, GraphicsDeviceOptions options, GraphicsBackend preferredBackend) {
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
    /// Creates a Direct3D11 graphics device and swapchain for the specified window.
    /// </summary>
    /// <param name="window">The window for which to create the graphics device.</param>
    /// <param name="options">Options for configuring the graphics device.</param>
    /// <returns>A <see cref="GraphicsDevice"/> instance configured for Direct3D11.</returns>
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
    /// Creates a Vulkan graphics device for the specified window and graphics device options.
    /// </summary>
    /// <param name="window">The window for which the graphics device is to be created.</param>
    /// <param name="options">The configuration options for the graphics device.</param>
    /// <returns>A Vulkan-based <see cref="GraphicsDevice"/> corresponding to the specified window and options.</returns>
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
    /// Creates a Metal graphics device for the specified window with the given options.
    /// </summary>
    /// <param name="window">The window for which the graphics device is created.</param>
    /// <param name="options">Options for configuring the graphics device.</param>
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
    /// Creates an OpenGL graphics device based on the specified parameters.
    /// </summary>
    /// <param name="window">The window for which the graphics device is being created.</param>
    /// <param name="options">Options for configuring the graphics device.</param>
    /// <param name="backend">The graphics backend creating the device.</param>
    /// <returns>The created OpenGL graphics device.</returns>
    private static GraphicsDevice CreateOpenGlGraphicsDevice(IWindow window, GraphicsDeviceOptions options, GraphicsBackend backend) {
        return GraphicsDevice.CreateOpenGL(options, window.GetOrCreateOpenGlPlatformInfo(options, backend), (uint) window.GetWidth(), (uint) window.GetHeight());
    }
}