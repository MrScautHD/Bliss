using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.VertexTypes;
using Veldrith;

namespace Bliss.ImGUI.CSharp.VertexTypes;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ImGuiVertex : IVertexType {
    
    /// <summary>
    /// Represents the layout description for the <see cref="ImGuiVertex"/> structure.
    /// </summary>
    public static VertexFormat VertexLayout = new VertexFormat("ImGuiVertex", new VertexLayoutDescription(
        new VertexElementDescription("vPosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
        new VertexElementDescription("vTexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
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
    /// The texture coordinates of the vertex.
    /// </summary>
    public Vector2 TexCoords;
    
    /// <summary>
    /// The color of the vertex.
    /// </summary>
    public Vector4 Color;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SpriteVertex2D"/> struct with the specified position, texture coordinates, and color.
    /// </summary>
    /// <param name="position">The 2D position of the vertex (Z is used for layout depth).</param>
    /// <param name="texCoords">The texture coordinates of the vertex.</param>
    /// <param name="color">The color of the vertex as a vector with RGBA components.</param>
    public ImGuiVertex(Vector3 position, Vector2 texCoords, Vector4 color) {
        this.Position = position;
        this.TexCoords = texCoords;
        this.Color = color;
    }
}