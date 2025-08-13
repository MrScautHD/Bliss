using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Vortice.Mathematics;

namespace Bliss.CSharp.Graphics.VertexTypes;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Vertex3D {
    
    /// <summary>
    /// Represents the layout description for the <see cref="Vertex3D"/> structure.
    /// </summary>
    public static VertexLayoutDescription VertexLayout = new VertexLayoutDescription(
        new VertexElementDescription("vPosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
        new VertexElementDescription("vTexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
        new VertexElementDescription("vTexCoords2", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
        new VertexElementDescription("vNormal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
        new VertexElementDescription("vTangent", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
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
    public Vector4 Tangent;

    /// <summary>
    /// The color of the vertex.
    /// </summary>
    public Vector4 Color;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Vertex3D"/> struct with the specified position, bone weights, bone indices, texture coordinates, normal, tangent, and color values.
    /// </summary>
    /// <param name="position">The position of the vertex in 3D space.</param>
    /// <param name="texCoords">The primary texture coordinates of the vertex.</param>
    /// <param name="texCoords2">The secondary texture coordinates of the vertex.</param>
    /// <param name="normal">The normal vector at the vertex, used for lighting calculations.</param>
    /// <param name="tangent">The tangent vector at the vertex, used for normal mapping. Also includes the sign for bitangent.</param>
    /// <param name="color">The color of the vertex, stored as an RGBA float vector.</param>
    public Vertex3D(Vector3 position, Vector2 texCoords, Vector2 texCoords2, Vector3 normal, Vector4 tangent, Vector4 color) {
        this.Position = position;
        this.TexCoords = texCoords;
        this.TexCoords2 = texCoords2;
        this.Normal = normal;
        this.Tangent = tangent;
        this.Color = color;
    }
}