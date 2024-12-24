namespace Bliss.CSharp.Interact.Gamepads;

public interface IGamepad : IDisposable {
    
    /// <summary>
    /// Gets the name of the gamepad.
    /// </summary>
    /// <returns>A string representing the gamepad's name.</returns>
    string GetName();

    /// <summary>
    /// Gets the index of the gamepad.
    /// </summary>
    /// <returns>An unsigned integer representing the gamepad's index.</returns>
    uint GetIndex();

    /// <summary>
    /// Gets the handle of the gamepad.
    /// </summary>
    /// <returns>An integer pointer representing the gamepad's handle.</returns>
    nint GetHandle();

    /// <summary>
    /// Cleans or resets the internal states of the gamepad (e.g., button states).
    /// </summary>
    void CleanStates();

    /// <summary>
    /// Gets the movement value of a specified axis on the gamepad.
    /// </summary>
    /// <param name="axis">The axis to check.</param>
    /// <returns>A float representing the axis movement.</returns>
    float GetAxisMovement(GamepadAxis axis);

    /// <summary>
    /// Checks if the specified button was pressed in the current frame.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns>True if the button was pressed; otherwise, false.</returns>
    bool IsButtonPressed(GamepadButton button);

    /// <summary>
    /// Checks if the specified button is currently being held down.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns>True if the button is down; otherwise, false.</returns>
    bool IsButtonDown(GamepadButton button);

    /// <summary>
    /// Checks if the specified button was released in the current frame.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns>True if the button was released; otherwise, false.</returns>
    bool IsButtonReleased(GamepadButton button);

    /// <summary>
    /// Checks if the specified button is currently up (not pressed).
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns>True if the button is up; otherwise, false.</returns>
    bool IsButtonUp(GamepadButton button);
}