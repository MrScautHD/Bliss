using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace Bliss.CSharp.Graphics.VertexTypes;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PrimitiveVertex2D {
    
    /// <summary>
    /// Represents the layout description for the <see cref="PrimitiveVertex2D"/> structure.
    /// </summary>
    public static VertexLayoutDescription VertexLayout = new VertexLayoutDescription(
        new VertexElementDescription("vPosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
        new VertexElementDescription("vPadding", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
        new VertexElementDescription("vColor", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4)
    );
    
    /// <summary>
    /// The position of the vertex in 2D space.
    /// </summary>
    public Vector2 Position;
    
    /// <summary>
    /// Padding value, often used to align the vertex data in memory.
    /// </summary>
    public Vector2 Padding;

    /// <summary>
    /// The color of the vertex.
    /// </summary>
    public Vector4 Color;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PrimitiveVertex2D"/> struct with the specified position, padding, and color values.
    /// </summary>
    /// <param name="position">The 2D position of the vertex.</param>
    /// <param name="padding">Padding value, often used to align the vertex data in memory.</param>
    /// <param name="color">The color of the vertex, represented as a Vector4 (RGBA).</param>
    public PrimitiveVertex2D(Vector2 position, Vector2 padding, Vector4 color) {
        this.Position = position;
        this.Padding = padding;
        this.Color = color;
    }
}