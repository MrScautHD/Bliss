using Bliss.CSharp.Images;
using Bliss.CSharp.Logging;
using SDL;

namespace Bliss.CSharp.Interact.Mice.Cursors;

public class Sdl3Cursor : Disposable, ICursor {
    
    /// <summary>
    /// Represents the handle to the SDL cursor object.
    /// Used internally to manage the cursor's memory and operations.
    /// </summary>
    private unsafe SDL_Cursor* _cursor;

    /// <summary>
    /// Indicates if the cursor was created using an existing SDL system cursor.
    /// Used to determine if the cursor needs to be destroyed upon disposal.
    /// </summary>
    private bool _existingCursor;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Sdl3Cursor"/> class with a custom image cursor.
    /// </summary>
    /// <param name="image">The image used to create the cursor.</param>
    /// <param name="offsetX">The X-axis offset for the cursor's hotspot.</param>
    /// <param name="offsetY">The Y-axis offset for the cursor's hotspot.</param>
    public unsafe Sdl3Cursor(Image image, int offsetX, int offsetY) {
        this._cursor = this.CreateColorCursor(image, offsetX, offsetY);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Sdl3Cursor"/> class with a system cursor type.
    /// </summary>
    /// <param name="systemCursor">The type of system cursor to create.</param>
    public unsafe Sdl3Cursor(SystemCursor systemCursor) {
        this._cursor = this.CreateSystemCursor(systemCursor);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Sdl3Cursor"/> class with an existing SDL cursor pointer.
    /// </summary>
    /// <param name="cursor">A pointer to an existing SDL cursor.</param>
    public unsafe Sdl3Cursor(SDL_Cursor* cursor) {
        this._cursor = cursor;
        this._existingCursor = true;
    }
    
    public unsafe nint GetHandle() {
        return (nint) this._cursor;
    }

    /// <summary>
    /// Creates an SDL cursor from an Image object with specified offsets.
    /// </summary>
    /// <param name="image">The image to be used for creating the cursor.</param>
    /// <param name="offsetX">The x-coordinate offset for the cursor hotspot.</param>
    /// <param name="offsetY">The y-coordinate offset for the cursor hotspot.</param>
    /// <returns>Returns an SDL_Cursor pointer representing the created color cursor.</returns>
    private unsafe SDL_Cursor* CreateColorCursor(Image image, int offsetX, int offsetY) {
        fixed (byte* dataPtr = image.Data) {
            SDL_Surface* surface = SDL3.SDL_CreateSurfaceFrom(image.Width, image.Height, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888, (nint) dataPtr, image.Width * 4);

            if ((nint) surface == nint.Zero) {
                Logger.Error($"Failed to create color cursor: {SDL3.SDL_GetError()}");
            }

            SDL_Cursor* cursor = SDL3.SDL_CreateColorCursor(surface, offsetX, offsetY);
            SDL3.SDL_DestroySurface(surface);
            return cursor;
        }
    }

    /// <summary>
    /// Retrieves a system cursor based on the specified system cursor type.
    /// </summary>
    /// <param name="systemCursor">The type of system cursor to retrieve.</param>
    /// <returns>Returns an SDL_Cursor pointer to the specified system cursor.</returns>
    private unsafe SDL_Cursor* CreateSystemCursor(SystemCursor systemCursor) {
        return SDL3.SDL_CreateSystemCursor(this.MapSystemCursor(systemCursor));
    }

    /// <summary>
    /// Maps a given <see cref="SystemCursor"/> to the corresponding <see cref="SDL_SystemCursor"/> value.
    /// </summary>
    /// <param name="systemCursor">The <see cref="SystemCursor"/> that needs to be mapped.</param>
    /// <returns>The corresponding <see cref="SDL_SystemCursor"/> value.</returns>
    private SDL_SystemCursor MapSystemCursor(SystemCursor systemCursor) {
        return systemCursor switch {
            SystemCursor.Text => SDL_SystemCursor.SDL_SYSTEM_CURSOR_TEXT,
            SystemCursor.Wait => SDL_SystemCursor.SDL_SYSTEM_CURSOR_WAIT,
            SystemCursor.Crosshair => SDL_SystemCursor.SDL_SYSTEM_CURSOR_CROSSHAIR,
            SystemCursor.Progress => SDL_SystemCursor.SDL_SYSTEM_CURSOR_PROGRESS,
            SystemCursor.NWSEResize => SDL_SystemCursor.SDL_SYSTEM_CURSOR_NWSE_RESIZE,
            SystemCursor.NESWResize => SDL_SystemCursor.SDL_SYSTEM_CURSOR_NESW_RESIZE,
            SystemCursor.EWResize => SDL_SystemCursor.SDL_SYSTEM_CURSOR_EW_RESIZE,
            SystemCursor.NSResize => SDL_SystemCursor.SDL_SYSTEM_CURSOR_NS_RESIZE,
            SystemCursor.Move => SDL_SystemCursor.SDL_SYSTEM_CURSOR_MOVE,
            SystemCursor.NotAllowed => SDL_SystemCursor.SDL_SYSTEM_CURSOR_NOT_ALLOWED,
            SystemCursor.Pointer => SDL_SystemCursor.SDL_SYSTEM_CURSOR_POINTER,
            SystemCursor.NWResize => SDL_SystemCursor.SDL_SYSTEM_CURSOR_NW_RESIZE,
            SystemCursor.NResize => SDL_SystemCursor.SDL_SYSTEM_CURSOR_N_RESIZE,
            SystemCursor.NEResize => SDL_SystemCursor.SDL_SYSTEM_CURSOR_NE_RESIZE,
            SystemCursor.EResize => SDL_SystemCursor.SDL_SYSTEM_CURSOR_E_RESIZE,
            SystemCursor.SEResize => SDL_SystemCursor.SDL_SYSTEM_CURSOR_SE_RESIZE,
            SystemCursor.SResize => SDL_SystemCursor.SDL_SYSTEM_CURSOR_S_RESIZE,
            SystemCursor.SWResize => SDL_SystemCursor.SDL_SYSTEM_CURSOR_SW_RESIZE,
            SystemCursor.WResize => SDL_SystemCursor.SDL_SYSTEM_CURSOR_W_RESIZE,
            SystemCursor.Count => SDL_SystemCursor.SDL_SYSTEM_CURSOR_COUNT,
            _ => SDL_SystemCursor.SDL_SYSTEM_CURSOR_DEFAULT
        };
    }

    protected override unsafe void Dispose(bool disposing) {
        if (disposing && !this._existingCursor) {
            SDL3.SDL_DestroyCursor(this._cursor);
        }
    }
}