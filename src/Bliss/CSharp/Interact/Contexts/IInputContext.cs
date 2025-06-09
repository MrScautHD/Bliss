using System.Numerics;
using Bliss.CSharp.Interact.Gamepads;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Interact.Mice;
using Bliss.CSharp.Interact.Mice.Cursors;

namespace Bliss.CSharp.Interact.Contexts;

public interface IInputContext : IDisposable {
    
    /// <summary>
    /// Begins input processing for the current frame.
    /// </summary>
    void Begin();
    
    /// <summary>
    /// Ends input processing for the current frame.
    /// </summary>
    void End();
    
    /* ------------------------------------ Mouse ------------------------------------ */

    /// <summary>
    /// Checks if the cursor is currently shown.
    /// </summary>
    /// <returns>True if the cursor is shown; otherwise, false.</returns>
    bool IsCursorShown();
    
    /// <summary>
    /// Shows the mouse cursor.
    /// </summary>
    void ShowCursor();
    
    /// <summary>
    /// Hides the mouse cursor.
    /// </summary>
    void HideCursor();

    /// <summary>
    /// Gets the current mouse cursor.
    /// </summary>
    /// <returns>The current mouse cursor.</returns>
    ICursor GetMouseCursor();
    
    /// <summary>
    /// Sets the mouse cursor to the specified cursor.
    /// </summary>
    /// <param name="cursor">The cursor to set.</param>
    void SetMouseCursor(ICursor cursor);

    /// <summary>
    /// Checks if relative mouse mode is enabled, where the cursor is locked to the window.
    /// </summary>
    /// <returns>True if relative mouse mode is enabled; otherwise, false.</returns>
    bool IsRelativeMouseModeEnabled();
    
    /// <summary>
    /// Enables relative mouse mode.
    /// </summary>
    void EnableRelativeMouseMode();
    
    /// <summary>
    /// Disables relative mouse mode.
    /// </summary>
    void DisableRelativeMouseMode();

    /// <summary>
    /// Gets the current position of the mouse in window coordinates.
    /// </summary>
    /// <returns>The mouse position as a <see cref="Vector2"/>.</returns>
    Vector2 GetMousePosition();

    /// <summary>
    /// Retrieves the change in mouse position since the last frame.
    /// </summary>
    /// <returns>A Vector2 representing the delta of the mouse movement.</returns>
    Vector2 GetMouseDelta();
    
    /// <summary>
    /// Sets the mouse position to the specified coordinates.
    /// </summary>
    /// <param name="position">The position to set the mouse to.</param>
    void SetMousePosition(Vector2 position);

    /// <summary>
    /// Checks if the specified mouse button was pressed in the current frame.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button was pressed; otherwise, false.</returns>
    bool IsMouseButtonPressed(MouseButton button);
    
    /// <summary>
    /// Checks if the specified mouse button is currently being held down.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button is down; otherwise, false.</returns>
    bool IsMouseButtonDown(MouseButton button);
    
    /// <summary>
    /// Checks if the specified mouse button was released in the current frame.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button was released; otherwise, false.</returns>
    bool IsMouseButtonReleased(MouseButton button);
    
    /// <summary>
    /// Checks if the specified mouse button is currently up (not pressed).
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button is up; otherwise, false.</returns>
    bool IsMouseButtonUp(MouseButton button);
    
    /// <summary>
    /// Checks if the mouse is moving and returns the current position if it is.
    /// </summary>
    /// <param name="position">The current mouse position.</param>
    /// <returns>True if the mouse is moving; otherwise, false.</returns>
    bool IsMouseMoving(out Vector2 position);
    
    /// <summary>
    /// Checks if the mouse is scrolling and returns the scroll delta if it is.
    /// </summary>
    /// <param name="wheelDelta">The scroll delta of the mouse.</param>
    /// <returns>True if the mouse is scrolling; otherwise, false.</returns>
    bool IsMouseScrolling(out Vector2 wheelDelta);
    
    /* ------------------------------------ Keyboard ------------------------------------ */

    /// <summary>
    /// Checks if the specified keyboard key is currently pressed.
    /// </summary>
    /// <param name="key">The keyboard key to check.</param>
    /// <returns>True if the specified key is pressed; otherwise, false.</returns>
    bool IsKeyPressed(KeyboardKey key);

    /// <summary>
    /// Checks if a specified key is currently pressed down.
    /// </summary>
    /// <param name="key">The key to check the state of.</param>
    /// <returns>True if the key is pressed, false otherwise.</returns>
    bool IsKeyDown(KeyboardKey key);

    /// <summary>
    /// Checks if a specified keyboard key has been released.
    /// </summary>
    /// <param name="key">The keyboard key to check.</param>
    /// <returns>True if the key has been released, otherwise false.</returns>
    bool IsKeyReleased(KeyboardKey key);

    /// <summary>
    /// Checks if a specific keyboard key is currently in the released state.
    /// </summary>
    /// <param name="key">The keyboard key to check.</param>
    /// <returns>Returns true if the specified key is up; otherwise, false.</returns>
    bool IsKeyUp(KeyboardKey key);
    
    /// <summary>
    /// Retrieves any text that was typed since the last frame, but only while text input is active via <see cref="EnableTextInput"/>.
    /// </summary>
    /// <param name="text">The typed text collected since the previous frame. Will be empty if no text was entered.</param>
    /// <returns><c>true</c> if any text was entered; otherwise, <c>false</c>.</returns>
    bool GetTypedText(out string text);

    /// <summary>
    /// Determines whether text input mode is currently active.
    /// </summary>
    /// <returns>True if text input is active; otherwise, false.</returns>
    bool IsTextInputActive();
    
    /// <summary>
    /// Activates text input mode, allowing the application to receive typed text events.
    /// </summary>
    void EnableTextInput();
    
    /// <summary>
    /// Disable the text input process, ending any active text input session.
    /// </summary>
    void DisableTextInput();

    /// <summary>
    /// Retrieves the current text from the system clipboard.
    /// </summary>
    /// <returns>The clipboard text.</returns>
    string GetClipboardText();

    /// <summary>
    /// Sets the clipboard content to the specified text.
    /// </summary>
    /// <param name="text">The text to set to the clipboard.</param>
    void SetClipboardText(string text);
    
    /* ------------------------------------ Gamepad ------------------------------------ */

    /// <summary>
    /// Gets the count of available gamepads.
    /// </summary>
    /// <returns>The number of available gamepads.</returns>
    uint GetAvailableGamepadCount();

    /// <summary>
    /// Checks if the specified gamepad is available.
    /// </summary>
    /// <param name="gamepad">The index of the gamepad to check.</param>
    /// <returns>True if the gamepad is available; otherwise, false.</returns>
    bool IsGamepadAvailable(uint gamepad);

    /// <summary>
    /// Gets the name of the specified gamepad.
    /// </summary>
    /// <param name="gamepad">The index of the gamepad.</param>
    /// <returns>The name of the specified gamepad.</returns>
    string GetGamepadName(uint gamepad);

    /// <summary>
    /// Generates a rumble effect on the specified gamepad.
    /// </summary>
    /// <param name="gamepad">The index of the gamepad to rumble.</param>
    /// <param name="lowFrequencyRumble">The intensity of the low-frequency rumble.</param>
    /// <param name="highFrequencyRumble">The intensity of the high-frequency rumble.</param>
    /// <param name="durationMs">Duration of the rumble effect in milliseconds.</param>
    void RumbleGamepad(uint gamepad, ushort lowFrequencyRumble, ushort highFrequencyRumble, uint durationMs);

    /// <summary>
    /// Retrieves the movement value of the specified axis on the given gamepad.
    /// </summary>
    /// <param name="gamepad">The index of the gamepad.</param>
    /// <param name="axis">The axis of the gamepad to check.</param>
    /// <returns>The movement value of the specified axis.</returns>
    float GetGamepadAxisMovement(uint gamepad, GamepadAxis axis);

    /// <summary>
    /// Checks if a specific gamepad button is pressed.
    /// </summary>
    /// <param name="gamepad">The identifier of the gamepad.</param>
    /// <param name="button">The button on the gamepad to check.</param>
    /// <returns>True if the specified button is pressed; otherwise, false.</returns>
    bool IsGamepadButtonPressed(uint gamepad, GamepadButton button);

    /// <summary>
    /// Checks if the specified button on the given gamepad is currently being pressed.
    /// </summary>
    /// <param name="gamepad">The index of the gamepad.</param>
    /// <param name="button">The button on the gamepad to check.</param>
    /// <returns>True if the specified button is pressed, otherwise false.</returns>
    bool IsGamepadButtonDown(uint gamepad, GamepadButton button);

    /// <summary>
    /// Determines whether a specific button on a specified gamepad has been released.
    /// </summary>
    /// <param name="gamepad">The identifier of the gamepad.</param>
    /// <param name="button">The gamepad button to check.</param>
    /// <returns>True if the button was released; otherwise, false.</returns>
    bool IsGamepadButtonReleased(uint gamepad, GamepadButton button);

    /// <summary>
    /// Determines whether a specified button on the specified gamepad is currently not pressed.
    /// </summary>
    /// <param name="gamepad">The identifier of the gamepad.</param>
    /// <param name="button">The gamepad button to check.</param>
    /// <returns>True if the button is up (not pressed); otherwise, false.</returns>
    bool IsGamepadButtonUp(uint gamepad, GamepadButton button);
    
    /* ------------------------------------ Other ------------------------------------ */

    /// <summary>
    /// Checks if a file has been drag-dropped onto the application.
    /// </summary>
    /// <param name="path">When this method returns, contains the path of the file that was drag-dropped.</param>
    /// <returns>True if a file was drag-dropped; otherwise, false.</returns>
    bool IsFileDragDropped(out string path);
}