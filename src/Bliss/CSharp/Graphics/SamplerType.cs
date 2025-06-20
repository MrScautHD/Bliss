namespace Bliss.CSharp.Graphics;

public enum SamplerType {
    
    /// <summary>
    /// Uses point (nearest neighbor) filtering with clamped texture coordinates.
    /// </summary>
    PointClamp,
    
    /// <summary>
    /// Uses point (nearest neighbor) filtering with wrapping texture coordinates.
    /// </summary>
    PointWrap,
    
    /// <summary>
    /// Uses linear (bilinear) filtering with clamped texture coordinates.
    /// </summary>
    LinearClamp,
    
    /// <summary>
    /// Uses linear (bilinear) filtering with wrapping texture coordinates.
    /// </summary>
    LinearWrap,
    
    /// <summary>
    /// Uses 4x anisotropic filtering with clamped texture coordinates for improved texture quality at oblique viewing angles.
    /// </summary>
    Aniso4XClamp,
    
    /// <summary>
    /// Uses 4x anisotropic filtering with wrapping texture coordinates for improved texture quality at oblique viewing angles.
    /// </summary>
    Aniso4XWrap
}