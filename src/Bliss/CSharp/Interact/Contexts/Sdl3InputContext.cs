using System.Numerics;
using Bliss.CSharp.Interact.Gamepads;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Interact.Mice;
using Bliss.CSharp.Interact.Mice.Cursors;
using Bliss.CSharp.Windowing;
using Bliss.CSharp.Windowing.Events;
using SDL;

namespace Bliss.CSharp.Interact.Contexts;

public class Sdl3InputContext : Disposable, IInputContext {
    
    /// <summary>
    /// Represents the window that this input context is associated with.
    /// </summary>
    private IWindow _window;
    
    /// <summary>
    /// Stores the change in mouse position since the last frame.
    /// </summary>
    private Vector2 _mouseDelta;
    
    /// <summary>
    /// Stores the latest position of the mouse movement.
    /// </summary>
    private Vector2 _mouseMoving;

    /// <summary>
    /// Stores the amount of mouse scrolling in the input context, represented as a vector indicating scroll direction and magnitude.
    /// </summary>
    private Vector2 _mouseScrolling;

    /// <summary>
    /// List of mouse buttons that were pressed during the current input context cycle.
    /// </summary>
    private List<MouseButton> _mouseButtonsPressed;

    /// <summary>
    /// Holds the current state of mouse buttons that are currently being pressed down.
    /// </summary>
    private List<MouseButton> _mouseButtonsDown;

    /// <summary>
    /// Holds the list of mouse buttons that have been released during the current frame.
    /// </summary>
    private List<MouseButton> _mouseButtonsReleased;

    /// <summary>
    /// Holds the current state of keys that are pressed on the keyboard.
    /// </summary>
    private List<KeyboardKey> _keyboardKeysPressed;
    
    /// <summary>
    /// Tracks the keys that are currently pressed down on the keyboard.
    /// </summary>
    private List<KeyboardKey> _keyboardKeysDown;

    /// <summary>
    /// A list of keyboard keys that have been released in the current input context.
    /// </summary>
    private List<KeyboardKey> _keyboardKeysReleased;

    /// <summary>
    /// A collection of gamepad devices currently connected and managed by the input context.
    /// </summary>
    private Dictionary<uint, IGamepad> _gamepads;
    
    /// <summary>
    /// Stores the text input received from the user during text input operations.
    /// </summary>
    private string _textFromTextInput;

    /// <summary>
    /// Stores information about the file that was dragged and dropped onto the window.
    /// The tuple includes the x and y coordinates where the file was dropped, and the path of the dropped file.
    /// </summary>
    private string _dragDroppedFile;

    /// <summary>
    /// Initializes a new instance of the <see cref="Sdl3InputContext"/> class for handling input events from a window.
    /// </summary>
    /// <param name="window">The window associated with the input context. Must be an SDL3 window.</param>
    /// <exception cref="Exception">Thrown if the provided window is not an SDL3 window.</exception>
    public Sdl3InputContext(IWindow window) {
        if (window is not Sdl3Window) {
            throw new Exception("You need a SDL3 window for the SDL3 input context!");
        }
        
        this._window = window;

        this._mouseButtonsPressed = new List<MouseButton>();
        this._mouseButtonsDown = new List<MouseButton>();
        this._mouseButtonsReleased = new List<MouseButton>();

        this._keyboardKeysPressed = new List<KeyboardKey>();
        this._keyboardKeysDown = new List<KeyboardKey>();
        this._keyboardKeysReleased = new List<KeyboardKey>();

        this._gamepads = new Dictionary<uint, IGamepad>();
        
        this._textFromTextInput = string.Empty;
        this._dragDroppedFile = string.Empty;
        
        this._window.MouseMove += this.OnMouseMove;
        this._window.MouseWheel += this.OnMouseWheel;
        this._window.MouseButtonDown += this.OnMouseDown;
        this._window.MouseButtonUp += this.OnMouseUp;
        
        this._window.KeyDown += this.OnKeyDown;
        this._window.KeyUp += this.OnKeyUp;
        this._window.TextInput += this.OnTextInput;

        this._window.GamepadAdded += this.OnGamepadAdded;
        this._window.GamepadRemoved += this.OnGamepadRemoved;

        this._window.DragDrop += this.OnFileDragDropped;
    }

    public void Begin() { } // TODO: REMOVE that! and replace it with ProccesInput or something...
    
    public unsafe void End() {
        float mouseDeltaX;
        float mouseDeltaY;
        
        SDL3.SDL_GetRelativeMouseState(&mouseDeltaX, &mouseDeltaY);
        this._mouseDelta = new Vector2(mouseDeltaX, mouseDeltaY);
        
        this._mouseMoving = Vector2.Zero;
        this._mouseScrolling = Vector2.Zero;
        this._mouseButtonsPressed.Clear();
        this._mouseButtonsReleased.Clear();
        
        this._keyboardKeysPressed.Clear();
        this._keyboardKeysReleased.Clear();
        
        foreach (IGamepad gamepad in this._gamepads.Values) {
            gamepad.CleanStates();
        }
        
        this._textFromTextInput = string.Empty;
        this._dragDroppedFile = string.Empty;
    }
    
    /* ------------------------------------ Mouse ------------------------------------ */

    public bool IsCursorShown() {
        return SDL3.SDL_CursorVisible();
    }

    public void ShowCursor() {
        SDL3.SDL_ShowCursor();
    }

    public void HideCursor() {
        SDL3.SDL_HideCursor();
    }

    public unsafe ICursor GetMouseCursor() {
        return new Sdl3Cursor(SDL3.SDL_GetCursor());
    }

    public unsafe void SetMouseCursor(ICursor cursor) {
        SDL3.SDL_SetCursor((SDL_Cursor*) cursor.GetHandle());
    }

    public unsafe bool IsRelativeMouseModeEnabled() {
        return SDL3.SDL_GetWindowRelativeMouseMode((SDL_Window*) this._window.Handle);
    }

    public unsafe void EnableRelativeMouseMode() {
        SDL3.SDL_SetWindowRelativeMouseMode((SDL_Window*) this._window.Handle, true);
    }

    public unsafe void DisableRelativeMouseMode() {
        SDL3.SDL_SetWindowRelativeMouseMode((SDL_Window*) this._window.Handle, false);
    }

    public unsafe Vector2 GetMousePosition() {
        float x;
        float y;

        SDL3.SDL_GetMouseState(&x, &y);
        return new Vector2(x, y);
    }

    public Vector2 GetMouseDelta() {
        return this._mouseDelta;
    }

    public unsafe void SetMousePosition(Vector2 position) {
        SDL3.SDL_WarpMouseInWindow((SDL_Window*) this._window.Handle, position.X, position.Y);
    }

    public bool IsMouseButtonPressed(MouseButton button) {
        return this._mouseButtonsPressed.Contains(button);
    }

    public bool IsMouseButtonDown(MouseButton button) {
        return this._mouseButtonsDown.Contains(button);
    }

    public bool IsMouseButtonReleased(MouseButton button) {
        return this._mouseButtonsReleased.Contains(button);
    }

    public bool IsMouseButtonUp(MouseButton button) {
        return !this._mouseButtonsDown.Contains(button);
    }

    public bool IsMouseMoving(out Vector2 position) {
        position = this._mouseMoving;
        return this._mouseMoving != Vector2.Zero;
    }

    public bool IsMouseScrolling(out Vector2 wheelDelta) {
        wheelDelta = this._mouseScrolling;
        return this._mouseScrolling != Vector2.Zero;
    }

    /* ------------------------------------ Keyboard ------------------------------------ */
    
    public bool IsKeyPressed(KeyboardKey key) {
        return this._keyboardKeysPressed.Contains(key);
    }

    public bool IsKeyDown(KeyboardKey key) {
        return this._keyboardKeysDown.Contains(key);
    }

    public bool IsKeyReleased(KeyboardKey key) {
        return this._keyboardKeysReleased.Contains(key);
    }

    public bool IsKeyUp(KeyboardKey key) {
        return !this._keyboardKeysDown.Contains(key);
    }
    
    public bool GetTypedText(out string text) {
        text = this._textFromTextInput;
        return text != string.Empty;
    }

    public unsafe bool IsTextInputActive() {
        return SDL3.SDL_TextInputActive((SDL_Window*) this._window.Handle);
    }
    
    public unsafe void StartTextInput() {
        SDL3.SDL_StartTextInput((SDL_Window*) this._window.Handle);
    }
    
    public unsafe void StopTextInput() {
        SDL3.SDL_StopTextInput((SDL_Window*) this._window.Handle);
    }
    
    public string GetClipboardText() {
        return SDL3.SDL_GetClipboardText() ?? string.Empty;
    }
    
    public void SetClipboardText(string text) {
        SDL3.SDL_SetClipboardText(text);
    }

    /* ------------------------------------ Gamepad ------------------------------------ */

    public uint GetAvailableGamepadCount() {
        return (uint) this._gamepads.Count;
    }
    
    public bool IsGamepadAvailable(uint gamepad) {
        return gamepad <= this._gamepads.Count - 1;
    }

    public string GetGamepadName(uint gamepad) {
        return this._gamepads[gamepad].GetName();
    }

    public unsafe void RumbleGamepad(uint gamepad, ushort lowFrequencyRumble, ushort highFrequencyRumble, uint durationMs) {
        SDL3.SDL_RumbleGamepad((SDL_Gamepad*) this._gamepads[gamepad].GetHandle(), lowFrequencyRumble, highFrequencyRumble, durationMs);
    }

    public float GetGamepadAxisMovement(uint gamepad, GamepadAxis axis) {
        return this._gamepads[gamepad].GetAxisMovement(axis);
    }
    
    public bool IsGamepadButtonPressed(uint gamepad, GamepadButton button) {
        return this._gamepads[gamepad].IsButtonPressed(button);
    }
    
    public bool IsGamepadButtonDown(uint gamepad, GamepadButton button) {
        return this._gamepads[gamepad].IsButtonDown(button);
    }
    
    public bool IsGamepadButtonReleased(uint gamepad, GamepadButton button) {
        return this._gamepads[gamepad].IsButtonReleased(button);
    }
    
    public bool IsGamepadButtonUp(uint gamepad, GamepadButton button) {
        return this._gamepads[gamepad].IsButtonUp(button);
    }

    /* ------------------------------------ Other ------------------------------------ */

    public bool IsFileDragDropped(out string path) {
        path = this._dragDroppedFile;
        return this._dragDroppedFile != string.Empty;
    }

    /* ------------------------------------ Mouse Events ------------------------------------ */

    /// <summary>
    /// Handles the mouse move event by updating the internal mouse position.
    /// </summary>
    /// <param name="position">The new position of the mouse cursor.</param>
    private void OnMouseMove(Vector2 position) {
        this._mouseMoving = position;
    }

    /// <summary>
    /// Handles the mouse wheel event by updating the mouse scrolling vector.
    /// </summary>
    /// <param name="wheelDelta">The vector indicating the amount of scrolling on the mouse wheel.</param>
    private void OnMouseWheel(Vector2 wheelDelta) {
        this._mouseScrolling = wheelDelta;
    }

    /// <summary>
    /// Handles the event when a mouse button is pressed.
    /// </summary>
    /// <param name="mouseEvent">The mouse event containing information about the button pressed and its state.</param>
    private void OnMouseDown(MouseEvent mouseEvent) {
        this._mouseButtonsPressed.Add(mouseEvent.Button);
        this._mouseButtonsDown.Add(mouseEvent.Button);
    }

    /// <summary>
    /// Handles the mouse button up event, updating the internal state of mouse buttons.
    /// </summary>
    /// <param name="mouseEvent">The event data for the mouse button up event, including which button was released.</param>
    private void OnMouseUp(MouseEvent mouseEvent) {
        this._mouseButtonsDown.Remove(mouseEvent.Button);
        this._mouseButtonsReleased.Add(mouseEvent.Button);
    }
    
    /* ------------------------------------ Keyboard Events ------------------------------------ */

    /// <summary>
    /// Handles the event when a key is pressed down.
    /// </summary>
    /// <param name="keyEvent">The key event containing information about the pressed key.</param>
    private void OnKeyDown(KeyEvent keyEvent) {
        if (!keyEvent.Repeat) {
            this._keyboardKeysPressed.Add(keyEvent.KeyboardKey);
            this._keyboardKeysDown.Add(keyEvent.KeyboardKey);
        }
    }

    /// <summary>
    /// Handles the event when a key is released.
    /// </summary>
    /// <param name="keyEvent">Contains details about the key event, including the key that was released.</param>
    private void OnKeyUp(KeyEvent keyEvent) {
        this._keyboardKeysDown.Remove(keyEvent.KeyboardKey);
        this._keyboardKeysReleased.Add(keyEvent.KeyboardKey);
    }


    /// <summary>
    /// Handles text input events by appending new input text to the existing buffer.
    /// </summary>
    /// <param name="text">The text input received from the user.</param>
    private void OnTextInput(string text) {
        this._textFromTextInput += text;
    }
    
    /* ------------------------------------ Gamepad Events ------------------------------------ */

    /// <summary>
    /// Handles the event when a gamepad is added to the system.
    /// </summary>
    /// <param name="which">The identifier of the newly added gamepad.</param>
    private void OnGamepadAdded(uint which) {
        Sdl3Gamepad gamepad = new Sdl3Gamepad(this._window, which);
        this._gamepads.Add(gamepad.GetIndex(), gamepad);
    }

    /// <summary>
    /// Handles the removal of a gamepad.
    /// </summary>
    /// <param name="which">The identifier of the gamepad that was removed.</param>
    private void OnGamepadRemoved(uint which) {
        Sdl3Gamepad gamepad = new Sdl3Gamepad(this._window, which);
        gamepad.Dispose();
        this._gamepads.Remove(which);
    }
    
    /* ------------------------------------ Other Events ------------------------------------ */
    
    /// <summary>
    /// Handles the event when a file is dragged and dropped onto the window.
    /// </summary>
    /// <param name="path">The file path of the dropped file.</param>
    private void OnFileDragDropped(string path) {
        this._dragDroppedFile = path;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._window.MouseMove -= this.OnMouseMove;
            this._window.MouseWheel -= this.OnMouseWheel;
            this._window.MouseButtonDown -= this.OnMouseDown;
            this._window.MouseButtonUp -= this.OnMouseUp;
        }
    }
}