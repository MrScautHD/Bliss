using System.Numerics;
using Bliss.CSharp.Interact.Mice;

namespace Bliss.CSharp.Interact.Contexts;

public interface IInputContext : IDisposable {

    void Begin();
    void End();

    bool IsCursorShown();
    void ShowCursor();
    void HideCursor();

    MouseCursor GetMouseCursor();
    void SetMouseCursor(MouseCursor cursor);

    bool IsRelativeMouseModeEnabled();
    void EnableRelativeMouseMode();
    void DisableRelativeMouseMode();

    Vector2 GetMousePosition();
    void SetMousePosition(Vector2 position);

    bool IsMouseButtonPressed(MouseButton button);
    bool IsMouseButtonDown(MouseButton button);
    bool IsMouseButtonReleased(MouseButton button);
    bool IsMouseButtonUp(MouseButton button);
    bool IsMouseMoving(out Vector2 pos);
    bool IsMouseScrolling(out float wheelDelta);
}