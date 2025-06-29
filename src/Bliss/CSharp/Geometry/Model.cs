using System.Numerics;
using System.Text;
using Assimp;
using Assimp.Configs;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Geometry.Animations;
using Bliss.CSharp.Geometry.Animations.Keyframes;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Images;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Materials;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
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
    private static Dictionary<(Model, Mesh), Effect> _effectCache = new();
    
    /// <summary>
    /// Caches loaded textures of the <see cref="Load"/> method.
    /// </summary>
    private static Dictionary<(Model, Mesh, string), Texture2D> _textureCache = new();
    
    /// <summary>
    /// The graphics device used for rendering the model.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// An array of meshes that make up the model.
    /// </summary>
    public Mesh[] Meshes { get; private set; }

    /// <summary>
    /// Collection of animations associated with the model.
    /// Each animation encapsulates skeletal transformations over time, enabling the model to exhibit complex motions.
    /// </summary>
    public ModelAnimation[] Animations { get; private set; }
    
    /// <summary>
    /// Represents the axis-aligned bounding box of the model.
    /// </summary>
    public BoundingBox BoundingBox { get; private set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Model"/> class with the specified graphics device, meshes, and animations.
    /// </summary>
    /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/> used for rendering and resource management.</param>
    /// <param name="meshes">An array of <see cref="Mesh"/> objects representing the geometric components of the model.</param>
    /// <param name="animations">An array of <see cref="ModelAnimation"/> objects defining the animations for the model.</param>
    public Model(GraphicsDevice graphicsDevice, Mesh[] meshes, ModelAnimation[] animations) {
        this.GraphicsDevice = graphicsDevice;
        this.Meshes = meshes;
        this.Animations = animations;
        this.BoundingBox = this.GenerateBoundingBox();
    }

    /// <summary>
    /// Loads a model from the specified file path using the provided graphics device and configuration options.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering and managing model resources.</param>
    /// <param name="path">The file path to the model to be loaded.</param>
    /// <param name="loadMaterial">Specifies whether the material should be loaded with the model. Default is true.</param>
    /// <param name="uniqueTexturePerMesh">Indicates whether each mesh should use a unique texture. Default is false.</param>
    /// <param name="uniqueEffectPerMesh">Indicates whether each mesh should use a unique effect. Default is false.</param>
    /// <param name="flipUv">Determines whether the UV coordinates of the model should be flipped. Default is false.</param>
    /// <returns>Returns a new instance of the <see cref="Model"/> class containing the loaded meshes and animations.</returns>
    public static Model Load(GraphicsDevice graphicsDevice, string path, bool loadMaterial = true, bool uniqueTexturePerMesh = false, bool uniqueEffectPerMesh = false, bool flipUv = false) {
        using AssimpContext context = new AssimpContext();
        
        foreach (PropertyConfig config in PropertyConfigs) {
            context.SetConfig(config);
        }
        
        Scene scene = context.ImportFile(path, DefaultPostProcessSteps);
        List<Mesh> meshes = new List<Mesh>();
        List<ModelAnimation> animations = new List<ModelAnimation>();
        
        // Load animations.
        for (int i = 0; i < scene.Animations.Count; i++) {
            Animation aAnimation = scene.Animations[i];
            
            // Setup channels.
            List<NodeAnimChannel> animChannels = new List<NodeAnimChannel>();
            
            foreach (NodeAnimationChannel aChannel in aAnimation.NodeAnimationChannels) {
                List<Vector3Key> positions = new List<Vector3Key>();
                List<QuatKey> rotations = new List<QuatKey>();
                List<Vector3Key> scales = new List<Vector3Key>();
                
                // Setup positions.
                foreach (VectorKey aPosition in aChannel.PositionKeys) {
                    Vector3Key position = new Vector3Key(aPosition.Time, aPosition.Value);
                    positions.Add(position);
                }
                
                // Setup rotations.
                foreach (QuaternionKey aRotation in aChannel.RotationKeys) {
                    QuatKey rotation = new QuatKey(aRotation.Time, aRotation.Value);
                    rotations.Add(rotation);
                }
                
                // Setup scales.
                foreach (VectorKey aScale in aChannel.ScalingKeys) {
                    Vector3Key scale = new Vector3Key(aScale.Time, aScale.Value);
                    scales.Add(scale);
                }
                
                NodeAnimChannel channel = new NodeAnimChannel(aChannel.NodeName, positions, rotations, scales);
                animChannels.Add(channel);
            }
            
            animations.Add(new ModelAnimation(aAnimation.Name, (float) aAnimation.DurationInTicks, (float) aAnimation.TicksPerSecond, animChannels));
        }
        
        // Setup amateur builder.
        MeshAmateurBuilder amateurBuilder = new MeshAmateurBuilder(scene.RootNode, animations.ToArray());
        
        // Cache effects and textures to load them just 1 time per mesh.
        Dictionary<string, Effect> cachedEffects = new Dictionary<string, Effect>();
        Dictionary<string, Texture2D> cachedTextures = new Dictionary<string, Texture2D>();
        
        for (int i = 0; i < scene.Meshes.Count; i++) {
            AMesh mesh = scene.Meshes[i];
            
            // Load effect.
            Effect effect;
            
            if (scene.HasMaterials && loadMaterial) {
                AMaterial aMaterial = scene.Materials[mesh.MaterialIndex];
                effect = LoadMaterialEffect(graphicsDevice, cachedEffects, aMaterial, uniqueEffectPerMesh) ?? GlobalResource.DefaultModelEffect;
            }
            else {
                effect = GlobalResource.DefaultModelEffect;
            }
            
            // Load material maps.
            Material material = new Material(graphicsDevice, effect);
            
            if (scene.HasMaterials && loadMaterial) {
                AMaterial aMaterial = scene.Materials[mesh.MaterialIndex];
                
                // Albedo map.
                if (aMaterial.HasTextureDiffuse || aMaterial.HasColorDiffuse ) {
                    material.AddMaterialMap(MaterialMapType.Albedo.GetName(), new MaterialMap() {
                        Texture = aMaterial.HasTextureDiffuse ? LoadMaterialTexture(graphicsDevice, cachedTextures, scene, aMaterial, path, TextureType.Diffuse, uniqueTexturePerMesh) : GlobalResource.DefaultModelTexture,
                        Color = aMaterial.HasColorDiffuse ? new Color(new RgbaFloat(aMaterial.ColorDiffuse)) : Color.White,
                    });
                }
                else {
                    material.AddMaterialMap(MaterialMapType.Albedo.GetName(), new MaterialMap() {
                        Texture = GlobalResource.DefaultModelTexture,
                        Color = Color.White
                    });
                }
                
                // Metallic map.
                if (aMaterial.PBR.HasTextureMetalness || aMaterial.HasColorSpecular ) {
                    material.AddMaterialMap(MaterialMapType.Metallic.GetName(), new MaterialMap() {
                        Texture = aMaterial.PBR.HasTextureMetalness ? LoadMaterialTexture(graphicsDevice, cachedTextures, scene, aMaterial, path, TextureType.Metalness, uniqueTexturePerMesh) : null,
                        Color = aMaterial.HasColorSpecular ? new Color(new RgbaFloat(aMaterial.ColorSpecular)) : Color.White
                    });
                }
                
                // Normal map.
                if (aMaterial.HasTextureNormal || aMaterial.HasShininess) {
                    material.AddMaterialMap(MaterialMapType.Normal.GetName(), new MaterialMap() {
                        Texture = aMaterial.HasTextureNormal ? LoadMaterialTexture(graphicsDevice, cachedTextures, scene, aMaterial, path, TextureType.Normals, uniqueTexturePerMesh) : null,
                        Color = Color.White,
                        Value = aMaterial.Shininess
                    });
                }
                
                // Roughness map.
                if (aMaterial.PBR.HasTextureRoughness) {
                    material.AddMaterialMap(MaterialMapType.Roughness.GetName(), new MaterialMap() {
                        Texture = LoadMaterialTexture(graphicsDevice, cachedTextures, scene, aMaterial, path, TextureType.Roughness, uniqueTexturePerMesh),
                        Color = Color.White
                    });
                }
                
                // Occlusion map.
                if (aMaterial.HasTextureAmbientOcclusion) {
                    material.AddMaterialMap(MaterialMapType.Occlusion.GetName(), new MaterialMap() {
                        Texture = LoadMaterialTexture(graphicsDevice, cachedTextures, scene, aMaterial, path, TextureType.AmbientOcclusion, uniqueTexturePerMesh),
                        Color = Color.White
                    });
                }
                
                // Emissive map.
                if (aMaterial.HasTextureEmissive || aMaterial.HasColorEmissive) {
                    material.AddMaterialMap(MaterialMapType.Emission.GetName(), new MaterialMap() {
                        Texture = aMaterial.HasTextureEmissive ? LoadMaterialTexture(graphicsDevice, cachedTextures, scene, aMaterial, path, TextureType.Emissive, uniqueTexturePerMesh) : null,
                        Color = aMaterial.HasColorEmissive ? new Color(new RgbaFloat(aMaterial.ColorEmissive)) : Color.Black,
                    });
                }
                
                // Height map.
                if (aMaterial.HasTextureHeight) {
                    material.AddMaterialMap(MaterialMapType.Height.GetName(), new MaterialMap() {
                        Texture = LoadMaterialTexture(graphicsDevice, cachedTextures, scene, aMaterial, path, TextureType.Height, uniqueTexturePerMesh)
                    });
                }
            }
            else {
                material.AddMaterialMap(MaterialMapType.Albedo.GetName(), new MaterialMap() {
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
                vertices[j].Color = material.GetMapColor(MaterialMapType.Albedo.GetName())?.ToRgbaFloatVec4() ?? Vector4.Zero;
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
            Dictionary<uint, Bone> bonesByName = new Dictionary<uint, Bone>();
            
            for (int j = 0; j < mesh.BoneCount; j++) {
                Bone bone = mesh.Bones[j];
                bonesByName.Add((uint) j, bone);
                
                foreach (VertexWeight vertexWeight in bone.VertexWeights) {
                    vertices[vertexWeight.VertexID].AddBone((uint) j, vertexWeight.Weight);
                }
            }

            meshes.Add(new Mesh(graphicsDevice, material, vertices, indices.ToArray(), amateurBuilder.Build(bonesByName)));
        }
        
        Logger.Info($"Model loaded successfully from path: [{path}]");
        Logger.Info($"\t> Meshes: {meshes.Count}");
        Logger.Info($"\t> Animations: {scene.AnimationCount}");
        Logger.Info($"\t> Effects: {cachedEffects.Count}");
        Logger.Info($"\t> Textures: {cachedTextures.Count}");
        
        // Create the model.
        Model model = new Model(graphicsDevice, meshes.ToArray(), animations.ToArray());
        
        // Store cache resources.
        foreach (Mesh mesh in model.Meshes) {
            
            // Store loaded effects.
            if (mesh.Material.Effect != GlobalResource.DefaultModelEffect) {
                _effectCache.Add((model, mesh), mesh.Material.Effect);
            }
            
            // Store loaded textures.
            foreach ((MaterialMap materialMap, string mapName) in mesh.Material.GetMaterialMaps().Zip(mesh.Material.GetMaterialMapNames())) {
                Texture2D? texture = materialMap.Texture;
                
                if (texture != null && texture != GlobalResource.DefaultModelTexture) {
                    _textureCache.Add((model, mesh, mapName), texture);
                }
            }
        }
        
        return model;
    }

    /// <summary>
    /// Creates or retrieves an <see cref="Effect"/> instance based on the provided material and shader information.
    /// </summary>
    /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/> used for creating the effect and managing resources.</param>
    /// <param name="cachedEffects">A dictionary containing pre-cached effects, indexed by their unique identifiers.</param>
    /// <param name="aMaterial">The <see cref="Assimp.Material"/> containing the material and shader properties.</param>
    /// <param name="uniqueEffectPerMesh">A flag indicating whether each mesh should have a unique effect, even if the same shaders are used.</param>
    /// <returns>A newly created or cached <see cref="Effect"/> if valid shaders exist; otherwise, null.</returns>
    private static Effect? LoadMaterialEffect(GraphicsDevice graphicsDevice, Dictionary<string, Effect> cachedEffects, AMaterial aMaterial, bool uniqueEffectPerMesh) {
        ShaderMaterialProperties shaderProperties = aMaterial.Shaders;
        
        if (shaderProperties.HasVertexShader && shaderProperties.HasFragmentShader) {
            string effectKey = $"{shaderProperties.VertexShader}::{shaderProperties.FragmentShader}";
            
            if (!uniqueEffectPerMesh) {
                if (cachedEffects.TryGetValue(effectKey, out Effect? cachedEffect)) {
                    return cachedEffect;
                }
            }
            
            Effect effect = new Effect(graphicsDevice, Vertex3D.VertexLayout, Encoding.UTF8.GetBytes(shaderProperties.VertexShader), Encoding.UTF8.GetBytes(shaderProperties.FragmentShader));
            
            if (!uniqueEffectPerMesh) {
                cachedEffects[effectKey] = effect;
            }
            
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
    /// <param name="uniqueTexturePerMesh">A flag indicating whether each mesh uses unique textures or shares textures globally.</param>
    /// <returns>A <see cref="Texture2D"/> object if the texture is successfully loaded, otherwise null.</returns>
    private static Texture2D? LoadMaterialTexture(GraphicsDevice graphicsDevice, Dictionary<string, Texture2D> cachedTextures, Scene scene, AMaterial aMaterial, string path, TextureType textureType, bool uniqueTexturePerMesh) {
        if (aMaterial.GetMaterialTexture(textureType, 0, out TextureSlot textureSlot)) {
            string filePath = textureSlot.FilePath;
            
            // Check if the texture already exists in the cache (if uniqueTexturePerMesh is false).
            if (!uniqueTexturePerMesh) {
                if (cachedTextures.TryGetValue(filePath, out Texture2D? cachedTexture)) {
                    return cachedTexture;
                }
            }
            
            // Handle embedded textures (file path starts with '*').
            if (filePath[0] == '*') {
                EmbeddedTexture embeddedTexture = scene.GetEmbeddedTexture(filePath);
                
                // Use compressed data to create the texture.
                if (embeddedTexture.HasCompressedData) {
                    byte[] compressedData = embeddedTexture.CompressedData;
                    
                    using (MemoryStream memoryStream = new MemoryStream(compressedData)) {
                        Texture2D texture = new Texture2D(graphicsDevice, new Image(memoryStream));
                        
                        // Cache the texture if it is not unique per mesh.
                        if (!uniqueTexturePerMesh) {
                            cachedTextures[filePath] = texture;
                        }
                        
                        return texture;
                    }
                }
            }
            else {
                string finalPath = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, filePath);
                Texture2D texture = new Texture2D(graphicsDevice, finalPath);
                
                // Cache the texture if it is not unique per mesh.
                if (!uniqueTexturePerMesh) {
                    cachedTextures[filePath] = texture;
                }
                
                return texture;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Updates the animation bones for all meshes in the model based on the specified animation and frame.
    /// </summary>
    /// <param name="commandList">The command list used to issue rendering commands.</param>
    /// <param name="animation">The animation whose bone transformations should be applied.</param>
    /// <param name="frame">The specific frame of the animation to update the bones to.</param>
    public void UpdateAnimationBones(CommandList commandList, ModelAnimation animation, int frame) {
        foreach (Mesh mesh in this.Meshes) {
            mesh.UpdateAnimationBones(commandList, animation, frame);
        }
    }

    /// <summary>
    /// Resets the animation bone transformations for all meshes in the model using the given command list.
    /// </summary>
    /// <param name="commandList">The command list used to execute the reset operation on the GPU.</param>
    public void ResetAnimationBones(CommandList commandList) {
        foreach (Mesh mesh in this.Meshes) {
            mesh.ResetAnimationBones(commandList);
        }
    }

    /// <summary>
    /// Draws the model using the specified command list, transform, and rendering configurations.
    /// </summary>
    /// <param name="commandList">The <see cref="CommandList"/> that issues the rendering commands.</param>
    /// <param name="transform">The <see cref="Transform"/> describing the position, rotation, and scale of the model in the scene.</param>
    /// <param name="output">The <see cref="OutputDescription"/> that defines the target rendering output settings, such as resolution and format.</param>
    /// <param name="sampler">An optional <see cref="Sampler"/> used for texture sampling. If not provided, a default sampler is applied.</param>
    /// <param name="depthStencilState">An optional <see cref="DepthStencilStateDescription"/> to configure depth and stencil testing for the drawing process.</param>
    /// <param name="rasterizerState">An optional <see cref="RasterizerStateDescription"/> that manages the rasterizer configuration, such as culling and fill mode.</param>
    /// <param name="color">An optional <see cref="Color"/> to override or apply a custom tint to the model's rendered output.</param>
    public void Draw(CommandList commandList, Transform transform, OutputDescription output, Sampler? sampler = null, DepthStencilStateDescription? depthStencilState = null, RasterizerStateDescription? rasterizerState = null, Color? color = null) {
        foreach (Mesh mesh in this.Meshes) {
            mesh.Draw(commandList, transform, output, sampler, depthStencilState, rasterizerState, color);
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
                // Dispose effects.
                if (_effectCache.TryGetValue((this, mesh), out Effect? effect)) {
                    effect.Dispose();
                }

                // Dispose textures.
                foreach (string mapName in mesh.Material.GetMaterialMapNames()) {
                    if (_textureCache.TryGetValue((this, mesh, mapName), out Texture2D? texture)) {
                        texture.Dispose();
                    }
                }
                
                // Dispose meshes.
                mesh.Dispose();
            }
        }
    }
}