using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics;
using Bliss.CSharp.Graphics.Pipelines.Textures;
using Bliss.CSharp.Textures;
using Veldrid;
using Vortice.Direct3D11;

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
    public BlendState BlendState { get; private set; }
    
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
    /// A cache of resource sets mapped by sampler and resource layout, improving efficiency when reusing resources.
    /// </summary>
    private Dictionary<(Sampler, ResourceLayout), ResourceSet> _cachedResourceSets;
    
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
        this._cachedResourceSets = new Dictionary<(Sampler, ResourceLayout), ResourceSet>();
    }
    
    public ResourceSet? GetResourceSet(ResourceLayout layout, string mapName) {
        Texture2D? texture = this._maps[mapName].Texture;
        
        if (texture == null) {
            return null;
        }

        Sampler sampler = texture.GetSampler();
        
        if (!this._cachedResourceSets.TryGetValue((sampler, layout), out ResourceSet? resourceSet)) {
            ResourceSet newResourceSet = this.GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(layout, texture.DeviceTexture, texture.GetSampler()));
            
            this._cachedResourceSets.Add((sampler, layout), newResourceSet);
            return newResourceSet;
        }

        return resourceSet;
    }
    
    public string[] GetTextureLayoutKeys() {
        return this._textureLayouts.Keys.ToArray();
    }

    public SimpleTextureLayout[] GetTextureLayouts() {
        return this._textureLayouts.Values.ToArray();
    }

    public SimpleTextureLayout GetTextureLayout(string name) {
        return this._textureLayouts[name];
    }
    
    
    public string[] GetMaterialMapKeys() {
        return this._maps.Keys.ToArray();
    }
    
    public MaterialMap[] GetMaterialMaps() {
        return this._maps.Values.ToArray();
    }

    public MaterialMap GetMaterialMap(string name) {
        return this._maps[name];
    }
    
    public void AddMaterialMap(string name, MaterialMap map) {
        this._maps.Add(name, map);
        this._textureLayouts.Add(name, new SimpleTextureLayout(this.GraphicsDevice, $"{name}Texture"));
    }
    
    
    
    
    public Texture2D? GetMapTexture(string name) {
        return this._maps[name].Texture;
    }
    
    public void SetMapTexture(string name, Texture2D? texture) {
        this._maps[name].Texture = texture;
    }
    
    public Color? GetMapColor(string name) {
        return this._maps[name].Color;
    }
    
    public void SetMapColor(string name, Color color) {
        this._maps[name].Color = color;
    }
    
    public float GetMapValue(string name) {
        return this._maps[name].Value;
    }
    
    public void SetMapValue(string name, float value) {
        this._maps[name].Value = value;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            foreach (SimpleTextureLayout textureLayout in this._textureLayouts.Values) {
                textureLayout.Dispose();
            }

            foreach (ResourceSet resourceSet in this._cachedResourceSets.Values) {
                resourceSet.Dispose();
            }
        }
    }
}