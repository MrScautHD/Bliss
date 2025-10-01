using System.Numerics;
using System.Runtime.InteropServices;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Lights.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DynamicLightData {
    
    /// <summary>
    /// Gets or sets the current number of lights in the container.
    /// </summary>
    public int NumOfLights;
    
    /// <summary>
    /// Padding for memory alignment to ensure the correct GPU struct layout.
    /// </summary>
    private Vector3 _padding0;
    
    /// <summary>
    /// Gets or sets the global ambient color applied to the scene, where W stores intensity.
    /// </summary>
    public Vector4 AmbientColor;
}