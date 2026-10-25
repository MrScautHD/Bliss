using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Images;
using Bliss.CSharp.Textures.Cubemaps;
using Veldrith;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Sky.Environment;

public class ProceduralEnvironment : Disposable, IEnvironmentProvider {
    
    /// <summary>
    /// Gets the graphics device used to render the procedural environment.
    /// </summary>
    public GraphicsDevice GraphicsDevice => this.Skybox.GraphicsDevice;
    
    /// <summary>
    /// Gets the procedural skybox used to generate the environment.
    /// </summary>
    public ProceduralSkybox Skybox { get; private set; }
    
    /// <summary>
    /// Gets the cubemap generated from the procedural skybox.
    /// </summary>
    public Cubemap SourceEnvironmentCubemap { get; private set; }
    
    /// <summary>
    /// Gets the environment color, with the skybox exposure applied to its alpha component.
    /// </summary>
    public Vector4 EnvironmentColor => new Vector4(1.0F, 1.0F, 1.0F, this.Skybox.Exposure);
    
    /// <summary>
    /// The effect used to render the procedural skybox.
    /// </summary>
    private Effect _effect;
    
    /// <summary>
    /// The color textures used to render the six cubemap faces.
    /// </summary>
    private Texture[]? _colorTextures;
    
    /// <summary>
    /// The depth textures used for depth testing while rendering the cubemap faces.
    /// </summary>
    private Texture[]? _depthTextures;
    
    /// <summary>
    /// The framebuffers used to render each cubemap face.
    /// </summary>
    private Framebuffer[]? _framebuffers;
    
    /// <summary>
    /// The vertex buffer containing the cubemap geometry.
    /// </summary>
    private DeviceBuffer _vertexBuffer;
    
    /// <summary>
    /// The index buffer containing the cubemap geometry indices.
    /// </summary>
    private DeviceBuffer _indexBuffer;
    
    /// <summary>
    /// The uniform buffer containing the projection and view matrices used during cubemap rendering.
    /// </summary>
    private SimpleUniformBuffer<Matrix4x4> _matrixBuffer;
    
    /// <summary>
    /// The uniform buffer containing the procedural skybox rendering parameters.
    /// </summary>
    private SimpleUniformBuffer<SkyboxData> _skyboxBuffer;
    
    /// <summary>
    /// The pipeline description used to configure the procedural skybox rendering pipeline.
    /// </summary>
    private SimplePipelineDescription _pipelineDescription;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ProceduralEnvironment"/> class.
    /// </summary>
    /// <param name="skybox">The procedural skybox used to generate the environment.</param>
    /// <param name="size">The width and height of each cubemap face in pixels.</param>
    public ProceduralEnvironment(ProceduralSkybox skybox, int size = 256) {
        this.Skybox = skybox;
        this._effect = GlobalResource.ProceduralSkyboxEffect;
        
        // Create vertex buffer.
        uint vertexBufferSize = (uint) Marshal.SizeOf<CubemapVertex3D>() * 8;
        this._vertexBuffer = this.GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        this._vertexBuffer.Name = "VertexBuffer";
        this.GraphicsDevice.UpdateBuffer(this._vertexBuffer, 0, this.GenVertices());
        
        // Create index buffer.
        uint indexBufferSize = sizeof(uint) * 36;
        this._indexBuffer = this.GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(indexBufferSize, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        this._indexBuffer.Name = "IndexBuffer";
        this.GraphicsDevice.UpdateBuffer(this._indexBuffer, 0, this.GenIndices());
        
        // Create matrix buffer.
        this._matrixBuffer = new SimpleUniformBuffer<Matrix4x4>(this.GraphicsDevice, 2, ShaderStages.Vertex | ShaderStages.Fragment);
        this._matrixBuffer.DeviceBuffer.Name = "MatrixBuffer";
        
        // Create skybox data buffer.
        this._skyboxBuffer = new SimpleUniformBuffer<SkyboxData>(this.GraphicsDevice, 1, ShaderStages.Fragment);
        this._skyboxBuffer.DeviceBuffer.Name = "SkyboxBuffer";
        
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
        
        // Bake environment.
        this.BakeEnvironment((uint) size);
    }
    
    /// <summary>
    /// Renders the procedural skybox into a cubemap and updates the environment texture.
    /// </summary>
    /// <param name="size">The width and height of each cubemap face in pixels.</param>
    public void BakeEnvironment(uint size = 256) {
        GraphicsDevice graphicsDevice = this.Skybox.GraphicsDevice;
        using CommandList commandList = graphicsDevice.ResourceFactory.CreateCommandList();
        
        commandList.Begin();
        this.BakeEnvironment(commandList, size);
        commandList.End();
        
        graphicsDevice.SubmitCommands(commandList);
        graphicsDevice.WaitForIdle();
        
        // Create final cubemap.
        Image cubemapImage = this.CreateCubemapImageFromFramebuffers(size);
        
        this.SourceEnvironmentCubemap?.Dispose();
        this.SourceEnvironmentCubemap = new Cubemap(this.GraphicsDevice, cubemapImage, CubemapLayout.CrossFourByThree, false);
        
        this.ClearResources();
    }
    
    /// <summary>
    /// Renders the skybox into six framebuffers to bake the environment into a cubemap layout.
    /// </summary>
    /// <param name="commandList">The command list used to record the rendering commands.</param>
    /// <param name="size">The width and height of each cubemap face in pixels.</param>
    private void BakeEnvironment(CommandList commandList, uint size = 256) {
        this.ClearResources();
        this.CreateFramebuffers(size);
        
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2.0F, 1.0F, 0.1F, 10.0F);
        Matrix4x4[] views = this.GetCubemapViews();
        
        // Create skybox data.
        SkyboxData data = new SkyboxData() {
            SkyColor = this.Skybox.SkyColor.ToRgbaFloatVec4(),
            GroundColor = this.Skybox.GroundColor.ToRgbaFloatVec4(),
            SunColor = this.Skybox.SunColor.ToRgbaFloatVec4(),
            Color = this.Skybox.Color.ToRgbaFloatVec4(),
            SunDirection = new Vector4(this.Skybox.SunDirection.LengthSquared() > 0.0F ? Vector3.Normalize(this.Skybox.SunDirection) : Vector3.UnitY, 0.0F),
            SunSize = this.Skybox.SunSize,
            SunSizeConvergence = this.Skybox.SunSizeConvergence,
            AtmosphereThickness = this.Skybox.AtmosphereThickness,
            Exposure = this.Skybox.Exposure,
            SunQuality = (int) this.Skybox.SunQuality
        };
        
        // Upload buffer to GPU.
        this._skyboxBuffer.SetValue(0, data);
        this._skyboxBuffer.UpdateBufferDeferred(commandList);
        
        // Set vertex and index buffer.
        commandList.SetVertexBuffer(0, this._vertexBuffer);
        commandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt32);
        
        for (int face = 0; face < 6; face++) {
            
            // Upload the capture matrices.
            this._matrixBuffer.SetValue(0, projection);
            this._matrixBuffer.SetValue(1, views[face]);
            this._matrixBuffer.UpdateBufferDeferred(commandList);
            
            // Set framebuffer.
            commandList.SetFramebuffer(this._framebuffers![face]);
            commandList.ClearColorTarget(0, RgbaFloat.BLACK);
            commandList.ClearDepthStencil(1.0F);
            
            // Update pipeline description.
            this._pipelineDescription.Outputs = this._framebuffers[face].OutputDescription;
            
            // Set pipeline.
            commandList.SetPipeline(this._effect.GetPipeline(this._pipelineDescription).Pipeline);
            
            // Set matrix buffer.
            commandList.SetGraphicsResourceSet(this._effect.GetBufferLayoutSlot("MatrixBuffer"), this._matrixBuffer.GetResourceSet(this._effect.GetBufferLayout("MatrixBuffer")));
            
            // Set skybox data buffer.
            commandList.SetGraphicsResourceSet(this._effect.GetBufferLayoutSlot("SkyboxBuffer"), this._skyboxBuffer.GetResourceSet(this._effect.GetBufferLayout("SkyboxBuffer")));
            
            // Apply effect.
            this._effect.Apply(commandList);
            
            // Draw.
            commandList.DrawIndexed(36);
        }
    }
    
    /// <summary>
    /// Combines the six rendered cubemap faces into a single cross-layout image.
    /// </summary>
    /// <param name="size">The width and height of each cubemap face in pixels.</param>
    /// <returns>An image containing all six cubemap faces arranged in a 4-by-3 cross layout.</returns>
    private Image CreateCubemapImageFromFramebuffers(uint size) {
        Image image = new Image((int) size * 4, (int) size * 3, Color.Black);
        
        // Define the position and vertical orientation of each cubemap face.
        (CubemapLayer Face, int CellX, int CellY, bool FlipVertical)[] layout = [
            (CubemapLayer.PositiveY, 1, 0, false),
            (CubemapLayer.NegativeX, 0, 1, true),
            (CubemapLayer.PositiveZ, 1, 1, true),
            (CubemapLayer.PositiveX, 2, 1, true),
            (CubemapLayer.NegativeZ, 3, 1, true),
            (CubemapLayer.NegativeY, 1, 2, false)
        ];
        
        foreach ((CubemapLayer face, int cellX, int cellY, bool flipVertical) in layout) {
            Image faceImage = this.TextureToImage(this._colorTextures![(int) face], size, size);
            
            // Correct the vertical orientation where required by the cross layout.
            if (flipVertical) {
                faceImage.FlipVertical();
            }
            
            // Copy the face into its designated cell in the destination image.
            this.CopyFaceToImage(faceImage, image, size, cellX, cellY);
        }
        
        return image;
    }
    
    /// <summary>
    /// Copies a cubemap face into a specified cell of the destination image.
    /// </summary>
    /// <param name="source">The source image containing the cubemap face.</param>
    /// <param name="destination">The destination image containing the cubemap cross layout.</param>
    /// <param name="faceSize">The width and height of the source face in pixels.</param>
    /// <param name="cellX">The horizontal cell index in the destination image.</param>
    /// <param name="cellY">The vertical cell index in the destination image.</param>
    private void CopyFaceToImage(Image source, Image destination, uint faceSize, int cellX, int cellY) {
        int size = (int) faceSize;
        int rowBytes = size * 4;
        int dstX = cellX * size;
        int dstY = cellY * size;
        
        // Copy each row into the corresponding position in the destination image.
        for (int y = 0; y < size; y++) {
            Array.Copy(source.Data, y * rowBytes, destination.Data, ((dstY + y) * destination.Width + dstX) * 4, rowBytes);
        }
    }
    
    /// <summary>
    /// Copies a GPU texture into a CPU-accessible image.
    /// </summary>
    /// <param name="texture">The source texture to read back from the GPU.</param>
    /// <param name="width">The width of the texture in pixels.</param>
    /// <param name="height">The height of the texture in pixels.</param>
    /// <returns>An image containing the texture's RGBA pixel data.</returns>
    private Image TextureToImage(Texture texture, uint width, uint height) {
        using Texture stagingTexture = this.GraphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(width, height, 1, 1, PixelFormat.R8G8B8A8UNorm, TextureUsage.Staging));
        using CommandList commandList = this.GraphicsDevice.ResourceFactory.CreateCommandList();
        
        // Copy the GPU texture into the staging texture.
        commandList.Begin();
        commandList.CopyTexture(texture, stagingTexture);
        commandList.End();
        
        this.GraphicsDevice.SubmitCommands(commandList);
        this.GraphicsDevice.WaitForIdle();
        
        // Allocate tightly packed RGBA pixel storage for the resulting image.
        int rowBytes = (int) width * 4;
        byte[] data = new byte[rowBytes * height];
        
        MappedResourceView<byte> mapped = this.GraphicsDevice.Map<byte>(stagingTexture, MapMode.Read);
        
        // Copy each row while accounting for the GPU texture's row pitch.
        try {
            for (uint row = 0; row < height; row++) {
                Marshal.Copy(IntPtr.Add(mapped.MappedResource.Data, (int) (row * mapped.MappedResource.RowPitch)), data, (int) row * rowBytes, rowBytes);
            }
        }
        finally {
            this.GraphicsDevice.Unmap(stagingTexture);
        }
        
        return new Image((int) width, (int) height, data);
    }
    
    /// <summary>
    /// Creates the six color textures, depth textures and framebuffers that the sky is rendered into when the environment is baked.
    /// </summary>
    /// <param name="size">The width and height of one cube face in pixels.</param>
    private void CreateFramebuffers(uint size) {
        this._colorTextures = new Texture[6];
        this._depthTextures = new Texture[6];
        this._framebuffers = new Framebuffer[6];
        
        for (uint face = 0; face < 6; face++) {
            Texture colorTexture = this.GraphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(size, size, 1, 1, PixelFormat.R8G8B8A8UNorm, TextureUsage.RenderTarget | TextureUsage.Sampled));
            Texture depthTexture = this.GraphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(size, size, 1, 1, PixelFormat.D32FloatS8UInt, TextureUsage.DepthStencil));
            
            this._colorTextures[face] = colorTexture;
            this._depthTextures[face] = depthTexture;
            
            this._framebuffers[face] = this.GraphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription(new FramebufferAttachmentDescription(depthTexture, 0), [
                    new FramebufferAttachmentDescription(colorTexture, 0)
                ]
            ));
        }
    }
    
    /// <summary>
    /// Disposes the framebuffers, color textures and depth textures of the bake and resets them to <see langword="null"/>.
    /// </summary>
    private void ClearResources() {
        if (this._framebuffers != null) {
            foreach (Framebuffer framebuffer in this._framebuffers) {
                framebuffer.Dispose();
            }
            
            this._framebuffers = null;
        }
        
        if (this._colorTextures != null) {
            foreach (Texture colorTexture in this._colorTextures) {
                colorTexture.Dispose();
            }
            
            this._colorTextures = null;
        }
        
        if (this._depthTextures != null) {
            foreach (Texture depthTexture in this._depthTextures) {
                depthTexture.Dispose();
            }
            
            this._depthTextures = null;
        }
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
    /// Creates the six view matrices that look from the origin towards each cubemap face.
    /// </summary>
    /// <returns>The view matrices in cubemap face order (+X, -X, +Y, -Y, +Z, -Z), with up vectors chosen to match the face orientation.</returns>
    private Matrix4x4[] GetCubemapViews() {
        return [
            Matrix4x4.CreateLookAt(Vector3.Zero, Vector3.UnitX, -Vector3.UnitY),
            Matrix4x4.CreateLookAt(Vector3.Zero, -Vector3.UnitX, -Vector3.UnitY),
            Matrix4x4.CreateLookAt(Vector3.Zero, Vector3.UnitY, Vector3.UnitZ),
            Matrix4x4.CreateLookAt(Vector3.Zero, -Vector3.UnitY, -Vector3.UnitZ),
            Matrix4x4.CreateLookAt(Vector3.Zero, Vector3.UnitZ, -Vector3.UnitY),
            Matrix4x4.CreateLookAt(Vector3.Zero, -Vector3.UnitZ, -Vector3.UnitY)
        ];
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
            this.ClearResources();
            this.SourceEnvironmentCubemap.Dispose();
            this._vertexBuffer.Dispose();
            this._indexBuffer.Dispose();
            this._matrixBuffer.Dispose();
            this._skyboxBuffer.Dispose();
        }
    }
}