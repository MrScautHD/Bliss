using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Textures;
using Veldrid;

namespace Bliss.CSharp.Materials;

public class MaterialOld : Disposable {

    /// <summary>
    /// The effect used to render the material. If not provided, a default effect is used.
    /// </summary>
    public Effect Effect { get; private set; }
    
    /// <summary>
    /// The albedo (diffuse) texture of the material.
    /// </summary>
    public Texture2D Albedo;
    
    /// <summary>
    /// The metalness texture of the material.
    /// </summary>
    public Texture2D Metalness;
    
    /// <summary>
    /// The normal map texture of the material.
    /// </summary>
    public Texture2D Normal;
    
    /// <summary>
    /// The roughness texture of the material.
    /// </summary>
    public Texture2D Roughness;
    
    /// <summary>
    /// The occlusion texture of the material.
    /// </summary>
    public Texture2D Occlusion;
    
    /// <summary>
    /// The emission texture of the material.
    /// </summary>
    public Texture2D Emission;
    
    /// <summary>
    /// The height map texture of the material.
    /// </summary>
    public Texture2D Height;
    
    /// <summary>
    /// The cubemap texture of the material for environment mapping.
    /// </summary>
    public Texture2D Cubemap;
    
    /// <summary>
    /// The irradiance map texture for ambient lighting.
    /// </summary>
    public Texture2D Irradiance;
    
    /// <summary>
    /// The prefilter texture for specular reflection.
    /// </summary>
    public Texture2D Prefilter;
    
    /// <summary>
    /// The BRDF lookup texture used for rendering.
    /// </summary>
    public Texture2D Brdf;
    
    /// <summary>
    /// The color value associated with the albedo texture.
    /// </summary>
    public Color AlbedoColor;
    
    /// <summary>
    /// The color value associated with the metalness texture.
    /// </summary>
    public Color MetallicColor;
    
    /// <summary>
    /// The color value associated with the normal map.
    /// </summary>
    public Color NormalColor;
    
    /// <summary>
    /// The color value associated with the roughness texture.
    /// </summary>
    public Color RoughnessColor;
    
    /// <summary>
    /// The color value associated with the occlusion texture.
    /// </summary>
    public Color OcclusionColor;
    
    /// <summary>
    /// The color value associated with the emission texture.
    /// </summary>
    public Color EmissionColor;
    
    /// <summary>
    /// The color value associated with the height map.
    /// </summary>
    public Color HeightColor;
    
    /// <summary>
    /// The color value associated with the cubemap.
    /// </summary>
    public Color CubemapColor;
    
    /// <summary>
    /// The color value associated with the irradiance map.
    /// </summary>
    public Color IrradianceColor;
    
    /// <summary>
    /// The color value associated with the prefilter texture.
    /// </summary>
    public Color PrefilterColor;
    
    /// <summary>
    /// The color value associated with the BRDF texture.
    /// </summary>
    public Color BrdfColor;
    
    /// <summary>
    /// The scalar value representing the influence of the albedo texture.
    /// </summary>
    public float AlbedoValue;
    
    /// <summary>
    /// The scalar value representing the influence of the metalness texture.
    /// </summary>
    public float MetallicValue;
    
    /// <summary>
    /// The scalar value representing the influence of the normal map.
    /// </summary>
    public float NormalValue;
    
    /// <summary>
    /// The scalar value representing the influence of the roughness texture.
    /// </summary>
    public float RoughnessValue;
    
    /// <summary>
    /// The scalar value representing the influence of the occlusion texture.
    /// </summary>
    public float OcclusionValue;
    
    /// <summary>
    /// The scalar value representing the influence of the emission texture.
    /// </summary>
    public float EmissionValue;
    
    /// <summary>
    /// The scalar value representing the influence of the height map.
    /// </summary>
    public float HeightValue;
    
    /// <summary>
    /// The scalar value representing the influence of the cubemap texture.
    /// </summary>
    public float CubemapValue;
    
    /// <summary>
    /// The scalar value representing the influence of the irradiance texture.
    /// </summary>
    public float IrradianceValue;
    
    /// <summary>
    /// The scalar value representing the influence of the prefilter texture.
    /// </summary>
    public float PrefilterValue;
    
    /// <summary>
    /// The scalar value representing the influence of the BRDF texture.
    /// </summary>
    public float BrdfValue;

    /// <summary>
    /// Represents the blending state used for rendering in order to determine how the colors of source and destination are combined.
    /// </summary>
    public BlendState BlendState;
    
    /// <summary>
    /// The default effect used by the material if no other effect is specified.
    /// </summary>
    private Effect _defaultEffect;

    /// <summary>
    /// Initializes a new instance of the <see cref="MaterialOld"/> class, with the option to provide a custom effect.
    /// If no effect is provided, a default effect is created.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering resources.</param>
    /// <param name="effect">An optional custom effect for the material. If null, a default effect is used.</param>
    public MaterialOld(GraphicsDevice graphicsDevice, Effect? effect = null) {
        this._defaultEffect = new Effect(graphicsDevice.ResourceFactory, Vertex3D.VertexLayout, "content/shaders/default_model.vert", "content/shaders/default_model.frag");
        this.Effect = effect ?? this._defaultEffect;
        this.BlendState = BlendState.AlphaBlend;
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._defaultEffect.Dispose();
        }
    }
}