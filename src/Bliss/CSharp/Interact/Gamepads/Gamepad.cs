using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid.Sdl2;

namespace Bliss.CSharp.Interact.Gamepads;

public class Gamepad : Disposable {
    
    public SDL_GameController Controller { get; private set; }
    public int ControllerIndex { get; private set; }
    public string Name { get; private set; }
    
    private readonly Dictionary<SDL_GameControllerAxis, float> _joystickAxis;
    
    private readonly List<SDL_GameControllerButton> _buttonsPressed;
    private readonly List<SDL_GameControllerButton> _buttonsDown;
    private readonly List<SDL_GameControllerButton> _buttonsReleased;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Gamepad"/> class, opening a game controller and setting up input tracking.
    /// </summary>
    /// <param name="index">The index of the game controller to open.</param>
    public unsafe Gamepad(int index) {
        this.Controller = Sdl2Native.SDL_GameControllerOpen(index);
        this.ControllerIndex = Sdl2Native.SDL_JoystickInstanceID(Sdl2Native.SDL_GameControllerGetJoystick(this.Controller));
        this.Name = Marshal.PtrToStringUTF8((nint) Sdl2Native.SDL_GameControllerName(this.Controller)) ?? "Unknown";

        this._joystickAxis = new Dictionary<SDL_GameControllerAxis, float>();
        
        this._buttonsPressed = new List<SDL_GameControllerButton>();
        this._buttonsDown = new List<SDL_GameControllerButton>();
        this._buttonsReleased = new List<SDL_GameControllerButton>();
    }

    /// <summary>
    /// Clears the states of the gamepad buttons that were pressed and released.
    /// </summary>
    public void CleanStates() {
        this._buttonsPressed.Clear();
        this._buttonsReleased.Clear();
    }

    /// <summary>
    /// Gets the movement value of a specified gamepad axis.
    /// </summary>
    /// <param name="axis">The axis to check the movement for.</param>
    /// <returns>The movement value of the specified axis.</returns>
    public float GetAxisMovement(GamepadAxis axis) {
        this._joystickAxis.TryGetValue((SDL_GameControllerAxis) axis, out float result);
        return result;
    }

    /// <summary>
    /// Checks if a specific gamepad button was pressed during the current frame.
    /// </summary>
    /// <param name="button">The gamepad button to check.</param>
    /// <returns>True if the button was pressed, false otherwise.</returns>
    public bool IsButtonPressed(GamepadButton button) {
        return this._buttonsPressed.Contains((SDL_GameControllerButton) button);
    }

    /// <summary>
    /// Checks if a specific gamepad button is currently held down.
    /// </summary>
    /// <param name="button">The gamepad button to check.</param>
    /// <returns>True if the button is down, false otherwise.</returns>
    public bool IsButtonDown(GamepadButton button) {
        return this._buttonsDown.Contains((SDL_GameControllerButton) button);
    }

    /// <summary>
    /// Checks if a specific gamepad button was released during the current frame.
    /// </summary>
    /// <param name="button">The gamepad button to check.</param>
    /// <returns>True if the button was released, false otherwise.</returns>
    public bool IsButtonReleased(GamepadButton button) {
        return this._buttonsReleased.Contains((SDL_GameControllerButton) button);
    }

    /// <summary>
    /// Checks if a specific gamepad button is currently not held down.
    /// </summary>
    /// <param name="button">The gamepad button to check.</param>
    /// <returns>True if the button is up, false otherwise.</returns>
    public bool IsButtonUp(GamepadButton button) {
        return !this._buttonsDown.Contains((SDL_GameControllerButton) button);
    }

    /// <summary>
    /// Processes input events for a gamepad.
    /// </summary>
    /// <param name="ev">The SDL_Event to process.</param>
    public void ProcessEvent(ref SDL_Event ev) {
        switch (ev.type) {
            case SDL_EventType.ControllerAxisMotion:
                SDL_ControllerAxisEvent axisEvent = Unsafe.As<SDL_Event, SDL_ControllerAxisEvent>(ref ev);
                
                if (axisEvent.which == this.ControllerIndex) {
                    this._joystickAxis[axisEvent.axis] = this.NormalizeJoystickAxis(axisEvent.value);
                }
                
                break;
            case SDL_EventType.ControllerButtonDown:
            case SDL_EventType.ControllerButtonUp:
                SDL_ControllerButtonEvent buttonEvent = Unsafe.As<SDL_Event, SDL_ControllerButtonEvent>(ref ev);
                
                if (buttonEvent.which == this.ControllerIndex) {
                    if (buttonEvent.state == 1) {
                        this._buttonsPressed.Add(buttonEvent.button);
                        this._buttonsDown.Add(buttonEvent.button);
                    }
                    else {
                        this._buttonsDown.Remove(buttonEvent.button);
                        this._buttonsReleased.Add(buttonEvent.button);
                    }
                }
                
                break;
        }
    }

    /// <summary>
    /// Normalizes the value of a joystick axis.
    /// </summary>
    /// <param name="value">The raw value of the joystick axis.</param>
    /// <returns>The normalized value of the joystick axis.</returns>
    private float NormalizeJoystickAxis(short value) {
        return value < 0 ? -(value / (float) short.MinValue) : (value / (float) short.MaxValue);
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            Sdl2Native.SDL_GameControllerClose(this.Controller);
        }
    }
}