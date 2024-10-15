using System.Collections.ObjectModel;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Pipelines.Textures;
using Veldrid;

namespace Bliss.CSharp.Materials;

public class Material : Disposable {
    
    public GraphicsDevice GraphicsDevice { get; private set; }
    public Effect Effect { get; private set; }
    public SimpleTextureLayout[] TextureLayouts { get; private set; }
    
    public ReadOnlyDictionary<MaterialMapType, MaterialMap> Maps { get; private set; }
    public List<float> Parameters;

    private bool _disposeResources;

    public Material(GraphicsDevice graphicsDevice, Effect effect, ReadOnlyDictionary<MaterialMapType, MaterialMap> maps, List<float>? parameters = null, bool disposeResources = false) {
        this.GraphicsDevice = graphicsDevice;
        this.Effect = effect;
        this.TextureLayouts = this.CreateTextureLayout(graphicsDevice);
        this.Maps = maps;
        this.Parameters = parameters ?? new List<float>();
        this._disposeResources = disposeResources;
    }

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
            if (this._disposeResources) {
                
                // Unload effect.
                this.Effect.Dispose();

                // Unload textures.
                foreach (MaterialMap map in this.Maps.Values) {
                    map.Texture.Dispose();
                }
            }

            foreach (SimpleTextureLayout textureLayout in this.TextureLayouts) {
                textureLayout.Dispose();
            }
        }
    }
}