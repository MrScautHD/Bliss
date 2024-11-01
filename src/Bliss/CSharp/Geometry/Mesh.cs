using System.Numerics;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Graphics;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Materials;
using Bliss.CSharp.Transformations;
using Veldrid;

namespace Bliss.CSharp.Geometry;

public class Mesh : Disposable {
    
    public GraphicsDevice GraphicsDevice { get; private set; }
    public Material Material { get; private set; }
    
    public Vertex3D[] Vertices { get; private set; }
    public uint[] Indices { get; private set; }
    
    public uint VertexCount { get; private set; }
    public uint IndexCount { get; private set; }
    
    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;
    
    private SimpleBuffer<Matrix4x4> _modelMatrixBuffer;
    
    private Dictionary<Material, SimplePipeline> _cachedPipelines;
    
    public Mesh(GraphicsDevice graphicsDevice, Material material, Vertex3D[]? vertices = default, uint[]? indices = default) {
        this.GraphicsDevice = graphicsDevice;
        this.Material = material;
        this.Vertices = vertices ?? [];
        this.Indices = indices ?? [];

        this.VertexCount = (uint) this.Vertices.Length;
        this.IndexCount = (uint) this.Indices.Length;
        
        uint vertexBufferSize = this.VertexCount * sizeof(float);
        uint indexBufferSize = this.IndexCount * sizeof(uint);
        
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        this._indexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(indexBufferSize, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        
        this._modelMatrixBuffer = new SimpleBuffer<Matrix4x4>(graphicsDevice, "MatrixBuffer", 3, SimpleBufferType.Uniform, ShaderStages.Vertex);
        
        this._cachedPipelines = new Dictionary<Material, SimplePipeline>();
    }

    public void Draw(CommandList commandList, OutputDescription output, SamplerType samplerType, Transform transform, BlendState blendState, Color color) {
        Cam3D? cam3D = Cam3D.ActiveCamera;

        if (cam3D == null) {
            return;
        }

        // Get Sampler.
        Sampler sampler = GraphicsHelper.GetSampler(this.GraphicsDevice, samplerType);
        
        // Update matrix buffer.
        this._modelMatrixBuffer.SetValue(0, cam3D.GetProjection());
        this._modelMatrixBuffer.SetValue(1, cam3D.GetView());
        this._modelMatrixBuffer.SetValue(2, transform.GetTransform());
        this._modelMatrixBuffer.UpdateBuffer();
        
        if (this.IndexCount > 0) {
            // Set vertex and index buffer.
            commandList.SetVertexBuffer(0, this._vertexBuffer);
            commandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt32);
            
            // Set pipeline.
            commandList.SetPipeline(this.GetOrCreatePipeline(this.Material, blendState, output).Pipeline);
            
            // Set projection view buffer.
            commandList.SetGraphicsResourceSet(0, this._modelMatrixBuffer.ResourceSet);
            
            // Set material.
            for (int i = 0; i < 11; i++) {
                MaterialMapType mapType = (MaterialMapType) i;
                ResourceSet? resourceSet = this.Material.GetResourceSet(sampler, this.Material.TextureLayouts[i].Layout, mapType);

                if (resourceSet != null) {
                    commandList.SetGraphicsResourceSet((uint) i + 1, resourceSet);
                }
            }
            
            // Draw.
            commandList.DrawIndexed(this.IndexCount, 1, 0, 0, 0);
        }
        else {
            // Set vertex buffer.
            commandList.SetVertexBuffer(0, this._vertexBuffer);
            
            // Set pipeline.
            commandList.SetPipeline(this.GetOrCreatePipeline(this.Material, blendState, output).Pipeline);
            
            // Set projection view buffer.
            commandList.SetGraphicsResourceSet(0, null);
            
            // Draw.
            commandList.Draw(this.VertexCount);
        }
    }
    
    public SimplePipeline GetOrCreatePipeline(Material material, BlendState blendState, OutputDescription output) {
        if (!this._cachedPipelines.TryGetValue(material, out SimplePipeline? pipeline)) {
            SimplePipeline newPipeline = new SimplePipeline(this.GraphicsDevice, new SimplePipelineDescription() {
                BlendState = blendState.Description,
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
                TextureLayouts = material.TextureLayouts,
                ShaderSet = new ShaderSetDescription() {
                    VertexLayouts = [
                        material.Effect.VertexLayout
                    ],
                    Shaders = [
                        material.Effect.Shader.Item1,
                        material.Effect.Shader.Item2
                    ]
                },
                Outputs = output
            });
            
            this._cachedPipelines.Add(material, newPipeline);
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