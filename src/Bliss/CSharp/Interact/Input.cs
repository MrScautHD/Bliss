using System.Numerics;
using Bliss.CSharp.Interact.Contexts;
using Bliss.CSharp.Interact.Gamepads;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Interact.Mice;
using Bliss.CSharp.Interact.Mice.Cursors;

namespace Bliss.CSharp.Interact;

public static class Input {
    
    /// <summary>
    /// Gets the current input context used to manage and handle input events.
    /// </summary>
    public static IInputContext InputContext { get; private set; }

    /// <summary>
    /// Initializes the Input class with the specified input context.
    /// </summary>
    /// <param name="inputContext">The input context to initialize.</param>
    public static void Init(IInputContext inputContext) {
        InputContext = inputContext;
    }

    /// <summary>
    /// Begins input processing by interacting with the InputContext.
    /// This method should be called at the start of an input processing phase.
    /// </summary>
    public static void Begin() {
        InputContext.Begin();
    }

    /// <summary>
    /// Finalizes the input handling for the current frame.
    /// </summary>
    public static void End() {
        InputContext.End();
    }
    
    /* ------------------------------------ Mouse ------------------------------------ */

    /// <summary>
    /// Checks if the cursor is currently shown.
    /// </summary>
    /// <returns>True if the cursor is shown; otherwise, false.</returns>
    public static bool IsCursorShown() {
        return InputContext.IsCursorShown();
    }

    /// <summary>
    /// Shows the mouse cursor.
    /// </summary>
    public static void ShowCursor() {
        InputContext.ShowCursor();
    }

    /// <summary>
    /// Hides the mouse cursor.
    /// </summary>
    public static void HideCursor() {
        InputContext.HideCursor();
    }

    /// <summary>
    /// Gets the current mouse cursor from the input context.
    /// </summary>
    /// <return>The current mouse cursor.</return>
    public static ICursor GetMouseCursor() {
        return InputContext.GetMouseCursor();
    }

    /// <summary>
    /// Sets the mouse cursor to the specified cursor.
    /// </summary>
    /// <param name="cursor">The cursor to set.</param>
    public static void SetMouseCursor(ICursor cursor) {
        InputContext.SetMouseCursor(cursor);
    }

    /// <summary>
    /// Checks if relative mouse mode is enabled, where the cursor is locked to the window.
    /// </summary>
    /// <returns>True if relative mouse mode is enabled; otherwise, false.</returns>
    public static bool IsRelativeMouseModeEnabled() {
        return InputContext.IsRelativeMouseModeEnabled();
    }

    /// <summary>
    /// Enables relative mouse mode, which locks the cursor to the center of the window
    /// and provides relative motion data instead of absolute position.
    /// </summary>
    public static void EnableRelativeMouseMode() {
        InputContext.EnableRelativeMouseMode();
    }

    /// <summary>
    /// Disables the relative mouse mode.
    /// </summary>
    public static void DisableRelativeMouseMode() {
        InputContext.DisableRelativeMouseMode();
    }

    /// <summary>
    /// Gets the current position of the mouse in window coordinates.
    /// </summary>
    /// <returns>The current position of the mouse as a Vector2.</returns>
    public static Vector2 GetMousePosition() {
        return InputContext.GetMousePosition();
    }

    /// <summary>
    /// Retrieves the change in mouse position since the last frame.
    /// </summary>
    /// <returns>A Vector2 representing the delta of the mouse movement.</returns>
    public static Vector2 GetMouseDelta() {
        return InputContext.GetMouseDelta();
    }

    /// <summary>
    /// Sets the mouse position to the specified coordinates.
    /// </summary>
    /// <param name="position">The position to set the mouse to.</param>
    public static void SetMousePosition(Vector2 position) { 
        InputContext.SetMousePosition(position);
    }

    /// <summary>
    /// Checks if the specified mouse button was pressed in the current frame.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button was pressed; otherwise, false.</returns>
    public static bool IsMouseButtonPressed(MouseButton button) { 
        return InputContext.IsMouseButtonPressed(button);
    }

    /// <summary>
    /// Checks if the specified mouse button is currently being pressed down.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button is down; otherwise, false.</returns>
    public static bool IsMouseButtonDown(MouseButton button) {
        return InputContext.IsMouseButtonDown(button);
    }

    /// <summary>
    /// Checks if the specified mouse button was released in the current frame.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button was released; otherwise, false.</returns>
    public static bool IsMouseButtonReleased(MouseButton button) {
        return InputContext.IsMouseButtonReleased(button);
    }

    /// <summary>
    /// Checks if the specified mouse button is currently up (not pressed).
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button is up; otherwise, false.</returns>
    public static bool IsMouseButtonUp(MouseButton button) {
        return InputContext.IsMouseButtonUp(button);
    }

    /// <summary>
    /// Checks if the mouse is currently moving and provides its position.
    /// </summary>
    /// <param name="position">The current position of the mouse if it is moving.</param>
    /// <returns>True if the mouse is moving; otherwise, false.</returns>
    public static bool IsMouseMoving(out Vector2 position) {
        return InputContext.IsMouseMoving(out position);
    }

    /// <summary>
    /// Checks if the mouse is currently scrolling and retrieves the wheel delta if it is.
    /// </summary>
    /// <param name="wheelDelta">The vector representing the scroll delta of the mouse.</param>
    /// <returns>True if the mouse is scrolling, otherwise false.</returns>
    public static bool IsMouseScrolling(out Vector2 wheelDelta) {
        return InputContext.IsMouseScrolling(out wheelDelta);
    }
    
    /* ------------------------------------ Keyboard ------------------------------------ */

    /// <summary>
    /// Checks if a specific key on the keyboard is currently pressed.
    /// </summary>
    /// <param name="key">The keyboard key to check.</param>
    /// <return>True if the key is pressed; otherwise, false.</return>
    public static bool IsKeyPressed(KeyboardKey key) {
        return InputContext.IsKeyPressed(key);
    }

    /// <summary>
    /// Checks if the specified keyboard key is currently pressed down.
    /// </summary>
    /// <param name="key">The keyboard key to check.</param>
    /// <returns>True if the key is pressed down; otherwise, false.</returns>
    public static bool IsKeyDown(KeyboardKey key) {
        return InputContext.IsKeyDown(key);
    }

    /// <summary>
    /// Checks if the specified key has been released.
    /// </summary>
    /// <param name="key">The keyboard key to check.</param>
    /// <returns>True if the key has been released; otherwise, false.</returns>
    public static bool IsKeyReleased(KeyboardKey key) {
        return InputContext.IsKeyReleased(key);
    }

    /// <summary>
    /// Determines whether the specified key is currently up (not pressed).
    /// </summary>
    /// <param name="key">The keyboard key to check.</param>
    /// <returns>True if the key is up; otherwise, false.</returns>
    public static bool IsKeyUp(KeyboardKey key) {
        return InputContext.IsKeyUp(key);
    }

    /// <summary>
    /// Retrieves characters that were pressed in the current frame.
    /// </summary>
    /// <returns>An array of characters pressed in the current frame.</returns>
    public static char[] GetPressedChars() {
        return InputContext.GetPressedChars();
    }

    /// <summary>
    /// Retrieves the current text from the system clipboard via the InputContext.
    /// </summary>
    /// <returns>The current clipboard text.</returns>
    public static string GetClipboardText() {
        return InputContext.GetClipboardText();
    }

    /// <summary>
    /// Sets the clipboard content to the specified text.
    /// </summary>
    /// <param name="text">The text to set to the clipboard.</param>
    public static void SetClipboardText(string text) { 
        InputContext.SetClipboardText(text);
    }
    
    /* ------------------------------------ Gamepad ------------------------------------ */

    /// <summary>
    /// Gets the count of available gamepads.
    /// </summary>
    /// <returns>The number of available gamepads.</returns>
    public static uint GetAvailableGamepadCount() {
        return InputContext.GetAvailableGamepadCount();
    }

    /// <summary>
    /// Checks if the specified gamepad is available.
    /// </summary>
    /// <param name="gamepad">The index of the gamepad to check.</param>
    /// <returns>True if the gamepad is available; otherwise, false.</returns>
    public static bool IsGamepadAvailable(uint gamepad) {
        return InputContext.IsGamepadAvailable(gamepad);
    }

    /// <summary>
    /// Gets the name of the specified gamepad.
    /// </summary>
    /// <param name="gamepad">The index of the gamepad.</param>
    /// <returns>The name of the specified gamepad.</returns>
    public static string GetGamepadName(uint gamepad) {
        return InputContext.GetGamepadName(gamepad);
    }

    /// <summary>
    /// Generates a rumble effect on the specified gamepad.
    /// </summary>
    /// <param name="gamepad">The index of the gamepad to rumble.</param>
    /// <param name="lowFrequencyRumble">The intensity of the low-frequency rumble.</param>
    /// <param name="highFrequencyRumble">The intensity of the high-frequency rumble.</param>
    /// <param name="durationMs">Duration of the rumble effect in milliseconds.</param>
    public static void RumbleGamepad(uint gamepad, ushort lowFrequencyRumble, ushort highFrequencyRumble, uint durationMs) {
        InputContext.RumbleGamepad(gamepad, lowFrequencyRumble, highFrequencyRumble, durationMs);
    }

    /// <summary>
    /// Retrieves the movement value of the specified axis on the given gamepad.
    /// </summary>
    /// <param name="gamepad">The index of the gamepad.</param>
    /// <param name="axis">The axis of the gamepad to check.</param>
    /// <returns>The movement value of the specified axis.</returns>
    public static float GetGamepadAxisMovement(uint gamepad, GamepadAxis axis) {
        return InputContext.GetGamepadAxisMovement(gamepad, axis);
    }

    /// <summary>
    /// Checks if the specified button on the given gamepad is pressed.
    /// </summary>
    /// <param name="gamepad">The identifier of the gamepad to check.</param>
    /// <param name="button">The button on the gamepad to check.</param>
    /// <returns>True if the button is pressed; otherwise, false.</returns>
    public static bool IsGamepadButtonPressed(uint gamepad, GamepadButton button) {
        return InputContext.IsGamepadButtonPressed(gamepad, button);
    }

    /// <summary>
    /// Checks if a specific gamepad button is currently pressed.
    /// </summary>
    /// <param name="gamepad">The ID of the gamepad to check.</param>
    /// <param name="button">The gamepad button to check.</param>
    /// <returns>True if the button is pressed, otherwise false.</returns>
    public static bool IsGamepadButtonDown(uint gamepad, GamepadButton button) {
        return InputContext.IsGamepadButtonDown(gamepad, button);
    }

    /// <summary>
    /// Checks if the specified gamepad button is released.
    /// </summary>
    /// <param name="gamepad">The gamepad identifier.</param>
    /// <param name="button">The gamepad button to check.</param>
    /// <returns>True if the specified gamepad button is released; otherwise, false.</returns>
    public static bool IsGamepadButtonReleased(uint gamepad, GamepadButton button) {
        return InputContext.IsGamepadButtonReleased(gamepad, button);
    }

    /// <summary>
    /// Checks if the specified gamepad button is not pressed.
    /// </summary>
    /// <param name="gamepad">The identifier of the gamepad.</param>
    /// <param name="button">The gamepad button to check.</param>
    /// <returns>True if the specified gamepad button is up; otherwise, false.</returns>
    public static bool IsGamepadButtonUp(uint gamepad, GamepadButton button) {
        return InputContext.IsGamepadButtonUp(gamepad, button);
    }
    
    /* ------------------------------------ Other ------------------------------------ */

    /// <summary>
    /// Determines whether a file has been drag-dropped onto the application.
    /// </summary>
    /// <param name="path">The path of the drag-dropped file, if one exists.</param>
    /// <returns>True if a file was drag-dropped; otherwise, false.</returns>
    public static bool IsFileDragDropped(out string path) {
        return InputContext.IsFileDragDropped(out path);
    }

    /// <summary>
    /// Destroys the Input context and releases any associated resources.
    /// </summary>
    public static void Destroy() {
        InputContext.Dispose();
    }
}