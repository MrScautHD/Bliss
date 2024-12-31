using System.Numerics;
using System.Text;
using Assimp;
using Assimp.Configs;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Geometry.Animations;
using Bliss.CSharp.Geometry.Animations.Keyframes;
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
    /// The graphics device used for rendering the model.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// An array of meshes that make up the model.
    /// </summary>
    public Mesh[] Meshes { get; private set; }

    /// <summary>
    /// Collection of animations associated with the model.
    /// Each animation encapsulates skeletal transformations over time,
    /// enabling the model to exhibit complex motions.
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
    /// Loads a model from the specified file path using the provided graphics device.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering the model.</param>
    /// <param name="path">The file path to the model to be loaded.</param>
    /// <param name="loadMaterial">Indicates whether the material should be loaded with the model. Default is true.</param>
    /// <param name="flipUv">Indicates whether the UV coordinates should be flipped. Default is false.</param>
    /// <returns>Returns a new instance of the <see cref="Model"/> class with the loaded meshes.</returns>
    public static Model Load(GraphicsDevice graphicsDevice, string path, bool loadMaterial = true, bool flipUv = false) {
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

        for (int i = 0; i < scene.Meshes.Count; i++) {
            AMesh mesh = scene.Meshes[i];
            
            // Load effect.
            Effect? effect = null;

            if (scene.HasMaterials && loadMaterial) {
                AMaterial aMaterial = scene.Materials[mesh.MaterialIndex];
                ShaderMaterialProperties shaderProperties = aMaterial.Shaders;
                
                if (shaderProperties.HasVertexShader && shaderProperties.HasFragmentShader) {
                    effect = new Effect(graphicsDevice, Vertex3D.VertexLayout, Encoding.UTF8.GetBytes(shaderProperties.VertexShader), Encoding.UTF8.GetBytes(shaderProperties.FragmentShader));
                }
            }

            effect ??= GlobalResource.DefaultModelEffect;
            
            // Load material maps.
            Material material = new Material(graphicsDevice, effect);

            if (scene.HasMaterials && loadMaterial) {
                AMaterial aMaterial = scene.Materials[mesh.MaterialIndex];

                if (aMaterial.HasTextureDiffuse) {
                    material.AddMaterialMap(MaterialMapType.Albedo.GetName(), new MaterialMap() {
                        Texture = LoadMaterialTexture(graphicsDevice, scene, aMaterial, path, TextureType.Diffuse),
                        Color = new Color(new RgbaFloat(aMaterial.ColorDiffuse))
                    });
                }

                if (aMaterial.PBR.HasTextureMetalness) {
                    material.AddMaterialMap(MaterialMapType.Metallic.GetName(), new MaterialMap() {
                        Texture = LoadMaterialTexture(graphicsDevice, scene, aMaterial, path, TextureType.Metalness),
                        Color = new Color(new RgbaFloat(aMaterial.ColorSpecular))
                    });
                }

                if (aMaterial.HasTextureNormal) {
                    material.AddMaterialMap(MaterialMapType.Normal.GetName(), new MaterialMap() {
                        Texture = LoadMaterialTexture(graphicsDevice, scene, aMaterial, path, TextureType.Normals)
                    });
                }

                if (aMaterial.PBR.HasTextureRoughness) {
                    material.AddMaterialMap(MaterialMapType.Roughness.GetName(), new MaterialMap() {
                        Texture = LoadMaterialTexture(graphicsDevice, scene, aMaterial, path, TextureType.Roughness)
                    });
                }

                if (aMaterial.HasTextureAmbientOcclusion) {
                    material.AddMaterialMap(MaterialMapType.Occlusion.GetName(), new MaterialMap() {
                        Texture = LoadMaterialTexture(graphicsDevice, scene, aMaterial, path, TextureType.AmbientOcclusion)
                    });
                }

                if (aMaterial.HasTextureEmissive) {
                    material.AddMaterialMap(MaterialMapType.Emission.GetName(), new MaterialMap() {
                        Texture = LoadMaterialTexture(graphicsDevice, scene, aMaterial, path, TextureType.Emissive),
                        Color = new Color(new RgbaFloat(aMaterial.ColorEmissive))
                    });
                }

                if (aMaterial.HasTextureHeight) {
                    material.AddMaterialMap(MaterialMapType.Height.GetName(), new MaterialMap() {
                        Texture = LoadMaterialTexture(graphicsDevice, scene, aMaterial, path, TextureType.Height)
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
                vertices[j].Tangent = mesh.HasTangentBasis ? mesh.Tangents[j] : Vector3.Zero;
                
                // Set Color.
                vertices[j].Color = material.GetMapColor(MaterialMapType.Albedo.GetName())?.ToRgbaFloat().ToVector4() ?? Vector4.Zero;
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

        return new Model(graphicsDevice, meshes.ToArray(), animations.ToArray());
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
    private static Texture2D? LoadMaterialTexture(GraphicsDevice graphicsDevice, Scene scene, AMaterial aMaterial, string path, TextureType textureType) { // TODO: Take care to dispose it!
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

            // TODO: Unload cached resources (materials)
        }
    }
}