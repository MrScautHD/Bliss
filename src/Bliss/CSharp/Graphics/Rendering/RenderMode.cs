namespace Bliss.CSharp.Graphics.Rendering;

public enum RenderMode {
    
    /// <summary>
    /// Opaque rendering (no transparency).
    /// </summary>
    Solid = 0,
    
    /// <summary>
    /// Alpha cutout rendering (pixels fully transparent are discarded).
    /// </summary>
    Cutout = 1,
    
    /// <summary>
    /// Transparent rendering (blends with background).
    /// </summary>
    Translucent = 2
}