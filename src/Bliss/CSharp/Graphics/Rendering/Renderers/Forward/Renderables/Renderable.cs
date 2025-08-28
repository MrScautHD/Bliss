using Bliss.CSharp.Geometry;
using Bliss.CSharp.Transformations;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Renderables;

public struct Renderable {
    
    /// <summary>
    /// The mesh data that defines the geometry of the renderable.
    /// </summary>
    public Mesh Mesh;
    
    /// <summary>
    /// The transformation applied to the mesh.
    /// </summary>
    public Transform Transform;
}