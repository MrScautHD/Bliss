using System.Runtime.InteropServices;
using System.Text;
using Bliss.CSharp.Interact.Gamepads;
using Bliss.CSharp.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid.Sdl2;

namespace Bliss.CSharp.Windowing;

public static class Sdl2Helper {
    
    /// <summary>
    /// Retrieves the current SDL error message, if any.
    /// </summary>
    /// <returns>Returns a <see cref="string"/> representing the current SDL error message. If no error message is available, returns an empty string.</returns>
    public static unsafe string GetErrorMessage() {
        byte* error = Sdl2Native.SDL_GetError();

        if (error == null) {
            return string.Empty;
        }

        int chars = 0;
        while (error[chars] != 0) {
            chars++;
        }

        return Encoding.UTF8.GetString(error, chars);
    }
    
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

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SdlSetWindowIcon(nint window, nint surface);
    
    private static SdlSetWindowIcon _setWindowIcon = Sdl2Native.LoadFunction<SdlSetWindowIcon>("SDL_SetWindowIcon");
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint CreateRgbSurfaceFrom(nint pixels, int width, int height, int depth, int pitch, uint rMask, uint gMask, uint bMask, uint aMask);

    private static CreateRgbSurfaceFrom _createRgbSurfaceFrom = Sdl2Native.LoadFunction<CreateRgbSurfaceFrom>("SDL_CreateRGBSurfaceFrom");
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SdlFreeSurface(nint surface);
    
    private static SdlFreeSurface _freeSurface = Sdl2Native.LoadFunction<SdlFreeSurface>("SDL_FreeSurface");

    /// <summary>
    /// Sets the window icon for a specified SDL2 window using the provided image.
    /// </summary>
    /// <param name="window">The SDL2 window for which the icon will be set.</param>
    /// <param name="image">The image to be used as the window icon.</param>
    public static unsafe void SetWindowIcon(Sdl2Window window, Image<Rgba32> image) {
        byte[] data = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(data);

        fixed (byte* dataPtr = data) {
            nint surface = _createRgbSurfaceFrom((nint) dataPtr, image.Width, image.Height, 32, image.Width * 4, 0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000);

            if (surface == nint.Zero) {
                Logger.Error($"Failed to set Sdl2 window icon: {GetErrorMessage()}");
            }

            _setWindowIcon(window.SdlWindowHandle, surface);
            _freeSurface(surface);
        }
    }
}