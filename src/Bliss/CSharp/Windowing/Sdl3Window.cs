using System.Runtime.InteropServices;
using Bliss.CSharp.Logging;
using SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;

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

    /// <summary>
    /// Retrieves the title of the SDL window.
    /// </summary>
    /// <returns>A string representing the title of the window. If the title is not set, an empty string is returned.</returns>
    public unsafe string GetTitle() {
        return SDL3.SDL_GetWindowTitle((SDL_Window*) this.Handle) ?? string.Empty;
    }

    /// <summary>
    /// Sets the title of the SDL window.
    /// </summary>
    /// <param name="title">A string representing the new title for the window.</param>
    public unsafe void SetTitle(string title) {
        if (SDL3.SDL_SetWindowTitle((SDL_Window*) this.Handle, title) == SDL_bool.SDL_FALSE) {
            Logger.Warn($"Failed to set the title of the window: [{this.Id}] Error: {SDL3.SDL_GetError()}");
        }
    }

    /// <summary>
    /// Retrieves the size (width and height) of the SDL window.
    /// </summary>
    /// <returns>A tuple containing the width and height of the window.</returns>
    public unsafe (int, int) GetSize() {
        int width;
        int height;
        
        if (SDL3.SDL_GetWindowSizeInPixels((SDL_Window*) this.Handle, &width, &height) == SDL_bool.SDL_FALSE) {
            Logger.Warn($"Failed to get the size of the window: [{this.Id}] Error: {SDL3.SDL_GetError()}");
        }

        return (width, height);
    }

    /// <summary>
    /// Sets the size of the SDL window.
    /// </summary>
    /// <param name="width">The new width for the window.</param>
    /// <param name="height">The new height for the window.</param>
    public unsafe void SetSize(int width, int height) {
        if (SDL3.SDL_SetWindowSize((SDL_Window*) this.Handle, width, height) == SDL_bool.SDL_FALSE) {
            Logger.Warn($"Failed to set the size of the window: [{this.Id}] Error: {SDL3.SDL_GetError()}");
        }
    }

    /// <summary>
    /// Retrieves the width of the SDL window.
    /// </summary>
    /// <returns>An integer representing the width of the window.</returns>
    public int GetWidth() {
        return this.GetSize().Item1;
    }

    /// <summary>
    /// Sets the width of the SDL window.
    /// </summary>
    /// <param name="width">The new width for the window.</param>
    public void SetWidth(int width) {
        this.SetSize(width, this.GetHeight());
    }

    /// <summary>
    /// Retrieves the height of the SDL window.
    /// </summary>
    /// <returns>An integer representing the height of the window.</returns>
    public int GetHeight() {
        return this.GetSize().Item2;
    }

    /// <summary>
    /// Sets the height of the SDL window.
    /// </summary>
    /// <param name="height">The new height for the window.</param>
    public void SetHeight(int height) {
        this.SetSize(this.GetWidth(), height);
    }

    /// <summary>
    /// Sets the icon for the SDL window using the provided image.
    /// </summary>
    /// <param name="image">The image to be used as the window icon. It should be an Image object with Rgba32 pixel format.</param>
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

    protected override void Dispose(bool disposing) {
        if (disposing) {
            
        }
    }
}