/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

using System.Runtime.Serialization;

namespace Bliss.CSharp.Materials;

/// <summary>
/// An undefined or uninitialized material map type.
/// </summary>
public enum MaterialMapType {
    
    /// <summary>
    /// The base color map.
    /// </summary>
    [EnumMember(Value = "fAlbedo")]
    Albedo,
    
    /// <summary>
    /// The metallic map.
    /// </summary>
    [EnumMember(Value = "fMetallic")]
    Metallic,
    
    /// <summary>
    /// The normal map.
    /// </summary>
    [EnumMember(Value = "fNormal")]
    Normal,
    
    /// <summary>
    /// The roughness map.
    /// </summary>
    [EnumMember(Value = "fRoughness")]
    Roughness,
    
    /// <summary>
    /// The occlusion map.
    /// </summary>
    [EnumMember(Value = "fOcclusion")]
    Occlusion,
    
    /// <summary>
    /// The emission map.
    /// </summary>
    [EnumMember(Value = "fEmissive")]
    Emission,
    
    /// <summary>
    /// The height map.
    /// </summary>
    [EnumMember(Value = "fHeight")]
    Height
}