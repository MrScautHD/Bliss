using System.Runtime.InteropServices;
using Bliss.CSharp.Interact.Gamepads;
using Veldrid.Sdl2;

namespace Bliss.CSharp.Windowing;

public static class Sdl2Helper {
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool SdlGetRelativeMouseMode();
    
    private static SdlGetRelativeMouseMode _getRelativeMouseMode = Sdl2Native.LoadFunction<SdlGetRelativeMouseMode>("SDL_GetRelativeMouseMode");

    /// <summary>
    /// Retrieves the current relative mouse mode.
    /// </summary>
    /// <returns>Returns a <see cref="bool"/> value indicating whether the relative mouse mode is enabled or disabled.</returns>
    public static bool GetRelativeMouseMode() {
        return _getRelativeMouseMode();
    }
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SdlGameControllerRumble(nint gameController, ushort lowFrequencyRumble, ushort highFrequencyRumble, uint durationMs);

    private static SdlGameControllerRumble _gameControllerRumble = Sdl2Native.LoadFunction<SdlGameControllerRumble>("SDL_GameControllerRumble");
    
    /// <summary>
    /// Sets the rumble effect on a gamepad controller.
    /// </summary>
    /// <param name="gamepad">The gamepad controller to apply the rumble effect to.</param>
    /// <param name="lowFrequencyRumble">The strength of the low-frequency rumble effect, ranging from 0 to 65535.</param>
    /// <param name="highFrequencyRumble">The strength of the high-frequency rumble effect, ranging from 0 to 65535.</param>
    /// <param name="durationMs">The duration of the rumble effect in milliseconds.</param>
    /// <returns>Returns a <see cref="bool"/> value indicating whether the rumble effect was successfully applied or not.</returns>
    public static bool SetControllerRumble(Gamepad gamepad, ushort lowFrequencyRumble, ushort highFrequencyRumble, uint durationMs) {
        return _gameControllerRumble(gamepad.Controller, lowFrequencyRumble, highFrequencyRumble, durationMs) == 0;
    }
}