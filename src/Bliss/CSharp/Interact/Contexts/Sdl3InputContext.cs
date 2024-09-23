using System.Numerics;
using Bliss.CSharp.Interact.Mice;
using Bliss.CSharp.Windowing;
using SDL;

namespace Bliss.CSharp.Interact.Contexts;

public class Sdl3InputContext : Disposable, IInputContext {

    private IWindow _window;

    public Sdl3InputContext(IWindow window) {
        this._window = window;
    }

    public void Begin() {
        
    }

    public void End() {
        
    }

    public bool IsCursorShown() {
        return SDL3.SDL_CursorVisible() == SDL_bool.SDL_TRUE;
    }

    public void ShowCursor() {
        SDL3.SDL_ShowCursor();
    }

    public void HideCursor() {
        SDL3.SDL_HideCursor();
    }

    public MouseCursor GetMouseCursor() {
        throw new NotImplementedException();
    }

    public void SetMouseCursor(MouseCursor cursor) {
        throw new NotImplementedException();
    }

    public bool IsRelativeMouseModeEnabled() {
        throw new NotImplementedException();
    }

    public void EnableRelativeMouseMode() {
        throw new NotImplementedException();
    }

    public void DisableRelativeMouseMode() {
        throw new NotImplementedException();
    }

    public Vector2 GetMousePosition() {
        throw new NotImplementedException();
    }

    public void SetMousePosition(Vector2 position) {
        throw new NotImplementedException();
    }

    public bool IsMouseButtonPressed(MouseButton button) {
        throw new NotImplementedException();
    }

    public bool IsMouseButtonDown(MouseButton button) {
        throw new NotImplementedException();
    }

    public bool IsMouseButtonReleased(MouseButton button) {
        throw new NotImplementedException();
    }

    public bool IsMouseButtonUp(MouseButton button) {
        throw new NotImplementedException();
    }

    public bool IsMouseMoving(out Vector2 pos) {
        throw new NotImplementedException();
    }

    public bool IsMouseScrolling(out float wheelDelta) {
        throw new NotImplementedException();
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            
        }
    }
}