using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Bliss.CSharp.Interact;

public static class Input {
    
    private static IKeyboard? _keyboard;
    private static IGamepad? _gamepad;
    private static IJoystick? _joystick;
    private static IMouse? _mouse;
        
    private static List<Key> _keyboardKeysPressed;
    private static List<Button> _gamepadButtonsPressed;
    private static List<Button> _joystickButtonsPressed;
    private static List<MouseButton> _mouseButtonsPressed;

    private static List<Key> _keyboardKeysDown;
    private static List<Button> _gamepadButtonsDown;
    private static List<Button> _joystickButtonsDown;
    private static List<MouseButton> _mouseButtonsDown;
    
    private static List<Key> _keyboardKeysReleased;
    private static List<Button> _gamepadButtonsReleased;
    private static List<Button> _joystickButtonsReleased;
    private static List<MouseButton> _mouseButtonsReleased;

    /// <summary>
    /// Initializes the input system for the given window.
    /// </summary>
    /// <param name="window">The window to initialize input for.</param>
    public static void Init(IWindow window) {
        _keyboardKeysPressed = new List<Key>();
        _gamepadButtonsPressed = new List<Button>();
        _joystickButtonsPressed = new List<Button>();
        _mouseButtonsPressed = new List<MouseButton>();
        
        _keyboardKeysDown = new List<Key>();
        _gamepadButtonsDown = new List<Button>();
        _joystickButtonsDown = new List<Button>();
        _mouseButtonsDown = new List<MouseButton>();
        
        _keyboardKeysReleased = new List<Key>();
        _gamepadButtonsReleased = new List<Button>();
        _joystickButtonsReleased = new List<Button>();
        _mouseButtonsReleased = new List<MouseButton>();
        
        IInputContext context = window.CreateInput();
        context.ConnectionChanged += DoConnect;

        foreach (IKeyboard keyboard in context.Keyboards) {
            if (keyboard.IsConnected) {
                DoConnect(keyboard, true);
            }
        }
        
        foreach (IGamepad gamepad in context.Gamepads) {
            if (gamepad.IsConnected) {
                DoConnect(gamepad, true);
            }
        }
        
        foreach (IJoystick joystick in context.Joysticks) {
            if (joystick.IsConnected) {
                DoConnect(joystick, true);
            }
        }
        
        foreach (IMouse mouse in context.Mice) {
            if (mouse.IsConnected) {
                DoConnect(mouse, true);
            }
        }
    }

    /// <summary>
    /// Begins input capture for the active window.
    /// </summary>
    public static void BeginInput() {
        _keyboard?.BeginInput();
    }

    /// <summary>
    /// Ends the input capture for the active window.
    /// </summary>
    public static void EndInput() {
        _keyboard?.EndInput();
        
        _keyboardKeysPressed.Clear();
        _gamepadButtonsPressed.Clear();
        _joystickButtonsPressed.Clear();
        _mouseButtonsPressed.Clear();
        
        _keyboardKeysReleased.Clear();
        _gamepadButtonsReleased.Clear();
        _joystickButtonsReleased.Clear();
        _mouseButtonsReleased.Clear();
    }
    
    /* ------------------------------------ Keyboard ------------------------------------ */

    /// <summary>
    /// Returns a read-only list of the supported keyboard keys.
    /// </summary>
    /// <returns>A read-only list of the supported keyboard keys.</returns>
    public static IReadOnlyList<Key> GetSupportedKeys() {
        return _keyboard?.SupportedKeys ?? new List<Key>();
    }

    /// <summary>
    /// Returns the text currently stored in the clipboard.
    /// </summary>
    /// <returns>The text currently stored in the clipboard, or an empty string if there is no text.</returns>
    public static string GetClipboardText() {
        return _keyboard?.ClipboardText ?? string.Empty;
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
    /// Checks whether the specified keyboard scancode is currently pressed.
    /// </summary>
    /// <param name="scancode">The scancode to check.</param>
    /// <returns>True if the specified scancode is currently pressed, otherwise false.</returns>
    public static bool IsScancodePressed(int scancode) {
        return _keyboard?.IsScancodePressed(scancode) ?? false;
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
    
    /* ------------------------------------ Gamepad ------------------------------------ */
    
    public static IReadOnlyList<Button> GetGamepadButtons() {
        return _gamepad?.Buttons ?? new List<Button>();
    }
    
    public static IReadOnlyList<Thumbstick> GetGamepadThumbsticks() {
        return _gamepad?.Thumbsticks ?? new List<Thumbstick>();
    }
    
    public static IReadOnlyList<Trigger> GetGamepadTriggers() {
        return _gamepad?.Triggers ?? new List<Trigger>();
    }
    
    public static IReadOnlyList<IMotor> GetGamepadVibrationMotors() {
        return _gamepad?.VibrationMotors ?? new List<IMotor>();
    }
    
    public static Deadzone? GetGamepadDeadzone() {
        return _gamepad?.Deadzone;
    }
    
    public static bool IsGamepadButtonPressed(Button button) {
        return _gamepadButtonsPressed.Contains(button);
    }
    
    public static bool IsGamepadButtonDown(Button button) {
        return _gamepadButtonsDown.Contains(button);
    }
    
    public static bool IsGamepadButtonReleased(Button button) {
        return _gamepadButtonsReleased.Contains(button);
    }
    
    public static bool IsGamepadButtonUp(Button button) {
        return !_gamepadButtonsDown.Contains(button);
    }
    
    private static void DoConnect(IInputDevice device, bool isConnected) {
        if (device is IKeyboard keyboard) {
            if (isConnected) {
                _keyboard = keyboard;
                
                keyboard.KeyDown += OnKeyboardKeyDown;
                keyboard.KeyUp += OnKeyboardKeUp;
                //keyboard.KeyChar += (keyboard1, c) => _keyboardKeysDown.Add();
            }
            else {
                keyboard.KeyDown -= OnKeyboardKeyDown;
                keyboard.KeyUp -= OnKeyboardKeUp;
                //keyboard.KeyChar -= ;
                _keyboard = null;
            }
        }
        
        if (device is IGamepad gamepad) {
            if (isConnected) {
                _gamepad = gamepad;
                gamepad.ButtonDown += OnGamepadButtonDown;
                gamepad.ButtonUp += OnGamepadButtonUp;
            }
            else {
                gamepad.ButtonDown -= OnGamepadButtonDown;
                gamepad.ButtonUp -= OnGamepadButtonUp;
                _gamepad = null;
            }
        }
        
        if (device is IJoystick joystick) {
            if (isConnected) {
                _joystick = joystick;
                joystick.ButtonDown += OnJoystickButtonDown;
                joystick.ButtonUp += OnJoystickButtonUp;
            }
            else {
                joystick.ButtonDown -= OnJoystickButtonDown;
                joystick.ButtonUp -= OnJoystickButtonUp;
                _joystick = null;
            }
        }
        
        if (device is IMouse mouse) {
            if (isConnected) {
                _mouse = mouse;
                mouse.MouseDown += OnMouseButtonDown;
                mouse.MouseUp += OnMouseButtonUp;
                //mouse.MouseMove += ;
            }
            else {
                mouse.MouseDown -= OnMouseButtonDown;
                mouse.MouseUp -= OnMouseButtonUp;
                //mouse.MouseMove -= ;
                _mouse = null;
            }
        }
    }

    private static void OnKeyboardKeyDown(IKeyboard keyboard, Key key, int scancode) {
        _keyboardKeysPressed.Add(key);
        _keyboardKeysDown.Add(key);
    }
    
    private static void OnKeyboardKeUp(IKeyboard keyboard, Key key, int scancode) {
        _keyboardKeysDown.Remove(key);
        _keyboardKeysReleased.Add(key);
    }
    
    private static void OnGamepadButtonDown(IGamepad gamepad, Button button) {
        _gamepadButtonsPressed.Add(button);
        _gamepadButtonsDown.Add(button);
    }
    
    private static void OnGamepadButtonUp(IGamepad gamepad, Button button) {
        _gamepadButtonsDown.Remove(button);
        _gamepadButtonsReleased.Add(button);
    }
    
    private static void OnJoystickButtonDown(IJoystick joystick, Button button) {
        _joystickButtonsPressed.Add(button);
        _joystickButtonsDown.Add(button);
    }
    
    private static void OnJoystickButtonUp(IJoystick joystick, Button button) {
        _joystickButtonsDown.Remove(button);
        _joystickButtonsReleased.Add(button);
    }
    
    private static void OnMouseButtonDown(IMouse mouse, MouseButton button) {
        _mouseButtonsPressed.Add(button);
        _mouseButtonsDown.Add(button);
    }
    
    private static void OnMouseButtonUp(IMouse mouse, MouseButton button) {
        _mouseButtonsDown.Remove(button);
        _mouseButtonsReleased.Add(button);
    }
}