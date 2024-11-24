using System.Numerics;
using System.Text;
using Assimp;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Geometry.Animations;
using Bliss.CSharp.Geometry.Animations.Bones;
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
using Matrix4x4 = System.Numerics.Matrix4x4;
using Quaternion = System.Numerics.Quaternion;
using TextureType = Assimp.TextureType;

namespace Bliss.CSharp.Geometry;

public class Model : Disposable {
    
    /// <summary>
    /// The default post-processing steps applied to the model during processing.
    /// This includes flipping the winding order, triangulating, pre-transforming vertices,
    /// calculating tangent space, and generating smooth normals.
    /// </summary>
    private const PostProcessSteps DefaultPostProcessSteps = PostProcessSteps.FlipWindingOrder | PostProcessSteps.Triangulate | PostProcessSteps.CalculateTangentSpace | PostProcessSteps.GenerateSmoothNormals;

    /// <summary>
    /// The default effect applied to models.
    /// This is instantiated with shaders for rendering models, and includes configuration for vertex layout.
    /// </summary>
    private static Effect? _defaultEffect;

    /// <summary>
    /// The default texture used when no other texture is specified.
    /// Typically a 1x1 pixel texture with a solid color.
    /// </summary>
    private static Texture2D? _defaultTexture;
    
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

                if (aMaterial.HasTextureDiffuse) {
                    material.AddMaterialMap(MaterialMapType.Albedo.ToString(), new MaterialMap() {
                        Texture = LoadMaterialTexture(graphicsDevice, scene, aMaterial, path, TextureType.Diffuse),
                        Color = ModelConversion.FromColor4D(aMaterial.ColorDiffuse)
                    });
                }

                if (aMaterial.PBR.HasTextureMetalness) {
                    material.AddMaterialMap(MaterialMapType.Metallic.ToString(), new MaterialMap() {
                        Texture = LoadMaterialTexture(graphicsDevice, scene, aMaterial, path, TextureType.Metalness),
                        Color = ModelConversion.FromColor4D(aMaterial.ColorSpecular)
                    });
                }

                if (aMaterial.HasTextureNormal) {
                    material.AddMaterialMap(MaterialMapType.Normal.ToString(), new MaterialMap() {
                        Texture = LoadMaterialTexture(graphicsDevice, scene, aMaterial, path, TextureType.Normals)
                    });
                }

                if (aMaterial.PBR.HasTextureRoughness) {
                    material.AddMaterialMap(MaterialMapType.Roughness.ToString(), new MaterialMap() {
                        Texture = LoadMaterialTexture(graphicsDevice, scene, aMaterial, path, TextureType.Roughness)
                    });
                }

                if (aMaterial.HasTextureAmbientOcclusion) {
                    material.AddMaterialMap(MaterialMapType.Occlusion.ToString(), new MaterialMap() {
                        Texture = LoadMaterialTexture(graphicsDevice, scene, aMaterial, path, TextureType.AmbientOcclusion)
                    });
                }

                if (aMaterial.HasTextureEmissive) {
                    material.AddMaterialMap(MaterialMapType.Emission.ToString(), new MaterialMap() {
                        Texture = LoadMaterialTexture(graphicsDevice, scene, aMaterial, path, TextureType.Emissive),
                        Color = ModelConversion.FromColor4D(aMaterial.ColorEmissive)
                    });
                }

                if (aMaterial.HasTextureHeight) {
                    material.AddMaterialMap(MaterialMapType.Height.ToString(), new MaterialMap() {
                        Texture = LoadMaterialTexture(graphicsDevice, scene, aMaterial, path, TextureType.Height)
                    });
                }
            }
            else {
                material.AddMaterialMap(MaterialMapType.Albedo.ToString(), new MaterialMap() {
                    Texture = GetDefaultTexture(graphicsDevice),
                    Color = Color.White
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
                vertices[j].Color = material.GetMapColor(MaterialMapType.Albedo.ToString())?.ToRgbaFloat().ToVector4() ?? Vector4.Zero;
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

        List<ModelAnimation> animations = new List<ModelAnimation>();
        List<BoneInfo> boneInfos = GetBones(scene);

        // Load animations
        for (int i = 0; i < scene.Animations.Count; i++) {
            Animation aAnimation = scene.Animations[i];
            double frameDuration = aAnimation.TicksPerSecond > 0 ? aAnimation.TicksPerSecond : 25.0; // Default fallback
            int frameCount = (int)(aAnimation.DurationInTicks / frameDuration * 60);

            Transform[][] framePoses = new Transform[frameCount][];
            for (int j = 0; j < frameCount; j++) {
                framePoses[j] = new Transform[boneInfos.Count];

                foreach (BoneInfo boneInfo in boneInfos) {
                    // Find corresponding node channel
                    NodeAnimationChannel? nodeChannel = aAnimation.NodeAnimationChannels.FirstOrDefault(c => c.NodeName == boneInfo.Name);
                    
                    

                    framePoses[j][boneInfo.Id] = nodeChannel != null ? CalculateBoneTransform(nodeChannel, j, frameDuration) : new Transform();
                }
            }

            animations.Add(new ModelAnimation(aAnimation.Name, boneInfos.ToArray(), framePoses, ModelConversion.FromAMatrix4X4(scene.RootNode.Transform)));
        }

        for (int i = 0; i < scene.Animations.Count; i++) {
            Animation animation = scene.Animations[i];

            for (int j = 0; j < animation.NodeAnimationChannelCount; j++) {
                NodeAnimationChannel channel = animation.NodeAnimationChannels[j];
                
            }
        }
        
        Logger.Info($"Model loaded successfully from path: [{path}]");
        Logger.Info($"\t> Meshes: {meshes.Count}");
        Logger.Info($"\t> Animations: {scene.AnimationCount}");

        return new Model(graphicsDevice, meshes.ToArray(), animations.ToArray());
    }

    private static List<BoneInfo> GetBones(Scene scene) {
        List<BoneInfo> boneInfos = new List<BoneInfo>();
        Dictionary<string, BoneInfo> boneInfoDict = new Dictionary<string, BoneInfo>();

        scene.RootNode.Transform.Inverse();
        Matrix4x4 transform = ModelConversion.FromAMatrix4X4(scene.RootNode.Transform);

        foreach (AMesh mesh in scene.Meshes) {
            for (int i = 0; i < mesh.BoneCount; i++) {
                Bone bone = mesh.Bones[i];

                // Add or retrieve BoneInfo for the parent node
                Node? node = scene.RootNode?.FindNode(bone.Name);
                if (node != null) {
                    Node? parentNode = node.Parent;
                    BoneInfo? parentBoneInfo = null;
                    if (parentNode != null) {
                        if (!boneInfoDict.TryGetValue(parentNode.Name, out parentBoneInfo)) {
                            parentBoneInfo = new BoneInfo(parentNode.Name, (uint) boneInfoDict.Count, null, Matrix4x4.Identity, transform);
                            boneInfos.Add(parentBoneInfo);
                            boneInfoDict[parentNode.Name] = parentBoneInfo;
                        }
                    }

                    // Create BoneInfo for the current bone
                    BoneInfo boneInfo = new BoneInfo(bone.Name, (uint) boneInfos.Count, parentBoneInfo, Matrix4x4.Transpose(ModelConversion.FromAMatrix4X4(bone.OffsetMatrix)), transform);
                    boneInfos.Add(boneInfo);
                    boneInfoDict[bone.Name] = boneInfo;
                }
            }
        }

        return boneInfos;
    }
    
    private static Transform CalculateBoneTransform(NodeAnimationChannel nodeChannel, int frame, double frameDuration) {
        // Compute the appropriate keyframe index
        double time = frame / 60.0 * frameDuration;

        // Position
        Vector3 position = nodeChannel.PositionKeys.Count > 0 ? InterpolatePosition(nodeChannel.PositionKeys, time) : Vector3.Zero;
        
        // Rotation
        Quaternion rotation = nodeChannel.RotationKeys.Count > 0 ? InterpolateRotation(nodeChannel.RotationKeys, time) : Quaternion.Identity;

        // Scaling
        Vector3 scale = nodeChannel.ScalingKeys.Count > 0 ? InterpolateScaling(nodeChannel.ScalingKeys, time) : Vector3.One;

        return new Transform {
            Translation = position,
            Rotation = rotation,
            Scale = scale
        };
    }
    
    private static Vector3 InterpolatePosition(List<VectorKey> keys, double time) {
        // Linear interpolation between keyframes
        for (int i = 0; i < keys.Count - 1; i++) {
            if (keys[i + 1].Time > time) {
                float factor = (float)((time - keys[i].Time) / (keys[i + 1].Time - keys[i].Time));
                return Vector3.Lerp(
                    ModelConversion.FromVector3D(keys[i].Value),
                    ModelConversion.FromVector3D(keys[i + 1].Value),
                    factor
                );
            }
        }
        return ModelConversion.FromVector3D(keys.Last().Value);
    }

    private static Quaternion InterpolateRotation(List<QuaternionKey> keys, double time) {
        for (int i = 0; i < keys.Count - 1; i++) {
            if (keys[i + 1].Time > time) {
                float factor = (float)((time - keys[i].Time) / (keys[i + 1].Time - keys[i].Time));
                Quaternion start = ModelConversion.FromAQuaternion(keys[i].Value);
                Quaternion end = ModelConversion.FromAQuaternion(keys[i + 1].Value);

                return Quaternion.Slerp(Quaternion.Normalize(start), Quaternion.Normalize(end), factor);
            }
        }
        return Quaternion.Normalize(ModelConversion.FromAQuaternion(keys.Last().Value));
    }

    private static Vector3 InterpolateScaling(List<VectorKey> keys, double time) {
        for (int i = 0; i < keys.Count - 1; i++) {
            if (keys[i + 1].Time > time) {
                float factor = (float)((time - keys[i].Time) / (keys[i + 1].Time - keys[i].Time));
                return Vector3.Lerp(
                    ModelConversion.FromVector3D(keys[i].Value),
                    ModelConversion.FromVector3D(keys[i + 1].Value),
                    factor
                );
            }
        }
        return ModelConversion.FromVector3D(keys.Last().Value);
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
    /// Retrieves the default texture for the specified graphics device.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device to associate with the default texture.</param>
    /// <returns>Returns a new instance of the <see cref="Texture2D"/> class with a default solid color texture.</returns>
    private static Texture2D GetDefaultTexture(GraphicsDevice graphicsDevice) { // TODO: Take care to dispose it!
        if (_defaultTexture != null) {
            return _defaultTexture;
        }
        else {
            using (Image<Rgba32> image = new Image<Rgba32>(1, 1, new Rgba32(128, 128, 128, 255))) {
                return _defaultTexture ??= new Texture2D(graphicsDevice, image);
            }
        }
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

    public void UpdateAnimation(CommandList commandList, ModelAnimation animation, int frame) {
        foreach (Mesh mesh in this.Meshes) {
            mesh.UpdateAnimation(commandList, animation, frame);
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

            // TODO: Unload cached resoucres (materials)
        }
    }
}