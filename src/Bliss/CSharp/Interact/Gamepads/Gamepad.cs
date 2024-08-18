using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid.Sdl2;

namespace Bliss.CSharp.Interact.Gamepads;

public class Gamepad : Disposable {
    
    public SDL_GameController Controller { get; private set; }
    public int ControllerIndex { get; private set; }
    public string Name { get; private set; }
    
    private readonly Dictionary<SDL_GameControllerAxis, float> _joystickAxis;
    
    private readonly List<SDL_GameControllerButton> _controllerButtonsPressed;
    private readonly List<SDL_GameControllerButton> _controllerButtonsDown;
    private readonly List<SDL_GameControllerButton> _controllerButtonsReleased;
    
    public unsafe Gamepad(int index) {
        this.Controller = Sdl2Native.SDL_GameControllerOpen(index);
        this.ControllerIndex = Sdl2Native.SDL_JoystickInstanceID(Sdl2Native.SDL_GameControllerGetJoystick(this.Controller));
        this.Name = Marshal.PtrToStringUTF8((nint) Sdl2Native.SDL_GameControllerName(this.Controller)) ?? "Unknown";

        this._joystickAxis = new Dictionary<SDL_GameControllerAxis, float>();
        
        this._controllerButtonsPressed = new List<SDL_GameControllerButton>();
        this._controllerButtonsDown = new List<SDL_GameControllerButton>();
        this._controllerButtonsReleased = new List<SDL_GameControllerButton>();
    }

    public void CleanStates() {
        this._controllerButtonsPressed.Clear();
        this._controllerButtonsReleased.Clear();
    }

    public float GetAxisMovement(GamepadAxis axis) {
        this._joystickAxis.TryGetValue((SDL_GameControllerAxis) axis, out float result);
        return result;
    }
    
    public bool IsButtonPressed(GamepadButton button) {
        return this._controllerButtonsPressed.Contains((SDL_GameControllerButton) button);
    }
    
    public bool IsButtonDown(GamepadButton button) {
        return this._controllerButtonsDown.Contains((SDL_GameControllerButton) button);
    }
    
    public bool IsButtonReleased(GamepadButton button) {
        return this._controllerButtonsReleased.Contains((SDL_GameControllerButton) button);
    }
    
    public bool IsButtonUp(GamepadButton button) {
        return !this._controllerButtonsDown.Contains((SDL_GameControllerButton) button);
    }
    
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
                        this._controllerButtonsPressed.Add(buttonEvent.button);
                        this._controllerButtonsDown.Add(buttonEvent.button);
                    }
                    else {
                        this._controllerButtonsDown.Remove(buttonEvent.button);
                        this._controllerButtonsReleased.Add(buttonEvent.button);
                    }
                }
                
                break;
        }
    }
    
    private float NormalizeJoystickAxis(short value) {
        return value < 0 ? -(value / (float) short.MinValue) : (value / (float) short.MaxValue);
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            Sdl2Native.SDL_GameControllerClose(this.Controller);
        }
    }
}