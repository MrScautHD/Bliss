using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Textures.Cubemaps;
using Veldrith;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Sky;

public class CubemapSkybox : Disposable, ISkybox {
    
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
    /// The exposure level of the skybox, controlling the brightness adjustment of the rendered scene.
    /// </summary>
    public float Exposure {
        get;
        set {
            if (Math.Abs(field - value) < float.Epsilon) {
                return;
            }
            
            field = value;
            this._isDirty = true;
        }
    }
    
    /// <summary>
    /// The rotational angle of the skybox in radians.
    /// </summary>
    public float Rotation {
        get;
        set {
            if (Math.Abs(field - value) < float.Epsilon) {
                return;
            }
            
            field = value;
            this._isDirty = true;
        }
    }
    
    /// <summary>
    /// Indicates whether the skybox buffer needs to be updated.
    /// </summary>
    private bool _isDirty;
    
    /// <summary>
    /// The effect used for rendering the skybox.
    /// </summary>
    private Effect _effect;
    
    /// <summary>
    /// The buffer containing vertex data.
    /// </summary>
    private DeviceBuffer _vertexBuffer;
    
    /// <summary>
    /// The buffer containing index data.
    /// </summary>
    private DeviceBuffer _indexBuffer;
    
    /// <summary>
    /// A uniform buffer used to store transformation matrices for rendering the skybox.
    /// </summary>
    private SimpleUniformBuffer<Matrix4x4> _matrixBuffer;
    
    /// <summary>
    /// Uniform buffer that holds the parameters for the skybox rendering.
    /// </summary>
    private SimpleUniformBuffer<SkyboxData> _skyboxBuffer;
    
    /// <summary>
    /// The description of the rendering pipeline used for the skybox.
    /// </summary>
    private SimplePipelineDescription _pipelineDescription;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CubemapSkybox"/> class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering.</param>
    /// <param name="cubemap">The cubemap texture used for the skybox.</param>
    /// <param name="sampler">The optional texture sampler.</param>
    /// <param name="color">The optional color tint applied to the skybox.</param>
    /// <param name="exposure">A brightness multiplier for the skybox. 1.0 leaves the sky unchanged, values below 1.0 darken it, and values above 1.0 brighten it.</param>
    /// <param name="rotation">The rotation of the skybox around the vertical axis. Changing it turns the sky without moving the camera.</param>
    public CubemapSkybox(GraphicsDevice graphicsDevice, Cubemap cubemap, Sampler? sampler = null, Color? color = null, float exposure = 1.0F, float rotation = 0.0F) {
        this.GraphicsDevice = graphicsDevice;
        this.Cubemap = cubemap;
        this.Sampler = sampler ?? GraphicsHelper.GetSampler(graphicsDevice, SamplerType.Aniso4XClamp);
        this.Color = color ?? Color.White;
        this.Exposure = exposure;
        this.Rotation = rotation;
        this._effect = GlobalResource.CubemapSkyboxEffect;
        
        // Create vertex buffer.
        uint vertexBufferSize = (uint) Marshal.SizeOf<CubemapVertex3D>() * 8;
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        this._vertexBuffer.Name = "VertexBuffer";
        graphicsDevice.UpdateBuffer(this._vertexBuffer, 0, this.GenVertices());
        
        // Create index buffer.
        uint indexBufferSize = sizeof(uint) * 36;
        this._indexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(indexBufferSize, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        this._indexBuffer.Name = "IndexBuffer";
        graphicsDevice.UpdateBuffer(this._indexBuffer, 0, this.GenIndices());
        
        // Create matrix buffer.
        this._matrixBuffer = new SimpleUniformBuffer<Matrix4x4>(this.GraphicsDevice, 2, ShaderStages.Vertex | ShaderStages.Fragment);
        this._matrixBuffer.DeviceBuffer.Name = "MatrixBuffer";
        
        // Create the data buffer.
        this._skyboxBuffer = new SimpleUniformBuffer<SkyboxData>(graphicsDevice, 1, ShaderStages.Fragment);
        this._skyboxBuffer.DeviceBuffer.Name = "SkyboxBuffer";
        this._isDirty = true;
        
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
    /// Renders the cubemap skybox into the specified framebuffer.
    /// </summary>
    /// <param name="commandList">The command list used to record the rendering commands.</param>
    /// <param name="framebuffer">The framebuffer to render the skybox into.</param>
    /// <param name="projection">The projection matrix used to transform the skybox geometry.</param>
    /// <param name="view">The view matrix used to determine the viewing orientation.</param>
    public void Draw(CommandList commandList, Framebuffer framebuffer, Matrix4x4 projection, Matrix4x4 view) {
        
        // Update pipeline description.
        this._pipelineDescription.Outputs = framebuffer.OutputDescription;
        
        // Upload matrix buffer.
        this._matrixBuffer.SetValue(0, projection);
        this._matrixBuffer.SetValue(1, view);
        this._matrixBuffer.UpdateBufferDeferred(commandList);
        
        // Update skybox buffer.
        if (this._isDirty) {
            Vector4 color = this.Color.ToRgbaFloatVec4();
            
            this._skyboxBuffer.SetValue(0, new SkyboxData() {
                Rotation = Matrix4x4.CreateRotationY(float.DegreesToRadians(this.Rotation)),
                Color = new Vector4(color.X, color.Y, color.Z, this.Exposure)
            });
            this._skyboxBuffer.UpdateBufferDeferred(commandList);
            this._isDirty = false;
        }
        
        // Set vertex and index buffer.
        commandList.SetVertexBuffer(0, this._vertexBuffer);
        commandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt32);
        
        // Set pipeline.
        commandList.SetPipeline(this._effect.GetPipeline(this._pipelineDescription).Pipeline);
        
        // Set matrix buffer.
        commandList.SetGraphicsResourceSet(this._effect.GetBufferLayoutSlot("MatrixBuffer"), this._matrixBuffer.GetResourceSet(this._effect.GetBufferLayout("MatrixBuffer")));
        
        // Set skybox data buffer.
        commandList.SetGraphicsResourceSet(this._effect.GetBufferLayoutSlot("SkyboxBuffer"), this._skyboxBuffer.GetResourceSet(this._effect.GetBufferLayout("SkyboxBuffer")));
        
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
        Vector4 color = Color.White.ToRgbaFloatVec4();
        
        return [
            new CubemapVertex3D(new Vector3(-1, -1, -1), color),
            new CubemapVertex3D(new Vector3(1, -1, -1), color),
            new CubemapVertex3D(new Vector3(1, 1, -1), color),
            new CubemapVertex3D(new Vector3(-1, 1, -1), color),
            
            new CubemapVertex3D(new Vector3(-1, -1, 1), color),
            new CubemapVertex3D(new Vector3(1, -1, 1), color),
            new CubemapVertex3D(new Vector3(1, 1, 1), color),
            new CubemapVertex3D(new Vector3(-1, 1, 1), color)
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
    
    /// <summary>
    /// Struct holding configurable Skybox data.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct SkyboxData {
        public Matrix4x4 Rotation;
        public Vector4 Color;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._vertexBuffer.Dispose();
            this._indexBuffer.Dispose();
            this._skyboxBuffer.Dispose();
        }
    }
}