using System.Numerics;
using Bliss.CSharp.Colors;

namespace Bliss.CSharp.Geometry;

public struct Vertex {
    
    public Vector3 Position;
    public Vector2 TexCoords;
    public Vector2 TexCoords2;
    public Vector3 Normal;
    public Vector3 Tangent;
    public Color Color;
}