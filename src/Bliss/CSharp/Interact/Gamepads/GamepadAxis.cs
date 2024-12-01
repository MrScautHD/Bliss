/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

namespace Bliss.CSharp.Interact.Gamepads;

public enum GamepadAxis {
    
    /// <summary>
    /// Invalid axis.
    /// </summary>
    Invalid = -1,

    /// <summary>
    /// Left stick horizontal axis.
    /// </summary>
    LeftX = 0,

    /// <summary>
    /// Left stick vertical axis.
    /// </summary>
    LeftY = 1,

    /// <summary>
    /// Right stick horizontal axis.
    /// </summary>
    RightX = 2,

    /// <summary>
    /// Right stick vertical axis.
    /// </summary>
    RightY = 3,

    /// <summary>
    /// Left trigger axis.
    /// </summary>
    TriggerLeft = 4,

    /// <summary>
    /// Right trigger axis.
    /// </summary>
    TriggerRight = 5,

    /// <summary>
    /// Maximum axis value.
    /// </summary>
    Max = 6
}