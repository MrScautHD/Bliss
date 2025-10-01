using System.Numerics;
using System.Runtime.InteropServices;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Materials.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MaterialData {
    
    /// <summary>
    /// The maximum number of material maps supported per material.
    /// </summary>
    public const int MaxMaterialMapCount = 8;
    
    /// <summary>
    /// The render mode of the material stored as an integer.
    /// </summary>
    private int _renderMode;
    
    /// <summary>
    /// Padding for memory alignment in GPU buffers.
    /// </summary>
    private Vector3 _padding;
    
    /// <summary>
    /// A fixed-size unmanaged buffer storing the material maps.
    /// </summary>
    private unsafe fixed byte _materialMaps[MaxMaterialMapCount * MaterialMapData.SizeInBytes];
    
    /// <summary>
    /// Gets or sets the render mode of the material.
    /// </summary>
    public RenderMode RenderMode {
        get => (RenderMode) this._renderMode;
        set => this._renderMode = (int) value;
    }
    
    /// <summary>
    /// Gets a reference to the <see cref="MaterialMapData"/> at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the material map.</param>
    /// <returns>A reference to the <see cref="MaterialMapData"/> at the given index.</returns>
    public unsafe ref MaterialMapData this[int index] {
        get {
            if (index < 0 || index >= MaxMaterialMapCount) {
                throw new IndexOutOfRangeException($"Index {index} is outside the valid range of 0 to {MaxMaterialMapCount - 1}.");
            }
            
            fixed (byte* mapPtr = this._materialMaps) {
                return ref ((MaterialMapData*) mapPtr)[index];
            }
        }
    }
}