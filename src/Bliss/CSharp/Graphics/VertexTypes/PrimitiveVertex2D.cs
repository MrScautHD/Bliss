using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Graphics.Pipelines;
using Veldrid;

namespace Bliss.CSharp.Graphics.VertexTypes;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PrimitiveVertex2D : IVertexType {
    
    /// <summary>
    /// Represents the layout description for the <see cref="PrimitiveVertex2D"/> structure.
    /// </summary>
    public static VertexFormat VertexLayout = new VertexFormat("PrimitiveVertex2D", new VertexLayoutDescription(
        new VertexElementDescription("vPosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
        new VertexElementDescription("vColor", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4)
    ));
    
    /// <summary>
    /// Defines the vertex layout configuration used to describe the structure and attributes of a vertex.
    /// </summary>
    VertexFormat IVertexType.VertexLayout => VertexLayout;
    
    /// <summary>
    /// The position of the vertex in 2D space (Z is used for layout depth).
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// The color of the vertex.
    /// </summary>
    public Vector4 Color;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrimitiveVertex2D"/> struct with the specified position and color values.
    /// </summary>
    /// <param name="position">The 2D position of the vertex (Z is used for layout depth).</param>
    /// <param name="color">The color of the vertex, represented as a Vector4 (RGBA).</param>
    public PrimitiveVertex2D(Vector3 position, Vector4 color) {
        this.Position = position;
        this.Color = color;
    }
}