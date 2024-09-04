using System.Numerics;
using Veldrid;

namespace Bliss.CSharp.Geometry;

public struct Vertex2D {
    
    public static VertexLayoutDescription VertexLayout = new VertexLayoutDescription(
        new VertexElementDescription("vPosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
        new VertexElementDescription("vTexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
        new VertexElementDescription("vColor", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Byte4_Norm)
    );
    
    public Vector2 Position;
    public Vector2 TexCoords;
    public Vector4 Color;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Vertex2D"/> struct with the specified position, texture coordinates, and color.
    /// </summary>
    /// <param name="position">The 2D position of the vertex.</param>
    /// <param name="texCoords">The texture coordinates of the vertex.</param>
    /// <param name="color">The color of the vertex as a vector with RGBA components.</param>
    public Vertex2D(Vector2 position, Vector2 texCoords, Vector4 color) {
        this.Position = position;
        this.TexCoords = texCoords;
        this.Color = color;
    }
}