using System.Numerics;
using System.Runtime.InteropServices;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Lighting.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ShadowData {
    
    /// <summary>
    /// The resolution of the shadow map texture used for shadow rendering.
    /// </summary>
    public float ShadowMapResolution;
    
    /// <summary>
    /// The position of the camera in view space used when calculating shadows.
    /// </summary>
    public Vector3 CameraViewPos;
    
    /// <summary>
    /// The ambient light color applied to the shadowed scene where intensity is stored in the W channel.
    /// </summary>
    public Vector4 AmbientColor;
    
    /// <summary>
    /// The combined view and projection matrix of the light used for shadow sampling.
    /// </summary>
    public Matrix4x4 LightVP;
    
    /// <summary>
    /// The world space direction of the shadow casting light.
    /// </summary>
    public Vector3 LightDirection;
    
    /// <summary>
    /// The color and intensity of the shadow casting light where intensity is stored in the W channel.
    /// </summary>
    public Vector4 LightColor;
}