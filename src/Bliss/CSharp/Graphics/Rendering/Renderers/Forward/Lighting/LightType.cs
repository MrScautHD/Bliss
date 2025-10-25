namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Lighting;

public enum LightType : int {
    
    /// <summary>
    /// A directional light, simulating a distant light source with parallel rays (e.g., sunlight).
    /// </summary>
    Directional = 0,
        
    /// <summary>
    /// A point light emitting light in all directions from a single position.
    /// </summary>
    Point = 1,
        
    /// <summary>
    /// A spotlight emitting light in a cone from a position in a specified direction.
    /// </summary>
    Spot = 2
}