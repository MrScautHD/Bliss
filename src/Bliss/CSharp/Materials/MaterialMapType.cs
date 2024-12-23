/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

namespace Bliss.CSharp.Materials;

public static class MaterialMapType {
    
    /// <summary>
    /// The name of the albedo map, representing base color and opacity.
    /// </summary>
    public const string Albedo = "fAlbedo";

    /// <summary>
    /// The name of the metallic map, representing the metallic property of a material.
    /// </summary>
    public const string Metallic = "fMetallic";

    /// <summary>
    /// The name of the normal map, used for simulating surface details without additional geometry.
    /// </summary>
    public const string Normal = "fNormal";

    /// <summary>
    /// The name of the roughness map, representing the roughness property of a material.
    /// </summary>
    public const string Roughness = "fRoughness";

    /// <summary>
    /// The name of the occlusion map, representing ambient occlusion for the material.
    /// </summary>
    public const string Occlusion = "fOcclusion";

    /// <summary>
    /// The name of the emission map, used for materials that emit light.
    /// </summary>
    public const string Emission = "fEmissive";

    /// <summary>
    /// The name of the height map, used for simulating displacement and depth.
    /// </summary>
    public const string Height = "fHeight";
}