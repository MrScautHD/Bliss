/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

using Bliss.CSharp.Interact.Mice;

namespace Bliss.CSharp.Windowing.Events;

public struct MouseEvent {
    
    /// <summary>
    /// Represents the mouse button involved in the event.
    /// </summary>
    public MouseButton Button;

    /// <summary>
    /// Indicates whether the mouse button is pressed down.
    /// </summary>
    public bool IsDown;

    /// <summary>
    /// Initializes a new instance of the MouseEvent class with the specified button and state.
    /// </summary>
    /// <param name="button">The mouse button that triggered the event.</param>
    /// <param name="isDown">A value indicating whether the button is pressed (true) or released (false).</param>
    public MouseEvent(MouseButton button, bool isDown) {
        this.Button = button;
        this.IsDown = isDown;
    }
}