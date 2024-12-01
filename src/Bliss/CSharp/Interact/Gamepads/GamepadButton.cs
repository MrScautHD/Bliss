/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

namespace Bliss.CSharp.Interact.Gamepads;

public enum GamepadButton {
    
    /// <summary>
    /// Represents an invalid button.
    /// </summary>
    Invalid = -1,

    /// <summary>
    /// The "South" face button (often the bottom button, typically labeled A or X).
    /// </summary>
    South = 0,
    
    /// <summary>
    /// The "East" face button (often the right button, typically labeled B or Circle).
    /// </summary>
    East = 1,
    
    /// <summary>
    /// The "West" face button (often the left button, typically labeled X or Square).
    /// </summary>
    West = 2,
    
    /// <summary>
    /// The "North" face button (often the top button, typically labeled Y or Triangle).
    /// </summary>
    North = 3,
    
    /// <summary>
    /// The "Back" button on the gamepad.
    /// </summary>
    Back = 4,
    
    /// <summary>
    /// The "Guide" button, often used to open system menus.
    /// </summary>
    Guide = 5,
    
    /// <summary>
    /// The "Start" button on the gamepad.
    /// </summary>
    Start = 6,
    
    /// <summary>
    /// The left analog stick button (when pressed down).
    /// </summary>
    LeftStick = 7,
    
    /// <summary>
    /// The right analog stick button (when pressed down).
    /// </summary>
    RightStick = 8,
    
    /// <summary>
    /// The left shoulder button.
    /// </summary>
    LeftShoulder = 9,
    
    /// <summary>
    /// The right shoulder button.
    /// </summary>
    RightShoulder = 10,
    
    /// <summary>
    /// The directional pad "Up" button.
    /// </summary>
    DpadUp = 11,
    
    /// <summary>
    /// The directional pad "Down" button.
    /// </summary>
    DpadDown = 12,
    
    /// <summary>
    /// The directional pad "Left" button.
    /// </summary>
    DpadLeft = 13,
    
    /// <summary>
    /// The directional pad "Right" button.
    /// </summary>
    DpadRight = 14,
    
    /// <summary>
    /// A miscellaneous button (Misc1).
    /// </summary>
    Misc1 = 15,
    
    /// <summary>
    /// The first right paddle button.
    /// </summary>
    RightPaddle1 = 16,
    
    /// <summary>
    /// The first left paddle button.
    /// </summary>
    LeftPaddle1 = 17,
    
    /// <summary>
    /// The second right paddle button.
    /// </summary>
    RightPaddle2 = 18,
    
    /// <summary>
    /// The second left paddle button.
    /// </summary>
    LeftPaddle2 = 19,
    
    /// <summary>
    /// The touchpad button, commonly found on some controllers.
    /// </summary>
    Touchpad = 20,
    
    /// <summary>
    /// A miscellaneous button (Misc2).
    /// </summary>
    Misc2 = 21,
    
    /// <summary>
    /// A miscellaneous button (Misc3).
    /// </summary>
    Misc3 = 22,
    
    /// <summary>
    /// A miscellaneous button (Misc4).
    /// </summary>
    Misc4 = 23,
    
    /// <summary>
    /// A miscellaneous button (Misc5).
    /// </summary>
    Misc5 = 24,
    
    /// <summary>
    /// A miscellaneous button (Misc6).
    /// </summary>
    Misc6 = 25,
    
    /// <summary>
    /// Represents the total number of available buttons.
    /// </summary>
    Count = 26
}