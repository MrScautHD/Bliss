using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Colors;
using Silk.NET.Vulkan;

namespace Bliss.CSharp.Geometry;

public class Vertex {
    
    public Vector3 Position;
    public Color Color;
    public Vector3 Normal;
    public Vector2 Uv;

    /// <summary>
    /// Initializes a new instance of the <see cref="Vertex"/> class.
    /// </summary>
    /// <param name="pos">The position of the vertex.</param>
    /// <param name="color">The color of the vertex.</param>
    public Vertex(Vector3 pos, Color color) {
        this.Position = pos;
        this.Color = color;
    }

    /// <summary>
    /// Get the binding descriptions for the Vertex class.
    /// </summary>
    /// <returns>An array of VertexInputBindingDescription objects.</returns>
    public static VertexInputBindingDescription[] GetBindingDescriptions() {
        return new[] {
            new VertexInputBindingDescription() {
                Binding = 0,
                Stride = (uint) Marshal.SizeOf<Vertex>(),
                InputRate = VertexInputRate.Vertex
            }
        };
    }

    /// <summary>
    /// Get the attribute descriptions for the Vertex class.
    /// </summary>
    /// <returns>An array of VertexInputAttributeDescription objects.</returns>
    public static VertexInputAttributeDescription[] GetAttributeDescriptions() {
        return new[] {
            new VertexInputAttributeDescription() {
                Binding = 0,
                Location = 0,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint) Marshal.OffsetOf<Vertex>(nameof(Position))
            },
            new VertexInputAttributeDescription() {
                Binding = 0,
                Location = 1,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint) Marshal.OffsetOf<Vertex>(nameof(Color.GetHsv))
            },
            new VertexInputAttributeDescription() {
                Binding = 0,
                Location = 2,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint) Marshal.OffsetOf<Vertex>(nameof(Normal))
            },
            new VertexInputAttributeDescription() {
                Binding = 0,
                Location = 3,
                Format = Format.R32G32Sfloat,
                Offset = (uint) Marshal.OffsetOf<Vertex>(nameof(Uv))
            }
        };
    }
}