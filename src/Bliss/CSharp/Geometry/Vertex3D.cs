using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Colors;

namespace Bliss.CSharp.Geometry;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Vertex3D {
    
    // TODO: Add VertexLayout like Vertex2D.
    
    public Vector3 Position;
    public Vector2 TexCoords;
    public Vector2 TexCoords2;
    public Vector3 Normal;
    public Vector3 Tangent;
    public Vector4 Color;

    /// <summary>
    /// Initializes a new instance of the <see cref="Vertex3D"/> struct with the specified position, texture coordinates, secondary texture coordinates, normal, tangent, and color.
    /// </summary>
    /// <param name="position">The vertex position in 3D space.</param>
    /// <param name="texCoords">The primary texture coordinates for the vertex.</param>
    /// <param name="texCoords2">The secondary texture coordinates for the vertex.</param>
    /// <param name="normal">The normal vector for the vertex.</param>
    /// <param name="tangent">The tangent vector for the vertex.</param>
    /// <param name="color">The color of the vertex.</param>
    public Vertex3D(Vector3 position, Vector2 texCoords, Vector2 texCoords2, Vector3 normal, Vector3 tangent, Color color) {
        this.Position = position;
        this.TexCoords = texCoords;
        this.TexCoords2 = texCoords2;
        this.Normal = normal;
        this.Tangent = tangent;
        this.Color = color.ToRgbaFloat().ToVector4();
    }
}