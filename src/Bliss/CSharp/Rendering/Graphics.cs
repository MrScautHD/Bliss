using Bliss.CSharp.Textures;
using Veldrid;

namespace Bliss.CSharp.Rendering;

public class Graphics {
    public GraphicsDevice GraphicsDevice { get; private set; }
    public CommandList CommandList { get; private set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Graphics"/> class with the specified graphics device and command list.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering operations.</param>
    /// <param name="commandList">The command list used to issue rendering commands.</param>
    public Graphics(GraphicsDevice graphicsDevice, CommandList commandList) {
        this.GraphicsDevice = graphicsDevice;
        this.CommandList = commandList;
    }

    /// <summary>
    /// Begins the drawing operations.
    /// </summary>
    public void BeginDrawing() {
        this.CommandList.Begin();
        this.CommandList.SetFramebuffer(this.GraphicsDevice.SwapchainFramebuffer);
    }

    /// <summary>
    /// Ends the drawing operations.
    /// </summary>
    public void EndDrawing() {
        this.CommandList.End();
        this.GraphicsDevice.SubmitCommands(this.CommandList);
        this.GraphicsDevice.SwapBuffers();
    }

    /// <summary>
    /// Clears the background color and depth stencil of the command list.
    /// </summary>
    /// <param name="index">The index of the color target to clear.</param>
    /// <param name="clearColor">The color used to clear the background.</param>
    public void ClearBackground(uint index, RgbaFloat clearColor) {
        this.CommandList.ClearColorTarget(index, clearColor);

        if (this.GraphicsDevice.SwapchainFramebuffer.DepthTarget != null) {
            this.CommandList.ClearDepthStencil(1, 0);
        }
    }
    
    /* --------------------------------- Texture Drawing --------------------------------- */
    
    public void Draw(Texture2D texture) {
        // Something like that get added here!
    }
    
    /* --------------------------------- Text Drawing --------------------------------- */
    
    /* --------------------------------- Shape Drawing --------------------------------- */
    
    /* --------------------------------- Model Drawing --------------------------------- */
}