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
    /// Initializes a new instance of the <see cref="KeyEvent"/> class with the specified keyboard key and key state.
    /// </summary>
    /// <param name="keyboardKey">The <see cref="KeyboardKey"/> representing the key involved in the event.</param>
    /// <param name="isDown">Indicates whether the key is pressed down (<c>true</c>) or released (<c>false</c>).</param>
    public KeyEvent(KeyboardKey keyboardKey, bool isDown) {
        this.KeyboardKey = keyboardKey;
        this.IsDown = isDown;
    }
}