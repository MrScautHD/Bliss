using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace Bliss.CSharp.Graphics.VertexTypes;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ImmediateVertex3D {

    /// <summary>
    /// Represents the layout description for the <see cref="ImmediateVertex3D"/> structure.
    /// </summary>
    public static VertexLayoutDescription VertexLayout = new VertexLayoutDescription(
        new VertexElementDescription("vPosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
        new VertexElementDescription("vTexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
        new VertexElementDescription("vColor", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
        new VertexElementDescription("vLineData", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4)
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
    /// The color of the vertex.
    /// </summary>
    public Vector4 Color;

    /// <summary>
    /// The line length of the vertex.
    /// </summary>
    public Vector4 LineData;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmediateVertex3D"/> structure with the specified position, texture coordinates, and color.
    /// </summary>
    /// <param name="position">The position of the vertex in 3D space.</param>
    /// <param name="texCoords">The texture coordinates associated with the vertex.</param>
    /// <param name="color">The color of the vertex.</param>
    /// <param name="lineData">The line data of the vertex.</param>
    public ImmediateVertex3D(Vector3 position, Vector2 texCoords, Vector4 color, Vector4 lineData) {
        this.Position = position;
        this.TexCoords = texCoords;
        this.Color = color;
        this.LineData = lineData;
    }
}