using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Colors;
using Veldrid;

namespace Bliss.CSharp.Geometry;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Vertex3D {
    
    /// <summary>
    /// Represents the layout description for the <see cref="Vertex3D"/> structure.
    /// </summary>
    public static VertexLayoutDescription VertexLayout = new VertexLayoutDescription(
        new VertexElementDescription("vPosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
        new VertexElementDescription("vTexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
        new VertexElementDescription("vTexCoords2", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
        new VertexElementDescription("vNormal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
        new VertexElementDescription("vTangent", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
        new VertexElementDescription("vColor", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4)
    );
    
    /// <summary>
    /// The position of the vertex in 3D space.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// The primary texture coordinates of the vertex.
    /// </summary>
    public Vector2 TexCoords;

    /// <summary>
    /// The secondary texture coordinates of the vertex.
    /// </summary>
    public Vector2 TexCoords2;

    /// <summary>
    /// The normal vector of the vertex, used for lighting calculations.
    /// </summary>
    public Vector3 Normal;

    /// <summary>
    /// The tangent vector of the vertex, used for normal mapping.
    /// </summary>
    public Vector3 Tangent;

    /// <summary>
    /// The color of the vertex.
    /// </summary>
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