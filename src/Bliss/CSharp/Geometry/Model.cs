using System.Numerics;
using Assimp;
using Bliss.CSharp.Geometry.Conversions;
using Bliss.CSharp.Logging;
using Veldrid;
using AMesh = Assimp.Mesh;

namespace Bliss.CSharp.Geometry;

public class Model : Disposable {
    
    private const PostProcessSteps DefaultPostProcessSteps = PostProcessSteps.FlipWindingOrder | PostProcessSteps.Triangulate | PostProcessSteps.PreTransformVertices | PostProcessSteps.CalculateTangentSpace | PostProcessSteps.GenerateSmoothNormals;
    
    public Mesh[] Meshes { get; private set; }

    public uint VertexCount { get; private set; }
    public uint IndexCount { get; private set; }
    
    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;
    
    public Model(ResourceFactory factory, Mesh[] meshes) {
        this.Meshes = meshes;

        foreach (Mesh mesh in meshes) {
            this.VertexCount += (uint) mesh.Vertices.Length;
            this.IndexCount += (uint) mesh.Indices.Length;
        }
        
        uint vertexBufferSize = this.VertexCount * sizeof(float);
        uint indexBufferSize = this.IndexCount * sizeof(uint);
        
        this._vertexBuffer = factory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer));
        this._indexBuffer = factory.CreateBuffer(new BufferDescription(indexBufferSize, BufferUsage.IndexBuffer));
    }
    
    // TODO: bool loadMaterials = false
    public static Model Load(ResourceFactory factory, string path, bool flipUv = false) {
        using AssimpContext context = new AssimpContext();
        Scene scene = context.ImportFile(path, DefaultPostProcessSteps);

        List<Mesh> meshes = new List<Mesh>();

        for (int i = 0; i < scene.Meshes.Count; i++) {
            AMesh mesh = scene.Meshes[i];
            
            // Materials
            Color4D? color = null;
            
            if (scene.HasMaterials) {
                color = scene.Materials[mesh.MaterialIndex].ColorDiffuse;
            }
            
            // Vertices
            Vertex[] vertices = new Vertex[scene.Meshes[i].VertexCount];

            for (int j = 0; j < mesh.VertexCount; j++) {
                
                // Pos
                vertices[j].Position = mesh.HasVertices ? ModelConversion.FromVector3D(mesh.Vertices[i]) : Vector3.Zero;

                // TexCoord
                if (mesh.HasTextureCoords(0)) {
                    Vector3 texCoord = ModelConversion.FromVector3D(mesh.TextureCoordinateChannels[0][j]);
                    Vector2 finalTexCoord = new Vector2(texCoord.X, texCoord.Y);
                    
                    vertices[j].TexCoord = flipUv ? -finalTexCoord : finalTexCoord;
                }
                else {
                    vertices[j].TexCoord = Vector2.Zero;
                }
                
                // TexCoord2
                if (mesh.HasTextureCoords(1)) {
                    Vector3 texCoord2 = ModelConversion.FromVector3D(mesh.TextureCoordinateChannels[1][j]);
                    Vector2 finalTexCoord2 = new Vector2(texCoord2.X, texCoord2.Y);
                    
                    vertices[j].TexCoord2 = flipUv ? -finalTexCoord2 : finalTexCoord2;
                }
                else {
                    vertices[j].TexCoord2 = Vector2.Zero;
                }
                
                // Normal
                vertices[j].Normal = mesh.HasNormals ? ModelConversion.FromVector3D(mesh.Normals[i]) : Vector3.Zero;

                // Tangent
                vertices[j].Tangent = mesh.HasTangentBasis ? ModelConversion.FromVector3D(mesh.Tangents[j]) : Vector3.Zero;
                
                // Color
                vertices[j].Color = color != null ? new Vector4(color.Value.R, color.Value.G, color.Value.B, color.Value.A) : Vector4.Zero;
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

            meshes.Add(new Mesh(vertices, indices.ToArray()));
        }
        
        Logger.Info($"Model successfully loaded from the path: [{path}]");
        Logger.Info($"\t> Meshes: {meshes.Count}");
        
        return new Model(factory, meshes.ToArray());
    }

    // TODO: Finish that.
    public void Draw(CommandList commandList) {
        commandList.SetPipeline(null);
        commandList.SetGraphicsResourceSet(0, null);
        commandList.SetVertexBuffer(0, this._vertexBuffer);
        commandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt32);
        commandList.DrawIndexed(this.IndexCount, 1, 0, 0, 0);
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._vertexBuffer.Dispose();
            this._indexBuffer.Dispose();
        }
    }
}