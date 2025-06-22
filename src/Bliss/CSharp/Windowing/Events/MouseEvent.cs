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
    /// Indicates whether the mouse button was double-clicked during the event.
    /// </summary>
    public bool DoubleClicked;

    /// <summary>
    /// Initializes a new instance of the MouseEvent class with the specified button and state.
    /// </summary>
    /// <param name="button">The mouse button that triggered the event.</param>
    /// <param name="isDown">A value indicating whether the button is pressed (true) or released (false).</param>
    /// <param name="doubleClicked">A value indicating whether the event was triggered by a double-click.</param>
    public MouseEvent(MouseButton button, bool isDown, bool doubleClicked) {
        this.Button = button;
        this.IsDown = isDown;
        this.DoubleClicked = doubleClicked;
    }
}