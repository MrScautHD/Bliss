using System.Runtime.Serialization;

namespace Bliss.CSharp.Materials;

public enum MaterialMapType {

    /// <summary>
    /// Albedo map, which defines the base color of the material.
    /// </summary>
    [EnumMember(Value = "fAlbedo")]
    Albedo = 0,
    
    /// <summary>
    /// Metallic map, which defines the metallic properties of the material.
    /// </summary>
    [EnumMember(Value = "fMetallic")]
    Metallic = 1,
    
    /// <summary>
    /// Normal map, which adds detail to the surface of the material by simulating bumps and grooves.
    /// </summary>
    [EnumMember(Value = "fNormal")]
    Normal = 2,
    
    /// <summary>
    /// Roughness map, which defines the roughness level of the material's surface.
    /// </summary>
    [EnumMember(Value = "fRoughness")]
    Roughness = 3,
    
    /// <summary>
    /// Occlusion map, which adds shadows in crevices to enhance the appearance of depth.
    /// </summary>
    [EnumMember(Value = "fOcclusion")]
    Occlusion = 4,
    
    /// <summary>
    /// Emission map, which defines areas of the material that emit light.
    /// </summary>
    [EnumMember(Value = "fEmissive")]
    Emission = 5,

    /// <summary>
    /// Opacity map, which defines the transparency levels of the material.
    /// </summary>
    [EnumMember(Value = "fOpacity")]
    Opacity = 6,
    
    /// <summary>
    /// Height map, which provides height data for the material to simulate depth effects.
    /// </summary>
    [EnumMember(Value = "fHeight")]
    Height = 7
}