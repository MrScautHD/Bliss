namespace Bliss.CSharp.Interact.Mice;

public enum MouseCursor {
    
    /// <summary>
    /// The default arrow cursor.
    /// </summary>
    Default = 0,

    /// <summary>
    /// A cursor indicating text selection or editing.
    /// </summary>
    Text = 1,

    /// <summary>
    /// A cursor indicating that the application is busy and the user should wait.
    /// </summary>
    Wait = 2,

    /// <summary>
    /// A cursor indicating a crosshair, typically used for precise selection or drawing.
    /// </summary>
    Crosshair = 3,

    /// <summary>
    /// A cursor indicating a progress or loading state.
    /// </summary>
    Progress = 4,

    /// <summary>
    /// A cursor indicating a resize action from the northwest to the southeast.
    /// </summary>
    NWSEResize = 5,

    /// <summary>
    /// A cursor indicating a resize action from the northeast to the southwest.
    /// </summary>
    NESWResize = 6,

    /// <summary>
    /// A cursor indicating a horizontal resize action.
    /// </summary>
    EWResize = 7,

    /// <summary>
    /// A cursor indicating a vertical resize action.
    /// </summary>
    NSResize = 8,

    /// <summary>
    /// A cursor indicating a move action.
    /// </summary>
    Move = 9,

    /// <summary>
    /// A cursor indicating that the action is not allowed.
    /// </summary>
    NotAllowed = 10,

    /// <summary>
    /// A cursor indicating a pointing hand, often used for hyperlinks.
    /// </summary>
    Pointer = 11,

    /// <summary>
    /// A cursor indicating a resize action from the northwest.
    /// </summary>
    NWResize = 12,

    /// <summary>
    /// A cursor indicating a resize action from the north.
    /// </summary>
    NResize = 13,

    /// <summary>
    /// A cursor indicating a resize action from the northeast.
    /// </summary>
    NEResize = 14,

    /// <summary>
    /// A cursor indicating a resize action from the east.
    /// </summary>
    EResize = 15,

    /// <summary>
    /// A cursor indicating a resize action from the southeast.
    /// </summary>
    SEResize = 16,

    /// <summary>
    /// A cursor indicating a resize action from the south.
    /// </summary>
    SResize = 17,

    /// <summary>
    /// A cursor indicating a resize action from the southwest.
    /// </summary>
    SWResize = 18,

    /// <summary>
    /// A cursor indicating a resize action from the west.
    /// </summary>
    WResize = 19,

    /// <summary>
    /// The total number of cursor types available.
    /// </summary>
    Count = 20
}