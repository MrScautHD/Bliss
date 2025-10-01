using System.Numerics;
using System.Runtime.CompilerServices;
using Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Lights.Data;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Lights.Handlers;

public class FixedLightHandler : Disposable, ILightHandler<FixedLightData> {
    
    /// <summary>
    /// Gets a value indicating whether this handler uses a storage buffer.
    /// </summary>
    public bool UseStorageBuffer => false;
    
    /// <summary>
    /// Gets the maximum number of lights supported by this handler.
    /// </summary>
    public int LightCapacity => FixedLightData.MaxLightCount;
    
    /// <summary>
    /// Gets the underlying light data managed by this handler.
    /// </summary>
    public FixedLightData LightData => this._lightData;
    
    /// <summary>
    /// Stores the light data in a fixed buffer format.
    /// </summary>
    private FixedLightData _lightData;
    
    /// <summary>
    /// Stores the counter used to generate unique light IDs.
    /// </summary>
    private uint _lightIds;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="FixedLightHandler"/> class.
    /// </summary>
    /// <param name="ambientColor">The global ambient color applied to the scene.</param>
    /// <param name="ambientColorIntensity">The intensity of the ambient color.</param>
    public FixedLightHandler(Vector3 ambientColor = default, float ambientColorIntensity = 0.1F) {
        this._lightData = new FixedLightData() {
            AmbientColor = new Vector4(ambientColor, ambientColorIntensity)
        };
    }
    
    /// <summary>
    /// Gets or sets the global ambient color applied to the scene.
    /// </summary>
    public Vector3 AmbientColor {
        get => this._lightData.AmbientColor.AsVector3();
        set => this._lightData.AmbientColor = new Vector4(value, this._lightData.AmbientColor.W);
    }
    
    /// <summary>
    /// Gets or sets the intensity of the ambient color.
    /// </summary>
    public float AmbientColorIntensity {
        get => this._lightData.AmbientColor.W;
        set => this._lightData.AmbientColor.W = value;
    }
    
    /// <summary>
    /// Gets the current number of lights in this handler.
    /// </summary>
    /// <returns>The number of lights currently stored.</returns>
    public int GetNumOfLights() {
        return this.LightData.NumOfLights;
    }
    
    /// <summary>
    /// Retrieves all lights managed by this handler as a read-only span.
    /// </summary>
    /// <returns>A <see cref="ReadOnlySpan{Light}"/> containing all lights.</returns>
    public unsafe ReadOnlySpan<Light> GetLights() {
        fixed (byte* lightsPtr = this._lightData.Lights) {
            return new ReadOnlySpan<Light>(lightsPtr, this._lightData.NumOfLights);
        }
    }
    
    /// <summary>
    /// Provides indexed access to the lights managed by this handler.
    /// </summary>
    /// <param name="index">The zero-based index of the light.</param>
    /// <returns>A reference to the light at the specified index.</returns>
    public unsafe ref Light this[int index] {
        get {
            if (index < 0 || index >= this._lightData.NumOfLights) {
                throw new IndexOutOfRangeException($"Index {index} is outside the valid range of 0 to {this._lightData.NumOfLights - 1}.");
            }
            
            fixed (byte* lightsPtr = this._lightData.Lights) {
                return ref ((Light*) lightsPtr)[index];
            }
        }
    }
    
    /// <summary>
    /// Retrieves a reference to a light with the specified ID.
    /// </summary>
    /// <param name="id">The unique identifier of the light.</param>
    /// <returns>A reference to the light with the given ID.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if no light with the given ID exists.</exception>
    public unsafe ref Light GetLightById(uint id) {
        fixed (byte* lightsPtr = this._lightData.Lights) {
            Light* lights = (Light*) lightsPtr;
            
            for (int i = 0; i < this._lightData.NumOfLights; i++) {
                ref Light light = ref lights[i];
                
                if (light.Id == id) {
                    return ref light;
                }
            }
        }
        
        throw new KeyNotFoundException($"Light with ID {id} not found.");
    }
    
    /// <summary>
    /// Adds a new light to this handler or throws if it cannot be added.
    /// </summary>
    /// <param name="lightDef">The light definition used to create the light.</param>
    /// <param name="id">The unique identifier assigned to the new light.</param>
    /// <exception cref="Exception">Thrown if the light cannot be added due to capacity or duplicate ID.</exception>
    public void AddLight(LightDefinition lightDef, out uint id) {
        if (!this.TryAddLight(lightDef, out id)) {
            throw new Exception($"Unable to add the light. Either the maximum limit of {this.LightCapacity} lights has been reached, or a light with the same ID already exists in the container.");
        }
    }
    
    /// <summary>
    /// Attempts to add a new light to this handler.
    /// </summary>
    /// <param name="lightDef">The light definition used to create the light.</param>
    /// <param name="id">The unique identifier assigned to the new light.</param>
    /// <returns><c>true</c> if the light was added successfully; otherwise, <c>false</c>.</returns>
    public unsafe bool TryAddLight(LightDefinition lightDef, out uint id) {
        if (this._lightData.NumOfLights >= this.LightCapacity) {
            id = 0;
            return false;
        }
        
        fixed (byte* lightsPtr = this._lightData.Lights) {
            Light* lights = (Light*) lightsPtr;
            
            id = ++this._lightIds;
            Light light = new Light(lightDef.LightType, (int) id, lightDef.Position, lightDef.Direction, lightDef.Color, lightDef.Intensity, lightDef.Range, lightDef.SpotAngle);
            
            lights[this._lightData.NumOfLights] = light;
            this._lightData.NumOfLights++;
            return true;
        }
    }
    
    /// <summary>
    /// Removes the light with the specified ID or throws if it cannot be removed.
    /// </summary>
    /// <param name="id">The unique identifier of the light to remove.</param>
    /// <exception cref="Exception">Thrown if no light with the given ID exists.</exception>
    public void RemoveLight(uint id) {
        if (!this.TryRemoveLight(id)) {
            throw new Exception($"Unable to remove the light. There is no light with the ID {id} in the container.");
        }
    }
    
    /// <summary>
    /// Attempts to remove the light with the specified ID.
    /// </summary>
    /// <param name="id">The unique identifier of the light to remove.</param>
    /// <returns><c>true</c> if the light was removed successfully; otherwise, <c>false</c>.</returns>
    public unsafe bool TryRemoveLight(uint id) {
        fixed (byte* lightsPtr = this._lightData.Lights) {
            Light* lights = (Light*) lightsPtr;
            
            for (int i = 0; i < this._lightData.NumOfLights; i++) {
                if (lights[i].Id == id) {
                    
                    // Shift all lights after the removed one.
                    for (int j = i; j < this._lightData.NumOfLights - 1; j++) {
                        lights[j] = lights[j + 1];
                    }
                    
                    // Decrease the light count and clear the last light entry.
                    this._lightData.NumOfLights--;
                    Unsafe.InitBlock(&lights[this._lightData.NumOfLights], 0, Light.SizeInBytes);
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Removes all lights from this handler.
    /// </summary>
    public unsafe void ClearLights() {
        this._lightData.NumOfLights = 0;
        
        fixed (byte* lightsPtr = this._lightData.Lights) {
            Unsafe.InitBlock(lightsPtr, 0, FixedLightData.MaxLightCount * Light.SizeInBytes);
        }
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.ClearLights();
        }
    }
}