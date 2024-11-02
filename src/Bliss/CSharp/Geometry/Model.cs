using System.Numerics;
using System.Text;
using Assimp;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Geometry.Conversions;
using Bliss.CSharp.Graphics;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Materials;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
using Veldrid;
using AMesh = Assimp.Mesh;
using ShaderMaterialProperties = Assimp.Material.ShaderMaterialProperties;
using Material = Bliss.CSharp.Materials.Material;
using AMaterial = Assimp.Material;
using TextureType = Assimp.TextureType;

namespace Bliss.CSharp.Geometry;

public class Model : Disposable {
    
    /// <summary>
    /// The default post-processing steps applied to the model during processing.
    /// This includes flipping the winding order, triangulating, pre-transforming vertices,
    /// calculating tangent space, and generating smooth normals.
    /// </summary>
    private const PostProcessSteps DefaultPostProcessSteps = PostProcessSteps.FlipWindingOrder | PostProcessSteps.Triangulate | PostProcessSteps.PreTransformVertices | PostProcessSteps.CalculateTangentSpace | PostProcessSteps.GenerateSmoothNormals;

    /// <summary>
    /// The default effect applied to models.
    /// This is instantiated with shaders for rendering models, and includes configuration for vertex layout.
    /// </summary>
    private static Effect? _defaultEffect;
    
    /// <summary>
    /// The graphics device used for rendering the model.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// An array of meshes that make up the model.
    /// </summary>
    public Mesh[] Meshes { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Model"/> class with the specified graphics device and meshes.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering the model.</param>
    /// <param name="meshes">An array of meshes that compose the model.</param>
    public Model(GraphicsDevice graphicsDevice, Mesh[] meshes) {
        this.GraphicsDevice = graphicsDevice;
        this.Meshes = meshes;
    }
    
    // TODO: Check if the UV flip works maybe it should just the Y axis get fliped and add Materials loading (with a boolean to disable it) and add Animations loading and add a option to load with Stream instead of the path.
    public static Model Load(GraphicsDevice graphicsDevice, string path, bool loadMaterial = true, bool flipUv = false) {
        using AssimpContext context = new AssimpContext();
        Scene scene = context.ImportFile(path, DefaultPostProcessSteps);

        List<Mesh> meshes = new List<Mesh>();

        for (int i = 0; i < scene.Meshes.Count; i++) {
            AMesh mesh = scene.Meshes[i];
            
            // Load effect.
            Effect? effect = null;

            if (scene.HasMaterials && loadMaterial) {
                AMaterial aMaterial = scene.Materials[mesh.MaterialIndex];
                ShaderMaterialProperties shaderProperties = aMaterial.Shaders;
                
                if (shaderProperties.HasVertexShader && shaderProperties.HasFragmentShader) {
                    Logger.Error(shaderProperties.VertexShader);
                    effect = new Effect(graphicsDevice.ResourceFactory, Vertex3D.VertexLayout, Encoding.UTF8.GetBytes(shaderProperties.VertexShader), Encoding.UTF8.GetBytes(shaderProperties.FragmentShader));
                }
                else {
                    Logger.Warn($"Failed to load material effect for model at path: [{path}]");
                }
            }

            effect ??= GetDefaultEffect(graphicsDevice);
            
            // Load material maps. // TODO: loading texture are still broken!
            Material material = new Material(graphicsDevice, effect);

            if (scene.HasMaterials && loadMaterial) {
                AMaterial aMaterial = scene.Materials[mesh.MaterialIndex];
                
                for (int j = 0; j < 11; j++) {
                    MaterialMapType mapType = (MaterialMapType) j;
                    material.SetMaterialMap(mapType, GetMaterialMap(graphicsDevice, mapType, aMaterial));
                }
            }
            
            // Setup vertices.
            Vertex3D[] vertices = new Vertex3D[scene.Meshes[i].VertexCount];

            for (int j = 0; j < mesh.VertexCount; j++) {
                
                // Set Position.
                vertices[j].Position = ModelConversion.FromVector3D(mesh.Vertices[j]);

                // Set TexCoord.
                if (mesh.HasTextureCoords(0)) {
                    Vector3 texCoord = ModelConversion.FromVector3D(mesh.TextureCoordinateChannels[0][j]);
                    Vector2 finalTexCoord = new Vector2(texCoord.X, -texCoord.Y);
                    
                    vertices[j].TexCoords = flipUv ? -finalTexCoord : finalTexCoord;
                }
                else {
                    vertices[j].TexCoords = Vector2.Zero;
                }
                
                // Set TexCoord2.
                if (mesh.HasTextureCoords(1)) {
                    Vector3 texCoord2 = ModelConversion.FromVector3D(mesh.TextureCoordinateChannels[1][j]);
                    Vector2 finalTexCoord2 = new Vector2(texCoord2.X, -texCoord2.Y);
                    
                    vertices[j].TexCoords2 = flipUv ? -finalTexCoord2 : finalTexCoord2;
                }
                else {
                    vertices[j].TexCoords2 = Vector2.Zero;
                }
                
                // Set Normal.
                vertices[j].Normal = mesh.HasNormals ? ModelConversion.FromVector3D(mesh.Normals[i]) : Vector3.Zero;

                // Set Tangent.
                vertices[j].Tangent = mesh.HasTangentBasis ? ModelConversion.FromVector3D(mesh.Tangents[j]) : Vector3.Zero;
                
                // Set Color.
                vertices[j].Color = material.GetMapColor(MaterialMapType.Albedo)?.ToVector4() ?? Vector4.Zero;
            }

            // Setup indices.
            List<uint> indices = new List<uint>();
            
            for (int j = 0; j < mesh.FaceCount; j++) {
                Face face = mesh.Faces[j];

                if (face.IndexCount != 3) {
                    continue;
                }
                
                indices.Add((uint) face.Indices[0]);
                indices.Add((uint) face.Indices[1]);
                indices.Add((uint) face.Indices[2]);
            }

            meshes.Add(new Mesh(graphicsDevice, material, vertices, indices.ToArray()));
        }
        
        Logger.Info($"Model loaded successfully from path: [{path}]");
        Logger.Info($"\t> Meshes: {meshes.Count}");

        return new Model(graphicsDevice, meshes.ToArray());
    }

    /// <summary>
    /// Retrieves the default effect for the model.
    /// If the default effect is not already created, it initializes a new <see cref="Effect"/> with the specified graphics device.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create the default effect.</param>
    /// <return>The default <see cref="Effect"/> for the model.</return>
    private static Effect GetDefaultEffect(GraphicsDevice graphicsDevice) {
        return _defaultEffect ??= new Effect(graphicsDevice.ResourceFactory, Vertex3D.VertexLayout, "content/shaders/default_model.vert", "content/shaders/default_model.frag");
    }

    /// <summary>
    /// Creates a <see cref="MaterialMap"/> from the given graphics device, material map type, and Assimp material.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device to use for creating textures.</param>
    /// <param name="mapType">The type of material map to be created.</param>
    /// <param name="aMaterial">The Assimp material source.</param>
    /// <returns>A <see cref="MaterialMap"/> containing the texture and color corresponding to the specified material map type.</returns>
    private static MaterialMap GetMaterialMap(GraphicsDevice graphicsDevice, MaterialMapType mapType, AMaterial aMaterial) {
        string? texturePath = GetMapTypeTexturePath(mapType, aMaterial);
        Color4D? color4D = GetMapTypeColor(mapType, aMaterial);

        Texture2D? texture = !string.IsNullOrEmpty(texturePath) ? new Texture2D(graphicsDevice, texturePath) : null;
        Color? color = color4D != null ? new Color(new RgbaFloat(color4D.Value.R, color4D.Value.G, color4D.Value.B, color4D.Value.A)) : null;
        
        return new MaterialMap {
            Texture = texture,
            Color = color
        };
    }

    /// <summary>
    /// Retrieves the file path for a texture associated with the specified material map type from the given Assimp material.
    /// </summary>
    /// <param name="mapType">The type of material map whose texture path should be retrieved.</param>
    /// <param name="aMaterial">The Assimp material containing texture information.</param>
    /// <returns>A string representing the file path to the texture associated with the specified material map type, or null if no texture is found.</returns>
    private static string? GetMapTypeTexturePath(MaterialMapType mapType, AMaterial aMaterial) {
        return mapType switch {
            MaterialMapType.Albedo => aMaterial.HasTextureDiffuse ? aMaterial.GetMaterialTextures(TextureType.Diffuse).FirstOrDefault().FilePath : null,
            MaterialMapType.Metalness => aMaterial.HasTextureSpecular ? aMaterial.GetMaterialTextures(TextureType.Metalness).FirstOrDefault().FilePath : null,
            MaterialMapType.Normal => aMaterial.HasTextureNormal ? aMaterial.GetMaterialTextures(TextureType.Normals).FirstOrDefault().FilePath : null,
            MaterialMapType.Roughness => aMaterial.PBR.HasTextureRoughness ? aMaterial.GetMaterialTextures(TextureType.Roughness).FirstOrDefault().FilePath : null,
            MaterialMapType.Occlusion => aMaterial.HasTextureAmbientOcclusion ? aMaterial.GetMaterialTextures(TextureType.AmbientOcclusion).FirstOrDefault().FilePath : null,
            MaterialMapType.Emission => aMaterial.HasTextureEmissive ? aMaterial.GetMaterialTextures(TextureType.Emissive).FirstOrDefault().FilePath : null,
            MaterialMapType.Height => aMaterial.HasTextureHeight ? aMaterial.GetMaterialTextures(TextureType.Height).FirstOrDefault().FilePath : null,
            _ => null
        };
    }

    /// <summary>
    /// Retrieves the color corresponding to a specific material map type from the given Assimp material.
    /// </summary>
    /// <param name="mapType">The type of material map to retrieve the color for.</param>
    /// <param name="aMaterial">The Assimp material from which the color is retrieved.</param>
    /// <returns>A nullable <see cref="Color4D"/> representing the color corresponding to the specified material map type, or null if the color is not available.</returns>
    private static Color4D? GetMapTypeColor(MaterialMapType mapType, AMaterial aMaterial) {
        return mapType switch {
            MaterialMapType.Albedo => aMaterial.HasColorDiffuse ? aMaterial.ColorDiffuse : null,
            MaterialMapType.Metalness => aMaterial.HasColorSpecular ? aMaterial.ColorSpecular : null,
            MaterialMapType.Emission => aMaterial.HasTextureEmissive ? aMaterial.ColorDiffuse : null,
            MaterialMapType.Height => aMaterial.HasTextureHeight ? aMaterial.ColorDiffuse : null,
            _ => null
        };
    }
    
    /// <summary>
    /// Draws the model using the specified command list, output description, sampler type, transform, blend state, and color.
    /// </summary>
    /// <param name="commandList">The command list used to issue drawing commands.</param>
    /// <param name="output">The output description of the render target.</param>
    /// <param name="transform">The transformation applied to the model.</param>
    /// <param name="blendState">The blend state used during rendering.</param>
    /// <param name="color">The color applied to the model.</param>
    public void Draw(CommandList commandList, OutputDescription output, Transform transform, BlendState blendState, Color color) {
        foreach (Mesh mesh in this.Meshes) {
            mesh.Draw(commandList, output, transform, blendState, color);
        }
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            foreach (Mesh mesh in this.Meshes) {
                mesh.Dispose();
            }
            
            // TODO: Unload cached resoucres (materials)
        }
    }
}