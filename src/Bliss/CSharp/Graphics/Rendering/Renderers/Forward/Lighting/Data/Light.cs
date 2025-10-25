using System.Numerics;
using System.Runtime.InteropServices;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Lighting.Data;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Light {
    
    /// <summary>
    /// Internal integer representation of the <see cref="LightType"/>.
    /// </summary>
    private int _lightType;
    
    /// <summary>
    /// An internal identifier used to uniquely distinguish the light instance.
    /// </summary>
    private int _id;
    
    /// <summary>
    /// The effective range of the light.
    /// </summary>
    private float _range;
    
    /// <summary>
    /// The spotlight cone angle in radians.
    /// </summary>
    private float _spotAngle;
    
    /// <summary>
    /// The light's position in world space, stored as a <see cref="Vector4"/> for GPU alignment.
    /// </summary>
    private Vector4 _position;
    
    /// <summary>
    /// The light's direction in world space, stored as a <see cref="Vector4"/> for GPU alignment.
    /// </summary>
    private Vector4 _direction;
    
    /// <summary>
    /// The light's color, with W storing intensity.
    /// </summary>
    private Vector4 _color;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Light"/> struct.
    /// </summary>
    /// <param name="type">The type of the light (Directional, Point, or Spot).</param>
    /// <param name="id">The unique identifier of the light.</param>
    /// <param name="position">The position of the light in world space.</param>
    /// <param name="direction">The direction of the light (used for directional and spot lights).</param>
    /// <param name="color">The base color of the light as a <see cref="Vector3"/>.</param>
    /// <param name="intensity">The brightness or intensity of the light.</param>
    /// <param name="range">The range of the light (used for point and spot lights).</param>
    /// <param name="spotAngle">The angle of the spotlight cone in radians.</param>
    public Light(LightType type, int id, Vector3 position = default, Vector3 direction = default, Vector3 color = default, float intensity = 1.0F, float range = 0.0F, float spotAngle = 0.0F) {
        this._lightType = (int) type;
        this._id = id;
        this._position = position.AsVector4();
        this._direction = direction.AsVector4();
        this._color = new Vector4(color, intensity); 
        this._range = range;
        this._spotAngle = spotAngle;
    }
    
    /// <summary>
    /// Gets or sets the type of the light (Directional, Point, or Spot).
    /// </summary>
    public LightType LightType {
        get => (LightType) this._lightType;
        set => this._lightType = (int) value;
    }
    
    /// <summary>
    /// Gets the unique identifier of the <see cref="Light"/> instance.
    /// </summary>
    public int Id => this._id;
    
    /// <summary>
    /// Gets or sets the position of the light in world space.
    /// </summary>
    public Vector3 Position {
        get => this._position.AsVector3();
        set => this._position = value.AsVector4();
    }
    
    /// <summary>
    /// Gets or sets the direction of the light in world space. (Relevant for directional and spot lights)
    /// </summary>
    public Vector3 Direction {
        get => this._direction.AsVector3();
        set => this._direction = value.AsVector4();
    }
    
    /// <summary>
    /// Gets or sets the color of the light as an RGB vector.
    /// </summary>
    public Vector3 Color {
        get => this._color.AsVector3();
        set => this._color = new Vector4(value, this._color.W);
    }
    
    /// <summary>
    /// Gets or sets the intensity (brightness) of the light.
    /// </summary>
    public float Intensity {
        get => this._color.W;
        set => this._color.W = value;
    }
    
    /// <summary>
    /// Gets or sets the range of the light. (Relevant for point and spot lights)
    /// </summary>
    public float Range {
        get => this._range;
        set => this._range = value;
    }
    
    /// <summary>
    /// Gets or sets the cone angle of the spotlight in radians. (Relevant for spot lights)
    /// </summary>
    public float SpotAngle {
        get => this._spotAngle;
        set => this._spotAngle = value;
    }
}