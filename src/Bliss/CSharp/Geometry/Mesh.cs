using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Materials;

namespace Bliss.CSharp.Geometry;

// TODO: Add Material
public struct Mesh {

    public Vertex3D[] Vertices;
    public uint[] Indices;
    public MaterialOld MaterialOld;

    /// <summary>
    /// Initializes a new instance of the <see cref="Mesh"/> class with optional vertices and indices.
    /// </summary>
    /// <param name="vertices">An array of <see cref="Vertex3D"/> objects. If null, an empty array is used.</param>
    /// <param name="indices">An array of indices. If null, an empty array is used.</param>
    public Mesh(Vertex3D[]? vertices = default, uint[]? indices = default, MaterialOld? material = default) {
        this.Vertices = vertices ?? [];
        this.Indices = indices ?? [];
        this.MaterialOld = material ?? default; // TODO: Do a default material;
    }
}