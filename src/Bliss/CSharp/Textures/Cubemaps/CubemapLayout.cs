namespace Bliss.CSharp.Textures.Cubemaps;

public enum CubemapLayout {
    
    /// <summary>
    /// Automatically detects the cubemap layout based on the texture dimensions and aspect ratio.
    /// </summary>
    AutoDetect,

    /// <summary>
    /// Specifies a vertical line layout where cubemap faces are stacked vertically in a single column.
    /// </summary>
    LineVertical,

    /// <summary>
    /// Specifies a horizontal line layout where cubemap faces are aligned horizontally in a single row.
    /// </summary>
    LineHorizontal,

    /// <summary>
    /// Specifies a 3x4 cross layout for cubemaps, with three columns and four rows of faces.
    /// </summary>
    CrossThreeByFour,

    /// <summary>
    /// Specifies a 4x3 cross layout for cubemaps, with four columns and three rows of faces.
    /// </summary>
    CrossFourByThree,

    /// <summary>
    /// Specifies a panoramic layout, used for spherical or equirectangular textures that can be converted to cubemaps.
    /// </summary>
    Panorama,
}