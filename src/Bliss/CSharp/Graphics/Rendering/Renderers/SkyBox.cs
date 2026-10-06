using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Textures.Cubemaps;
using Veldrith;

namespace Bliss.CSharp.Graphics.Rendering.Renderers;

public class SkyBox : Disposable {
    
    /// <summary>
    /// The graphics device used for rendering.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// The cubemap texture used for the skybox.
    /// </summary>
    public Cubemap Cubemap;
    
    /// <summary>
    /// The sampler used for texture sampling.
    /// </summary>
    public Sampler Sampler;
    
    /// <summary>
    /// The color tint applied to the skybox.
    /// </summary>
    public Color Color {
        get;
        set {
            if (field == value) {
                return;
            }
            
            field = value;
            this._isDirty = true;
        }
    }
    
    /// <summary>
    /// Indicates whether the vertex buffer needs to be updated.
    /// </summary>
    private bool _isDirty;
    
    /// <summary>
    /// The effect used for rendering the skybox.
    /// </summary>
    private Effect _effect;
    
    /// <summary>
    /// The vertex data for the skybox.
    /// </summary>
    private CubemapVertex3D[] _vertices;
    
    /// <summary>
    /// The index data defining the skybox faces.
    /// </summary>
    private uint[] _indices;
    
    /// <summary>
    /// The buffer containing vertex data.
    /// </summary>
    private DeviceBuffer _vertexBuffer;
    
    /// <summary>
    /// The buffer containing index data.
    /// </summary>
    private DeviceBuffer _indexBuffer;

    /// <summary>
    /// The description of the rendering pipeline used for the skybox.
    /// </summary>
    private SimplePipelineDescription _pipelineDescription;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SkyBox"/> class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering.</param>
    /// <param name="cubemap">The cubemap texture used for the skybox.</param>
    /// <param name="sampler">The optional texture sampler.</param>
    /// <param name="color">The optional color tint applied to the skybox.</param>
    public SkyBox(GraphicsDevice graphicsDevice, Cubemap cubemap, Sampler? sampler = null, Color? color = null) {
        this.GraphicsDevice = graphicsDevice;
        this.Cubemap = cubemap;
        this.Sampler = sampler ?? GraphicsHelper.GetSampler(graphicsDevice, SamplerType.Aniso4XClamp);
        this.Color = color ?? Color.White;
        this._effect = GlobalResource.SkyboxEffect;
        
        // Create vertex buffer.
        uint vertexBufferSize = (uint) Marshal.SizeOf<CubemapVertex3D>() * 8;
        this._vertices = this.GenVertices();
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        graphicsDevice.UpdateBuffer(this._vertexBuffer, 0, this._vertices);
        
        // Create index buffer.
        uint indexBufferSize = sizeof(uint) * 36;
        this._indices = this.GenIndices();
        this._indexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(indexBufferSize, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        graphicsDevice.UpdateBuffer(this._indexBuffer, 0, this._indices);
        
        // Create pipeline.
        this._pipelineDescription = new SimplePipelineDescription() {
            BlendState = BlendStateDescription.SINGLE_DISABLED,
            DepthStencilState = new DepthStencilStateDescription() {
                DepthTestEnabled = true,
                DepthWriteEnabled = false,
                DepthComparison = ComparisonKind.LessEqual
            },
            RasterizerState = new RasterizerStateDescription() {
                CullMode = FaceCullMode.None,
                FillMode = PolygonFillMode.Solid,
                FrontFace = FrontFace.Clockwise,
                DepthClipEnabled = true,
                ScissorTestEnabled = false
            },
            BufferLayouts = this._effect.GetBufferLayouts(),
            TextureLayouts = this._effect.GetTextureLayouts(),
            ShaderSet = new ShaderSetDescription(CubemapVertex3D.VertexLayout.Layouts, this._effect.Shaders),
            PrimitiveTopology = PrimitiveTopology.TriangleList,
        };
    }
    
    /// <summary>
    /// Renders the skybox using the specified command list and framebuffer.
    /// </summary>
    /// <param name="commandList">The command list used for issuing draw commands.</param>
    /// <param name="framebuffer">The framebuffer representing the current rendering target.</param>
    public void Draw(CommandList commandList, Framebuffer framebuffer) {
        Cam3D? cam3D = Cam3D.ActiveCamera;
        
        if (cam3D == null) {
            return;
        }
        
        // Update pipeline description.
        this._pipelineDescription.Outputs = framebuffer.OutputDescription;
        
        // Update vertex buffer.
        if (this._isDirty) {
            for (int i = 0; i < this._vertices.Length; i++) {
                this._vertices[i].Color = this.Color.ToRgbaFloatVec4();
            }
            
            commandList.UpdateBuffer(this._vertexBuffer, 0, this._vertices);
        }
        
        // Set vertex and index buffer.
        commandList.SetVertexBuffer(0, this._vertexBuffer);
        commandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt32);
        
        // Set pipeline.
        commandList.SetPipeline(this._effect.GetPipeline(this._pipelineDescription).Pipeline);
        
        // Set matrix buffer.
        commandList.SetGraphicsResourceSet(this._effect.GetBufferLayoutSlot("MatrixBuffer"), cam3D.GetMatrixBuffer().GetResourceSet(this._effect.GetBufferLayout("MatrixBuffer")));
        
        // Set resourceSet of the cubemap.
        commandList.SetGraphicsResourceSet(this._effect.GetTextureLayoutSlot("fCubemap"), this.Cubemap.GetResourceSet(this.Sampler, this._effect.GetTextureLayout("fCubemap")));
        
        // Apply effect.
        this._effect.Apply(commandList);
        
        // Draw.
        commandList.DrawIndexed(36);
    }
    
    /// <summary>
    /// Generates and returns an array of vertices for the skybox.
    /// </summary>
    /// <returns>An array of <see cref="CubemapVertex3D"/> objects representing the vertices of the skybox.</returns>
    private CubemapVertex3D[] GenVertices() {
        Color color = Color.White;
        
        return [
            new CubemapVertex3D(new Vector3(-1, -1, -1), color.ToRgbaFloatVec4()),
            new CubemapVertex3D(new Vector3(1, -1, -1), color.ToRgbaFloatVec4()),
            new CubemapVertex3D(new Vector3(1, 1, -1), color.ToRgbaFloatVec4()),
            new CubemapVertex3D(new Vector3(-1, 1, -1), color.ToRgbaFloatVec4()),
            
            new CubemapVertex3D(new Vector3(-1, -1, 1), color.ToRgbaFloatVec4()),
            new CubemapVertex3D(new Vector3(1, -1, 1), color.ToRgbaFloatVec4()),
            new CubemapVertex3D(new Vector3(1, 1, 1), color.ToRgbaFloatVec4()),
            new CubemapVertex3D(new Vector3(-1, 1, 1), color.ToRgbaFloatVec4())
        ];
    }
    
    /// <summary>
    /// Generates and returns an array of indices that define the connectivity of the vertices for the skybox.
    /// </summary>
    /// <returns>An array of unsigned integer values representing the vertex indices used to construct the skybox.</returns>
    private uint[] GenIndices() {
        return [
            0, 1, 2,
            2, 3, 0,
            
            4, 5, 6,
            6, 7, 4,
            
            4, 0, 3,
            3, 7, 4,
            
            1, 5, 6,
            6, 2, 1,
            
            3, 2, 6,
            6, 7, 3,
            
            4, 5, 1,
            1, 0, 4
        ];
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._vertexBuffer.Dispose();
            this._indexBuffer.Dispose();
        }
    }
}