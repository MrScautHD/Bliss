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
        new VertexElementDescription("vBoneWeights", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
        new VertexElementDescription("vBoneIndices", VertexElementSemantic.TextureCoordinate, VertexElementFormat.UInt4),
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
    /// Represents the weights of the vertex's associated bones, used for skinning in skeletal animation.
    /// </summary>
    public Vector4 BoneWeights;

    /// <summary>
    /// Represents the indices of the vertex's associated bones, used in conjunction with bone weights for skeletal animation.
    /// </summary>
    public UInt4 BoneIndices;

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
    /// <param name="boneWeights">The weights associated with bones for skeletal animation.</param>
    /// <param name="boneIndices">The indices of bones influencing this vertex.</param>
    /// <param name="texCoords">The primary texture coordinates of the vertex.</param>
    /// <param name="texCoords2">The secondary texture coordinates of the vertex.</param>
    /// <param name="normal">The normal vector at the vertex, used for lighting calculations.</param>
    /// <param name="tangent">The tangent vector at the vertex, used for normal mapping. Also includes the sign for bitangent.</param>
    /// <param name="color">The color of the vertex, stored as an RGBA float vector.</param>
    public Vertex3D(Vector3 position, Vector4 boneWeights, UInt4 boneIndices, Vector2 texCoords, Vector2 texCoords2, Vector3 normal, Vector4 tangent, Vector4 color) {
        this.Position = position;
        this.BoneWeights = boneWeights;
        this.BoneIndices = boneIndices;
        this.TexCoords = texCoords;
        this.TexCoords2 = texCoords2;
        this.Normal = normal;
        this.Tangent = tangent;
        this.Color = color;
    }

    /// <summary>
    /// Adds a bone to the vertex and assigns a weight to it.
    /// </summary>
    /// <param name="id">The identifier of the bone.</param>
    /// <param name="weight">The weight of the bone influence.</param>
    public void AddBone(uint id, float weight) {
        if (this.BoneWeights.X == 0) {
            this.BoneWeights.X = weight;
            this.BoneIndices = new UInt4(id, this.BoneIndices.Y, this.BoneIndices.Z, this.BoneIndices.W);
        }
        else if (this.BoneWeights.Y == 0) {
            this.BoneWeights.Y = weight;
            this.BoneIndices = new UInt4(this.BoneIndices.X, id, this.BoneIndices.Z, this.BoneIndices.W);
        }
        else if (this.BoneWeights.Z == 0) {
            this.BoneWeights.Z = weight;
            this.BoneIndices = new UInt4(this.BoneIndices.X, this.BoneIndices.Y, id, this.BoneIndices.W);
        }
        else if (this.BoneWeights.W == 0) {
            this.BoneWeights.W = weight;
            this.BoneIndices = new UInt4(this.BoneIndices.X, this.BoneIndices.Y, this.BoneIndices.Z, id);
        }
    }
}