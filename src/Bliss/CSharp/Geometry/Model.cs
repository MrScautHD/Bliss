using System.Numerics;
using System.Text;
using Assimp;
using Assimp.Configs;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Geometry.Animation;
using Bliss.CSharp.Geometry.Animation.Builders;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Images;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Materials;
using Bliss.CSharp.Textures;
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
    private const PostProcessSteps DefaultPostProcessSteps = PostProcessSteps.FlipWindingOrder |
                                                             PostProcessSteps.Triangulate |
                                                             PostProcessSteps.CalculateTangentSpace |
                                                             PostProcessSteps.GenerateSmoothNormals |
                                                             PostProcessSteps.JoinIdenticalVertices |
                                                             PostProcessSteps.FindInvalidData |
                                                             PostProcessSteps.ImproveCacheLocality |
                                                             PostProcessSteps.FixInFacingNormals |
                                                             PostProcessSteps.GenerateUVCoords |
                                                             PostProcessSteps.ValidateDataStructure |
                                                             PostProcessSteps.FindInstances |
                                                             PostProcessSteps.GlobalScale;
    
    /// <summary>
    /// A collection of default property configurations applied to the 3D model importer.
    /// These configurations include settings to limit vertex bone weights, exclude certain elements,
    /// and adjust import behavior for specific file formats like FBX.
    /// </summary>
    private static readonly List<PropertyConfig> PropertyConfigs = [
        new NoSkeletonMeshesConfig(true),
        new VertexBoneWeightLimitConfig(4),
        new FBXImportCamerasConfig(false),
        new FBXStrictModeConfig(false)
    ];

    /// <summary>
    /// Caches loaded effect of the <see cref="Load"/> method.
    /// </summary>
    private static Dictionary<Model, Effect[]> _effectCache = new();
    
    /// <summary>
    /// Caches loaded textures of the <see cref="Load"/> method.
    /// </summary>
    private static Dictionary<Model, Texture2D[]> _textureCache = new();
    
    /// <summary>
    /// The graphics device used for rendering the model.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// An array of meshes that make up the model.
    /// </summary>
    public Mesh[] Meshes { get; private set; }
    
    /// <summary>
    /// The hierarchical skeletal structure of the model.
    /// </summary>
    public Skeleton? Skeleton { get; private set; }
    
    /// <summary>
    /// Collection of animations associated with the model.
    /// </summary>
    public ModelAnimation[] Animations { get; private set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Model"/> class with the specified graphics device, meshes, and animations.
    /// </summary>
    /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/> used for rendering and resource management.</param>
    /// <param name="meshes">An array of <see cref="Mesh"/> objects representing the geometric components of the model.</param>
    /// <param name="skeleton">The optional skeleton used for skeletal animation, or <c>null</c> if not present.</param>
    /// <param name="animations">An array of <see cref="ModelAnimation"/> objects defining the animations for the model.</param>
    public Model(GraphicsDevice graphicsDevice, Mesh[] meshes, Skeleton? skeleton, ModelAnimation[] animations) {
        this.GraphicsDevice = graphicsDevice;
        this.Meshes = meshes;
        this.Skeleton = skeleton;
        this.Animations = animations;
    }

    /// <summary>
    /// Loads a model from the specified file path using the provided graphics device and configuration options.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering and managing model resources.</param>
    /// <param name="path">The file path to the model to be loaded.</param>
    /// <param name="loadMaterial">Specifies whether the material should be loaded with the model. Default is true.</param>
    /// <param name="flipUv">Determines whether the UV coordinates of the model should be flipped. Default is false.</param>
    /// <returns>Returns a new instance of the <see cref="Model"/> class containing the loaded meshes and animations.</returns>
    public static Model Load(GraphicsDevice graphicsDevice, string path, bool loadMaterial = true, bool flipUv = false) {
        using AssimpContext context = new AssimpContext();
        
        foreach (PropertyConfig config in PropertyConfigs) {
            context.SetConfig(config);
        }
        
        Scene scene = context.ImportFile(path, DefaultPostProcessSteps);
        List<Mesh> meshes = new List<Mesh>();
        
        // Cache effects and textures to load them just 1 time per model.
        Dictionary<string, Effect> cachedEffects = new Dictionary<string, Effect>();
        Dictionary<string, Texture2D> cachedTextures = new Dictionary<string, Texture2D>();
        
        for (int i = 0; i < scene.Meshes.Count; i++) {
            AMesh mesh = scene.Meshes[i];
            
            // Load effect.
            Effect effect;
            
            if (scene.HasMaterials && loadMaterial) {
                AMaterial aMaterial = scene.Materials[mesh.MaterialIndex];
                effect = LoadMaterialEffect(graphicsDevice, cachedEffects, aMaterial) ?? GlobalResource.DefaultModelEffect;
            }
            else {
                effect = GlobalResource.DefaultModelEffect;
            }
            
            // Load material maps.
            Material material = new Material(effect);
            
            if (scene.HasMaterials && loadMaterial) {
                AMaterial aMaterial = scene.Materials[mesh.MaterialIndex];
                
                // Albedo map.
                if (aMaterial.HasTextureDiffuse || aMaterial.HasColorDiffuse ) {
                    material.AddMaterialMap(MaterialMapType.Albedo, new MaterialMap() {
                        Texture = aMaterial.HasTextureDiffuse ? LoadMaterialTexture(graphicsDevice, cachedTextures, scene, aMaterial, path, TextureType.Diffuse) : GlobalResource.DefaultModelTexture,
                        Color = aMaterial.HasColorDiffuse ? new Color(new RgbaFloat(aMaterial.ColorDiffuse)) : Color.White,
                    });
                }
                else {
                    material.AddMaterialMap(MaterialMapType.Albedo, new MaterialMap() {
                        Texture = GlobalResource.DefaultModelTexture,
                        Color = Color.White
                    });
                }
                
                // Metallic map.
                if (aMaterial.PBR.HasTextureMetalness || aMaterial.HasColorSpecular ) {
                    material.AddMaterialMap(MaterialMapType.Metallic, new MaterialMap() {
                        Texture = aMaterial.PBR.HasTextureMetalness ? LoadMaterialTexture(graphicsDevice, cachedTextures, scene, aMaterial, path, TextureType.Metalness) : null,
                        Color = aMaterial.HasColorSpecular ? new Color(new RgbaFloat(aMaterial.ColorSpecular)) : Color.White
                    });
                }
                
                // Normal map.
                if (aMaterial.HasTextureNormal || aMaterial.HasShininess) {
                    material.AddMaterialMap(MaterialMapType.Normal, new MaterialMap() {
                        Texture = aMaterial.HasTextureNormal ? LoadMaterialTexture(graphicsDevice, cachedTextures, scene, aMaterial, path, TextureType.Normals) : null,
                        Color = Color.White,
                        Value = aMaterial.Shininess
                    });
                }
                
                // Roughness map.
                if (aMaterial.PBR.HasTextureRoughness) {
                    material.AddMaterialMap(MaterialMapType.Roughness, new MaterialMap() {
                        Texture = LoadMaterialTexture(graphicsDevice, cachedTextures, scene, aMaterial, path, TextureType.Roughness),
                        Color = Color.White
                    });
                }
                
                // Occlusion map.
                if (aMaterial.HasTextureAmbientOcclusion) {
                    material.AddMaterialMap(MaterialMapType.Occlusion, new MaterialMap() {
                        Texture = LoadMaterialTexture(graphicsDevice, cachedTextures, scene, aMaterial, path, TextureType.AmbientOcclusion),
                        Color = Color.White
                    });
                }
                
                // Emissive map.
                if (aMaterial.HasTextureEmissive || aMaterial.HasColorEmissive) {
                    material.AddMaterialMap(MaterialMapType.Emission, new MaterialMap() {
                        Texture = aMaterial.HasTextureEmissive ? LoadMaterialTexture(graphicsDevice, cachedTextures, scene, aMaterial, path, TextureType.Emissive) : null,
                        Color = aMaterial.HasColorEmissive ? new Color(new RgbaFloat(aMaterial.ColorEmissive)) : Color.Black,
                    });
                }
                
                // Opacity map.
                if (aMaterial.HasTextureOpacity) {
                    material.AddMaterialMap(MaterialMapType.Opacity, new MaterialMap() {
                        Texture = LoadMaterialTexture(graphicsDevice, cachedTextures, scene, aMaterial, path, TextureType.Opacity)
                    });
                }
                
                // Height map.
                if (aMaterial.HasTextureHeight) {
                    material.AddMaterialMap(MaterialMapType.Height, new MaterialMap() {
                        Texture = LoadMaterialTexture(graphicsDevice, cachedTextures, scene, aMaterial, path, TextureType.Height)
                    });
                }
            }
            else {
                material.AddMaterialMap(MaterialMapType.Albedo, new MaterialMap() {
                    Texture = GlobalResource.DefaultModelTexture,
                    Color = Color.White
                });
            }
            
            // Setup vertices.
            Vertex3D[] vertices = new Vertex3D[scene.Meshes[i].VertexCount];
            
            for (int j = 0; j < mesh.VertexCount; j++) {
                
                // Set Position.
                vertices[j].Position = mesh.Vertices[j];
                
                // Set TexCoord.
                if (mesh.HasTextureCoords(0)) {
                    Vector3 texCoord = mesh.TextureCoordinateChannels[0][j];
                    Vector2 finalTexCoord = new Vector2(texCoord.X, -texCoord.Y);
                    
                    vertices[j].TexCoords = flipUv ? -finalTexCoord : finalTexCoord;
                }
                else {
                    vertices[j].TexCoords = Vector2.Zero;
                }
                
                // Set TexCoord2.
                if (mesh.HasTextureCoords(1)) {
                    Vector3 texCoord2 = mesh.TextureCoordinateChannels[1][j];
                    Vector2 finalTexCoord2 = new Vector2(texCoord2.X, -texCoord2.Y);
                    
                    vertices[j].TexCoords2 = flipUv ? -finalTexCoord2 : finalTexCoord2;
                }
                else {
                    vertices[j].TexCoords2 = Vector2.Zero;
                }
                
                // Set Normal.
                vertices[j].Normal = mesh.HasNormals ? mesh.Normals[j] : Vector3.Zero;
                
                // Set Tangent.
                float tangentSign = 1.0F;
                
                if (mesh.HasTangentBasis) {
                    Vector3 normal = mesh.Normals[j];
                    Vector3 tangent = mesh.Tangents[j];
                    Vector3 bitangent = Vector3.Cross(normal, tangent);
                    
                    tangentSign = Vector3.Dot(bitangent, mesh.BiTangents[j]) > 0.0F ? 1.0F : -1.0F;
                }
                
                vertices[j].Tangent = mesh.HasTangentBasis ? new Vector4(mesh.Tangents[j], tangentSign) : Vector4.Zero;
                
                // Set Color.
                vertices[j].Color = material.GetMapColor(MaterialMapType.Albedo)?.ToRgbaFloatVec4() ?? Vector4.Zero;
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
        
        // Apply the final world transform.
        ProcessNode(scene.RootNode, meshes, Matrix4x4.Identity);
        
        // Load skeleton.
        Skeleton skeleton = new SkeletonBuilder(scene).Build();
        
        // Load animations.
        ModelAnimation[] animations = new ModelAnimation[scene.AnimationCount];
        
        for (int i = 0; i < scene.AnimationCount; i++) {
            animations[i] = new ModelAnimationBuilder(scene.RootNode, skeleton, scene.Animations[i]).Build();
        }
        
        Logger.Info($"Model loaded successfully from path: [{path}]");
        Logger.Info($"\t> Meshes: {meshes.Count}");
        Logger.Info($"\t> Animations: {scene.AnimationCount}");
        Logger.Info($"\t> Effects: {cachedEffects.Count}");
        Logger.Info($"\t> Textures: {cachedTextures.Count}");
        
        // Create the model.
        Model model = new Model(graphicsDevice, meshes.ToArray(), skeleton, animations.ToArray());
        
        // Store loaded effects.
        _effectCache.Add(model, cachedEffects.Values.ToArray());
        
        // Store loaded textures.
        _textureCache.Add(model, cachedTextures.Values.ToArray());
        
        return model;
    }
    
    /// <summary>
    /// Processes a node in the scene hierarchy, transforming and updating the associated meshes.
    /// </summary>
    /// <param name="node">The <see cref="Node"/> to be processed, representing a part of the scene hierarchy.</param>
    /// <param name="meshes">A list of <see cref="Mesh"/> objects to be transformed based on the node's transformations.</param>
    /// <param name="transform">The cumulative transformation matrix applied to the current node and its children.</param>
    private static void ProcessNode(Node node, List<Mesh> meshes, Matrix4x4 transform) {
        Matrix4x4 nodeTransform = Matrix4x4.Transpose(node.Transform) * transform;
        
        foreach (int meshIndex in node.MeshIndices) {
            Mesh mesh = meshes[meshIndex];
            
            for (int i = 0; i < mesh.Vertices.Length; i++) {
                
                // Set Position.
                mesh.Vertices[i].Position = Vector3.Transform(mesh.Vertices[i].Position, nodeTransform);
                
                // Set Normal.
                Matrix4x4.Invert(Matrix4x4.Transpose(nodeTransform), out Matrix4x4 nodeInverseTransform);
                mesh.Vertices[i].Normal = Vector3.Normalize(Vector3.TransformNormal(mesh.Vertices[i].Normal, nodeInverseTransform));
                
                // Set Tangent.
                mesh.Vertices[i].Tangent = Vector4.Transform(mesh.Vertices[i].Tangent, nodeTransform);
            }
            
            mesh.UpdateVertexBufferImmediate();
        }
        
        foreach (var childNode in node.Children) {
            ProcessNode(childNode, meshes, nodeTransform);
        }
    }
    
    /// <summary>
    /// Creates or retrieves an <see cref="Effect"/> instance based on the provided material and shader information.
    /// </summary>
    /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/> used for creating the effect and managing resources.</param>
    /// <param name="cachedEffects">A dictionary containing pre-cached effects, indexed by their unique identifiers.</param>
    /// <param name="aMaterial">The <see cref="Assimp.Material"/> containing the material and shader properties.</param>
    /// <returns>A newly created or cached <see cref="Effect"/> if valid shaders exist; otherwise, null.</returns>
    private static Effect? LoadMaterialEffect(GraphicsDevice graphicsDevice, Dictionary<string, Effect> cachedEffects, AMaterial aMaterial) {
        ShaderMaterialProperties shaderProperties = aMaterial.Shaders;
        
        if (shaderProperties.HasVertexShader && shaderProperties.HasFragmentShader) {
            string effectKey = $"{shaderProperties.VertexShader}::{shaderProperties.FragmentShader}";
            
            // Check if the effect already exists in the cache.
            if (cachedEffects.TryGetValue(effectKey, out Effect? cachedEffect)) {
                return cachedEffect;
            }
            
            Effect effect = new Effect(graphicsDevice, Vertex3D.VertexLayout, Encoding.UTF8.GetBytes(shaderProperties.VertexShader), Encoding.UTF8.GetBytes(shaderProperties.FragmentShader));
            cachedEffects[effectKey] = effect;
            return effect;
        }
        
        return null;
    }
    
    /// <summary>
    /// Loads a texture from a material and returns it as a Texture2D object.
    /// </summary>
    /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/> used for rendering the texture.</param>
    /// <param name="cachedTextures">A dictionary containing cached textures, used for optimizing texture loading.</param>
    /// <param name="scene">The scene containing the material and textures.</param>
    /// <param name="aMaterial">The <see cref="Assimp.Material"/> from which the texture should be loaded.</param>
    /// <param name="path">The path to the model file, used to resolve texture file paths.</param>
    /// <param name="textureType">The type of texture to load from the material.</param>
    /// <returns>A <see cref="Texture2D"/> object if the texture is successfully loaded, otherwise null.</returns>
    private static Texture2D? LoadMaterialTexture(GraphicsDevice graphicsDevice, Dictionary<string, Texture2D> cachedTextures, Scene scene, AMaterial aMaterial, string path, TextureType textureType) {
        if (aMaterial.GetMaterialTexture(textureType, 0, out TextureSlot textureSlot)) {
            string filePath = textureSlot.FilePath;
            
            // Check if the texture already exists in the cache.
            if (cachedTextures.TryGetValue(filePath, out Texture2D? cachedTexture)) {
                return cachedTexture;
            }
            
            // Handle embedded textures (file path starts with '*').
            if (filePath[0] == '*') {
                EmbeddedTexture embeddedTexture = scene.GetEmbeddedTexture(filePath);
                
                // Use compressed data to create the texture.
                if (embeddedTexture.HasCompressedData) {
                    byte[] compressedData = embeddedTexture.CompressedData;
                    
                    using (MemoryStream memoryStream = new MemoryStream(compressedData)) {
                        Texture2D texture = new Texture2D(graphicsDevice, new Image(memoryStream));
                        cachedTextures[filePath] = texture;
                        return texture;
                    }
                }
            }
            else {
                string finalPath = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, filePath);
                Texture2D texture = new Texture2D(graphicsDevice, finalPath);
                cachedTextures[filePath] = texture;
                return texture;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Generates a bounding box that encapsulates all the vertices in the model's meshes.
    /// </summary>
    /// <returns>A <see cref="BoundingBox"/> that represents the minimum and maximum bounds of the model.</returns>
    public BoundingBox GenBoundingBox() {
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        
        foreach (Mesh mesh in this.Meshes) {
            BoundingBox meshBox = mesh.GenBoundingBox();
            
            min = Vector3.Min(min, meshBox.Min);
            max = Vector3.Max(max, meshBox.Max);
        }
        
        return new BoundingBox(min, max);
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            
            // Dispose meshes.
            foreach (Mesh mesh in this.Meshes) {
                mesh.Dispose();
            }
            
            // Dispose effects.
            if (_effectCache.TryGetValue(this, out Effect[]? effects)) {
                foreach (Effect effect in effects) {
                    effect.Dispose();
                }
            }
            
            // Dispose textures.
            if (_textureCache.TryGetValue(this, out Texture2D[]? textures)) {
                foreach (Texture2D texture in textures) {
                    texture.Dispose();
                }
            }
        }
    }
}