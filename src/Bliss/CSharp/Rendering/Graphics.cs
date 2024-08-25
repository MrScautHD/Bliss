using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Geometry;
using Bliss.CSharp.Shaders;
using Bliss.CSharp.Textures;
using Veldrid;

namespace Bliss.CSharp.Rendering;

public class Graphics : Disposable {
    
    public GraphicsDevice GraphicsDevice { get; private set; }
    public CommandList CommandList { get; private set; }
    
    public (Shader, Shader) DefaultShader { get; private set; }
    public ResourceLayout ResourceLayout { get; private set; }
    public ResourceSet ResourceSet { get; private set; }
    public Pipeline DefaultPipeline { get; private set; }
    
    public DeviceBuffer MvpBuffer { get; private set; }
    public DeviceBuffer ColorBuffer { get; private set; }
    
    public DeviceBuffer VertexBuffer { get; private set; }
    public DeviceBuffer IndexBuffer { get; private set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Graphics"/> class with the specified graphics device and command list.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering operations.</param>
    /// <param name="commandList">The command list used to issue rendering commands.</param>
    public Graphics(GraphicsDevice graphicsDevice, CommandList commandList) {
        this.GraphicsDevice = graphicsDevice;
        this.CommandList = commandList;
        
        // Load Default Shader
        this.DefaultShader = ShaderHelper.Load(graphicsDevice.ResourceFactory, "content/shaders/default_shader.vert", "content/shaders/default_shader.frag");
        
        // Create uniform buffers
        this.MvpBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));  // 64 bytes for mat4
        this.ColorBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));  // 16 bytes for vec4
        
        // Load texture
        Texture2D texture = new Texture2D(graphicsDevice, "content/image.png");
        
        // Create resource layout
        this.ResourceLayout = graphicsDevice.ResourceFactory.CreateResourceLayout(
            new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("MVP", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("Color", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("texture0", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)
            )
        );
        
        // Create resource set
        this.ResourceSet = graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(this.ResourceLayout, this.MvpBuffer, this.ColorBuffer, texture.DeviceTexture, graphicsDevice.PointSampler));
        
        // Create Default Pipeline
        this.DefaultPipeline = graphicsDevice.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription() {
            BlendState = BlendStateDescription.SingleOverrideBlend,
            DepthStencilState = new DepthStencilStateDescription() {
                DepthTestEnabled = true,
                DepthWriteEnabled = true,
                DepthComparison = ComparisonKind.LessEqual
            },
            RasterizerState = new RasterizerStateDescription() {
                CullMode = FaceCullMode.Back,
                FillMode = PolygonFillMode.Solid,
                FrontFace = FrontFace.Clockwise,
                DepthClipEnabled = true,
                ScissorTestEnabled = false
            },
            PrimitiveTopology = PrimitiveTopology.TriangleStrip,
            ResourceLayouts = [
                this.ResourceLayout
            ],
            ShaderSet = new ShaderSetDescription() {
                VertexLayouts = [
                    new VertexLayoutDescription(
                    new VertexElementDescription("vertexPosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                    new VertexElementDescription("vertexTexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                    new VertexElementDescription("vertexColor", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4)
                    )
                ],
                Shaders = [
                    this.DefaultShader.Item1,
                    this.DefaultShader.Item2
                ]
            },
            Outputs = graphicsDevice.SwapchainFramebuffer.OutputDescription
        });
        
        // Create vertex and index buffer
        this.VertexBuffer = this.GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint) (4 * Marshal.SizeOf<Vertex>()), BufferUsage.VertexBuffer));
        this.IndexBuffer = this.GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(4 * sizeof(ushort), BufferUsage.IndexBuffer));
    }
    
    /* --------------------------------- Normal --------------------------------- */

    /// <summary>
    /// Begins the drawing operations.
    /// </summary>
    public void BeginDrawing() {
        this.CommandList.Begin();
        this.CommandList.SetFramebuffer(this.GraphicsDevice.SwapchainFramebuffer);
    }

    /// <summary>
    /// Ends the drawing operations.
    /// </summary>
    public void EndDrawing() {
        this.CommandList.End();
        this.GraphicsDevice.SubmitCommands(this.CommandList);
        this.GraphicsDevice.SwapBuffers();
        this.GraphicsDevice.WaitForIdle();
    }

    /// <summary>
    /// Clears the background color and depth stencil of the command list.
    /// </summary>
    /// <param name="index">The index of the color target to clear.</param>
    /// <param name="clearColor">The color used to clear the background.</param>
    public void ClearBackground(uint index, Color clearColor) {
        this.CommandList.ClearColorTarget(index, clearColor.ToRgbaFloat());

        if (this.GraphicsDevice.SwapchainFramebuffer.DepthTarget != null) {
            this.CommandList.ClearDepthStencil(this.GraphicsDevice.IsDepthRangeZeroToOne ? 0.0F : 1.0F, 0);
        }
    }
    
    /* --------------------------------- Texture Drawing --------------------------------- */
    
    public void DrawTexture(Texture2D texture) {
        // Get the screen dimensions
        float screenWidth = this.GraphicsDevice.SwapchainFramebuffer.Width;
        float screenHeight = this.GraphicsDevice.SwapchainFramebuffer.Height;
    
        // Get the texture dimensions
        float textureWidth = texture.Width;
        float textureHeight = texture.Height;
    
        // Calculate normalized device coordinates for the texture's position and size
        Vector2 topLeft = new Vector2(
            -1.0f, // Start at the left of the screen (NDC -1.0)
            1.0f // Start at the top of the screen (NDC 1.0)
        );
    
        Vector2 bottomRight = new Vector2(
            (textureWidth / screenWidth) * 2.0f - 1.0f,  // Scale texture width to NDC
            1.0f - (textureHeight / screenHeight) * 2.0f // Scale texture height to NDC
        );
    
        // Update Vertex Buffer with positions and texture coordinates
        this.GraphicsDevice.UpdateBuffer(this.VertexBuffer, 0, new Vertex[] {
            new Vertex()
            {
                Position = new Vector3(topLeft.X, topLeft.Y, 0.0f),
                TexCoords = new Vector2(0.0f, 0.0f),
                Color = Vector4.One
            },
            new Vertex()
            {
                Position = new Vector3(bottomRight.X, topLeft.Y, 0.0f),
                TexCoords = new Vector2(1.0f, 0.0f),
                Color = Vector4.One
            },
            new Vertex()
            {
                Position = new Vector3(topLeft.X, bottomRight.Y, 0.0f),
                TexCoords = new Vector2(0.0f, 1.0f),
                Color = Vector4.One
            },
            new Vertex()
            {
                Position = new Vector3(bottomRight.X, bottomRight.Y, 0.0f),
                TexCoords = new Vector2(1.0f, 1.0f),
                Color = Vector4.One
            }
        });
    
        // Update Index Buffer
        ushort[] quadIndices = [0, 1, 2, 3];
        this.GraphicsDevice.UpdateBuffer(this.IndexBuffer, 0, quadIndices);
    
        // Update MVP Buffer (Identity matrix in this case)
        Matrix4x4 mvp = Matrix4x4.Identity; 
        this.GraphicsDevice.UpdateBuffer(this.MvpBuffer, 0, ref mvp);
    
        // Setup pipeline and resources
        this.CommandList.SetVertexBuffer(0, this.VertexBuffer);
        this.CommandList.SetIndexBuffer(this.IndexBuffer, IndexFormat.UInt16);
        this.CommandList.SetPipeline(this.DefaultPipeline);
        this.CommandList.SetGraphicsResourceSet(0, this.ResourceSet);
        
        // Draw the textured quad
        this.CommandList.DrawIndexed(4, 1, 0, 0, 0);
    }
    
    /* --------------------------------- Text Drawing --------------------------------- */

    public void DrawText(string text) {
    }
    
    /* --------------------------------- Shape Drawing --------------------------------- */

    public void DrawRectangle(Rectangle rectangle, Color color) {
        
        // Update Vertex Buffer
        this.GraphicsDevice.UpdateBuffer(this.VertexBuffer, 0, new Vertex[] {
            new Vertex() {
                Position = new Vector3(-0.5F, 0.5F, 0.0F),
                TexCoords = new Vector2(0.0F, 0.0F),
                Color = color.ToRgbaFloat().ToVector4()
            },
            new Vertex() {
                Position = new Vector3(0.5F, 0.5F, 0.0F),
                TexCoords = new Vector2(1.0F, 0.0F),
                Color = color.ToRgbaFloat().ToVector4()
            },
            new Vertex() {
                Position = new Vector3(-0.5F, -0.5F, 0.0F),
                TexCoords = new Vector2(0.0F, 1.0F),
                Color = color.ToRgbaFloat().ToVector4()
            },
            new Vertex() {
                Position = new Vector3(0.5F, -0.5F, 0.0F),
                TexCoords = new Vector2(1.0F, 1.0F),
                Color = color.ToRgbaFloat().ToVector4()
            }
        });
        
        // Update Index Buffer
        ushort[] quadIndices = [0, 1, 2, 3];
        this.GraphicsDevice.UpdateBuffer(this.IndexBuffer, 0, quadIndices);
        
        // Update Uniform Buffers
        Matrix4x4 mvp = Matrix4x4.Identity; // Normally you'd calculate a proper MVP matrix here
        this.GraphicsDevice.UpdateBuffer(this.MvpBuffer, 0, ref mvp);

        Vector4 finalColor = color.ToVector4();
        this.GraphicsDevice.UpdateBuffer(this.ColorBuffer, 0, ref finalColor);
        
        // Setup pipeline and resources
        this.CommandList.SetVertexBuffer(0, this.VertexBuffer);
        this.CommandList.SetIndexBuffer(this.IndexBuffer, IndexFormat.UInt16);
        this.CommandList.SetPipeline(this.DefaultPipeline);
        this.CommandList.SetGraphicsResourceSet(0, this.ResourceSet);
        
        // Draw rectangle
        this.CommandList.DrawIndexed(4, 1, 0, 0, 0);
    }
    
    /* --------------------------------- Model Drawing --------------------------------- */

    public void DrawModel(Model model) {
        
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.DefaultPipeline.Dispose();
            this.DefaultShader.Item1.Dispose();
            this.DefaultShader.Item2.Dispose();
            this.VertexBuffer.Dispose();
            this.IndexBuffer.Dispose();
        }
    }
}