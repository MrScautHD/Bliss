namespace Bliss.CSharp.Graphics;

public enum SamplerType {
    
    /// <summary>
    /// Point sampling, which selects the nearest texel without filtering.
    /// </summary>
    Point,
    
    /// <summary>
    /// Linear sampling, which performs linear interpolation between texels.
    /// </summary>
    Linear,
    
    /// <summary>
    /// Anisotropic sampling with a maximum of 4x sampling, providing better quality at glancing angles.
    /// </summary>
    Aniso4X
}