/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

using Bliss.CSharp.Interact.Keyboards;

namespace Bliss.CSharp.Windowing.Events;

public struct KeyEvent {
    
    /// <summary>
    /// Represents a key on the keyboard that can trigger key events.
    /// </summary>
    public KeyboardKey KeyboardKey;

    /// <summary>
    /// Indicates whether the key is currently pressed down.
    /// </summary>
    public bool IsDown;

    /// <summary>
    /// Indicates whether the key press event is a repeat of the previous key press action.
    /// </summary>
    public bool Repeat;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyEvent"/> class with the specified keyboard key, key state, and repeat flag.
    /// </summary>
    /// <param name="keyboardKey">The <see cref="KeyboardKey"/> representing the key involved in the event.</param>
    /// <param name="isDown">Indicates whether the key is pressed down (<c>true</c>) or released (<c>false</c>).</param>
    /// <param name="repeat">Indicates whether the key event is a repeated key press (<c>true</c>) or a single press (<c>false</c>).</param>
    public KeyEvent(KeyboardKey keyboardKey, bool isDown, bool repeat) {
        this.KeyboardKey = keyboardKey;
        this.IsDown = isDown;
        this.Repeat = repeat;
    }
}