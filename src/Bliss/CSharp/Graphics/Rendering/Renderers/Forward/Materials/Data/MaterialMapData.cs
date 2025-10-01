using System.Numerics;
using System.Runtime.InteropServices;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Materials.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MaterialMapData {
    
    /// <summary>
    /// The total size of this structure in bytes.
    /// </summary>
    public const int SizeInBytes = 32;
    
    /// <summary>
    /// The RGBA color associated with the material map.
    /// </summary>
    public Vector4 Color;
    
    /// <summary>
    /// A scalar value parameter used by the material map.
    /// </summary>
    public float Value;
    
    /// <summary>
    /// Padding for memory alignment in GPU buffers.
    /// </summary>
    private Vector3 _padding;
}