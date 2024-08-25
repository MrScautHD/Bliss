using Veldrid;

namespace Bliss.CSharp.Rendering;

public class SpriteBatch : Disposable {

    public GraphicsDevice GraphicsDevice { get; private set; }
    public CommandList CommandList { get; private set; }
    
    public SpriteBatch(GraphicsDevice graphicsDevice, CommandList commandList) {
        this.GraphicsDevice = graphicsDevice;
        this.CommandList = commandList;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            
        }
    }
}