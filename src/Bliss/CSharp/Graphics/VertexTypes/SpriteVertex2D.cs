/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace Bliss.CSharp.Graphics.VertexTypes;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SpriteVertex2D {
    
    /// <summary>
    /// Represents the layout description for the <see cref="SpriteVertex2D"/> structure.
    /// </summary>
    public static VertexLayoutDescription VertexLayout = new VertexLayoutDescription(
        new VertexElementDescription("vPosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
        new VertexElementDescription("vTexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
        new VertexElementDescription("vColor", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4)
    );
    
    /// <summary>
    /// The position of the vertex in 2D space.
    /// </summary>
    public Vector2 Position;

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
    /// <param name="position">The 2D position of the vertex.</param>
    /// <param name="texCoords">The texture coordinates of the vertex.</param>
    /// <param name="color">The color of the vertex as a vector with RGBA components.</param>
    public SpriteVertex2D(Vector2 position, Vector2 texCoords, Vector4 color) {
        this.Position = position;
        this.TexCoords = texCoords;
        this.Color = color;
    }
}