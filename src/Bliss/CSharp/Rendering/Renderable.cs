using Bliss.CSharp.Colors;
using Bliss.CSharp.Geometry;
using Bliss.CSharp.Transformations;

namespace Bliss.CSharp.Rendering;

public class Renderable {
    
    public Model Model;
    public Color Color;
    public Transform Transform;

    public readonly uint Id;
    private static uint _ids;

    public Renderable(Model model, Color color, Transform transform) {
        this.Id = ++_ids;
        this.Model = model;
        this.Color = color;
        this.Transform = transform;
    }
}