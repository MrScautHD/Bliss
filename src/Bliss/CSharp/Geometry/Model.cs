using System.Numerics;
using Assimp;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Geometry.Conversions;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.Pipelines.Textures;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Materials;
using Bliss.CSharp.Transformations;
using Veldrid;
using AMesh = Assimp.Mesh;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace Bliss.CSharp.Geometry;

public class Model : Disposable {
    
    private const PostProcessSteps DefaultPostProcessSteps = PostProcessSteps.FlipWindingOrder | PostProcessSteps.Triangulate | PostProcessSteps.PreTransformVertices | PostProcessSteps.CalculateTangentSpace | PostProcessSteps.GenerateSmoothNormals;
    
    public GraphicsDevice GraphicsDevice { get; private set; }
    public Mesh[] Meshes { get; private set; }
    
    public uint VertexCount { get; private set; }
    public uint IndexCount { get; private set; }
    
    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;
    
    private SimpleBuffer<Matrix4x4> _modelMatrixBuffer;
    
    private SimpleTextureLayout _textureLayout;
    
    private Dictionary<MaterialOld, SimplePipeline> _cachedPipelines;

    public Model(GraphicsDevice graphicsDevice, Mesh[] meshes) {
        this.GraphicsDevice = graphicsDevice;
        this.Meshes = meshes;

        foreach (Mesh mesh in meshes) {
            this.VertexCount += (uint) mesh.Vertices.Length;
            this.IndexCount += (uint) mesh.Indices.Length;
        }
        
        uint vertexBufferSize = this.VertexCount * sizeof(float);
        uint indexBufferSize = this.IndexCount * sizeof(uint);
        
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer));
        this._indexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(indexBufferSize, BufferUsage.IndexBuffer));

        // Create model matrix buffer.
        this._modelMatrixBuffer = new SimpleBuffer<Matrix4x4>(graphicsDevice, "MatrixBuffer", 2, SimpleBufferType.Uniform, ShaderStages.Vertex);

        // Create texture layout.
        this._textureLayout = new SimpleTextureLayout(graphicsDevice, "fTexture");
        
        this._cachedPipelines = new Dictionary<MaterialOld, SimplePipeline>();
    }
    
    // TODO: Check if the UV flip works maybe it should just the Y axis get fliped and add Materials loading (with a boolean to disable it) and add Animations loading and add a option to load with Stream instead of the path.
    public static Model Load(GraphicsDevice graphicsDevice, string path, bool flipUv = false) {
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
        
        return new Model(graphicsDevice, meshes.ToArray());
    }

    // TODO: Finish that.
    public void Draw(CommandList commandList, OutputDescription output, Transform transform, Color color) {
        Cam3D? cam3D = Cam3D.ActiveCamera;

        if (cam3D == null) {
            return;
        }
        
        // Update matrix buffer.
        this._modelMatrixBuffer.SetValue(0, cam3D.GetView() * cam3D.GetProjection());
        this._modelMatrixBuffer.SetValue(1, transform.GetTransform());
        this._modelMatrixBuffer.UpdateBuffer();
        
        if (this.IndexCount > 0) {
            // Set vertex and index buffer.
            commandList.SetVertexBuffer(0, this._vertexBuffer);
            commandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt32);
            
            // Set pipeline.
            commandList.SetPipeline(this.GetOrCreatePipeline(this.Meshes[0].MaterialOld, output).Pipeline);
            
            // Set projection view buffer.
            commandList.SetGraphicsResourceSet(0, null);
            
            // Draw.
            commandList.DrawIndexed(this.IndexCount, 1, 0, 0, 0);
        }
        else {
            // Set vertex buffer.
            commandList.SetVertexBuffer(0, this._vertexBuffer);
            
            // Set pipeline.
            commandList.SetPipeline(this.GetOrCreatePipeline(this.Meshes[0].MaterialOld, output).Pipeline);
            
            // Set projection view buffer.
            commandList.SetGraphicsResourceSet(0, null);
            
            // Draw.
            commandList.Draw(this.VertexCount);
        }
    }

    public SimplePipeline GetOrCreatePipeline(MaterialOld materialOld, OutputDescription output) {
        if (!this._cachedPipelines.TryGetValue(materialOld, out SimplePipeline? pipeline)) {
            SimplePipeline newPipeline = new SimplePipeline(this.GraphicsDevice, new SimplePipelineDescription() {
                BlendState = materialOld.BlendState.Description,
                DepthStencilState = new DepthStencilStateDescription(true, true, ComparisonKind.LessEqual),
                RasterizerState = new RasterizerStateDescription() {
                    CullMode = FaceCullMode.Front,
                    FillMode = PolygonFillMode.Solid,
                    FrontFace = FrontFace.Clockwise,
                    DepthClipEnabled = true,
                    ScissorTestEnabled = false
                },
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                Buffers = [
                    this._modelMatrixBuffer
                ],
                TextureLayouts = [
                    this._textureLayout
                ],
                ShaderSet = new ShaderSetDescription() {
                    VertexLayouts = [
                        materialOld.Effect.VertexLayout
                    ],
                    Shaders = [
                        materialOld.Effect.Shader.Item1,
                        materialOld.Effect.Shader.Item2
                    ]
                },
                Outputs = output
            });
            
            this._cachedPipelines.Add(materialOld, newPipeline);
            return newPipeline;
        }

        return pipeline;
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._vertexBuffer.Dispose();
            this._indexBuffer.Dispose();
        }
    }
}