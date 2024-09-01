using System.Numerics;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Textures;
using Veldrid;

namespace Bliss.CSharp.Graphics.Rendering.Sprites;

public struct Sprite {
    
    public Matrix4x4 Transform;
    public Texture2D Texture;
    public Sampler Sampler;
    public Color Color;
}