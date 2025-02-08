using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Transformations;
using Veldrid;

namespace Bliss.CSharp.Graphics.Rendering.Renderers;

public class ImmediateRenderer : Disposable {
    
    public GraphicsDevice GraphicsDevice { get; private set; }
    public OutputDescription Output { get; private set; }
    public Effect Effect { get; private set; }
    public uint Capacity { get; private set; }
    public int DrawCallCount { get; private set; }

    private ImmediateVertex3D[] _vertices;
    private uint[] _indices;

    private int _vertexCount;
    private int _indexCount;

    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;

    private SimpleBuffer<Matrix4x4> _matrixBuffer;

    private SimplePipelineDescription _pipelineDescription;
    
    private bool _begun;
    private CommandList _currentCommandList;
    private BlendState _currentBlendState;
    
    public ImmediateRenderer(GraphicsDevice graphicsDevice, OutputDescription output, Effect? effect = null, uint capacity = 30720) {
        this.GraphicsDevice = graphicsDevice;
        this.Output = output;
        this.Effect = effect ?? GlobalResource.FullScreenRenderPassEffect;
        this.Capacity = capacity;
        
        // Create vertex buffer.
        uint vertexBufferSize = capacity * (uint) Marshal.SizeOf<Vertex3D>();
        this._vertices = new ImmediateVertex3D[capacity];
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        
        // Create index buffer.
        uint indexBufferSize = capacity * sizeof(uint);
        this._indices = new uint[capacity * 3];
        this._indexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(indexBufferSize, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        
        // Create matrix buffer.
        this._matrixBuffer = new SimpleBuffer<Matrix4x4>(graphicsDevice, 3, SimpleBufferType.Uniform, ShaderStages.Vertex);

        // Create pipeline description.
        this._pipelineDescription = this.CreatePipelineDescription();
    }

    public void Begin(CommandList commandList, BlendState? blendState = null) {
        if (this._begun) {
            throw new Exception("The ImmediateRenderer has already begun!");
        }
        
        this._begun = true;
        this._currentCommandList = commandList;

        if (this._currentBlendState != blendState) {
            this.Flush();
        }

        this._currentBlendState = blendState ?? BlendState.AlphaBlend;
        this._pipelineDescription.BlendState = this._currentBlendState.Description;
        this.DrawCallCount = 0;
    }

    public void End() {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        this._begun = false;
        this.Flush();
    }

    public void DrawVertices(ImmediateVertex3D[] vertices, uint[] indices, Transform transform) {
        if (!this._begun) {
            throw new Exception("You must begin the ImmediateRenderer before calling draw methods!");
        }
        
        Cam3D? cam3D = Cam3D.ActiveCamera;

        if (cam3D == null) {
            return;
        }

        if (this._vertexCount + vertices.Length > this.Capacity) {
            this.Flush();
        }

        if (this._indexCount + indices.Length > this.Capacity) {
            this.Flush();
        }

        if (this._vertexCount + vertices.Length > this.Capacity) {
            Logger.Fatal(new InvalidOperationException($"The number of provided vertices exceeds the maximum batch size! [{this._vertices.Length + vertices.Length} > {this.Capacity}]"));
        }

        if (this._indexCount + indices.Length > this.Capacity) {
            Logger.Fatal(new InvalidOperationException($"The number of provided indices exceeds the maximum batch size! [{this._indices.Length + indices.Length} > {this.Capacity}]"));
        }
        
        // Add vertices.
        for (int i = 0; i < vertices.Length; i++) {
            this._vertices[this._vertexCount + i] = vertices[i];
        }
        
        this._vertexCount += vertices.Length;
        
        // Add indices.
        for (int i = 0; i < indices.Length; i++) {
            this._indices[this._indexCount + i] = indices[i];
        }

        this._indexCount += indices.Length;
        
        // Update matrix buffer.
        this._matrixBuffer.SetValue(0, cam3D.GetProjection());
        this._matrixBuffer.SetValue(1, cam3D.GetView());
        this._matrixBuffer.SetValue(2, transform.GetTransform());
        this._matrixBuffer.UpdateBuffer(this._currentCommandList);
    }
    
    private void Flush() {
        if (this._vertexCount == 0) {
            return;
        }

        if (this._indexCount > 0) {
            
            // Update vertex and index buffer.
            this._currentCommandList.UpdateBuffer(this._vertexBuffer, 0, this._vertices);
            this._currentCommandList.UpdateBuffer(this._indexBuffer, 0, this._indices);
            
            // Set vertex and index buffer.
            this._currentCommandList.SetVertexBuffer(0, this._vertexBuffer);
            this._currentCommandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt32);

            // Set pipeline.
            this._currentCommandList.SetPipeline(this.Effect.GetPipeline(this._pipelineDescription).Pipeline);
        
            // Set matrix buffer.
            this._currentCommandList.SetGraphicsResourceSet(0, this._matrixBuffer.GetResourceSet(this.Effect.GetBufferLayout("MatrixBuffer")));
        
            // Draw.
            this._currentCommandList.DrawIndexed((uint) this._indexCount);
        }
        else {
            
            // Update vertex buffer.
            this._currentCommandList.UpdateBuffer(this._vertexBuffer, 0, this._vertices);
            
            // Set vertex buffer.
            this._currentCommandList.SetVertexBuffer(0, this._vertexBuffer);

            // Set pipeline.
            this._currentCommandList.SetPipeline(this.Effect.GetPipeline(this._pipelineDescription).Pipeline);
        
            // Set matrix buffer.
            this._currentCommandList.SetGraphicsResourceSet(0, this._matrixBuffer.GetResourceSet(this.Effect.GetBufferLayout("MatrixBuffer")));
        
            // Draw.
            this._currentCommandList.Draw((uint) this._vertexCount);
        }
        
        this._vertexCount = 0;
        this._indexCount = 0;
        
        Array.Clear(this._vertices);
        Array.Clear(this._indices);
        
        this.DrawCallCount++;
    }
    
    private SimplePipelineDescription CreatePipelineDescription() {
        return new SimplePipelineDescription() {
            DepthStencilState = new DepthStencilStateDescription(true, true, ComparisonKind.LessEqual),
            RasterizerState = new RasterizerStateDescription() {
                CullMode = FaceCullMode.Back,
                FillMode = PolygonFillMode.Solid,
                FrontFace = FrontFace.Clockwise,
                DepthClipEnabled = true,
                ScissorTestEnabled = false
            },
            PrimitiveTopology = PrimitiveTopology.TriangleList, // TODO: Maybe need handled like blendstate
            BufferLayouts = this.Effect.GetBufferLayouts(),
            TextureLayouts = this.Effect.GetTextureLayouts(),
            ShaderSet = new ShaderSetDescription() {
                VertexLayouts = [
                    this.Effect.VertexLayout
                ],
                Shaders = [
                    this.Effect.Shader.Item1,
                    this.Effect.Shader.Item2
                ]
            },
            Outputs = this.Output
        };
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._vertexBuffer.Dispose();
            this._indexBuffer.Dispose();
            this._matrixBuffer.Dispose();
        }
    }
}