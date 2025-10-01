using System.Numerics;
using System.Runtime.InteropServices;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Lights.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct FixedLightData {
    
    /// <summary>
    /// The maximum number of lights that can be stored in the container.
    /// </summary>
    public const int MaxLightCount = 512;
    
    /// <summary>
    /// Gets or sets the current number of lights in the container.
    /// </summary>
    public int NumOfLights;
    
    /// <summary>
    /// Padding for memory alignment to ensure the correct GPU struct layout.
    /// </summary>
    private Vector3 _padding;
    
    /// <summary>
    /// Gets or sets the global ambient color applied to the scene, where W stores intensity.
    /// </summary>
    public Vector4 AmbientColor;
    
    /// <summary>
    /// A fixed-size unmanaged buffer storing the raw light data.
    /// </summary>
    public unsafe fixed byte Lights[MaxLightCount * Light.SizeInBytes];
}