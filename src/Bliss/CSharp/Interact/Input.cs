using Bliss.CSharp.Interact.Contexts;

namespace Bliss.CSharp.Interact;

public static class Input {
    
    public static IInputContext InputContext { get; private set; }

    public static void Init(IInputContext context) {
        InputContext = context;
    }

    public static void Begin() {
        InputContext.Begin();
    }

    public static void End() {
        InputContext.End();
    }

    public static bool IsCursorShown() {
        return InputContext.IsCursorShown();
    }

    public static void ShowCursor() {
        InputContext.ShowCursor();
    }

    public static void HideCursor() {
        InputContext.HideCursor();
    }
}