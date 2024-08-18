using Veldrid.Sdl2;

namespace Bliss.CSharp.Interact.Mice;

public enum MouseCursor {
    
    /// <summary>
    /// The default cursor. Represents the system's default cursor.
    /// </summary>
    Default = default,

    /// <summary>
    /// The arrow cursor. Typically used for standard pointing interactions.
    /// </summary>
    Arrow = SDL_SystemCursor.Arrow,

    /// <summary>
    /// The I-beam cursor. Typically used for text selection.
    /// </summary>
    IBeam = SDL_SystemCursor.IBeam,

    /// <summary>
    /// The wait cursor. Typically used to indicate that the application is busy.
    /// </summary>
    Wait = SDL_SystemCursor.Wait,

    /// <summary>
    /// The crosshair cursor. Typically used for precise selections or aiming.
    /// </summary>
    Crosshair = SDL_SystemCursor.Crosshair,

    /// <summary>
    /// The wait arrow cursor. Typically used to indicate that the application is busy, but still allows pointing.
    /// </summary>
    WaitArrow = SDL_SystemCursor.WaitArrow,

    /// <summary>
    /// The size NW-SE cursor. Typically used for resizing diagonally from the northwest to the southeast.
    /// </summary>
    SizeNWSE = SDL_SystemCursor.SizeNWSE,

    /// <summary>
    /// The size NE-SW cursor. Typically used for resizing diagonally from the northeast to the southwest.
    /// </summary>
    SizeNESW = SDL_SystemCursor.SizeNESW,

    /// <summary>
    /// The size horizontal cursor. Typically used for resizing horizontally.
    /// </summary>
    SizeWE = SDL_SystemCursor.SizeWE,

    /// <summary>
    /// The size vertical cursor. Typically used for resizing vertically.
    /// </summary>
    SizeNS = SDL_SystemCursor.SizeNS,

    /// <summary>
    /// The size all cursor. Typically used for resizing in any direction.
    /// </summary>
    SizeAll = SDL_SystemCursor.SizeAll,

    /// <summary>
    /// The no cursor. Typically used to indicate that an action is not allowed.
    /// </summary>
    No = SDL_SystemCursor.No,

    /// <summary>
    /// The hand cursor. Typically used for indicating clickable elements, such as hyperlinks.
    /// </summary>
    Hand = SDL_SystemCursor.Hand
}