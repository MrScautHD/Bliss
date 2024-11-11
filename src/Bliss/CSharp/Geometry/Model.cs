using System.Numerics;
using System.Text;
using Assimp;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Geometry.Conversions;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Materials;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using AMesh = Assimp.Mesh;
using ShaderMaterialProperties = Assimp.Material.ShaderMaterialProperties;
using Material = Bliss.CSharp.Materials.Material;
using AMaterial = Assimp.Material;
using Color = Bliss.CSharp.Colors.Color;
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
    /// Represents the axis-aligned bounding box of the model.
    /// </summary>
    public BoundingBox BoundingBox { get; private set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Model"/> class with the specified graphics device and meshes.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering the model.</param>
    /// <param name="meshes">An array of meshes that compose the model.</param>
    public Model(GraphicsDevice graphicsDevice, Mesh[] meshes) {
        this.GraphicsDevice = graphicsDevice;
        this.Meshes = meshes;
        this.BoundingBox = this.GenerateBoundingBox();
    }

    /// <summary>
    /// Loads a model from the specified file path using the provided graphics device.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering the model.</param>
    /// <param name="path">The file path to the model to be loaded.</param>
    /// <param name="loadMaterial">Indicates whether the material should be loaded with the model. Default is true.</param>
    /// <param name="flipUv">Indicates whether the UV coordinates should be flipped. Default is false.</param>
    /// <returns>Returns a new instance of the <see cref="Model"/> class with the loaded meshes.</returns>
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
                    effect = new Effect(graphicsDevice.ResourceFactory, Vertex3D.VertexLayout, Encoding.UTF8.GetBytes(shaderProperties.VertexShader), Encoding.UTF8.GetBytes(shaderProperties.FragmentShader));
                }
            }

            effect ??= GetDefaultEffect(graphicsDevice);
            
            // Load material maps.
            Material material = new Material(graphicsDevice, effect);

            if (scene.HasMaterials && loadMaterial) {
                AMaterial aMaterial = scene.Materials[mesh.MaterialIndex];
                
                material.SetMaterialMap(MaterialMapType.Albedo, new MaterialMap() {
                    Texture = LoadMaterialTexture(graphicsDevice, scene, aMaterial, path, TextureType.Diffuse),
                    Color = new Color((byte) aMaterial.ColorDiffuse.R, (byte) aMaterial.ColorDiffuse.G, (byte) aMaterial.ColorDiffuse.B, (byte) aMaterial.ColorDiffuse.A)
                });
                
                material.SetMaterialMap(MaterialMapType.Metalness, new MaterialMap() {
                    Texture = LoadMaterialTexture(graphicsDevice, scene, aMaterial, path, TextureType.Metalness),
                    Color = new Color((byte) aMaterial.ColorSpecular.R, (byte) aMaterial.ColorSpecular.G, (byte) aMaterial.ColorSpecular.B, (byte) aMaterial.ColorSpecular.A)
                });
                
                material.SetMaterialMap(MaterialMapType.Normal, new MaterialMap() {
                    Texture = LoadMaterialTexture(graphicsDevice, scene, aMaterial, path, TextureType.Normals)
                });
                
                material.SetMaterialMap(MaterialMapType.Roughness, new MaterialMap() {
                    Texture = LoadMaterialTexture(graphicsDevice, scene, aMaterial, path, TextureType.Roughness)
                });
                
                material.SetMaterialMap(MaterialMapType.Occlusion, new MaterialMap() {
                    Texture = LoadMaterialTexture(graphicsDevice, scene, aMaterial, path, TextureType.AmbientOcclusion)
                });
                
                material.SetMaterialMap(MaterialMapType.Emission, new MaterialMap() {
                    Texture = LoadMaterialTexture(graphicsDevice, scene, aMaterial, path, TextureType.Emissive),
                    Color = new Color((byte) aMaterial.ColorEmissive.R, (byte) aMaterial.ColorEmissive.G, (byte) aMaterial.ColorEmissive.B, (byte) aMaterial.ColorEmissive.A)
                });
                
                material.SetMaterialMap(MaterialMapType.Height, new MaterialMap() {
                    Texture = LoadMaterialTexture(graphicsDevice, scene, aMaterial, path, TextureType.Height)
                });
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
                vertices[j].Normal = mesh.HasNormals ? ModelConversion.FromVector3D(mesh.Normals[j]) : Vector3.Zero;

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
            
            // Setup bones.
            for (int j = 0; j < mesh.BoneCount; j++) {
                Bone bone = mesh.Bones[j];

                foreach (VertexWeight vertexWeight in bone.VertexWeights) {
                    vertices[vertexWeight.VertexID].AddBone((uint) j, vertexWeight.Weight);
                }
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
    private static Effect GetDefaultEffect(GraphicsDevice graphicsDevice) { // TODO: Take care to dispose it!
        return _defaultEffect ??= new Effect(graphicsDevice.ResourceFactory, Vertex3D.VertexLayout, "content/shaders/default_model.vert", "content/shaders/default_model.frag");
    }

    /// <summary>
    /// Loads a texture from a material and returns it as a Texture2D object. If the texture is embedded, it extracts from the embedded data.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering the texture.</param>
    /// <param name="scene">The scene containing the material and textures.</param>
    /// <param name="aMaterial">The material from which the texture should be loaded.</param>
    /// <param name="path">The path to the model file, used to resolve texture file paths.</param>
    /// <param name="textureType">The type of texture to load from the material.</param>
    /// <returns>A Texture2D object if the texture is successfully loaded, otherwise null.</returns>
    private static Texture2D? LoadMaterialTexture(GraphicsDevice graphicsDevice, Scene scene, AMaterial aMaterial, string path, TextureType textureType) {
        if (aMaterial.GetMaterialTexture(textureType, 0, out TextureSlot textureSlot)) {
            string filePath = textureSlot.FilePath;
            
            if (filePath[0] == '*') {
                EmbeddedTexture embeddedTexture = scene.GetEmbeddedTexture(filePath);

                if (embeddedTexture.HasCompressedData) {
                    byte[] compressedData = embeddedTexture.CompressedData;

                    using (MemoryStream memoryStream = new MemoryStream(compressedData)) {
                        using (Image<Rgba32> image = Image.Load<Rgba32>(memoryStream)) {
                            return new Texture2D(graphicsDevice, image);
                        }
                    }
                }
            }
            else {
                string finalPath = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, filePath);
                return new Texture2D(graphicsDevice, finalPath);
            }
        }

        return null;
    }
    
    /// <summary>
    /// Draws the model using the specified command list, output description, sampler type, transform, blend state, and color.
    /// </summary>
    /// <param name="commandList">The command list used to issue drawing commands.</param>
    /// <param name="output">The output description of the render target.</param>
    /// <param name="transform">The transformation applied to the model.</param>
    /// <param name="color">The color applied to the model.</param>
    public void Draw(CommandList commandList, OutputDescription output, Transform transform, Color color) {
        foreach (Mesh mesh in this.Meshes) {
            mesh.Draw(commandList, output, transform, color);
        }
    }

    /// <summary>
    /// Generates a bounding box that encapsulates all the vertices in the model's meshes.
    /// </summary>
    /// <returns>A <see cref="BoundingBox"/> that represents the minimum and maximum bounds of the model.</returns>
    private BoundingBox GenerateBoundingBox() {
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (Mesh mesh in this.Meshes) {
            foreach (Vertex3D vertex in mesh.Vertices) {
                min = Vector3.Min(min, vertex.Position);
                max = Vector3.Max(max, vertex.Position);
            }
        }

        return new BoundingBox(min, max);
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