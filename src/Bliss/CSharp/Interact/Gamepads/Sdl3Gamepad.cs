using Bliss.CSharp.Windowing;
using SDL3;

namespace Bliss.CSharp.Interact.Gamepads;

public class Sdl3Gamepad : Disposable, IGamepad {
    
    /// <summary>
    /// Gets the window associated with the gamepad.
    /// </summary>
    public IWindow Window { get; private set; }

    /// <summary>
    /// Holds the reference to the underlying SDL gamepad controller.
    /// </summary>
    private nint _sdlGamepad;
    
    /// <summary>
    /// Stores the index of the gamepad controller.
    /// </summary>
    private uint _gamepadIndex;
    
    /// <summary>
    /// The name of the gamepad, or "Unknown" if the name cannot be retrieved.
    /// </summary>
    private string _name;
    
    /// <summary>
    /// A dictionary storing the state of the gamepad's joystick axes, 
    /// mapping <see cref="SDL.GamepadAxis"/> to the axis movement values.
    /// </summary>
    private readonly Dictionary<SDL.GamepadAxis, float> _joystickAxis;
    
    /// <summary>
    /// A list of gamepad buttons that were pressed during the current frame.
    /// </summary>
    private readonly List<SDL.GamepadButton> _buttonsPressed;
    
    /// <summary>
    /// A list of gamepad buttons that are currently being held down.
    /// </summary>
    private readonly List<SDL.GamepadButton> _buttonsDown;
    
    /// <summary>
    /// A list of gamepad buttons that were released during the current frame.
    /// </summary>
    private readonly List<SDL.GamepadButton> _buttonsReleased;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Sdl3Gamepad"/> class, which manages gamepad input for a specific window.
    /// </summary>
    /// <param name="window">The window associated with the gamepad.</param>
    /// <param name="index">The index of the gamepad to be opened.</param>
    public Sdl3Gamepad(IWindow window, uint index) {
        this.Window = window;
        this._sdlGamepad = SDL.OpenGamepad(index);
        this._gamepadIndex = SDL.GetJoystickID(SDL.GetGamepadJoystick(this._sdlGamepad));
        this._name = SDL.GetGamepadName(this._sdlGamepad) ?? "Unknown";
        
        this._joystickAxis = new Dictionary<SDL.GamepadAxis, float>();
        
        this._buttonsPressed = new List<SDL.GamepadButton>();
        this._buttonsDown = new List<SDL.GamepadButton>();
        this._buttonsReleased = new List<SDL.GamepadButton>();
        
        window.GamepadAxisMoved += this.OnGamepadAxisMoved;
        window.GamepadButtonDown += this.OnGamepadButtonDown;
        window.GamepadButtonUp += this.OnGamepadButtonUp;
    }
    
    public string GetName() {
        return this._name;
    }

    public uint GetIndex() {
        return this._gamepadIndex;
    }

    public nint GetHandle() {
        return this._sdlGamepad;
    }
    
    public void CleanStates() {
        this._buttonsPressed.Clear();
        this._buttonsReleased.Clear();
    }
    
    public float GetAxisMovement(GamepadAxis axis) {
        this._joystickAxis.TryGetValue(this.MapGamepadAxis(axis), out float result);
        return result;
    }

    public bool IsButtonPressed(GamepadButton button) {
        return this._buttonsPressed.Contains(this.MapGamepadButton(button));
    }

    public bool IsButtonDown(GamepadButton button) {
        return this._buttonsDown.Contains(this.MapGamepadButton(button));
    }
    
    public bool IsButtonReleased(GamepadButton button) {
        return this._buttonsReleased.Contains(this.MapGamepadButton(button));
    }

    public bool IsButtonUp(GamepadButton button) {
        return !this._buttonsDown.Contains(this.MapGamepadButton(button));
    }

    /// <summary>
    /// Handles movement events for a gamepad axis.
    /// </summary>
    /// <param name="which">The identifier of the gamepad that moved.</param>
    /// <param name="axis">The specific axis that was moved.</param>
    /// <param name="value">The new value of the axis movement.</param>
    private void OnGamepadAxisMoved(uint which, GamepadAxis axis, short value) {
        if (which == this._gamepadIndex) {
            this._joystickAxis[this.MapGamepadAxis(axis)] = this.NormalizeJoystickAxis(value);
        }
    }

    /// <summary>
    /// Handles the event when a gamepad button is pressed down.
    /// </summary>
    /// <param name="which">The index of the gamepad controller.</param>
    /// <param name="button">The button on the gamepad that was pressed.</param>
    private void OnGamepadButtonDown(uint which, GamepadButton button) {
        if (which == this._gamepadIndex) {
            this._buttonsPressed.Add(this.MapGamepadButton(button));
            this._buttonsDown.Add(this.MapGamepadButton(button));
        }
    }

    /// <summary>
    /// Handles the event when a gamepad button is released.
    /// </summary>
    /// <param name="which">The index of the gamepad that triggered the event.</param>
    /// <param name="button">The button that was released.</param>
    private void OnGamepadButtonUp(uint which, GamepadButton button) {
        if (which == this._gamepadIndex) {
            this._buttonsDown.Remove(this.MapGamepadButton(button));
            this._buttonsReleased.Add(this.MapGamepadButton(button));
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
    
    /// <summary>
    /// Maps a GamepadAxis to the corresponding SDL_GamepadAxis.
    /// </summary>
    /// <param name="axis">The GamepadAxis to be mapped.</param>
    /// <return>The corresponding SDL_GamepadAxis.</return>
    private SDL.GamepadAxis MapGamepadAxis(GamepadAxis axis) {
        return axis switch {
            GamepadAxis.LeftX => SDL.GamepadAxis.LeftX,
            GamepadAxis.LeftY => SDL.GamepadAxis.LeftY,
            GamepadAxis.RightX => SDL.GamepadAxis.RightX,
            GamepadAxis.RightY => SDL.GamepadAxis.RightY,
            GamepadAxis.TriggerLeft => SDL.GamepadAxis.LeftTrigger,
            GamepadAxis.TriggerRight => SDL.GamepadAxis.RightTrigger,
            GamepadAxis.Max => SDL.GamepadAxis.Count,
            _ => SDL.GamepadAxis.Invalid
        };
    }

    /// <summary>
    /// Maps a GamepadButton to its corresponding SDL_GamepadButton.
    /// </summary>
    /// <param name="button">The GamepadButton to be mapped.</param>
    /// <returns>The corresponding SDL_GamepadButton.</returns>
    private SDL.GamepadButton MapGamepadButton(GamepadButton button) {
        return button switch {
            GamepadButton.South => SDL.GamepadButton.South,
            GamepadButton.East => SDL.GamepadButton.East,
            GamepadButton.West => SDL.GamepadButton.West,
            GamepadButton.North => SDL.GamepadButton.North,
            GamepadButton.Back => SDL.GamepadButton.Back,
            GamepadButton.Guide => SDL.GamepadButton.Guide,
            GamepadButton.Start => SDL.GamepadButton.Start,
            GamepadButton.LeftStick => SDL.GamepadButton.LeftStick,
            GamepadButton.RightStick => SDL.GamepadButton.RightStick,
            GamepadButton.LeftShoulder => SDL.GamepadButton.LeftShoulder,
            GamepadButton.RightShoulder => SDL.GamepadButton.RightShoulder,
            GamepadButton.DpadUp => SDL.GamepadButton.DPadUp,
            GamepadButton.DpadDown => SDL.GamepadButton.DPadDown,
            GamepadButton.DpadLeft => SDL.GamepadButton.DPadLeft,
            GamepadButton.DpadRight => SDL.GamepadButton.DPadRight,
            GamepadButton.RightPaddle1 => SDL.GamepadButton.RightPaddle1,
            GamepadButton.LeftPaddle1 => SDL.GamepadButton.LeftPaddle1,
            GamepadButton.RightPaddle2 => SDL.GamepadButton.RightPaddle2,
            GamepadButton.LeftPaddle2 => SDL.GamepadButton.LeftPaddle2,
            GamepadButton.Touchpad => SDL.GamepadButton.Touchpad,
            GamepadButton.Misc1 => SDL.GamepadButton.Misc1,
            GamepadButton.Misc2 => SDL.GamepadButton.Misc2,
            GamepadButton.Misc3 => SDL.GamepadButton.Misc3,
            GamepadButton.Misc4 => SDL.GamepadButton.Misc4,
            GamepadButton.Misc5 => SDL.GamepadButton.Misc5,
            GamepadButton.Misc6 => SDL.GamepadButton.Misc6,
            GamepadButton.Count => SDL.GamepadButton.Count,
            _ => SDL.GamepadButton.Invalid
        };
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            SDL.CloseGamepad(this._sdlGamepad);
        }
    }
}