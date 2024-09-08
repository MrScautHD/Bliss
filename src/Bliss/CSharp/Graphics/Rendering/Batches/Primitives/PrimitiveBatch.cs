using System.Numerics;

namespace Bliss.CSharp.Graphics.Rendering.Batches.Primitives;

public class PrimitiveBatch : Disposable {
    
    /// <summary>
    /// Defines a template for vertex positions used to create a quad. 
    /// The array contains four <see cref="Vector2"/> instances representing the corners of the quad.
    /// </summary>
    private static readonly Vector2[] VertexTemplate = new Vector2[] {
        new Vector2(0.0F, 0.0F),
        new Vector2(1.0F, 0.0F),
        new Vector2(0.0F, 1.0F),
        new Vector2(1.0F, 1.0F),
    };
    
    /// <summary>
    /// Defines an index template for rendering two triangles as a quad.
    /// The array contains six <see cref="ushort"/> values, representing the vertex indices for two triangles.
    /// </summary>
    private static readonly ushort[] IndicesTemplate = new ushort[] {
        2, 1, 0,
        2, 3, 1
    };

    public PrimitiveBatch() {
        
    }

    public void Begin() {
        
    }

    public void End() {
        
    }

    private void Flush() {
        
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            
        }
    }
}