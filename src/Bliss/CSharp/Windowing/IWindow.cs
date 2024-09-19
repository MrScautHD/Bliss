using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;

namespace Bliss.CSharp.Windowing;

// TODO: Replace "Veldrid.Sdl2" wit "ppy.SDL2-CS" (https://github.com/ppy/SDL2-CS/tree/master).
// TODO: That would even fix the problem that on other platforms then windows the SDL2.dll is missing. (like Linux and for MacOS for the source code.)
public interface IWindow : IDisposable {
    
    public nint Handle { get; }
    public uint Id { get; }
    
    SwapchainSource SwapchainSource { get; }
    
    bool Exists { get; }
    bool IsFocused { get; }
    
    WindowState State { get; set; }
    bool Visible { get; set; }
    float Opacity { get; set; }
    bool Resizable { get; set; }
    bool BorderVisible { get; set; }
    
    string GetTitle();
    void SetTitle(string title);

    (int, int) GetSize();
    void SetSize(int width, int height);

    int GetWidth();
    void SetWidth(int width);

    int GetHeight();
    void SetHeight(int height);

    void SetIcon(Image<Rgba32> image);
    public void PumpEvents();
    
    Point ClientToScreen(Point point); //TODO: DO A own point or use a diffrent type!
    Point ScreenToClient(Point point);
}