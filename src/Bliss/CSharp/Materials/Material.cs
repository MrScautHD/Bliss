using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Rendering;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Textures;
using Veldrid;

namespace Bliss.CSharp.Materials;

public class Material : ICloneable {
    
    /// <summary>
    /// The effect (shader program) applied to this material.
    /// </summary>
    public Effect Effect;

    /// <summary>
    /// Defines the rasterizer state for the material.
    /// </summary>
    public RasterizerStateDescription RasterizerState;
    
    /// <summary>
    /// Specifies the blend state for rendering, determining how colors are blended on the screen.
    /// </summary>
    public BlendStateDescription BlendState;

    /// <summary>
    /// Specifies the rendering mode for the material, determining how the material is drawn (e.g., Solid, Cutout, Transparent).
    /// </summary>
    public RenderMode RenderMode;
    
    /// <summary>
    /// A list of floating-point parameters for configuring material properties.
    /// </summary>
    public List<float> Parameters;
    
    /// <summary>
    /// A dictionary mapping material map types to material map data, used for managing material textures.
    /// </summary>
    private Dictionary<MaterialMapType, MaterialMap> _maps;

    /// <summary>
    /// Initializes a new instance of the <see cref="Material"/> class.
    /// </summary>
    /// <param name="effect">The effect (shader) to apply to the material.</param>
    /// <param name="rasterizerState">The optional rasterizer state.</param>
    /// <param name="blendState">The optional blend state to define how this material blends with others during rendering. If not specified, blending is disabled by default.</param>
    /// <param name="renderMode">The rendering mode for this material. Defaults to <see cref="RenderMode.Solid"/>.</param>
    public Material(Effect effect, RasterizerStateDescription? rasterizerState = null, BlendStateDescription? blendState = null, RenderMode renderMode = RenderMode.Solid) {
        this.Effect = effect;
        this.RasterizerState = rasterizerState ?? RasterizerStateDescription.DEFAULT;
        this.BlendState = blendState ?? BlendStateDescription.SINGLE_DISABLED;
        this.RenderMode = renderMode;
        this.Parameters = new List<float>();
        this._maps = new Dictionary<MaterialMapType, MaterialMap>();
    }

    /// <summary>
    /// Retrieves the collection of material map types associated with the material.
    /// </summary>
    /// <returns>A collection of keys representing the material map types defined for this material.</returns>
    public Dictionary<MaterialMapType, MaterialMap>.KeyCollection GetMaterialMapTypes() {
        return this._maps.Keys;
    }

    /// <summary>
    /// Retrieves the collection of material maps associated with the material.
    /// </summary>
    /// <returns>A collection containing the values of the material maps defined for this material.</returns>
    public Dictionary<MaterialMapType, MaterialMap>.ValueCollection GetMaterialMaps() {
        return this._maps.Values;
    }

    /// <summary>
    /// Retrieves a specific material map associated with the specified material map type.
    /// </summary>
    /// <param name="type">The type of the material map to retrieve.</param>
    /// <returns>The <see cref="MaterialMap"/> associated with the specified type if it exists; otherwise, null.</returns>
    public MaterialMap? GetMaterialMap(MaterialMapType type) {
        if (this._maps.TryGetValue(type, out MaterialMap? map)) {
            return map;
        }
        
        return null;
    }

    /// <summary>
    /// Adds a material map to the material, associating it with the specified material map type.
    /// </summary>
    /// <param name="type">The type of the material map, represented as a <see cref="MaterialMapType"/> enum value.</param>
    /// <param name="map">The material map to associate with the specified type.</param>
    public void AddMaterialMap(MaterialMapType type, MaterialMap map) {
        if (!this._maps.TryAdd(type, map)) {
            Logger.Warn($"Failed to add MaterialMap with the type [{type.GetName()}]. A material map with this type might already exist.");
        }
    }

    /// <summary>
    /// Retrieves the texture associated with the specified material map type.
    /// </summary>
    /// <param name="type">The type of material map for which the texture is requested.</param>
    /// <returns>The texture associated with the specified material map type, or null if no texture is found.</returns>
    public Texture2D? GetMapTexture(MaterialMapType type) {
        if (this._maps.TryGetValue(type, out MaterialMap? map)) {
            return map.Texture;
        }
        
        return null;
    }

    /// <summary>
    /// Assigns a texture to a material map of the specified type. If the map type exists, its texture is updated; otherwise, a warning is logged.
    /// </summary>
    /// <param name="type">The type of material map to which the texture will be assigned. This determines the type of visual effect applied (e.g., Albedo, Normal, Metallic, etc.).</param>
    /// <param name="texture">The texture to assign to the specified material map. If null, the texture will be removed from the map.</param>
    public void SetMapTexture(MaterialMapType type, Texture2D? texture) {
        if (this._maps.TryGetValue(type, out MaterialMap? map)) {
            map.Texture = texture;
        }
        else {
            Logger.Warn($"Failed to set texture for: [{type.GetName()}]. The map might not exist.");
        }
    }

    /// <summary>
    /// Retrieves the color associated with a specific material map type, if defined.
    /// </summary>
    /// <param name="type">The type of the material map whose color is to be retrieved.</param>
    /// <returns>The color associated with the specified material map type, or null if the map is not defined.</returns>
    public Color? GetMapColor(MaterialMapType type) {
        if (this._maps.TryGetValue(type, out MaterialMap? map)) {
            return map.Color;
        }
        
        return null;
    }

    /// <summary>
    /// Sets the color for a specific material map type in the current material instance.
    /// </summary>
    /// <param name="type">The type of the material map to update, represented by <see cref="MaterialMapType"/>.</param>
    /// <param name="color">The new color to assign to the specified material map.</param>
    public void SetMapColor(MaterialMapType type, Color color) {
        if (this._maps.TryGetValue(type, out MaterialMap? map)) {
            map.Color = color;
        }
        else {
            Logger.Warn($"Failed to set color for: [{type.GetName()}]. The map might not exist.");
        }
    }

    /// <summary>
    /// Retrieves the value associated with the specified material map type.
    /// </summary>
    /// <param name="type">The type of the material map whose value is to be retrieved.</param>
    /// <returns>The value of the material map if it exists; otherwise, returns 0.0.</returns>
    public float GetMapValue(MaterialMapType type) {
        if (this._maps.TryGetValue(type, out MaterialMap? map)) {
            return map.Value;
        }
        
        return 0.0F;
    }

    /// <summary>
    /// Sets the numeric value associated with the specified material map type. If the map does not exist, a warning is logged instead of modifying any values.
    /// </summary>
    /// <param name="type">The type of material map whose value is being set.</param>
    /// <param name="value">The numeric value to assign to the material map.</param>
    public void SetMapValue(MaterialMapType type, float value) {
        if (this._maps.TryGetValue(type, out MaterialMap? map)) {
            map.Value = value;
        }
        else {
            Logger.Warn($"Failed to set value for: [{type.GetName()}]. The map might not exist.");
        }
    }

    /// <summary>
    /// Creates a new instance of the <see cref="Material"/> class that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="Material"/> object that is a clone of the current instance.</returns>
    public object Clone() {
        Material material = new Material(this.Effect) {
            RasterizerState = this.RasterizerState,
            BlendState = this.BlendState,
            RenderMode = this.RenderMode,
            Parameters = this.Parameters
        };
        
        foreach (var mapPair in this._maps) {
            material.AddMaterialMap(mapPair.Key, mapPair.Value);
        }
        
        return material;
    }
}