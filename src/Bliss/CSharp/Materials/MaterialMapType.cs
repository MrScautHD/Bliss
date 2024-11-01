namespace Bliss.CSharp.Materials;

/// <summary>
/// An undefined or uninitialized material map type.
/// </summary>
public enum MaterialMapType {
    
    /// <summary>
    /// The base color map.
    /// </summary>
    Albedo,
    
    /// <summary>
    /// The metalness map.
    /// </summary>
    Metalness,
    
    /// <summary>
    /// The normal map.
    /// </summary>
    Normal,
    
    /// <summary>
    /// The roughness map.
    /// </summary>
    Roughness,
    
    /// <summary>
    /// The occlusion map.
    /// </summary>
    Occlusion,
    
    /// <summary>
    /// The emission map.
    /// </summary>
    Emission,
    
    /// <summary>
    /// The height map.
    /// </summary>
    Height,
    
    /// <summary>
    /// The cubemap.
    /// </summary>
    Cubemap,
    
    /// <summary>
    /// The irradiance map.
    /// </summary>
    Irradiance,
    
    /// <summary>
    /// The precomputed cubemap levels.
    /// </summary>
    Prefilter,
    
    /// <summary>
    /// The bidirectional reflectance distribution function (BRDF) map.
    /// </summary>
    Brdf
}