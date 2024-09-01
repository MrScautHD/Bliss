using System.Numerics;
using Bliss.CSharp.Colors;

namespace Bliss.CSharp.Geometry;

public struct Vertex3D {
    
    public Vector3 Position;
    public Vector2 TexCoord;
    public Vector2 TexCoord2;
    public Vector3 Normal;
    public Vector3 Tangent;
    public Vector4 Color;

    /// <summary>
    /// Initializes a new instance of the <see cref="Vertex3D"/> struct with the specified position, texture coordinates, secondary texture coordinates, normal, tangent, and color.
    /// </summary>
    /// <param name="position">The vertex position in 3D space.</param>
    /// <param name="texCoord">The primary texture coordinates for the vertex.</param>
    /// <param name="texCoord2">The secondary texture coordinates for the vertex.</param>
    /// <param name="normal">The normal vector for the vertex.</param>
    /// <param name="tangent">The tangent vector for the vertex.</param>
    /// <param name="color">The color of the vertex.</param>
    public Vertex3D(Vector3 position, Vector2 texCoord, Vector2 texCoord2, Vector3 normal, Vector3 tangent, Color color) {
        this.Position = position;
        this.TexCoord = texCoord;
        this.TexCoord2 = texCoord2;
        this.Normal = normal;
        this.Tangent = tangent;
        this.Color = color.ToRgbaFloat().ToVector4();
    }
}