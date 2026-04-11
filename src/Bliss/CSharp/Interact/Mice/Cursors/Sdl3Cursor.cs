using Bliss.CSharp.Logging;
using SDL3;
using Image = Bliss.CSharp.Images.Image;

namespace Bliss.CSharp.Interact.Mice.Cursors;

public class Sdl3Cursor : Disposable, ICursor {
    
    /// <summary>
    /// Represents the handle to the SDL cursor object.
    /// Used internally to manage the cursor's memory and operations.
    /// </summary>
    private nint _cursor;

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
    public Sdl3Cursor(Image image, int offsetX, int offsetY) {
        this._cursor = this.CreateColorCursor(image, offsetX, offsetY);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Sdl3Cursor"/> class with a system cursor type.
    /// </summary>
    /// <param name="systemCursor">The type of system cursor to create.</param>
    public Sdl3Cursor(SystemCursor systemCursor) {
        this._cursor = this.CreateSystemCursor(systemCursor);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Sdl3Cursor"/> class with an existing SDL cursor pointer.
    /// </summary>
    /// <param name="cursor">A pointer to an existing SDL cursor.</param>
    public Sdl3Cursor(nint cursor) {
        this._cursor = cursor;
        this._existingCursor = true;
    }
    
    public nint GetHandle() {
        return this._cursor;
    }
    
    /// <summary>
    /// Creates an SDL cursor from an Image object with specified offsets.
    /// </summary>
    /// <param name="image">The image to be used for creating the cursor.</param>
    /// <param name="offsetX">The x-coordinate offset for the cursor hotspot.</param>
    /// <param name="offsetY">The y-coordinate offset for the cursor hotspot.</param>
    /// <returns>Returns an SDL_Cursor pointer representing the created color cursor.</returns>
    private unsafe nint CreateColorCursor(Image image, int offsetX, int offsetY) {
        fixed (byte* dataPtr = image.Data) {
            nint surface = SDL.CreateSurfaceFrom(image.Width, image.Height, SDL.PixelFormat.ABGR8888, (nint) dataPtr, image.Width * 4);
            
            if (surface == nint.Zero) {
                Logger.Error($"Failed to create color cursor: {SDL.GetError()}");
            }
            
            nint cursor = SDL.CreateColorCursor(surface, offsetX, offsetY);
            SDL.DestroySurface(surface);
            return cursor;
        }
    }

    /// <summary>
    /// Retrieves a system cursor based on the specified system cursor type.
    /// </summary>
    /// <param name="systemCursor">The type of system cursor to retrieve.</param>
    /// <returns>Returns an SDL_Cursor pointer to the specified system cursor.</returns>
    private nint CreateSystemCursor(SystemCursor systemCursor) {
        return SDL.CreateSystemCursor(this.MapSystemCursor(systemCursor));
    }

    /// <summary>
    /// Maps a given <see cref="SystemCursor"/> to the corresponding <see cref="SDL.SystemCursor"/> value.
    /// </summary>
    /// <param name="systemCursor">The <see cref="SystemCursor"/> that needs to be mapped.</param>
    /// <returns>The corresponding <see cref="SDL.SystemCursor"/> value.</returns>
    private SDL.SystemCursor MapSystemCursor(SystemCursor systemCursor) {
        return systemCursor switch {
            SystemCursor.Text => SDL.SystemCursor.Text,
            SystemCursor.Wait => SDL.SystemCursor.Wait,
            SystemCursor.Crosshair => SDL.SystemCursor.Crosshair,
            SystemCursor.Progress => SDL.SystemCursor.Progress,
            SystemCursor.NWSEResize => SDL.SystemCursor.NWSEResize,
            SystemCursor.NESWResize => SDL.SystemCursor.NESWResize,
            SystemCursor.EWResize => SDL.SystemCursor.EWResize,
            SystemCursor.NSResize => SDL.SystemCursor.NSResize,
            SystemCursor.Move => SDL.SystemCursor.Move,
            SystemCursor.NotAllowed => SDL.SystemCursor.NotAllowed,
            SystemCursor.Pointer => SDL.SystemCursor.Pointer,
            SystemCursor.NWResize => SDL.SystemCursor.NWResize,
            SystemCursor.NResize => SDL.SystemCursor.NResize,
            SystemCursor.NEResize => SDL.SystemCursor.NEResize,
            SystemCursor.EResize => SDL.SystemCursor.EResize,
            SystemCursor.SEResize => SDL.SystemCursor.SEResize,
            SystemCursor.SResize => SDL.SystemCursor.SResize,
            SystemCursor.SWResize => SDL.SystemCursor.SWResize,
            SystemCursor.WResize => SDL.SystemCursor.WResize,
            SystemCursor.Count => SDL.SystemCursor.SDLNumSystemCursors,
            _ => SDL.SystemCursor.Default
        };
    }

    protected override void Dispose(bool disposing) {
        if (disposing && !this._existingCursor) {
            SDL.DestroyCursor(this._cursor);
        }
    }
}