using System.Numerics;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Lights;

public struct LightDefinition {
    
    /// <summary>
    /// Gets or sets the type of the light (Directional, Point, or Spot).
    /// </summary>
    public LightType LightType;
    
    /// <summary>
    /// Gets or sets the position of the light in world space.
    /// </summary>
    public Vector3 Position;
    
    /// <summary>
    /// Gets or sets the direction of the light in world space.
    /// </summary>
    public Vector3 Direction;
    
    /// <summary>
    /// Gets or sets the color of the light as an RGB vector.
    /// </summary>
    public Vector3 Color;
    
    /// <summary>
    /// Gets or sets the brightness or intensity of the light.
    /// </summary>
    public float Intensity;
    
    /// <summary>
    /// Gets or sets the range of the light.
    /// </summary>
    public float Range;
    
    /// <summary>
    /// Gets or sets the angle of the spotlight cone in radians.
    /// </summary>
    public float SpotAngle;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="LightDefinition"/> struct.
    /// </summary>
    /// <param name="lightType">The type of the light (Directional, Point, or Spot).</param>
    /// <param name="position">The position of the light in world space.</param>
    /// <param name="direction">The direction of the light in world space.</param>
    /// <param name="color">The base color of the light as a <see cref="Vector3"/>.</param>
    /// <param name="intensity">The brightness or intensity of the light.</param>
    /// <param name="range">The range of the light.</param>
    /// <param name="spotAngle">The angle of the spotlight cone in radians.</param>
    public LightDefinition(LightType lightType, Vector3? position = null, Vector3? direction = null, Vector3? color = null, float intensity = 1.0F, float range = 0.0F, float spotAngle = 0.0F) {
        this.LightType = lightType;
        this.Position = position ?? Vector3.Zero;
        this.Direction = direction ?? Vector3.Zero;
        this.Color = color ?? Vector3.Zero;
        this.Intensity = intensity;
        this.Range = range;
        this.SpotAngle = spotAngle;
    }
}