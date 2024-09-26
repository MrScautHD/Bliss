using System.Numerics;
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
    /// Retrieves characters that were pressed in the current frame.
    /// </summary>
    /// <returns>An array of characters pressed in the current frame.</returns>
    char[] GetPressedChars();

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
}