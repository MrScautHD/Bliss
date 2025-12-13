using System.Numerics;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Lighting.Data;
using Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Lighting.Shadowing;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Lighting.Handlers;

public interface ILightHandler<T> : IDisposable where T : unmanaged {
    
    /// <summary>
    /// Gets a value indicating whether this light handler uses a storage buffer.
    /// </summary>
    bool UseStorageBuffer { get; }
    
    /// <summary>
    /// Gets the maximum number of lights supported by this handler.
    /// </summary>
    int LightCapacity { get; }
    
    /// <summary>
    /// Gets the shadow effect associated with this light handler.
    /// </summary>
    Effect? ShadowEffect { get; }
    
    /// <summary>
    /// Gets the underlying unmanaged light data structure.
    /// </summary>
    T LightData { get; }
    
    /// <summary>
    /// Gets or sets the ambient color applied globally to the scene.
    /// </summary>
    Vector3 AmbientColor { get; set; }
    
    /// <summary>
    /// Gets or sets the intensity of the ambient color.
    /// </summary>
    float AmbientColorIntensity { get; set; }
    
    /// <summary>
    /// Gets the number of active lights stored in the container.
    /// </summary>
    /// <returns>The total number of lights.</returns>
    int GetNumOfLights();
    
    /// <summary>
    /// Retrieves all lights in this container as a read-only span.
    /// </summary>
    /// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="Light"/> instances.</returns>
    ReadOnlySpan<Light> GetLights();
    
    /// <summary>
    /// Gets a reference to the light at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the light.</param>
    /// <returns>A reference to the <see cref="Light"/> at the given index.</returns>
    ref Light this[int index] { get; }
    
    /// <summary>
    /// Gets a reference to the light with the specified unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the light.</param>
    /// <returns>A reference to the <see cref="Light"/> with the given ID.</returns>
    ref Light GetLightById(uint id);
    
    /// <summary>
    /// Adds a new light definition to the container and outputs its assigned identifier.
    /// </summary>
    /// <param name="lightDef">The light definition to add.</param>
    /// <param name="id">The unique identifier assigned to the new light.</param>
    void AddLight(LightDefinition lightDef, out uint id);
    
    /// <summary>
    /// Attempts to add a new light definition to the container and outputs its identifier if successful.
    /// </summary>
    /// <param name="lightDef">The light definition to add.</param>
    /// <param name="id">The unique identifier assigned to the new light.</param>
    /// <returns><c>true</c> if the light was added successfully; otherwise, <c>false</c>.</returns>
    bool TryAddLight(LightDefinition lightDef, out uint id);
    
    /// <summary>
    /// Removes the light with the specified identifier from the container.
    /// </summary>
    /// <param name="id">The unique identifier of the light to remove.</param>
    void RemoveLight(uint id);
    
    /// <summary>
    /// Attempts to remove the light with the specified identifier from the container.
    /// </summary>
    /// <param name="id">The unique identifier of the light to remove.</param>
    /// <returns><c>true</c> if the light was removed successfully; otherwise, <c>false</c>.</returns>
    bool TryRemoveLight(uint id);
    
    /// <summary>
    /// Clears all lights stored within the container.
    /// </summary>
    void ClearLights();
    
    /// <summary>
    /// The shadow map associated with the specified light ID.
    /// </summary>
    /// <param name="id">The unique identifier of the light.</param>
    /// <returns>The <see cref="ShadowMap"/> associated with the given light ID, or null if no shadow map is assigned.</returns>
    ShadowMap? GetShadowMapByLightId(uint id);
    
    /// <summary>
    /// Associates a shadow map with the light specified by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the light to associate the shadow map with.</param>
    /// <param name="shadowMap">The shadow map to be set for the specified light. Can be null to remove an existing shadow map.</param>
    void SetShadowMapForLightId(uint id, ShadowMap? shadowMap);
    
    /// <summary>
    /// Attempts to assign a shadow map to a light specified by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the light.</param>
    /// <param name="shadowMap">The shadow map to assign, or null to remove the association.</param>
    /// <returns>True if the shadow map was successfully assigned to the light; otherwise, false.</returns>
    bool TrySetShadowMapForLightId(uint id, ShadowMap? shadowMap);
}