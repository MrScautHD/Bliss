using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Lighting.Data;

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
    /// The collection of lights used in the rendering process.
    /// </summary>
    public LightArray Lights;
    
    /// <summary>
    /// Represents a fixed-size, inline array for storing light information.
    /// </summary>
    [InlineArray(MaxLightCount)]
    public struct LightArray {
        private Light _firstElement;
        
        [UnscopedRef]
        public Span<Light> AsSpan() {
            return MemoryMarshal.CreateSpan(ref this._firstElement, MaxLightCount);
        }
        
        [UnscopedRef]
        public readonly ReadOnlySpan<Light> AsReadOnlySpan() {
            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this._firstElement), MaxLightCount);
        }
    }
}