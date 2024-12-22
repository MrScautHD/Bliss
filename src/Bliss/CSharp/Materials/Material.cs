/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics;
using Bliss.CSharp.Graphics.Pipelines.Textures;
using Bliss.CSharp.Textures;
using Veldrid;

namespace Bliss.CSharp.Materials;

public class Material : Disposable {
    
    /// <summary>
    /// The graphics device associated with this material, used to manage rendering resources.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// The effect (shader program) applied to this material.
    /// </summary>
    public Effect Effect { get; private set; }

    /// <summary>
    /// Specifies the blend state for rendering, determining how colors are blended on the screen.
    /// </summary>
    public BlendState BlendState;
    
    /// <summary>
    /// A list of floating-point parameters for configuring material properties.
    /// </summary>
    public List<float> Parameters;

    /// <summary>
    /// A dictionary that maps texture names to their corresponding simple texture layouts.
    /// Used for managing texture configurations associated with material maps.
    /// </summary>
    private Dictionary<string, SimpleTextureLayout> _textureLayouts;

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
    public Material(GraphicsDevice graphicsDevice, Effect effect, BlendState? blendState = default) {
        this.GraphicsDevice = graphicsDevice;
        this.Effect = effect;
        this.BlendState = blendState ?? BlendState.Disabled;
        this.Parameters = new List<float>();
        this._textureLayouts = new Dictionary<string, SimpleTextureLayout>();
        this._maps = new Dictionary<string, MaterialMap>();
    }
    
    /// <summary>
    /// Retrieves a <see cref="ResourceSet"/> associated with the specified layout and map name.
    /// </summary>
    /// <param name="layout">The <see cref="ResourceLayout"/> that defines the structure of the resources.</param>
    /// <param name="mapName">The name of the map to retrieve the texture for.</param>
    /// <returns>The corresponding <see cref="ResourceSet"/> if available; otherwise, null.</returns>
    public ResourceSet? GetResourceSet(ResourceLayout layout, string mapName) {
        Texture2D? texture = this._maps[mapName].Texture;
        return texture?.GetResourceSet(texture.GetSampler(), layout);
    }

    /// <summary>
    /// Retrieves the keys of the texture layouts associated with this material.
    /// </summary>
    /// <returns>An array of strings representing the keys of the texture layouts.</returns>
    public string[] GetTextureLayoutKeys() {
        return this._textureLayouts.Keys.ToArray();
    }

    /// <summary>
    /// Retrieves all the texture layouts associated with this material.
    /// </summary>
    /// <returns>An array of <see cref="SimpleTextureLayout"/> objects that includes all the texture layouts defined for this material.</returns>
    public SimpleTextureLayout[] GetTextureLayouts() {
        return this._textureLayouts.Values.ToArray();
    }

    /// <summary>
    /// Retrieves the texture layout associated with the specified name.
    /// </summary>
    /// <param name="name">The name of the texture layout to retrieve.</param>
    /// <returns>A <see cref="SimpleTextureLayout"/> instance corresponding to the provided name.</returns>
    public SimpleTextureLayout GetTextureLayout(string name) {
        return this._textureLayouts[name];
    }

    /// <summary>
    /// Retrieves an array containing the keys of all material maps currently stored in the material.
    /// </summary>
    /// <return>An array of strings, each representing a key for a material map.</return>
    public string[] GetMaterialMapKeys() {
        return this._maps.Keys.ToArray();
    }

    /// <summary>
    /// Retrieves an array of all material maps associated with the current material.
    /// </summary>
    /// <returns>
    /// An array of <see cref="MaterialMap"/> objects representing the material maps.
    /// </returns>
    public MaterialMap[] GetMaterialMaps() {
        return this._maps.Values.ToArray();
    }

    /// <summary>
    /// Retrieves the material map associated with the specified name.
    /// </summary>
    /// <param name="name">The name of the material map to retrieve.</param>
    /// <returns>The <see cref="MaterialMap"/> associated with the specified name.</returns>
    public MaterialMap GetMaterialMap(string name) {
        return this._maps[name];
    }

    /// <summary>
    /// Adds a MaterialMap to the material's collection, associating it with a specified name.
    /// </summary>
    /// <param name="name">The name to associate with the MaterialMap.</param>
    /// <param name="map">The MaterialMap to be added to the material's collection.</param>
    public void AddMaterialMap(string name, MaterialMap map) {
        this._maps.Add(name, map);
        this._textureLayouts.Add(name, new SimpleTextureLayout(this.GraphicsDevice, $"{name}Texture"));
    }

    /// <summary>
    /// Retrieves the texture associated with the specified material map name.
    /// </summary>
    /// <param name="name">The name of the material map whose texture is to be retrieved.</param>
    /// <returns>The texture associated with the specified material map, or null if no such texture exists.</returns>
    public Texture2D? GetMapTexture(string name) {
        return this._maps[name].Texture;
    }

    /// <summary>
    /// Sets the texture for the specified material map.
    /// </summary>
    /// <param name="name">The name of the material map to set the texture for.</param>
    /// <param name="texture">The texture to be set. If null, the material map's texture will be removed.</param>
    public void SetMapTexture(string name, Texture2D? texture) {
        this._maps[name].Texture = texture;
    }

    /// <summary>
    /// Retrieves the color associated with the specified material map.
    /// </summary>
    /// <param name="name">The name of the material map from which to retrieve the color.</param>
    /// <returns>The <see cref="Color"/> associated with the specified material map, or null if the map does not exist or does not have a color defined.</returns>
    public Color? GetMapColor(string name) {
        return this._maps[name].Color;
    }

    /// <summary>
    /// Sets the color of a specified material map.
    /// </summary>
    /// <param name="name">The name of the material map whose color is to be set.</param>
    /// <param name="color">The color to assign to the material map.</param>
    public void SetMapColor(string name, Color color) {
        this._maps[name].Color = color;
    }

    /// <summary>
    /// Gets the value associated with the specified material map name.
    /// </summary>
    /// <param name="name">The name of the material map for which to retrieve the value.</param>
    /// <returns>The floating-point value associated with the specified material map name.</returns>
    public float GetMapValue(string name) {
        return this._maps[name].Value;
    }

    /// <summary>
    /// Sets the value of the specified material map.
    /// </summary>
    /// <param name="name">The name of the material map to update.</param>
    /// <param name="value">The floating-point value to set for the specified material map.</param>
    public void SetMapValue(string name, float value) {
        this._maps[name].Value = value;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            foreach (SimpleTextureLayout textureLayout in this._textureLayouts.Values) {
                textureLayout.Dispose();
            }
        }
    }
}