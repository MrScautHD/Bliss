using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Windowing;
using MouseButton = Silk.NET.Input.MouseButton;

namespace Bliss.CSharp.Interact;

public static class Input {

    private static IInputContext _context;
    
    private static List<MouseButton> _mouseButtonsPressed;
    private static List<MouseButton> _mouseButtonsDown;
    private static List<MouseButton> _mouseButtonsReleased;
    private static Dictionary<MouseButton, Vector2> _mousesClicked;
    private static Dictionary<MouseButton, Vector2> _mousesDoubleClicked;
    private static List<Vector2> _mousesMoving;
    private static List<ScrollWheel> _mousesSrolling;
    
    private static List<Key> _keyboardKeysPressed;
    private static List<Key> _keyboardKeysDown;
    private static List<Key> _keyboardKeysReleased;
    private static List<char> _KeyboardCharsPressed;

    private static List<ButtonName> _gamepadButtonsPressed;
    private static List<ButtonName> _gamepadButtonsDown;
    private static List<ButtonName> _gamepadButtonsReleased;
    private static List<int> _gamepadThumbsticksMoved;
    private static List<int> _gamepadTriggersMoved;
    
    private static List<ButtonName> _joystickButtonsPressed;
    private static List<ButtonName> _joystickButtonsDown;
    private static List<ButtonName> _joystickButtonsReleased;
    private static List<int> _joystickAxisMoved;
    private static List<int> _joystickHatsMoved;

    /// <summary>
    /// Initializes the input system for the given window.
    /// </summary>
    /// <param name="window">The window to initialize input for.</param>
    public static void Init(IWindow window) {
        _mouseButtonsPressed = new List<MouseButton>();
        _mouseButtonsDown = new List<MouseButton>();
        _mouseButtonsReleased = new List<MouseButton>();
        _mousesClicked = new Dictionary<MouseButton, Vector2>();
        _mousesDoubleClicked = new Dictionary<MouseButton, Vector2>();
        _mousesMoving = new List<Vector2>();
        _mousesSrolling = new List<ScrollWheel>();

        _keyboardKeysPressed = new List<Key>();
        _keyboardKeysDown = new List<Key>();
        _keyboardKeysReleased = new List<Key>();
        _KeyboardCharsPressed = new List<char>();
        
        _gamepadButtonsPressed = new List<ButtonName>();
        _gamepadButtonsDown = new List<ButtonName>();
        _gamepadButtonsReleased = new List<ButtonName>();
        _gamepadThumbsticksMoved = new List<int>();
        _gamepadTriggersMoved = new List<int>();
        
        _joystickButtonsPressed = new List<ButtonName>();
        _joystickButtonsDown = new List<ButtonName>();
        _joystickButtonsReleased = new List<ButtonName>();
        _joystickAxisMoved = new List<int>();
        _joystickHatsMoved = new List<int>();
        
        _context = window.CreateInput();
        _context.ConnectionChanged += DoConnect;
        
        foreach (IMouse mouse in _context.Mice) {
            if (mouse.IsConnected) {
                DoConnect(mouse, true);
            }
        }

        foreach (IKeyboard keyboard in _context.Keyboards) {
            if (keyboard.IsConnected) {
                DoConnect(keyboard, true);
            }
        }
        
        foreach (IGamepad gamepad in _context.Gamepads) {
            if (gamepad.IsConnected) {
                DoConnect(gamepad, true);
            }
        }
        
        foreach (IJoystick joystick in _context.Joysticks) {
            if (joystick.IsConnected) {
                DoConnect(joystick, true);
            }
        }
    }

    /// <summary>
    /// Begins input capture for the active window.
    /// </summary>
    public static void BeginInput() {
        _context.Keyboards[0].BeginInput();
    }

    /// <summary>
    /// Ends the input capture for the active window.
    /// </summary>
    public static void EndInput() {
        _context.Keyboards[0].EndInput();
        
        _mouseButtonsPressed.Clear();
        _mouseButtonsReleased.Clear();
        _mousesClicked.Clear();
        _mousesDoubleClicked.Clear();
        _mousesMoving.Clear();
        _mousesSrolling.Clear();
        
        _keyboardKeysPressed.Clear();
        _keyboardKeysReleased.Clear();
        _KeyboardCharsPressed.Clear();
        
        _gamepadButtonsPressed.Clear();
        _gamepadButtonsReleased.Clear();
        _gamepadThumbsticksMoved.Clear();
        _gamepadTriggersMoved.Clear();
        
        _joystickButtonsPressed.Clear();
        _joystickButtonsReleased.Clear();
        _joystickAxisMoved.Clear();
        _joystickHatsMoved.Clear();
    }
    
    /* ------------------------------------ Mouse ------------------------------------ */

    /// <summary>
    /// Retrieves a list of connected mice.
    /// </summary>
    /// <returns>A read-only list of IMouse objects representing the connected mice.</returns>
    public static IReadOnlyList<IMouse> GetMice() {
        return _context.Mice;
    }
    
    /// <summary>
    /// Gets the list of mouse buttons supported by the specified mouse.
    /// </summary>
    /// <param name="mouse">The index of the mouse.</param>
    /// <returns>The list of mouse buttons supported by the specified mouse.</returns>
    public static IReadOnlyList<MouseButton> GetMouseSupportedButtons(int mouse) {
        return _context.Mice[mouse].SupportedButtons;
    }

    /// <summary>
    /// Retrieves a list of supported scroll wheels for the specified mouse.
    /// </summary>
    /// <param name="mouse">The index of the mouse to retrieve scroll wheels for.</param>
    /// <returns>A readonly list of supported scroll wheels for the specified mouse.</returns>
    public static IReadOnlyList<ScrollWheel> GetMouseScrollWheels(int mouse) {
        return _context.Mice[mouse].ScrollWheels;
    }

    /// <summary>
    /// Gets the position of the mouse on the screen.
    /// </summary>
    /// <param name="mouse">The ID of the mouse to get the position from.</param>
    /// <returns>
    /// The position of the mouse as a Vector2 object.
    /// </returns>
    public static Vector2 GetMousePosition(int mouse) {
        return _context.Mice[mouse].Position;
    }

    /// <summary>
    /// Sets the position of the mouse for the specified mouse.
    /// </summary>
    /// <param name="mouse">The index of the mouse.</param>
    /// <param name="pos">The new position of the mouse.</param>
    public static void SetMousePosition(int mouse, Vector2 pos) {
        _context.Mice[mouse].Position = pos;
    }

    /// <summary>
    /// Retrieves the cursor associated with the specified mouse.
    /// </summary>
    /// <param name="mouse">The index of the mouse.</param>
    /// <returns>The cursor associated with the specified mouse.</returns>
    public static ICursor GetMouseCursor(int mouse) {
        return _context.Mice[mouse].Cursor;
    }

    /// <summary>
    /// Gets the double-click time for the specified mouse.
    /// </summary>
    /// <param name="mouse">The index of the mouse.</param>
    /// <returns>The double-click time in milliseconds.</returns>
    public static int GetMouseDoubleClickTime(int mouse) {
        return _context.Mice[mouse].DoubleClickTime;
    }

    /// <summary>
    /// Sets the double click time for the specified mouse.
    /// </summary>
    /// <param name="mouse">The index of the mouse.</param>
    /// <param name="time">The double click time in milliseconds.</param>
    public static void SetMouseDoubleClickTime(int mouse, int time) {
        _context.Mice[mouse].DoubleClickTime = time;
    }

    /// <summary>
    /// Returns the double click range for the specified mouse.
    /// </summary>
    /// <param name="mouse">The index of the mouse.</param>
    /// <returns>The double click range for the specified mouse.</returns>
    public static int GetMouseDoubleClickRange(int mouse) {
        return _context.Mice[mouse].DoubleClickRange;
    }

    /// <summary>
    /// Sets the double-click range for a specific mouse.
    /// </summary>
    /// <param name="mouse">The index of the mouse.</param>
    /// <param name="range">The double-click range to set.</param>
    public static void SetMouseDoubleClickRange(int mouse, int range) {
        _context.Mice[mouse].DoubleClickRange = range;
    }

    /// <summary>
    /// Checks if the specified mouse button is currently pressed.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the mouse button is currently pressed; otherwise, false.</returns>
    public static bool IsMouseButtonPressed(MouseButton button) {
        return _mouseButtonsPressed.Contains(button);
    }

    /// <summary>
    /// Checks if the specified mouse button is currently down.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>Returns true if the mouse button is down, otherwise false.</returns>
    public static bool IsMouseButtonDown(MouseButton button) {
        return _mouseButtonsDown.Contains(button);
    }

    /// <summary>
    /// Checks if the specified mouse button was released.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the specified mouse button was released; otherwise, false.</returns>
    public static bool IsMouseButtonReleased(MouseButton button) {
        return _mouseButtonsReleased.Contains(button);
    }

    /// <summary>
    /// Check if a mouse button is released.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the specified mouse button is released, otherwise false.</returns>
    public static bool IsMouseButtonUp(MouseButton button) {
        return !_mouseButtonsDown.Contains(button);
    }

    /// <summary>
    /// Checks if the specified mouse button has been clicked and provides the position of the click.
    /// </summary>
    /// <param name="button">The mouse button to check for click.</param>
    /// <param name="pos">The position of the click, if the button was clicked.</param>
    /// <returns>True if the mouse button has been clicked, otherwise false.</returns>
    public static bool IsMouseClicked(MouseButton button, out Vector2 pos) {
        if (_mousesClicked.ContainsKey(button)) {
            pos = _mousesClicked[button];
            return true;
        }
        
        pos = Vector2.Zero;
        return false;
    }

    /// <summary>
    /// Checks if the specified mouse button was double-clicked and retrieves the position of the click.
    /// </summary>
    /// <param name="button">The mouse button to check for double-click.</param>
    /// <param name="pos">The position of the click if double-clicked, otherwise set to Vector2.Zero.</param>
    /// <returns>true if the mouse button was double-clicked, false otherwise.</returns>
    public static bool IsMouseDoubleClicked(MouseButton button, out Vector2 pos) {
        if (_mousesDoubleClicked.ContainsKey(button)) {
            pos = _mousesDoubleClicked[button];
            return true;
        }
        
        pos = Vector2.Zero;
        return false;
    }

    /// <summary>
    /// Checks if the mouse is currently moving.
    /// </summary>
    /// <param name="pos">The current position of the mouse.</param>
    /// <returns>True if the mouse is moving, false otherwise.</returns>
    public static bool IsMouseMoving(out Vector2 pos) {
        if (_mousesMoving.Count > 0) {
            pos = _mousesMoving[0];
            return true;
        }

        pos = Vector2.Zero;
        return false;
    }

    /// <summary>
    /// Checks if the mouse is currently scrolling.
    /// </summary>
    /// <param name="scrollWheel">The scroll wheel data if scrolling, null otherwise.</param>
    /// <returns>True if the mouse is scrolling, false otherwise.</returns>
    public static bool IsMouseScrolling(out ScrollWheel? scrollWheel) {
        if (_mousesSrolling.Count > 0) {
            scrollWheel = _mousesSrolling[0];
            return true;
        }

        scrollWheel = null;
        return false;
    }
    
    /* ------------------------------------ Keyboard ------------------------------------ */

    /// <summary>
    /// Retrieves the list of keyboards connected to the input context.
    /// </summary>
    /// <returns>The list of keyboards connected to the input context.</returns>
    public static IReadOnlyList<IKeyboard> GetKeyboards() {
        return _context.Keyboards;
    }

    /// <summary>
    /// Returns a read-only list of the supported keyboard keys.
    /// </summary>
    /// <returns>A read-only list of the supported keyboard keys.</returns>
    public static IReadOnlyList<Key> GetSupportedKeys(int keyboard) {
        return _context.Keyboards[keyboard].SupportedKeys;
    }

    /// <summary>
    /// Returns the text currently stored in the clipboard.
    /// </summary>
    /// <returns>The text currently stored in the clipboard, or an empty string if there is no text.</returns>
    public static string GetClipboardText(int keyboard) {
        return _context.Keyboards[keyboard].ClipboardText;
    }

    /// <summary>
    /// Sets the text content of the clipboard for the specified keyboard.
    /// </summary>
    /// <param name="keyboard">The index of the keyboard.</param>
    /// <param name="text">The text to set as clipboard content.</param>
    public static void SetClipboardText(int keyboard, string text) {
        _context.Keyboards[keyboard].ClipboardText = text;
    }

    /// <summary>
    /// Checks whether the specified keyboard key is currently pressed.
    /// </summary>
    /// <param name="key">The keyboard key to check.</param>
    /// <returns>True if the specified keyboard key is currently pressed, otherwise false.</returns>
    public static bool IsKeyPressed(Key key) {
        return _keyboardKeysPressed.Contains(key);
    }

    /// <summary>
    /// Checks if the given scancode is currently being pressed down.
    /// </summary>
    /// <param name="scancode">The scancode to check.</param>
    /// <returns>True if the scancode is currently being pressed down, otherwise false.</returns>
    public static bool IsScancodeDown(int scancode) {
        foreach (IKeyboard keyboard in _context.Keyboards) {
            return keyboard.IsScancodePressed(scancode);
        }

        return false;
    }

    /// <summary>
    /// Checks whether the specified keyboard key is currently down.
    /// </summary>
    /// <param name="key">The keyboard key to check.</param>
    /// <returns>True if the specified keyboard key is currently down, otherwise false.</returns>
    public static bool IsKeyDown(Key key) {
        return _keyboardKeysDown.Contains(key);
    }

    /// <summary>
    /// Checks if a given key is released.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key is released, false otherwise.</returns>
    public static bool IsKeyReleased(Key key) {
        return _keyboardKeysReleased.Contains(key);
    }

    /// <summary>
    /// Checks whether the specified keyboard key is currently up (not pressed).
    /// </summary>
    /// <param name="key">The keyboard key to check.</param>
    /// <returns>True if the specified keyboard key is currently up (not pressed), otherwise false.</returns>
    public static bool IsKeyUp(Key key) {
        return !_keyboardKeysDown.Contains(key);
    }

    /// <summary>
    /// Returns an array of characters representing the keys currently pressed on the keyboard.
    /// </summary>
    /// <returns>An array of characters representing the keys currently pressed on the keyboard.</returns>
    public static char[] GetPressedChars() {
        return _KeyboardCharsPressed.ToArray();
    }
    
    /* ------------------------------------ Gamepad ------------------------------------ */

    /// <summary>
    /// Retrieves a list of connected gamepads.
    /// </summary>
    /// <returns>A read-only list of IGamepad objects representing the connected gamepads.</returns>
    public static IReadOnlyList<IGamepad> GetGamepads() {
        return _context.Gamepads;
    }

    /// <summary>
    /// Gets the list of buttons on the specified gamepad.
    /// </summary>
    /// <param name="gamepad">The index of the gamepad.</param>
    /// <returns>The list of buttons on the specified gamepad.</returns>
    public static IReadOnlyList<Button> GetGamepadButtons(int gamepad) {
        return _context.Gamepads[gamepad].Buttons;
    }

    /// <summary>
    /// Gets the thumbstick data for a specific gamepad.
    /// </summary>
    /// <param name="gamepad">The index of the gamepad.</param>
    /// <returns>The list of thumbsticks for the specified gamepad.</returns>
    public static IReadOnlyList<Thumbstick> GetGamepadThumbsticks(int gamepad) {
        return _context.Gamepads[gamepad].Thumbsticks;
    }

    /// <summary>
    /// Gets the triggers of a gamepad.
    /// </summary>
    /// <param name="gamepad">The index of the gamepad.</param>
    /// <returns>The triggers of the gamepad.</returns>
    public static IReadOnlyList<Trigger> GetGamepadTriggers(int gamepad) {
        return _context.Gamepads[gamepad].Triggers;
    }

    /// <summary>
    /// Gets the list of vibration motors available for a specific gamepad.
    /// </summary>
    /// <param name="gamepad">The index of the gamepad to retrieve the vibration motors for.</param>
    /// <returns>The list of vibration motors for the specified gamepad.</returns>
    public static IReadOnlyList<IMotor> GetGamepadVibrationMotors(int gamepad) {
        return _context.Gamepads[gamepad].VibrationMotors;
    }

    /// <summary>
    /// Retrieves the deadzone value for the specified gamepad.
    /// </summary>
    /// <param name="gamepad">The index of the gamepad.</param>
    /// <returns>The deadzone value for the gamepad.</returns>
    public static Deadzone GetGamepadDeadzone(int gamepad) {
        return _context.Gamepads[gamepad].Deadzone;
    }

    /// <summary>
    /// Checks if the specified gamepad button is currently pressed.
    /// </summary>
    /// <param name="button">The gamepad button to check.</param>
    /// <returns>True if the gamepad button is pressed, false otherwise.</returns>
    public static bool IsGamepadButtonPressed(ButtonName button) {
        return _gamepadButtonsPressed.Contains(button);
    }

    /// <summary>
    /// Checks if the specified gamepad button is currently down.
    /// </summary>
    /// <param name="button">The gamepad button to check.</param>
    /// <returns>True if the gamepad button is down, otherwise false.</returns>
    public static bool IsGamepadButtonDown(ButtonName button) {
        return _gamepadButtonsDown.Contains(button);
    }

    /// <summary>
    /// Determines if the specified gamepad button has been released.
    /// </summary>
    /// <param name="button">The gamepad button to check.</param>
    /// <returns>True if the button has been released; otherwise, false.</returns>
    public static bool IsGamepadButtonReleased(ButtonName button) {
        return _gamepadButtonsReleased.Contains(button);
    }

    /// <summary>
    /// Determines whether the specified gamepad button is up (not currently pressed down).
    /// </summary>
    /// <param name="button">The gamepad button to check.</param>
    /// <returns>Returns true if the button is up, otherwise false.</returns>
    public static bool IsGamepadButtonUp(ButtonName button) {
        return !_gamepadButtonsDown.Contains(button);
    }

    /// <summary>
    /// Checks if the specified gamepad thumbstick is moved.
    /// </summary>
    /// <param name="thumbstick">The index of the gamepad thumbstick to check.</param>
    /// <returns>True if the specified gamepad thumbstick is moved; otherwise, false.</returns>
    public static bool IsGamepadThumpStickMoved(int thumbstick) {
        return _gamepadThumbsticksMoved.Contains(thumbstick);
    }

    /// <summary>
    /// Determines if the trigger on the specified gamepad has moved.
    /// </summary>
    /// <param name="trigger">The index of the gamepad trigger.</param>
    /// <returns>True if the gamepad trigger has moved; otherwise, false.</returns>
    public static bool IsGamepadTriggerMoved(int trigger) {
        return _gamepadTriggersMoved.Contains(trigger);
    }
    
    /* ------------------------------------ Joystick ------------------------------------ */

    /// <summary>
    /// Retrieves a list of available joysticks.
    /// </summary>
    /// <returns>A read-only list of IJoystick objects representing the available joysticks.</returns>
    public static IReadOnlyList<IJoystick> GetJoysticks() {
        return _context.Joysticks;
    }

    /// <summary>
    /// Retrieves the axes values of a specific joystick.
    /// </summary>
    /// <param name="joystick">The index of the joystick to retrieve axes values from.</param>
    /// <returns>A read-only list of Axis values for the specified joystick.</returns>
    public static IReadOnlyList<Axis> GetJoystickAxes(int joystick) {
        return _context.Joysticks[joystick].Axes;
    }

    /// <summary>
    /// Retrieves the currently pressed buttons of the specified joystick.
    /// </summary>
    /// <param name="joystick">The index of the joystick to get buttons from.</param>
    /// <returns>The list of buttons currently pressed on the specified joystick.</returns>
    public static IReadOnlyList<Button> GetJoystickButtons(int joystick) {
        return _context.Joysticks[joystick].Buttons;
    }

    /// <summary>
    /// Gets the list of hats on the specified joystick.
    /// </summary>
    /// <param name="joystick">The index of the joystick to get the hats from.</param>
    /// <returns>The list of hats on the specified joystick.</returns>
    public static IReadOnlyList<Hat> GetJoystickHats(int joystick) {
        return _context.Joysticks[joystick].Hats;
    }

    /// <summary>
    /// Gets the deadzone value for the specified joystick.
    /// </summary>
    /// <param name="joystick">The index of the joystick.</param>
    /// <returns>The deadzone value for the specified joystick.</returns>
    public static Deadzone GetJoystickDeadzone(int joystick) {
        return _context.Joysticks[joystick].Deadzone;
    }

    /// Determines whether the specified joystick button is currently pressed.
    /// @param button The button to check.
    /// @return True if the button is pressed; otherwise, false.
    /// /
    public static bool IsJoystickButtonPressed(ButtonName button) {
        return _joystickButtonsPressed.Contains(button);
    }

    /// <summary>
    /// Checks if a joystick button is currently down.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns>Returns true if the joystick button is currently down, otherwise false.</returns>
    public static bool IsJoystickButtonDown(ButtonName button) {
        return _joystickButtonsDown.Contains(button);
    }

    /// <summary>
    /// Checks if a joystick button has been released.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns>True if the button has been released, otherwise false.</returns>
    public static bool IsJoystickButtonReleased(ButtonName button) {
        return _joystickButtonsReleased.Contains(button);
    }

    /// <summary>
    /// Determines if the specified joystick button is in the up state.
    /// </summary>
    /// <param name="button">The joystick button to check.</param>
    /// <returns>
    /// <c>true</c> if the joystick button is in the up state; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsJoystickButtonUp(ButtonName button) {
        return !_joystickButtonsDown.Contains(button);
    }

    /// <summary>
    /// Checks if the specified joystick axis has been moved.
    /// </summary>
    /// <param name="axis">The axis to check.</param>
    /// <returns>True if the joystick axis has been moved; otherwise, false.</returns>
    public static bool IsJoystickAxisMoved(int axis) {
        return !_joystickAxisMoved.Contains(axis);
    }

    /// <summary>
    /// Determines whether a joystick hat has been moved.
    /// </summary>
    /// <param name="hat">The ID of the joystick hat to check.</param>
    /// <returns>True if the joystick hat has been moved; otherwise, false.</returns>
    public static bool IsJoystickHatMoved(int hat) {
        return !_joystickHatsMoved.Contains(hat);
    }
    
    /* ------------------------------------ Other Devices ------------------------------------ */

    /// <summary>
    /// Retrieves a list of other input devices that are not mice, keyboards, gamepads, or joysticks.
    /// </summary>
    /// <returns>A read-only list of other input devices.</returns>
    public static IReadOnlyList<IInputDevice> GetOtherDevices() {
        return _context.OtherDevices;
    }

    /// <summary>
    /// Handles connecting and disconnecting input devices.
    /// </summary>
    /// <param name="device">The input device (mouse, keyboard, gamepad, joystick).</param>
    /// <param name="isConnected">True if the device is being connected, false if it is being disconnected.</param>
    private static void DoConnect(IInputDevice device, bool isConnected) {
        if (device is IMouse mouse) {
            if (isConnected) {
                mouse.MouseDown += OnMouseButtonDown;
                mouse.MouseUp += OnMouseButtonUp;
                mouse.Click += OnMouseClick;
                mouse.DoubleClick += OnMouseDoubleClick;
                mouse.MouseMove += OnMouseMoving;
                mouse.Scroll += OnMouseScrolling;
            }
            else {
                mouse.MouseDown -= OnMouseButtonDown;
                mouse.MouseUp -= OnMouseButtonUp;
                mouse.Click -= OnMouseClick;
                mouse.DoubleClick -= OnMouseDoubleClick;
                mouse.MouseMove -= OnMouseMoving;
                mouse.Scroll -= OnMouseScrolling;
            }
        }
        
        if (device is IKeyboard keyboard) {
            if (isConnected) {
                keyboard.KeyDown += OnKeyboardKeyDown;
                keyboard.KeyUp += OnKeyboardKeyUp;
                keyboard.KeyChar += OnKeyboardChar;
            }
            else {
                keyboard.KeyDown -= OnKeyboardKeyDown;
                keyboard.KeyUp -= OnKeyboardKeyUp;
                keyboard.KeyChar -= OnKeyboardChar;
            }
        }
        
        if (device is IGamepad gamepad) {
            if (isConnected) {
                gamepad.ButtonDown += OnGamepadButtonDown;
                gamepad.ButtonUp += OnGamepadButtonUp;
                gamepad.ThumbstickMoved += OnGamepadThumbstickMoved;
                gamepad.TriggerMoved += OnGamepadTriggerMoved;
            }
            else {
                gamepad.ButtonDown -= OnGamepadButtonDown;
                gamepad.ButtonUp -= OnGamepadButtonUp;
                gamepad.ThumbstickMoved -= OnGamepadThumbstickMoved;
                gamepad.TriggerMoved -= OnGamepadTriggerMoved;
            }
        }
        
        if (device is IJoystick joystick) {
            if (isConnected) {
                joystick.ButtonDown += OnJoystickButtonDown;
                joystick.ButtonUp += OnJoystickButtonUp;
                joystick.AxisMoved += OnJoystickAxisMoved;
                joystick.HatMoved += OnJoystickHatMoved;
            }
            else {
                joystick.ButtonDown -= OnJoystickButtonDown;
                joystick.ButtonUp -= OnJoystickButtonUp;
                joystick.AxisMoved -= OnJoystickAxisMoved;
                joystick.HatMoved -= OnJoystickHatMoved;
            }
        }
    }
        
    /* ------------------------------------ Mouse ------------------------------------ */
    
    private static void OnMouseButtonDown(IMouse mouse, MouseButton button) {
        _mouseButtonsPressed.Add(button);
        _mouseButtonsDown.Add(button);
    }
    
    private static void OnMouseButtonUp(IMouse mouse, MouseButton button) {
        _mouseButtonsDown.Remove(button);
        _mouseButtonsReleased.Add(button);
    }
    
    private static void OnMouseClick(IMouse mouse, MouseButton button, Vector2 pos) {
        _mousesClicked.Add(button, pos);
    }
    
    private static void OnMouseDoubleClick(IMouse mouse, MouseButton button, Vector2 pos) {
        _mousesDoubleClicked.Add(button, pos);
    }
    
    private static void OnMouseMoving(IMouse mouse, Vector2 pos) {
        _mousesMoving.Add(pos);
    }
    
    private static void OnMouseScrolling(IMouse mouse, ScrollWheel scrollWheel) {
        _mousesSrolling.Add(scrollWheel);
    }
    
    /* ------------------------------------ Keyboard ------------------------------------ */

    private static void OnKeyboardKeyDown(IKeyboard keyboard, Key key, int scancode) {
        _keyboardKeysPressed.Add(key);
        _keyboardKeysDown.Add(key);
    }
    
    private static void OnKeyboardKeyUp(IKeyboard keyboard, Key key, int scancode) {
        _keyboardKeysDown.Remove(key);
        _keyboardKeysReleased.Add(key);
    }
    
    private static void OnKeyboardChar(IKeyboard keyboard, char scancode) {
        _KeyboardCharsPressed.Add(scancode);
    }
    
    /* ------------------------------------ Gamepad ------------------------------------ */
    
    private static void OnGamepadButtonDown(IGamepad gamepad, Button button) {
        _gamepadButtonsPressed.Add(button.Name);
        _gamepadButtonsDown.Add(button.Name);
    }
    
    private static void OnGamepadButtonUp(IGamepad gamepad, Button button) {
        _gamepadButtonsDown.Remove(button.Name);
        _gamepadButtonsReleased.Add(button.Name);
    }
    
    private static void OnGamepadThumbstickMoved(IGamepad gamepad, Thumbstick thumbstick) {
        _gamepadThumbsticksMoved.Add(thumbstick.Index);
    }
    
    private static void OnGamepadTriggerMoved(IGamepad gamepad, Trigger trigger) {
        _gamepadTriggersMoved.Add(trigger.Index);
    }
    
    /* ------------------------------------ Joystick ------------------------------------ */
    
    private static void OnJoystickButtonDown(IJoystick joystick, Button button) {
        _joystickButtonsPressed.Add(button.Name);
        _joystickButtonsDown.Add(button.Name);
    }
    
    private static void OnJoystickButtonUp(IJoystick joystick, Button button) {
        _joystickButtonsDown.Remove(button.Name);
        _joystickButtonsReleased.Add(button.Name);
    }
    
    private static void OnJoystickAxisMoved(IJoystick joystick, Axis axis) {
        _joystickAxisMoved.Add(axis.Index);
    }
    
    private static void OnJoystickHatMoved(IJoystick joystick, Hat hat) {
        _joystickHatsMoved.Add(hat.Index);
    }
}