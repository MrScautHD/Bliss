/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

namespace Bliss.CSharp.Interact.Mice.Cursors;

public enum SystemCursor {
    
    /// <summary>
    /// The default system cursor.
    /// </summary>
    Default = 0,
    
    /// <summary>
    /// The cursor displayed when over text.
    /// </summary>
    Text = 1,
    
    /// <summary>
    /// The cursor indicating the system is busy (wait state).
    /// </summary>
    Wait = 2,
    
    /// <summary>
    /// The crosshair cursor, typically used for precise selection.
    /// </summary>
    Crosshair = 3,
    
    /// <summary>
    /// The cursor indicating progress without preventing interaction.
    /// </summary>
    Progress = 4,
    
    /// <summary>
    /// The resize cursor used for diagonal resizing (NW-SE direction).
    /// </summary>
    NWSEResize = 5,
    
    /// <summary>
    /// The resize cursor used for diagonal resizing (NE-SW direction).
    /// </summary>
    NESWResize = 6,
    
    /// <summary>
    /// The cursor used for horizontal resizing (E-W direction).
    /// </summary>
    EWResize = 7,
    
    /// <summary>
    /// The cursor used for vertical resizing (N-S direction).
    /// </summary>
    NSResize = 8,
    
    /// <summary>
    /// The move cursor, typically used for dragging objects.
    /// </summary>
    Move = 9,
    
    /// <summary>
    /// The cursor indicating an action is not allowed.
    /// </summary>
    NotAllowed = 10,
    
    /// <summary>
    /// The pointer cursor, typically used for links and clickable items.
    /// </summary>
    Pointer = 11,
    
    /// <summary>
    /// The resize cursor for resizing from the northwest corner.
    /// </summary>
    NWResize = 12,
    
    /// <summary>
    /// The resize cursor for resizing from the north side.
    /// </summary>
    NResize = 13,
    
    /// <summary>
    /// The resize cursor for resizing from the northeast corner.
    /// </summary>
    NEResize = 14,
    
    /// <summary>
    /// The resize cursor for resizing from the east side.
    /// </summary>
    EResize = 15,
    
    /// <summary>
    /// The resize cursor for resizing from the southeast corner.
    /// </summary>
    SEResize = 16,
    
    /// <summary>
    /// The resize cursor for resizing from the south side.
    /// </summary>
    SResize = 17,
    
    /// <summary>
    /// The resize cursor for resizing from the southwest corner.
    /// </summary>
    SWResize = 18,
    
    /// <summary>
    /// The resize cursor for resizing from the west side.
    /// </summary>
    WResize = 19,
    
    /// <summary>
    /// The total number of cursor types available.
    /// </summary>
    Count = 20
}