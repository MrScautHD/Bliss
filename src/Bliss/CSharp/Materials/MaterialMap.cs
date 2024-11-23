using Bliss.CSharp.Colors;
using Bliss.CSharp.Textures;

namespace Bliss.CSharp.Materials;

public class MaterialMap {
    
    public Texture2D? Texture;
    public Color? Color;
    public float Value;
    
    public MaterialMap(Texture2D? texture = null, Color? color = null, float value = 0.0F) {
        this.Texture = texture;
        this.Color = color;
        this.Value = value;
    }
}