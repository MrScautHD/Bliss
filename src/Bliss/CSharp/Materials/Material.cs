using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Pipelines.Textures;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Textures;
using Veldrid;

namespace Bliss.CSharp.Materials;

public class Material {
    
    /// <summary>
    /// The graphics device associated with this material, used to manage rendering resources.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }

    /// <summary>
    /// The effect (shader program) applied to this material.
    /// </summary>
    public Effect Effect;

    /// <summary>
    /// Specifies the blend state for rendering, determining how colors are blended on the screen.
    /// </summary>
    public BlendStateDescription BlendState;
    
    /// <summary>
    /// A list of floating-point parameters for configuring material properties.
    /// </summary>
    public List<float> Parameters;

    /// <summary>
    /// A dictionary mapping material map types to material map data, used for managing material textures.
    /// </summary>
    private Dictionary<string, MaterialMap> _maps;

    /// <summary>
    /// Initializes a new instance of the <see cref="Material"/> class, configuring it with the specified
    /// graphics device, shader effect, and optional blend state.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device to associate with this material.</param>
    /// <param name="effect">The effect (shader) to apply to the material.</param>
    /// <param name="blendState">The optional blend state to define how this material blends with others during rendering. If not specified, blending is disabled by default.</param>
    public Material(GraphicsDevice graphicsDevice, Effect effect, BlendStateDescription? blendState = null) {
        this.GraphicsDevice = graphicsDevice;
        this.Effect = effect;
        this.BlendState = blendState ?? BlendStateDescription.SINGLE_DISABLED;
        this.Parameters = new List<float>();
        this._maps = new Dictionary<string, MaterialMap>();
    }
    
    /// <summary>
    /// Retrieves the <see cref="ResourceSet"/>.
    /// </summary>
    /// <param name="sampler">The sampler to use for binding the texture. Defines how the texture will be sampled.</param>
    /// <param name="layout">The layout that provides the name and structure for the texture association.</param>
    /// <returns>The corresponding <see cref="ResourceSet"/> if the texture is available; otherwise, null.</returns>
    public ResourceSet? GetResourceSet(Sampler sampler, SimpleTextureLayout layout) {
        Texture2D? texture = this._maps[layout.Name].Texture;
        return texture?.GetResourceSet(sampler, layout);
    }
    
    /// <summary>
    /// Retrieves the names of all material maps.
    /// </summary>
    /// <returns>A collection of strings representing the names of the material maps.</returns>
    public IEnumerable<string> GetMaterialMapNames() {
        return this._maps.Keys;
    }

    /// <summary>
    /// Retrieves all material maps associated with this material.
    /// </summary>
    /// <returns>A collection of <see cref="MaterialMap"/> objects.</returns>
    public IEnumerable<MaterialMap> GetMaterialMaps() {
        return this._maps.Values;
    }

    /// <summary>
    /// Retrieves the material map associated with the specified name.
    /// </summary>
    /// <param name="name">The name of the material map to retrieve.</param>
    /// <returns>The <see cref="MaterialMap"/> associated with the specified name.</returns>
    public MaterialMap? GetMaterialMap(string name) {
        if (this._maps.TryGetValue(name, out MaterialMap? map)) {
            return map;
        }
        
        return null;
    }

    /// <summary>
    /// Adds a MaterialMap to the material's collection, associating it with a specified name.
    /// </summary>
    /// <param name="name">The name to associate with the MaterialMap.</param>
    /// <param name="map">The MaterialMap to be added to the material's collection.</param>
    public void AddMaterialMap(string name, MaterialMap map) {
        if (!this._maps.TryAdd(name, map)) {
            Logger.Warn($"Failed to add MaterialMap with name [{name}]. A material map with this name might already exist.");
        }
    }

    /// <summary>
    /// Retrieves the texture associated with the specified material map name.
    /// </summary>
    /// <param name="name">The name of the material map whose texture is to be retrieved.</param>
    /// <returns>The texture associated with the specified material map, or null if no such texture exists.</returns>
    public Texture2D? GetMapTexture(string name) {
        if (this._maps.TryGetValue(name, out MaterialMap? map)) {
            return map.Texture;
        }
        
        return null;
    }

    /// <summary>
    /// Sets the texture for the specified material map.
    /// </summary>
    /// <param name="name">The name of the material map to set the texture for.</param>
    /// <param name="texture">The texture to be set. If null, the material map's texture will be removed.</param>
    public void SetMapTexture(string name, Texture2D? texture) {
        if (this._maps.TryGetValue(name, out MaterialMap? map)) {
            map.Texture = texture;
        }
        else {
            Logger.Warn($"Failed to set texture for: [{name}]. The map might not exist.");
        }
    }

    /// <summary>
    /// Retrieves the color associated with the specified material map.
    /// </summary>
    /// <param name="name">The name of the material map from which to retrieve the color.</param>
    /// <returns>The <see cref="Color"/> associated with the specified material map, or null if the map does not exist or does not have a color defined.</returns>
    public Color? GetMapColor(string name) {
        if (this._maps.TryGetValue(name, out MaterialMap? map)) {
            return map.Color;
        }
        
        return null;
    }

    /// <summary>
    /// Sets the color of a specified material map.
    /// </summary>
    /// <param name="name">The name of the material map whose color is to be set.</param>
    /// <param name="color">The color to assign to the material map.</param>
    public void SetMapColor(string name, Color color) {
        if (this._maps.TryGetValue(name, out MaterialMap? map)) {
            map.Color = color;
        }
        else {
            Logger.Warn($"Failed to set color for: [{name}]. The map might not exist.");
        }
    }

    /// <summary>
    /// Gets the value associated with the specified material map name.
    /// </summary>
    /// <param name="name">The name of the material map for which to retrieve the value.</param>
    /// <returns>The floating-point value associated with the specified material map name.</returns>
    public float GetMapValue(string name) {
        if (this._maps.TryGetValue(name, out MaterialMap? map)) {
            return map.Value;
        }
        
        return 0.0F;
    }

    /// <summary>
    /// Sets the value of the specified material map.
    /// </summary>
    /// <param name="name">The name of the material map to update.</param>
    /// <param name="value">The floating-point value to set for the specified material map.</param>
    public void SetMapValue(string name, float value) {
        if (this._maps.TryGetValue(name, out MaterialMap? map)) {
            map.Value = value;
        }
        else {
            Logger.Warn($"Failed to set value for: [{name}]. The map might not exist.");
        }
    }
}