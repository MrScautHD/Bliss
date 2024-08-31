using Bliss.CSharp.Logging;
using Veldrid;

namespace Bliss.CSharp.Rendering;

public class SpriteBatch : Disposable {

    public GraphicsDevice GraphicsDevice { get; private set; }
    
    private CommandList _commandList;
    private Fence _fence;
    private bool _begin;
    
    public SpriteBatch(GraphicsDevice graphicsDevice) {
        this.GraphicsDevice = graphicsDevice;
        this._commandList = graphicsDevice.ResourceFactory.CreateCommandList();
        this._fence = graphicsDevice.ResourceFactory.CreateFence(false);
    }

    public void Begin() {
        if (this._begin) {
            throw new Exception("SpriteBatch started already.");
        }

        this._begin = true;
        
        this._commandList.Begin();
    }

    public CommandList End() {
        if (!this._begin) {
            throw new Exception("The SpriteBatch begin method get not called at first.");
        }

        this._begin = false;

        return this._commandList;
    }

    public CommandList GetCommandList() {
        if (this._begin) {
            Logger.Error("Cannot call .GetCommandList() while begin is true. Call .End() first.");
            return null;
        }

        return this._commandList;
    }

    private void Flush() {
        
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            
        }
    }
}