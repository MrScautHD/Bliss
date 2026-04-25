using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Rendering;
using Bliss.CSharp.Graphics.Rendering.Renderers.Forward.Materials.Data;
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
    /// Indicates whether the material has changed and needs its GPU buffer updated.
    /// </summary>
    public bool IsDirty { get; internal set; }
    
    /// <summary>
    /// A dictionary mapping material map types to material map data, used for managing material textures.
    /// </summary>
    private Dictionary<MaterialMapKey, (MaterialMap Map, int Slot)> _maps;
    
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
        this._maps = new Dictionary<MaterialMapKey, (MaterialMap, int)>();
    }
    
    /// <summary>
    /// Retrieves the collection of material map keys associated with the material.
    /// </summary>
    /// <returns>A collection of keys representing the material map keys defined for this material.</returns>
    public Dictionary<MaterialMapKey, (MaterialMap Map, int Slot)>.KeyCollection GetMaterialMapKeys() {
        return this._maps.Keys;
    }
    
    /// <summary>
    /// Retrieves the collection of material maps and their slot indices associated with the material.
    /// </summary>
    /// <returns>A collection containing the values of the material maps defined for this material.</returns>
    public Dictionary<MaterialMapKey, (MaterialMap Map, int Slot)>.ValueCollection GetMaterialMaps() {
        return this._maps.Values;
    }
    
    /// <summary>
    /// Retrieves a specific material map associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the material map to retrieve.</param>
    /// <returns>The <see cref="MaterialMap"/> associated with the specified key if it exists; otherwise, null.</returns>
    public MaterialMap? GetMaterialMap(MaterialMapKey key) {
        return this._maps.TryGetValue(key, out var entry) ? entry.Map : null;
    }
    
    /// <summary>
    /// Retrieves the shader slot index associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the material map whose slot is to be retrieved.</param>
    /// <returns>The slot index associated with the specified key, or -1 if not found.</returns>
    public int GetMaterialMapSlot(MaterialMapKey key) {
        return this._maps.TryGetValue(key, out var entry) ? entry.Slot : -1;
    }
    
    /// <summary>
    /// Adds a material map to the material, associating it with the specified key and shader slot.
    /// </summary>
    /// <param name="key">The key of the material map, either a <see cref="MaterialMapType"/> or a plain string.</param>
    /// <param name="slot">The shader buffer slot index this map occupies.</param>
    /// <param name="map">The material map to associate with the specified key.</param>
    public void AddMaterialMap(MaterialMapKey key, int slot, MaterialMap map) {
        if (slot < 0 || slot >= MaterialData.MaxMaterialMapCount) {
            Logger.Warn($"Failed to add MaterialMap [{key}]. Slot [{slot}] is out of range (0 - {MaterialData.MaxMaterialMapCount - 1}).");
            return;
        }
        
        foreach (var entry in this._maps.Values) {
            if (entry.Slot == slot) {
                Logger.Warn($"Failed to add MaterialMap [{key}]. Slot [{slot}] is already taken.");
                return;
            }
        }
        
        if (this._maps.TryAdd(key, (map, slot))) {
            this.IsDirty = true;
        }
        else {
            Logger.Warn($"Failed to add MaterialMap [{key}]. A map with this name might already exist.");
        }
    }
    
    /// <summary>
    /// Retrieves the texture associated with the specified material map key.
    /// </summary>
    /// <param name="key">The key of the material map for which the texture is requested.</param>
    /// <returns>The texture associated with the specified key, or null if no texture is found.</returns>
    public Texture2D? GetMapTexture(MaterialMapKey key) {
        return this._maps.TryGetValue(key, out var entry) ? entry.Map.Texture : null;
    }
    
    /// <summary>
    /// Assigns a texture to a material map of the specified key.
    /// </summary>
    /// <param name="key">The key of the material map to which the texture will be assigned.</param>
    /// <param name="texture">The texture to assign to the specified material map. If null, the texture will be removed from the map.</param>
    public void SetMapTexture(MaterialMapKey key, Texture2D? texture) {
        if (this._maps.TryGetValue(key, out var entry)) {
            entry.Map.Texture = texture;
        }
        else {
            Logger.Warn($"Failed to set texture for [{key}]. The map might not exist.");
        }
    }
    
    /// <summary>
    /// Retrieves the color associated with the specified material map key.
    /// </summary>
    /// <param name="key">The key of the material map whose color is to be retrieved.</param>
    /// <returns>The color associated with the specified key, or null if the map is not defined.</returns>
    public Color? GetMapColor(MaterialMapKey key) {
        return this._maps.TryGetValue(key, out var entry) ? entry.Map.Color : null;
    }
    
    /// <summary>
    /// Sets the color for a specific material map key.
    /// </summary>
    /// <param name="key">The key of the material map to update.</param>
    /// <param name="color">The new color to assign to the specified material map.</param>
    public void SetMapColor(MaterialMapKey key, Color color) {
        if (this._maps.TryGetValue(key, out var entry)) {
            entry.Map.Color = color;
            this.IsDirty = true;
        }
        else {
            Logger.Warn($"Failed to set color for [{key}]. The map might not exist.");
        }
    }
    
    /// <summary>
    /// Retrieves the value associated with the specified material map key.
    /// </summary>
    /// <param name="key">The key of the material map whose value is to be retrieved.</param>
    /// <returns>The value of the material map if it exists; otherwise, returns 0.0.</returns>
    public float GetMapValue(MaterialMapKey key) {
        return this._maps.TryGetValue(key, out var entry) ? entry.Map.Value : 0.0F;
    }
    
    /// <summary>
    /// Sets the numeric value associated with the specified material map key.
    /// </summary>
    /// <param name="key">The key of the material map whose value is being set.</param>
    /// <param name="value">The numeric value to assign to the material map.</param>
    public void SetMapValue(MaterialMapKey key, float value) {
        if (this._maps.TryGetValue(key, out var entry)) {
            entry.Map.Value = value;
            this.IsDirty = true;
        }
        else {
            Logger.Warn($"Failed to set value for [{key}]. The map might not exist.");
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
        
        foreach (var pair in this._maps) {
            material.AddMaterialMap(pair.Key, pair.Value.Slot, pair.Value.Map);
        }
        
        return material;
    }
}