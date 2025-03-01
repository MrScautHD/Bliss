namespace Bliss.CSharp.Textures.Cubemaps;

public enum CubemapLayer {
    
    /// <summary>
    /// The positive X face of the cubemap (right).
    /// </summary>
    PositiveX = 0,
    
    /// <summary>
    /// The negative X face of the cubemap (left).
    /// </summary>
    NegativeX = 1,
    
    /// <summary>
    /// The positive Y face of the cubemap (top).
    /// </summary>
    PositiveY = 2,
    
    /// <summary>
    /// The negative Y face of the cubemap (bottom).
    /// </summary>
    NegativeY = 3,
    
    /// <summary>
    /// The positive Z face of the cubemap (front).
    /// </summary>
    PositiveZ = 4,
    
    /// <summary>
    /// The negative Z face of the cubemap (back).
    /// </summary>
    NegativeZ = 5
}