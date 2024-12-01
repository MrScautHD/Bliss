/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

using Bliss.CSharp.Windowing;
using SDL;

namespace Bliss.CSharp.Interact.Gamepads;

public class Sdl3Gamepad : Disposable, IGamepad {
    
    /// <summary>
    /// Gets the window associated with the gamepad.
    /// </summary>
    public IWindow Window { get; private set; }

    /// <summary>
    /// Holds the reference to the underlying SDL gamepad controller.
    /// </summary>
    private unsafe SDL_Gamepad* _sdlGamepad;
    
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
    /// mapping <see cref="SDL_GamepadAxis"/> to the axis movement values.
    /// </summary>
    private readonly Dictionary<SDL_GamepadAxis, float> _joystickAxis;
    
    /// <summary>
    /// A list of gamepad buttons that were pressed during the current frame.
    /// </summary>
    private readonly List<SDL_GamepadButton> _buttonsPressed;
    
    /// <summary>
    /// A list of gamepad buttons that are currently being held down.
    /// </summary>
    private readonly List<SDL_GamepadButton> _buttonsDown;
    
    /// <summary>
    /// A list of gamepad buttons that were released during the current frame.
    /// </summary>
    private readonly List<SDL_GamepadButton> _buttonsReleased;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Sdl3Gamepad"/> class, which manages gamepad input for a specific window.
    /// </summary>
    /// <param name="window">The window associated with the gamepad.</param>
    /// <param name="index">The index of the gamepad to be opened.</param>
    public unsafe Sdl3Gamepad(IWindow window, uint index) {
        this.Window = window;
        this._sdlGamepad = SDL3.SDL_OpenGamepad((SDL_JoystickID) index);
        this._gamepadIndex = (uint) SDL3.SDL_GetJoystickID(SDL3.SDL_GetGamepadJoystick(this._sdlGamepad));
        this._name = SDL3.SDL_GetGamepadName(this._sdlGamepad) ?? "Unknown";

        this._joystickAxis = new Dictionary<SDL_GamepadAxis, float>();
        
        this._buttonsPressed = new List<SDL_GamepadButton>();
        this._buttonsDown = new List<SDL_GamepadButton>();
        this._buttonsReleased = new List<SDL_GamepadButton>();
        
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

    public unsafe nint GetHandle() {
        return (nint) this._sdlGamepad;
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
    private SDL_GamepadAxis MapGamepadAxis(GamepadAxis axis) {
        return axis switch {
            GamepadAxis.LeftX => SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTX,
            GamepadAxis.LeftY => SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTY,
            GamepadAxis.RightX => SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTX,
            GamepadAxis.RightY => SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTY,
            GamepadAxis.TriggerLeft => SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFT_TRIGGER,
            GamepadAxis.TriggerRight => SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHT_TRIGGER,
            GamepadAxis.Max => SDL_GamepadAxis.SDL_GAMEPAD_AXIS_COUNT,
            _ => SDL_GamepadAxis.SDL_GAMEPAD_AXIS_INVALID
        };
    }

    /// <summary>
    /// Maps a GamepadButton to its corresponding SDL_GamepadButton.
    /// </summary>
    /// <param name="button">The GamepadButton to be mapped.</param>
    /// <returns>The corresponding SDL_GamepadButton.</returns>
    private SDL_GamepadButton MapGamepadButton(GamepadButton button) {
        return button switch {
            GamepadButton.South => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH,
            GamepadButton.East => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_EAST,
            GamepadButton.West => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_WEST,
            GamepadButton.North => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_NORTH,
            GamepadButton.Back => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_BACK,
            GamepadButton.Guide => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_GUIDE,
            GamepadButton.Start => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_START,
            GamepadButton.LeftStick => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_STICK,
            GamepadButton.RightStick => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_STICK,
            GamepadButton.LeftShoulder => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_SHOULDER,
            GamepadButton.RightShoulder => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_SHOULDER,
            GamepadButton.DpadUp => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_UP,
            GamepadButton.DpadDown => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_DOWN,
            GamepadButton.DpadLeft => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_LEFT,
            GamepadButton.DpadRight => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_RIGHT,
            GamepadButton.Misc1 => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_MISC1,
            GamepadButton.RightPaddle1 => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_PADDLE1,
            GamepadButton.LeftPaddle1 => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_PADDLE1,
            GamepadButton.RightPaddle2 => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_PADDLE2,
            GamepadButton.LeftPaddle2 => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_PADDLE2,
            GamepadButton.Touchpad => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_TOUCHPAD,
            GamepadButton.Misc2 => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_MISC2,
            GamepadButton.Misc3 => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_MISC3,
            GamepadButton.Misc4 => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_MISC4,
            GamepadButton.Misc5 => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_MISC5,
            GamepadButton.Misc6 => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_MISC6,
            GamepadButton.Count => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_COUNT,
            _ => SDL_GamepadButton.SDL_GAMEPAD_BUTTON_INVALID
        };
    }

    protected override unsafe void Dispose(bool disposing) {
        if (disposing) {
            SDL3.SDL_CloseGamepad(this._sdlGamepad);
        }
    }
}