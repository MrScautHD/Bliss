using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Colors;
using Silk.NET.Vulkan;

namespace Bliss.CSharp.Geometry;

public struct Vertex {
    
    public Vector3 Position;
    public Vector2 TexCoords;
    public Vector2 TexCoords2;
    public Vector3 Normal;
    public Vector3 Tangent;
    public Color Color;
    
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
                Format = Format.R32G32Sfloat,
                Offset = (uint) Marshal.OffsetOf<Vertex>(nameof(TexCoords))
            },
            new VertexInputAttributeDescription() {
                Binding = 0,
                Location = 2,
                Format = Format.R32G32Sfloat,
                Offset = (uint) Marshal.OffsetOf<Vertex>(nameof(TexCoords2))
            },
            new VertexInputAttributeDescription() {
                Binding = 0,
                Location = 3,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint) Marshal.OffsetOf<Vertex>(nameof(Normal))
            },
            new VertexInputAttributeDescription() {
                Binding = 0,
                Location = 4,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint) Marshal.OffsetOf<Vertex>(nameof(Tangent))
            },
            new VertexInputAttributeDescription() {
                Binding = 0,
                Location = 5,
                Format = Format.R32G32B32A32Sfloat,
                Offset = (uint) Marshal.OffsetOf<Vertex>(nameof(Color))
            }
        };
    }
}