using Bliss.CSharp.Logging;
using SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Bliss.CSharp.Interact.Mice.Cursors;

public class Sdl3Cursor : Disposable, ICursor {
    
    /// <summary>
    /// Represents the handle to the SDL cursor object.
    /// Used internally to manage the cursor's memory and operations.
    /// </summary>
    private unsafe SDL_Cursor* _cursor;

    private bool _existingCursor;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Sdl3Cursor"/> class with a custom image cursor.
    /// </summary>
    /// <param name="image">The image used to create the cursor.</param>
    /// <param name="offsetX">The X-axis offset for the cursor's hotspot.</param>
    /// <param name="offsetY">The Y-axis offset for the cursor's hotspot.</param>
    public unsafe Sdl3Cursor(Image<Rgba32> image, int offsetX, int offsetY) { // TODO: REWORK CURSOR TO HAVE JUST 1 or do a static cursor creation class like Cursor.Create();
        this._cursor = this.GetColorCursor(image, offsetX, offsetY);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Sdl3Cursor"/> class with a system cursor type.
    /// </summary>
    /// <param name="systemCursor">The type of system cursor to create.</param>
    public unsafe Sdl3Cursor(SystemCursor systemCursor) {
        this._cursor = SDL3.SDL_CreateSystemCursor((SDL_SystemCursor) systemCursor);
        this._existingCursor = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Sdl3Cursor"/> class with an existing SDL cursor pointer.
    /// </summary>
    /// <param name="cursor">A pointer to an existing SDL cursor.</param>
    public unsafe Sdl3Cursor(SDL_Cursor* cursor) {
        this._cursor = cursor;
    }
    
    public unsafe nint GetCursorHandle() {
        return (nint) this._cursor;
    }

    /// <summary>
    /// Creates an SDL cursor from an Image object with specified offsets.
    /// </summary>
    /// <param name="image">The image to be used for creating the cursor.</param>
    /// <param name="offsetX">The x-coordinate offset for the cursor hotspot.</param>
    /// <param name="offsetY">The y-coordinate offset for the cursor hotspot.</param>
    /// <returns>Returns an SDL_Cursor pointer representing the created color cursor.</returns>
    private unsafe SDL_Cursor* GetColorCursor(Image<Rgba32> image, int offsetX, int offsetY) {
        byte[] data = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(data);

        fixed (byte* dataPtr = data) {
            SDL_Surface* surface = SDL3.SDL_CreateSurfaceFrom(image.Width, image.Height, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888, (nint) dataPtr, image.Width * 4);

            if ((nint) surface == nint.Zero) {
                Logger.Error($"Failed to create color cursor: {SDL3.SDL_GetError()}");
            }

            SDL_Cursor* cursor = SDL3.SDL_CreateColorCursor(surface, offsetX, offsetY);
            SDL3.SDL_DestroySurface(surface);
            return cursor;
        }
    }
    
    protected override unsafe void Dispose(bool disposing) {
        if (disposing && !this._existingCursor) {
            SDL3.SDL_DestroyCursor(this._cursor);
        }
    }
}