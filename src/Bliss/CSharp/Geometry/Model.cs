using System.Collections.ObjectModel;
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
    
    private const PostProcessSteps DefaultPostProcessSteps = PostProcessSteps.FlipWindingOrder | PostProcessSteps.Triangulate | PostProcessSteps.PreTransformVertices | PostProcessSteps.CalculateTangentSpace | PostProcessSteps.GenerateSmoothNormals;
    
    public GraphicsDevice GraphicsDevice { get; private set; }
    public Mesh[] Meshes { get; private set; }

    public Model(GraphicsDevice graphicsDevice, Mesh[] meshes) {
        this.GraphicsDevice = graphicsDevice;
        this.Meshes = meshes;
    }
    
    // TODO: Check if the UV flip works maybe it should just the Y axis get fliped and add Materials loading (with a boolean to disable it) and add Animations loading and add a option to load with Stream instead of the path.
    public static Model Load(GraphicsDevice graphicsDevice, string path, bool flipUv = false) {
        using AssimpContext context = new AssimpContext();
        Scene scene = context.ImportFile(path, DefaultPostProcessSteps);

        List<Mesh> meshes = new List<Mesh>();

        for (int i = 0; i < scene.Meshes.Count; i++) {
            AMesh mesh = scene.Meshes[i];
            
            // Materials
            Effect? effect = null;
            Dictionary<MaterialMapType, MaterialMap> maps = new Dictionary<MaterialMapType, MaterialMap>();
            
            if (scene.HasMaterials) {
                AMaterial aMaterial = scene.Materials[mesh.MaterialIndex];
                ShaderMaterialProperties shaderProperties = aMaterial.Shaders;

                if (shaderProperties.HasVertexShader && shaderProperties.HasFragmentShader) {
                    effect = new Effect(graphicsDevice.ResourceFactory, Vertex3D.VertexLayout, Encoding.UTF8.GetBytes(shaderProperties.VertexShader), Encoding.UTF8.GetBytes(shaderProperties.FragmentShader));
                }
                else {
                    throw new Exception("Something went wrong by loading the Effect of the model material!");
                }
                
                for (int j = 0; j < 11; j++) {
                    List<Texture2D> textures = new List<Texture2D>();
                    List<Color> colors = new List<Color>();
                    
                    foreach (TextureSlot textureSlot in aMaterial.GetAllMaterialTextures()) {
                        textures.Add(new Texture2D(graphicsDevice, textureSlot.FilePath));
                    }

                    if (aMaterial.HasColorDiffuse) {
                        Color4D color4D = aMaterial.ColorDiffuse;
                        colors.Add(new Color(new RgbaFloat(color4D.R, color4D.G, color4D.B, color4D.A)));
                    }

                    MaterialMapType mapType = (MaterialMapType) j;

                    switch (mapType) {
                        case MaterialMapType.Albedo:
                            maps.Add(MaterialMapType.Albedo, new MaterialMap {
                                Texture = textures[j],
                                Color = colors[j]
                            });
                            break;
                    }
                }
            }
            
            Material material = new Material(graphicsDevice, effect!, new ReadOnlyDictionary<MaterialMapType, MaterialMap>(maps), default, true);
            
            // Vertices
            Vertex3D[] vertices = new Vertex3D[scene.Meshes[i].VertexCount];

            for (int j = 0; j < mesh.VertexCount; j++) {
                
                // Pos
                vertices[j].Position = ModelConversion.FromVector3D(mesh.Vertices[i]);

                // TexCoord
                if (mesh.HasTextureCoords(0)) {
                    Vector3 texCoord = ModelConversion.FromVector3D(mesh.TextureCoordinateChannels[0][j]);
                    Vector2 finalTexCoord = new Vector2(texCoord.X, texCoord.Y);
                    
                    vertices[j].TexCoords = flipUv ? -finalTexCoord : finalTexCoord;
                }
                else {
                    vertices[j].TexCoords = Vector2.Zero;
                }
                
                // TexCoord2
                if (mesh.HasTextureCoords(1)) {
                    Vector3 texCoord2 = ModelConversion.FromVector3D(mesh.TextureCoordinateChannels[1][j]);
                    Vector2 finalTexCoord2 = new Vector2(texCoord2.X, texCoord2.Y);
                    
                    vertices[j].TexCoords2 = flipUv ? -finalTexCoord2 : finalTexCoord2;
                }
                else {
                    vertices[j].TexCoords2 = Vector2.Zero;
                }
                
                // Normal
                vertices[j].Normal = mesh.HasNormals ? ModelConversion.FromVector3D(mesh.Normals[i]) : Vector3.Zero;

                // Tangent
                vertices[j].Tangent = mesh.HasTangentBasis ? ModelConversion.FromVector3D(mesh.Tangents[j]) : Vector3.Zero;
                
                // Color
                vertices[j].Color = material.Maps[MaterialMapType.Albedo].Color.ToVector4();
            }

            // Indices
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
        
        Logger.Info($"Model successfully loaded from the path: [{path}]");
        Logger.Info($"\t> Meshes: {meshes.Count}");
        
        return new Model(graphicsDevice, meshes.ToArray());
    }

    //private static MaterialMapType MapMaterialMapType(TextureType textureType) {
    //    return textureType switch {
    //        TextureType.Diffuse => MaterialMapType.Albedo,
    //        _ => Logger.Warn($"Failed to load MaterialMapType: {textureType}!");
    //    };
    //}

    public void Draw(CommandList commandList, OutputDescription output, Transform transform, BlendState blendState, Color color) {
        foreach (Mesh mesh in this.Meshes) {
            mesh.Draw(commandList, output, transform, blendState, color);
        }
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            
        }
    }
}