using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.VertexTypes;
using Veldrith;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Sky;

public class ProceduralSkybox : Disposable, ISkybox {
    
    /// <summary>
    /// The graphics device used for rendering.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// Controls the density/strength of the atmospheric gradient.
    /// </summary>
    public float AtmosphereThickness {
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
    /// Controls how the sun is rendered.
    /// </summary>
    public SkyboxSunQuality SunQuality {
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
    /// The apparent size of the sun disk.
    /// </summary>
    public float SunSize {
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
    /// Controls how concentrated the sun disk is.
    /// </summary>
    public float SunSizeConvergence {
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
    /// Direction from the world toward the sun.
    /// </summary>
    public Vector3 SunDirection {
        get;
        set {
            if (field == value) {
                return;
            }
            
            field = value.LengthSquared() > 0.0F ? Vector3.Normalize(value) : Vector3.UnitY;
            this._isDirty = true;
        }
    }
    
    /// <summary>
    /// Color of the sun.
    /// </summary>
    public Color SunColor {
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
    /// Tint applied to the upper sky.
    /// </summary>
    public Color SkyColor {
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
    /// Color used below the horizon.
    /// </summary>
    public Color GroundColor {
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
    /// Brightness multiplier for the final sky color.
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
    /// Uniform buffer that holds the parameters for the skybox rendering.
    /// </summary>
    private SimpleUniformBuffer<SkyboxData> _skyboxBuffer;
    
    /// <summary>
    /// The description of the rendering pipeline used for the skybox.
    /// </summary>
    private SimplePipelineDescription _pipelineDescription;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ProceduralSkybox"/> class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering.</param>
    /// <param name="atmosphereThickness">The thickness of the atmosphere. Higher values scatter more light, which gives a deeper blue sky and stronger red and orange colors near the horizon at sunrise and sunset. 1.0 is an Earth-like atmosphere.</param>
    /// <param name="sunQuality">The quality of the sun disc rendering, which decides how the sun and its glow are drawn.</param>
    /// <param name="sunSize">The size of the sun disc. Higher values make the sun larger.</param>
    /// <param name="sunSizeConvergence">How quickly the glow around the sun disc fades into the sky. Higher values give a tighter glow, and lower values give a wide halo.</param>
    /// <param name="sunDirection">The direction that points from the viewer towards the sun. It does not have to be normalized. If <see langword="null"/>, a direction high in the sky is used.</param>
    /// <param name="sunColor">The color of the sun disc and its glow. If <see langword="null"/>, a warm white is used.</param>
    /// <param name="skyColor">The color of the sky, which tints the atmospheric scattering. If <see langword="null"/>, a light blue is used.</param>
    /// <param name="groundColor">The color of the ground below the horizon. If <see langword="null"/>, a medium gray is used.</param>
    /// <param name="color">The color tint that is multiplied with the whole skybox. If <see langword="null"/>, white is used, which leaves the sky unchanged.</param>
    /// <param name="exposure">A brightness multiplier for the skybox. 1.0 leaves the sky unchanged, values below 1.0 darken it, and values above 1.0 brighten it.</param>
    public ProceduralSkybox(
        GraphicsDevice graphicsDevice,
        float atmosphereThickness = 1.0F,
        SkyboxSunQuality sunQuality = SkyboxSunQuality.HighQuality,
        float sunSize = 0.002F,
        float sunSizeConvergence = 2.0F,
        Vector3? sunDirection = null,
        Color? sunColor = null,
        Color? skyColor = null,
        Color? groundColor = null,
        Color? color = null,
        float exposure = 1.0F) {
        this.GraphicsDevice = graphicsDevice;
        this.AtmosphereThickness = atmosphereThickness;
        this.SunQuality = sunQuality;
        this.SunSize = sunSize;
        this.SunSizeConvergence = sunSizeConvergence;
        this.SunDirection = sunDirection ?? Vector3.Normalize(new Vector3(0.25F, 0.85F, 0.15F));
        this.SunColor = sunColor ?? new Color(255, 244, 214, 255);
        this.SkyColor = skyColor ?? new Color(122, 155, 255, 255);
        this.GroundColor = groundColor ?? new Color(86, 86, 86, 255);
        this.Color = color ?? Color.White;
        this.Exposure = exposure;
        this._effect = GlobalResource.ProceduralSkyboxEffect;
        
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
        
        // Create the params buffer.
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
        
        // Update skybox buffer.
        if (this._isDirty) {
            
            // Create skybox data.
            Vector3 sunDirection = this.SunDirection.LengthSquared() > 0.0F ? Vector3.Normalize(this.SunDirection) : Vector3.UnitY;
            
            SkyboxData data = new SkyboxData() {
                SkyColor = this.SkyColor.ToRgbaFloatVec4(),
                GroundColor = this.GroundColor.ToRgbaFloatVec4(),
                SunColor = this.SunColor.ToRgbaFloatVec4(),
                Color = this.Color.ToRgbaFloatVec4(),
                SunDirection = new Vector4(sunDirection, 0.0F),
                SunSize = this.SunSize,
                SunSizeConvergence = this.SunSizeConvergence,
                AtmosphereThickness = this.AtmosphereThickness,
                Exposure = this.Exposure,
                SunQuality = (int) this.SunQuality
            };
            
            // Upload buffer to GPU.
            this._skyboxBuffer.SetValue(0, data);
            this._skyboxBuffer.UpdateBufferDeferred(commandList);
            this._isDirty = false;
        }
        
        // Set vertex and index buffer.
        commandList.SetVertexBuffer(0, this._vertexBuffer);
        commandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt32);
        
        // Set pipeline.
        commandList.SetPipeline(this._effect.GetPipeline(this._pipelineDescription).Pipeline);
        
        // Set matrix buffer.
        commandList.SetGraphicsResourceSet(this._effect.GetBufferLayoutSlot("MatrixBuffer"), cam3D.GetMatrixBuffer().GetResourceSet(this._effect.GetBufferLayout("MatrixBuffer")));
        
        // Set skybox data buffer.
        commandList.SetGraphicsResourceSet(this._effect.GetBufferLayoutSlot("SkyboxBuffer"), this._skyboxBuffer.GetResourceSet(this._effect.GetBufferLayout("SkyboxBuffer")));
        
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
    
    /// <summary>
    /// Specifies the quality level of the sun rendering in the procedural skybox.
    /// </summary>
    public enum SkyboxSunQuality {
        None = 0,
        Simple = 1,
        HighQuality = 2
    }
    
    /// <summary>
    /// Struct holding configurable Skybox data.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct SkyboxData {
        public Vector4 SkyColor;
        public Vector4 GroundColor;
        public Vector4 SunColor;
        public Vector4 Color;
        public Vector4 SunDirection;
        public float SunSize;
        public float SunSizeConvergence;
        public float AtmosphereThickness;
        public float Exposure;
        public int SunQuality;
        private Vector3 _padding;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._vertexBuffer.Dispose();
            this._indexBuffer.Dispose();
        }
    }
}