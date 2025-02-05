using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Transformations;
using Veldrid;

namespace Bliss.CSharp.Graphics.Rendering.Renderers;

public class ImmediateRenderer : Disposable {
    
    public GraphicsDevice GraphicsDevice { get; private set; }
    public OutputDescription Output { get; private set; }
    public uint Capacity { get; private set; }
    public int DrawCallCount { get; private set; }

    private Vertex3D[] _vertices;
    private uint[] _indices;

    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;

    private SimpleBuffer<Matrix4x4> _matrixBuffer;
    private SimpleBuffer<Vector4> _colorBuffer;
    
    private bool _begun;
    private CommandList _currentCommandList;
    
    public ImmediateRenderer(GraphicsDevice graphicsDevice, OutputDescription output, uint capacity = 30720) {
        this.GraphicsDevice = graphicsDevice;
        this.Output = output;
        this.Capacity = capacity;
        
        // Create vertex buffer.
        uint vertexBufferSize = capacity * (uint) Marshal.SizeOf<Vertex3D>();
        this._vertices = new Vertex3D[capacity];
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        
        // Create index buffer.
        uint indexBufferSize = capacity * sizeof(uint);
        this._indices = new uint[capacity * 3];
        this._indexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(indexBufferSize, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        
        this._matrixBuffer = new SimpleBuffer<Matrix4x4>(graphicsDevice, 3, SimpleBufferType.Uniform, ShaderStages.Vertex);
        this._colorBuffer = new SimpleBuffer<Vector4>(graphicsDevice, 1, SimpleBufferType.Uniform, ShaderStages.Fragment);
    }

    public void Begin(CommandList commandList) {
        if (this._begun) {
            throw new Exception("The PrimitiveBatch has already begun!");
        }
        
        this._begun = true;
        this._currentCommandList = commandList;
        this.DrawCallCount = 0;
    }

    public void End() {
        if (!this._begun) {
            throw new Exception("The PrimitiveBatch has not begun yet!");
        }
        
        this._begun = false;
        this.Flush();
    }

    public void DrawCube3D(Transform transform, Color? color = null) {
        Cam3D? cam3D = Cam3D.ActiveCamera;
        
        if (cam3D == null) {
            return;
        }
    }

    public void DrawLine3D() {
        
    }

    private void Flush() {
        
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            
        }
    }
}