using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Pipelines.Textures;
using Bliss.CSharp.Textures;
using Veldrid;

namespace Bliss.CSharp.Materials;

public class Material : Disposable {
    
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    public Effect Effect { get; private set; }
    public SimpleTextureLayout[] TextureLayouts { get; private set; }
    
    public List<float> Parameters;

    private Dictionary<MaterialMapType, MaterialMap> _maps;
    private Dictionary<(Sampler, ResourceLayout), ResourceSet> _cachedResourceSets;
    
    public Material(GraphicsDevice graphicsDevice, Effect effect) {
        this.GraphicsDevice = graphicsDevice;
        this.Effect = effect;
        this.TextureLayouts = this.CreateTextureLayout(graphicsDevice);
        this.Parameters = new List<float>();
        this._maps = this.SetDefaultMaterialMaps();
        this._cachedResourceSets = new Dictionary<(Sampler, ResourceLayout), ResourceSet>();
    }

    /// <summary>
    /// Retrieves the resource set associated with the specified resource layout and material map type.
    /// </summary>
    /// <param name="layout">The resource layout for which the resource set is to be retrieved.</param>
    /// <param name="mapType">The type of the material map.</param>
    /// <returns>The resource set associated with the specified layout and material map type, or null if the texture is not found.</returns>
    public ResourceSet? GetResourceSet(ResourceLayout layout, MaterialMapType mapType) {
        Texture2D? texture = this._maps[mapType].Texture;
        
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

    /// <summary>
    /// Retrieves the material map associated with the specified material map type.
    /// </summary>
    /// <param name="mapType">The type of the material map to retrieve.</param>
    /// <returns>The material map of the specified type.</returns>
    public MaterialMap GetMaterialMap(MaterialMapType mapType) {
        return this._maps[mapType];
    }

    /// <summary>
    /// Sets the material map for the specified material map type.
    /// </summary>
    /// <param name="mapType">The type of the material map to set.</param>
    /// <param name="map">The material map to assign to the specified type.</param>
    public void SetMaterialMap(MaterialMapType mapType, MaterialMap map) {
        this._maps[mapType] = map;
    }

    /// <summary>
    /// Retrieves the texture associated with the specified material map type.
    /// </summary>
    /// <param name="mapType">The type of the material map from which to retrieve the texture.</param>
    /// <returns>The texture associated with the specified material map type, or null if none exists.</returns>
    public Texture2D? GetMapTexture(MaterialMapType mapType) {
        return this._maps[mapType].Texture;
    }

    /// <summary>
    /// Sets the texture for the specified material map type.
    /// </summary>
    /// <param name="mapType">The type of material map to set the texture for.</param>
    /// <param name="texture">The texture to associate with the specified material map type.</param>
    public void SetMapTexture(MaterialMapType mapType, Texture2D? texture) {
        this._maps[mapType] = new MaterialMap() {
            Texture = texture,
            Color = this.GetMapColor(mapType),
            Value = this.GetMapValue(mapType)
        };
    }

    /// <summary>
    /// Retrieves the color associated with the specified material map type.
    /// </summary>
    /// <param name="mapType">The type of the material map to retrieve the color for.</param>
    /// <returns>The color of the specified material map type, or null if no color is associated.</returns>
    public Color? GetMapColor(MaterialMapType mapType) {
        return this._maps[mapType].Color;
    }

    /// <summary>
    /// Sets the color for the specified material map type.
    /// </summary>
    /// <param name="mapType">The type of the material map to set the color for.</param>
    /// <param name="color">The color to set for the specified material map type.</param>
    public void SetMapColor(MaterialMapType mapType, Color color) {
        this._maps[mapType] = new MaterialMap() {
            Texture = this.GetMapTexture(mapType),
            Color = color,
            Value = this.GetMapValue(mapType)
        };
    }

    /// <summary>
    /// Retrieves the value associated with the specified material map type.
    /// </summary>
    /// <param name="mapType">The type of the material map to retrieve the value from.</param>
    /// <returns>The numeric value of the specified material map type.</returns>
    public float GetMapValue(MaterialMapType mapType) {
        return this._maps[mapType].Value;
    }

    /// <summary>
    /// Sets the value for the specified material map type.
    /// </summary>
    /// <param name="mapType">The type of the material map to modify.</param>
    /// <param name="value">The value to set for the specified material map type.</param>
    public void SetMapValue(MaterialMapType mapType, float value) {
        this._maps[mapType] = new MaterialMap() {
            Texture = this.GetMapTexture(mapType),
            Color = this.GetMapColor(mapType),
            Value = value
        };
    }

    /// <summary>
    /// Initializes and returns a dictionary containing default material maps for various material map types.
    /// </summary>
    /// <returns>A dictionary where the key is the material map type and the value is the associated default material map.</returns>
    private Dictionary<MaterialMapType, MaterialMap> SetDefaultMaterialMaps() {
        return new Dictionary<MaterialMapType, MaterialMap> {
            {
                MaterialMapType.Albedo, new MaterialMap()
            },
            {
                MaterialMapType.Metalness, new MaterialMap()
            },
            {
                MaterialMapType.Normal, new MaterialMap()
            },
            {
                MaterialMapType.Roughness, new MaterialMap()
            },
            {
                MaterialMapType.Occlusion, new MaterialMap()
            },
            {
                MaterialMapType.Emission, new MaterialMap()
            },
            {
                MaterialMapType.Height, new MaterialMap()
            },
            {
                MaterialMapType.Cubemap, new MaterialMap()
            },
            {
                MaterialMapType.Irradiance, new MaterialMap()
            },
            {
                MaterialMapType.Prefilter, new MaterialMap()
            },
            {
                MaterialMapType.Brdf, new MaterialMap()
            }
        };
    }

    /// <summary>
    /// Creates an array of texture layouts for the specified graphics device.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create the texture layouts.</param>
    /// <returns>An array of <see cref="SimpleTextureLayout"/> objects corresponding to various material textures.</returns>
    private SimpleTextureLayout[] CreateTextureLayout(GraphicsDevice graphicsDevice) {
        return [
            new SimpleTextureLayout(graphicsDevice, "fAlbedoTexture"),
            new SimpleTextureLayout(graphicsDevice, "fMetallicTexture"),
            new SimpleTextureLayout(graphicsDevice, "fNormalTexture"),
            new SimpleTextureLayout(graphicsDevice, "fRoughnessTexture"),
            new SimpleTextureLayout(graphicsDevice, "fOcclusionTexture"),
            new SimpleTextureLayout(graphicsDevice, "fEmissionTexture"),
            new SimpleTextureLayout(graphicsDevice, "fHeightTexture"),
            new SimpleTextureLayout(graphicsDevice, "fCubemapTexture"),
            new SimpleTextureLayout(graphicsDevice, "fIrradianceTexture"),
            new SimpleTextureLayout(graphicsDevice, "fPrefilterTexture"),
            new SimpleTextureLayout(graphicsDevice, "fBrdfTexture")
        ];
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            foreach (SimpleTextureLayout textureLayout in this.TextureLayouts) {
                textureLayout.Dispose();
            }
        }
    }
}