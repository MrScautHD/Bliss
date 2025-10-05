using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Geometry;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
using Veldrid;

namespace Bliss.CSharp.Graphics.Rendering.Renderers;

public class ImmediateRenderer : Disposable {
    
    /// <summary>
    /// Gets the graphics device used for rendering.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// Gets the maximum number of vertices that can be drawn.
    /// </summary>
    public uint Capacity { get; private set; }
    
    /// <summary>
    /// The array of vertices used for batching immediate mode geometry.
    /// </summary>
    private ImmediateVertex3D[] _vertices;
    
    /// <summary>
    /// The array of indices used for batching immediate mode geometry.
    /// </summary>
    private uint[] _indices;

    /// <summary>
    /// A temporary storage of vertices used for rendering operations.
    /// </summary>
    private List<ImmediateVertex3D> _tempVertices;

    /// <summary>
    /// A Temporary storage of indices used for rendering operations.
    /// </summary>
    private List<uint> _tempIndices;
    
    /// <summary>
    /// The current count of batched vertices.
    /// </summary>
    private int _vertexCount;
    
    /// <summary>
    /// The current count of batched indices.
    /// </summary>
    private int _indexCount;

    /// <summary>
    /// The GPU buffer that stores vertex data.
    /// </summary>
    private DeviceBuffer _vertexBuffer;
    
    /// <summary>
    /// The GPU buffer that stores index data.
    /// </summary>
    private DeviceBuffer _indexBuffer;

    /// <summary>
    /// The uniform buffer that holds transformation matrices (projection, view, and transform).
    /// </summary>
    private SimpleUniformBuffer<Matrix4x4> _matrixBuffer;

    /// <summary>
    /// The pipeline description used to configure the graphics pipeline for rendering.
    /// </summary>
    private SimplePipelineDescription _pipelineDescription;

    /// <summary>
    /// The currently bound effect.
    /// </summary>
    private Effect _currentEffect;

    /// <summary>
    /// The currently bound blendState.
    /// </summary>
    private BlendStateDescription _currentBlendState;

    /// <summary>
    /// The currently bound depthStencilState.
    /// </summary>
    private DepthStencilStateDescription _currentDepthStencilState;

    /// <summary>
    /// The currently bound rasterizerState.
    /// </summary>
    private RasterizerStateDescription _currentRasterizerState;
    
    /// <summary>
    /// The currently bound texture.
    /// </summary>
    private Texture2D _currentTexture;
    
    /// <summary>
    /// The currently active sampler.
    /// </summary>
    private Sampler _currentSampler;

    /// <summary>
    /// The currently active source rectangle.
    /// </summary>
    private Rectangle _currentSourceRec;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ImmediateRenderer"/> class with the specified graphics device, output, effect, and capacity.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering.</param>
    /// <param name="capacity">The maximum number of vertices. Defaults to 10.240.</param>
    public ImmediateRenderer(GraphicsDevice graphicsDevice, uint capacity = 10240) {
        this.GraphicsDevice = graphicsDevice;
        this.Capacity = capacity;
        
        // Create vertex buffer.
        uint vertexBufferSize = capacity * (uint) Marshal.SizeOf<ImmediateVertex3D>();
        this._vertices = new ImmediateVertex3D[capacity];
        this._tempVertices = new List<ImmediateVertex3D>();
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        
        // Create index buffer.
        uint indexBufferSize = capacity * 3 * sizeof(uint);
        this._indices = new uint[capacity * 3];
        this._tempIndices = new List<uint>();
        this._indexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(indexBufferSize, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        
        // Create matrix buffer.
        this._matrixBuffer = new SimpleUniformBuffer<Matrix4x4>(graphicsDevice, 3, ShaderStages.Vertex);
        
        // Create pipeline description.
        this._pipelineDescription = new SimplePipelineDescription();
        
        // Set default settings.
        this.ResetSettings();
    }

    /// <summary>
    /// Retrieves the current effect being used by the renderer.
    /// </summary>
    /// <returns>The currently set <see cref="Effect"/> instance.</returns>
    public Effect GetCurrentEffect() {
        return this._currentEffect;
    }

    /// <summary>
    /// Sets the current rendering effect for the ImmediateRenderer.
    /// If the provided effect is null, it defaults to the global default immediate renderer effect.
    /// </summary>
    /// <param name="effect">The effect to be used. If null, the default ImmediateRenderer effect will be used.</param>
    public void SetEffect(Effect? effect) {
        this._currentEffect = effect ?? GlobalResource.DefaultImmediateRendererEffect;
        
        // Update pipeline description.
        this._pipelineDescription.BufferLayouts = this._currentEffect.GetBufferLayouts();
        this._pipelineDescription.TextureLayouts = this._currentEffect.GetTextureLayouts();
        this._pipelineDescription.ShaderSet = this._currentEffect.ShaderSet;
    }

    /// <summary>
    /// Retrieves the current blend state used by the renderer.
    /// </summary>
    /// <returns>The current <see cref="BlendStateDescription"/> instance.</returns>
    public BlendStateDescription GetBlendState() {
        return this._currentBlendState;
    }

    /// <summary>
    /// Sets the current blend state for the renderer. If no blend state is provided, it defaults to <see cref="BlendStateDescription.SINGLE_ALPHA_BLEND"/>.
    /// </summary>
    /// <param name="blendState">The blend state to apply. Defaults to <see cref="BlendStateDescription.SINGLE_ALPHA_BLEND"/> if null.</param>
    public void SetBlendState(BlendStateDescription? blendState) {
        this._currentBlendState = blendState ?? BlendStateDescription.SINGLE_DISABLED;

        // Update pipeline description.
        this._pipelineDescription.BlendState = this._currentBlendState;
    }

    /// <summary>
    /// Retrieves the currently bound depth stencil state description.
    /// </summary>
    /// <returns>The current <see cref="DepthStencilStateDescription"/> instance being used.</returns>
    public DepthStencilStateDescription GetCurrentDepthStencilState() {
        return this._currentDepthStencilState;
    }

    /// <summary>
    /// Sets the depth stencil state for the renderer. If no state is provided, a default state of depth-only less/equal is used.
    /// </summary>
    /// <param name="depthStencilState">The depth stencil state to set. If null, defaults to DepthStencilStateDescription.DEPTH_ONLY_LESS_EQUAL.</param>
    public void SetDepthStencilState(DepthStencilStateDescription? depthStencilState) {
        this._currentDepthStencilState = depthStencilState ?? DepthStencilStateDescription.DEPTH_ONLY_LESS_EQUAL;
        
        // Update pipeline description.
        this._pipelineDescription.DepthStencilState = this._currentDepthStencilState;
    }

    /// <summary>
    /// Retrieves the current rasterizer state description used by the renderer.
    /// </summary>
    /// <returns>The currently bound rasterizer state.</returns>
    public RasterizerStateDescription GetCurrentRasterizerState() {
        return this._currentRasterizerState;
    }

    /// <summary>
    /// Updates the current rasterizer state for the renderer. If no rasterizer state is provided, the default state is used.
    /// </summary>
    /// <param name="rasterizerState">The rasterizer state to apply to the pipeline. If null, the default rasterizer state is used.</param>
    public void SetRasterizerState(RasterizerStateDescription? rasterizerState) {
        this._currentRasterizerState = rasterizerState ?? RasterizerStateDescription.DEFAULT;
        
        // Update pipeline description.
        this._pipelineDescription.RasterizerState = this._currentRasterizerState;
    }

    /// <summary>
    /// Retrieves the currently active texture being used by the renderer.
    /// </summary>
    /// <returns>The active <see cref="Texture2D"/> object currently bound to the renderer.</returns>
    public Texture2D GetCurrentTexture() {
        return this._currentTexture;
    }

    /// <summary>
    /// Retrieves the currently active sampler used by the renderer.
    /// </summary>
    /// <returns>The active <see cref="Sampler"/> instance.</returns>
    public Sampler GetCurrentSampler() {
        return this._currentSampler;
    }

    /// <summary>
    /// Retrieves the current source rectangle used for rendering operations.
    /// </summary>
    /// <returns>The current <see cref="Rectangle"/> source used for rendering.</returns>
    public Rectangle GetCurrentSourceRec() {
        return this._currentSourceRec;
    }
    
    /// <summary>
    /// Sets the current texture, sampler, and optional source rectangle for rendering.
    /// </summary>
    /// <param name="texture">The texture to be used for rendering. If null, a default texture is used.</param>
    /// <param name="sampler">The sampler to be applied to the texture. Defaults to a point sampler if null.</param>
    /// <param name="sourceRect">The source rectangle specifying a portion of the texture to be used. If null, the entire texture is used.</param>
    public void SetTexture(Texture2D? texture, Sampler? sampler = null, Rectangle? sourceRect = null) {
        this._currentTexture = texture ?? GlobalResource.DefaultImmediateRendererTexture;
        this._currentSampler = sampler ?? GraphicsHelper.GetSampler(this.GraphicsDevice, SamplerType.PointWrap);
        this._currentSourceRec = sourceRect ?? new Rectangle(0, 0, (int) this._currentTexture.Width, (int) this._currentTexture.Height);
    }

    /// <summary>
    /// Resets the <see cref="ImmediateRenderer"/> to default settings.
    /// </summary>
    public void ResetSettings() {
        this.SetEffect(null);
        this.SetBlendState(null);
        this.SetDepthStencilState(null);
        this.SetRasterizerState(null);
        this.SetTexture(null);
    }

    /// <summary>
    /// Draws a cube with the specified command list, output description, transformation, size, and optional color.
    /// </summary>
    /// <param name="commandList">The command list used for submitting rendering commands.</param>
    /// <param name="output">The output description specifying the render target details.</param>
    /// <param name="transform">The transformation to be applied to the cube.</param>
    /// <param name="size">The size of the cube in 3D space.</param>
    /// <param name="color">An optional color for the cube; if null, white is used.</param>
    public void DrawCube(CommandList commandList, OutputDescription output, Transform transform, Vector3 size, Color? color = null) {
        Color finalColor = color ?? Color.White;
        Texture2D texture = this._currentTexture;
        Rectangle sourceRec = this._currentSourceRec;

        // Calculate source rectangle UVs.
        float uLeft = sourceRec.X / (float) texture.Width;
        float uRight = (sourceRec.X + sourceRec.Width) / (float) texture.Width;
        float vTop = sourceRec.Y / (float) texture.Height;
        float vBottom = (sourceRec.Y + sourceRec.Height) / (float) texture.Height;
        
        for (int face = 0; face < 6; face++) {
            
            // Define the normal for each face.
            Vector3 faceNormal = face switch {
                // Front.
                0 => new Vector3(0.0F, 0.0F, -1.0F),
                // Back.
                1 => new Vector3(0.0F, 0.0F, 1.0F),
                // Left.
                2 => new Vector3(-1.0F, 0.0F, 0.0F),
                // Right.
                3 => new Vector3(1.0F, 0.0F, 0.0F),
                // Top.
                4 => new Vector3(0.0F, 1.0F, 0.0F),
                // Bottom.
                5 => new Vector3(0.0F, -1.0F, 0.0F),
                _ => Vector3.Zero
            };

            // Define the tangent for each face.
            Vector3 tangent = face switch {
                // Front.
                0 => new Vector3(1.0F, 0.0F, 0.0F),
                // Back.
                1 => new Vector3(-1.0F, 0.0F, 0.0F),
                // Left.
                2 => new Vector3(0.0F, 0.0F, -1.0F),
                // Right.
                3 => new Vector3(0.0F, 0.0F, 1.0F),
                // Top.
                4 => new Vector3(1.0F, 0.0F, 0.0F),
                // Bottom.
                5 => new Vector3(1.0F, 0.0F, 0.0F),
                _ => Vector3.Zero
            };

            // Compute the bitangent as the cross product of the normal and tangent.
            Vector3 bitangent = Vector3.Cross(faceNormal, tangent);

            // Generate the 4 corners for the current face.
            for (int corner = 0; corner < 4; corner++) {
                
                // Calculate the position of each corner.
                Vector3 position = faceNormal
                    + (corner == 0 || corner == 3 ? -tangent : tangent)
                    + (corner == 0 || corner == 1 ? -bitangent : bitangent);

                // Assign texture coordinates for the current corner.
                Vector2 texCoord = corner switch {
                    0 => new Vector2(uLeft, vTop),
                    1 => new Vector2(uRight, vTop),
                    2 => new Vector2(uRight, vBottom),
                    3 => new Vector2(uLeft, vBottom),
                    _ => Vector2.Zero
                };
                
                // Add the generated vertex.
                this._tempVertices.Add(new ImmediateVertex3D {
                    Position = position * new Vector3(size.X / 2.0F, size.Y / 2.0F, size.Z / 2.0F),
                    TexCoords = texCoord,
                    Color = finalColor.ToRgbaFloatVec4()
                });
            }

            // Add the indices for the two triangles that make up the current face.
            int vertexOffset = face * 4;
            this._tempIndices.Add((uint) (vertexOffset + 0));
            this._tempIndices.Add((uint) (vertexOffset + 3));
            this._tempIndices.Add((uint) (vertexOffset + 2));
            this._tempIndices.Add((uint) (vertexOffset + 2));
            this._tempIndices.Add((uint) (vertexOffset + 1));
            this._tempIndices.Add((uint) (vertexOffset + 0));
        }

        this.DrawVertices(commandList, output, transform, this._tempVertices, this._tempIndices);
    }

    /// <summary>
    /// Draws the wireframe of a cube with the specified transform, size, and optional color.
    /// </summary>
    /// <param name="commandList">The command list used to issue rendering commands.</param>
    /// <param name="output">The output description that specifies the target render output.</param>
    /// <param name="transform">The transformation to apply to the cube, including position, rotation, and scale.</param>
    /// <param name="size">The dimensions of the cube to be drawn.</param>
    /// <param name="color">An optional color for the wireframe. If not provided, the default color is white.</param>
    public void DrawCubeWires(CommandList commandList, OutputDescription output, Transform transform, Vector3 size, Color? color = null) {
        Color finalColor = color ?? Color.White;

        for (int i = 0; i < 8; i++) {
            
            // Calculate the x, y, z coordinates.
            float x = (i & 1) == 0 ? -1.0F : 1.0F;
            float y = (i & 2) == 0 ? -1.0F : 1.0F;
            float z = (i & 4) == 0 ? -1.0F : 1.0F;
            
            // Add the vertex to the list.
            this._tempVertices.Add(new ImmediateVertex3D {
                Position = new Vector3(x, y, z) * new Vector3(size.X / 2.0F, size.Y / 2.0F, size.Z / 2.0F),
                Color = finalColor.ToRgbaFloatVec4()
            });
            
            // Connect the vertex to its neighbors.
            for (int bit = 0; bit < 3; bit++) {
                if ((i & (1 << bit)) == 0) {
                    int neighbor = i | (1 << bit);
                    this._tempIndices.Add((uint) i);
                    this._tempIndices.Add((uint) neighbor);
                }
            }
        }

        this.DrawVertices(commandList, output, transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }

    /// <summary>
    /// Draws a sphere with the specified transformation, radius, number of rings, slices, and optional color.
    /// </summary>
    /// <param name="commandList">The command list used for issuing rendering commands.</param>
    /// <param name="output">The output description determining the render target.</param>
    /// <param name="transform">The transformation to be applied to the sphere.</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <param name="rings">The number of horizontal subdivisions of the sphere.</param>
    /// <param name="slices">The number of vertical subdivisions of the sphere.</param>
    /// <param name="color">An optional color for the sphere; defaults to white if not provided.</param>
    public void DrawSphere(CommandList commandList, OutputDescription output, Transform transform, float radius, int rings, int slices, Color? color = null) {
        Color finalColor = color ?? Color.White;
        Texture2D texture = this._currentTexture;
        Rectangle sourceRec = this._currentSourceRec;
        
        if (rings < 3) {
            rings = 3;
        }
        
        if (slices < 3) {
            slices = 3;
        }
        
        // Calculate source rectangle UVs.
        float uLeft = sourceRec.X / (float) texture.Width;
        float uRight = (sourceRec.X + sourceRec.Width) / (float) texture.Width;
        float vTop = sourceRec.Y / (float) texture.Height;
        float vBottom = (sourceRec.Y + sourceRec.Height) / (float) texture.Height;
        
        // Generate vertices.
        for (int ring = 0; ring <= rings; ring++) {
            float ringAngle = MathF.PI * ring / rings;
            
            for (int slice = 0; slice <= slices; slice++) {
                float sliceAngle = MathF.PI * 2 * slice / slices;
                
                float x = MathF.Sin(ringAngle) * MathF.Cos(sliceAngle);
                float y = MathF.Cos(ringAngle);
                float z = MathF.Sin(ringAngle) * MathF.Sin(sliceAngle);
                
                this._tempVertices.Add(new ImmediateVertex3D() {
                    Position = new Vector3(x, y, z) * (radius / 2),
                    TexCoords = new Vector2(
                        uLeft + (uRight - uLeft) * (slice / (float) slices),
                        vTop + (vBottom - vTop) * (ring / (float) rings)
                    ),
                    Color = finalColor.ToRgbaFloatVec4()
                });
            }
        }
        
        // Generate indices.
        for (int ring = 0; ring < rings; ring++) {
            for (int slice = 0; slice < slices; slice++) {
                int current = ring * (slices + 1) + slice;
                int next = current + slices + 1;
                
                // Two triangles per quad.
                this._tempIndices.Add((uint) current);
                this._tempIndices.Add((uint) (next + 1));
                this._tempIndices.Add((uint) (current + 1));
                
                this._tempIndices.Add((uint) current);
                this._tempIndices.Add((uint) next);
                this._tempIndices.Add((uint) (next + 1));
            }
        }
        
        this.DrawVertices(commandList, output, transform, this._tempVertices, this._tempIndices);
    }
    
    /// <summary>
    /// Draws the wireframe of a sphere with the specified transform, radius, number of rings, slices, and optional color.
    /// </summary>
    /// <param name="commandList">The command list used for issuing draw commands to the GPU.</param>
    /// <param name="output">The output description of the render target where the sphere will be drawn.</param>
    /// <param name="transform">The transformation to apply to the sphere, including position, rotation, and scale.</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <param name="rings">The number of horizontal subdivisions (rings) of the sphere.</param>
    /// <param name="slices">The number of vertical subdivisions (slices) of the sphere.</param>
    /// <param name="color">The optional color for the sphere's wireframe. Defaults to white if not specified.</param>
    public void DrawSphereWires(CommandList commandList, OutputDescription output, Transform transform, float radius, int rings, int slices, Color? color = null) {
        Color finalColor = color ?? Color.White;
        
        if (rings < 3) {
            rings = 3;
        }
        
        if (slices < 3) {
            slices = 3;
        }
        
        // Generate wireframe vertices.
        for (int ring = 0; ring <= rings; ring++) {
            float ringAngle = MathF.PI * ring / rings;
            float y = MathF.Cos(ringAngle) * (radius / 2);
            float ringRadius = MathF.Sin(ringAngle) * (radius / 2);
            
            for (int slice = 0; slice <= slices; slice++) {
                float sliceAngle = MathF.PI * 2 * slice / slices;
                
                float x = MathF.Cos(sliceAngle) * ringRadius;
                float z = MathF.Sin(sliceAngle) * ringRadius;
                
                this._tempVertices.Add(new ImmediateVertex3D {
                    Position = new Vector3(x, y, z),
                    Color = finalColor.ToRgbaFloatVec4()
                });
            }
        }
        
        // Generate wireframe indices.
        for (int ring = 0; ring < rings; ring++) {
            for (int slice = 0; slice < slices; slice++) {
                int current = ring * (slices + 1) + slice;
                int next = current + slices + 1;
                
                // Connect horizontal lines.
                this._tempIndices.Add((uint) current);
                this._tempIndices.Add((uint) (current + 1));
                
                // Connect vertical lines.
                this._tempIndices.Add((uint) current);
                this._tempIndices.Add((uint) next);
            }
        }
        
        this.DrawVertices(commandList, output, transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }
    
    /// <summary>
    /// Renders a 3D hemisphere with the specified transformation, radius, rings, slices, and optional color.
    /// </summary>
    /// <param name="commandList">The command list used for issuing draw commands.</param>
    /// <param name="output">The output description for the current render target.</param>
    /// <param name="transform">The transformation to apply to the hemisphere.</param>
    /// <param name="radius">The radius of the hemisphere.</param>
    /// <param name="rings">The number of rings used to construct the hemisphere. Must be 3 or greater.</param>
    /// <param name="slices">The number of slices used to construct the hemisphere. Must be 3 or greater.</param>
    /// <param name="color">An optional color to apply to the hemisphere. If null, the default color will be white.</param>
    public void DrawHemisphere(CommandList commandList, OutputDescription output, Transform transform, float radius, int rings, int slices, Color? color = null) {
        Color finalColor = color ?? Color.White;
        Texture2D texture = this._currentTexture;
        Rectangle sourceRec = this._currentSourceRec;

        if (rings < 3) {
            rings = 3;
        }

        if (slices < 3) {
            slices = 3;
        }

        // Calculate source rectangle UVs.
        float uLeft = sourceRec.X / (float)texture.Width;
        float uRight = (sourceRec.X + sourceRec.Width) / (float)texture.Width;
        float vTop = sourceRec.Y / (float) texture.Height;
        float vBottom = (sourceRec.Y + sourceRec.Height) / (float) texture.Height;
        
        float halfHeight = radius / 4.0F;
        
        // Generate positions, normals, and texture coordinates for the hemisphere.
        for (int ring = 0; ring <= rings / 2; ring++) {
            float theta = ring * MathF.PI / (rings % 2 == 0 ? rings : rings - 1);
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);

            for (int slice = 0; slice <= slices; slice++) {
                float phi = slice * 2.0F * MathF.PI / slices;
                float sinPhi = MathF.Sin(phi);
                float cosPhi = MathF.Cos(phi);

                Vector3 position = new Vector3(
                    radius / 2.0F * sinTheta * cosPhi,
                    radius / 2.0F * cosTheta - halfHeight,
                    radius / 2.0F * sinTheta * sinPhi
                );

                this._tempVertices.Add(new ImmediateVertex3D() {
                    Position = position,
                    TexCoords = new Vector2(
                        uLeft + (uRight - uLeft) * (0.5F + cosPhi * sinTheta * 0.5F),
                        vTop + (vBottom - vTop) * (0.5F + sinPhi * sinTheta * 0.5F)
                    ),
                    Color = finalColor.ToRgbaFloatVec4()
                });
            }
        }

        int baseCenterIndex = this._tempVertices.Count;

        // Add center vertex for the circular base.
        this._tempVertices.Add(new ImmediateVertex3D() {
            Position = new Vector3(0.0F, -halfHeight, 0.0F),
            TexCoords = new Vector2(
                uLeft + (uRight - uLeft) * 0.5F,
                vTop + (vBottom - vTop) * 0.5F
            ),
            Color = finalColor.ToRgbaFloatVec4()
        });

        // Generate vertices for the base circle.
        for (int slice = 0; slice <= slices; slice++) {
            float sliceAngle = MathF.PI * 2.0F * slice / slices;

            float x = MathF.Cos(sliceAngle) * (radius / 2.0F);
            float z = MathF.Sin(sliceAngle) * (radius / 2.0F);

            this._tempVertices.Add(new ImmediateVertex3D() {
                Position = new Vector3(x, -halfHeight, z),
                TexCoords = new Vector2(
                    uLeft + (uRight - uLeft) * (0.5F + x / radius),
                    vTop + (vBottom - vTop) * (0.5F + z / radius)
                ),
                Color = finalColor.ToRgbaFloatVec4()
            });
        }

        // Generate indices for the hemisphere.
        for (int ring = 0; ring < rings / 2; ring++) {
            for (int slice = 0; slice < slices; slice++) {
                int current = ring * (slices + 1) + slice;
                int next = current + slices + 1;

                // Two triangles per quad.
                this._tempIndices.Add((uint) current);
                this._tempIndices.Add((uint) (next + 1));
                this._tempIndices.Add((uint) (current + 1));

                this._tempIndices.Add((uint) current);
                this._tempIndices.Add((uint) next);
                this._tempIndices.Add((uint) (next + 1));
            }
        }

        // Generate indices for the base circle.
        for (int slice = 0; slice < slices; slice++) {
            this._tempIndices.Add((uint) baseCenterIndex);
            this._tempIndices.Add((uint) (baseCenterIndex + slice + 2));
            this._tempIndices.Add((uint) (baseCenterIndex + slice + 1));
        }

        this.DrawVertices(commandList, output, transform, this._tempVertices, this._tempIndices);
    }

    /// <summary>
    /// Draws the wireframe outline of a hemisphere using the specified parameters.
    /// </summary>
    /// <param name="commandList">The command list used for rendering commands.</param>
    /// <param name="output">The output description that determines the rendering target.</param>
    /// <param name="transform">The transformation to apply to the hemisphere.</param>
    /// <param name="radius">The radius of the hemisphere.</param>
    /// <param name="rings">The number of horizontal subdivisions (rings) for the hemisphere.</param>
    /// <param name="slices">The number of vertical subdivisions (slices) for the hemisphere.</param>
    /// <param name="color">An optional color for the hemisphere. If null, it defaults to white.</param>
    public void DrawHemisphereWires(CommandList commandList, OutputDescription output, Transform transform, float radius, int rings, int slices, Color? color = null) {
        Color finalColor = color ?? Color.White;
        float halfHeight = radius / 4.0F;

        // Generate vertices for the hemisphere.
        for (int ring = 0; ring <= rings / 2; ring++) {
            float ringAngle = ring * MathF.PI / (rings % 2 == 0 ? rings : rings - 1);
            float y = MathF.Cos(ringAngle) * (radius / 2.0F);
            float ringRadius = MathF.Sin(ringAngle) * (radius / 2.0F);

            for (int slice = 0; slice <= slices; slice++) {
                float sliceAngle = MathF.PI * 2.0F * slice / slices;
    
                float x = MathF.Cos(sliceAngle) * ringRadius;
                float z = MathF.Sin(sliceAngle) * ringRadius;
    
                this._tempVertices.Add(new ImmediateVertex3D {
                    Position = new Vector3(x, y - halfHeight, z),
                    Color = finalColor.ToRgbaFloatVec4()
                });
            }
        }
    
        // Generate vertices for the base circle.
        int baseCenterIndex = this._tempVertices.Count;
        
        this._tempVertices.Add(new ImmediateVertex3D {
            Position = new Vector3(0.0F, -halfHeight, 0.0F),
            Color = finalColor.ToRgbaFloatVec4()
        });
        
        for (int slice = 0; slice <= slices; slice++) {
            float sliceAngle = MathF.PI * 2.0F * slice / slices;
        
            float x = MathF.Cos(sliceAngle) * (radius / 2.0F);
            float z = MathF.Sin(sliceAngle) * (radius / 2.0F);
        
            this._tempVertices.Add(new ImmediateVertex3D {
                Position = new Vector3(x, -halfHeight, z),
                Color = finalColor.ToRgbaFloatVec4()
            });
        
            // Connect the central vertex to the base circle.
            this._tempIndices.Add((uint) baseCenterIndex);
            this._tempIndices.Add((uint) (baseCenterIndex + slice));
        }
    
        // Generate wireframe indices.
        for (int ring = 0; ring < rings / 2; ring++) {
            for (int slice = 0; slice < slices; slice++) {
                int current = ring * (slices + 1) + slice;
                int next = current + slices + 1;
    
                // Connect horizontal lines for hemispherical surface.
                this._tempIndices.Add((uint) current);
                this._tempIndices.Add((uint) (current + 1));
    
                // Connect vertical lines for hemispherical surface.
                this._tempIndices.Add((uint) current);
                this._tempIndices.Add((uint) next);
            }
        }
    
        // Connect base circle.
        int baseStartIndex = baseCenterIndex + 1;
        
        for (int slice = 0; slice < slices; slice++) {
            this._tempIndices.Add((uint) (baseStartIndex + slice));
            this._tempIndices.Add((uint) (baseStartIndex + slice + 1));
        }
    
        this.DrawVertices(commandList, output, transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }

    /// <summary>
    /// Renders a cylinder using the specified command list, output description, transform, dimensions, slice count, and optional color.
    /// </summary>
    /// <param name="commandList">The command list used for issuing rendering commands.</param>
    /// <param name="output">The output description that defines the rendering target.</param>
    /// <param name="transform">The transform used to position and orient the cylinder in 3D space.</param>
    /// <param name="radius">The radius of the cylinder's top and bottom caps.</param>
    /// <param name="height">The height of the cylinder from bottom to top cap.</param>
    /// <param name="slices">The number of segments used to approximate the cylindrical surface. Minimum value is 3.</param>
    /// <param name="color">The optional color of the cylinder. If null, a default white color is applied.</param>
    public void DrawCylinder(CommandList commandList, OutputDescription output, Transform transform, float radius, float height, int slices, Color? color = null) {
        Color finalColor = color ?? Color.White;
        Texture2D texture = this._currentTexture;
        Rectangle sourceRec = this._currentSourceRec;

        if (slices < 3) {
            slices = 3;
        }

        // Calculate source rectangle UVs.
        float uLeft = sourceRec.X / (float) texture.Width;
        float uRight = (sourceRec.X + sourceRec.Width) / (float) texture.Width;
        float vTop = sourceRec.Y / (float) texture.Height;
        float vBottom = (sourceRec.Y + sourceRec.Height) / (float) texture.Height;
    
        float halfHeight = height / 2.0F;
    
        // Generate vertices for the side.
        for (int slice = 0; slice <= slices; slice++) {
            float angle = slice * MathF.Tau / slices;
            float x = MathF.Cos(angle) * (radius / 2.0F);
            float z = MathF.Sin(angle) * (radius / 2.0F);
            float u = (float) slice / slices;
    
            // Bottom vertex.
            this._tempVertices.Add(new ImmediateVertex3D {
                Position = new Vector3(x, -halfHeight, z),
                TexCoords = new Vector2(float.Lerp(uLeft, uRight, u), vBottom),
                Color = finalColor.ToRgbaFloatVec4()
            });
    
            // Top vertex.
            this._tempVertices.Add(new ImmediateVertex3D {
                Position = new Vector3(x, halfHeight, z),
                TexCoords = new Vector2(float.Lerp(uLeft, uRight, u), vTop),
                Color = finalColor.ToRgbaFloatVec4()
            });
        }
    
        // Generate indices for the side.
        for (int slice = 0; slice < slices; slice++) {
            int baseIndex = slice * 2;
            int nextIndex = (slice + 1) * 2;
    
            this._tempIndices.Add((uint) (baseIndex + 1));
            this._tempIndices.Add((uint) baseIndex);
            this._tempIndices.Add((uint) nextIndex);
            
            this._tempIndices.Add((uint) (baseIndex + 1));
            this._tempIndices.Add((uint) nextIndex);
            this._tempIndices.Add((uint) (nextIndex + 1));
        }
    
        // Bottom cap.
        int bottomCenterIndex = this._tempVertices.Count;
    
        this._tempVertices.Add(new ImmediateVertex3D {
            Position = new Vector3(0, -halfHeight, 0),
            TexCoords = new Vector2(
                float.Lerp(uLeft, uRight, 0.5F), 
                float.Lerp(vBottom, vTop, 0.5F)
            ),
            Color = finalColor.ToRgbaFloatVec4()
        });
    
        for (int slice = 0; slice < slices; slice++) {
            int baseIndex = slice * 2;
    
            this._tempIndices.Add((uint) bottomCenterIndex);
            this._tempIndices.Add((uint) (baseIndex + 2));
            this._tempIndices.Add((uint) baseIndex);
        }
    
        // Top cap.
        int topCenterIndex = this._tempVertices.Count;
    
        this._tempVertices.Add(new ImmediateVertex3D {
            Position = new Vector3(0.0F, halfHeight, 0.0F),
            TexCoords = new Vector2(
                float.Lerp(uLeft, uRight, 0.5F),
                float.Lerp(vBottom, vTop, 0.5F)
            ),
            Color = finalColor.ToRgbaFloatVec4()
        });
    
        for (int slice = 0; slice < slices; slice++) {
            int baseIndex = slice * 2 + 1;
    
            this._tempIndices.Add((uint) topCenterIndex);
            this._tempIndices.Add((uint) baseIndex);
            this._tempIndices.Add((uint) (baseIndex + 2));
        }
    
        this.DrawVertices(commandList, output, transform, this._tempVertices, this._tempIndices);
    }

    /// <summary>
    /// Draws the wireframe representation of a cylinder using the specified transformation, radius, height, and number of slices.
    /// </summary>
    /// <param name="commandList">The command list used to issue rendering commands.</param>
    /// <param name="output">The output description that defines the rendering target.</param>
    /// <param name="transform">The transformation applied to position, rotate, and scale the cylinder in the scene.</param>
    /// <param name="radius">The radius of the cylinder.</param>
    /// <param name="height">The height of the cylinder.</param>
    /// <param name="slices">The number of divisions around the cylinder's circumference. Must be 3 or greater.</param>
    /// <param name="color">The color of the cylinder's wireframe. Defaults to white if not specified.</param>
    public void DrawCylinderWires(CommandList commandList, OutputDescription output, Transform transform, float radius, float height, int slices, Color? color = null) {
        Color finalColor = color ?? Color.White;

        if (slices < 3) {
            slices = 3;
        }

        float halfHeight = height / 2.0F;

        // Generate vertices for top and bottom circles, and the side edges.
        for (int slice = 0; slice <= slices; slice++) {
            float angle = slice * MathF.Tau / slices;
            float x = MathF.Cos(angle) * (radius / 2.0F);
            float z = MathF.Sin(angle) * (radius / 2.0F);

            // Bottom circle vertex.
            this._tempVertices.Add(new ImmediateVertex3D {
                Position = new Vector3(x, -halfHeight, z),
                Color = finalColor.ToRgbaFloatVec4()
            });

            // Top circle vertex.
            this._tempVertices.Add(new ImmediateVertex3D {
                Position = new Vector3(x, halfHeight, z),
                Color = finalColor.ToRgbaFloatVec4()
            });
        }

        // Generate indices for the top and bottom circles.
        for (int slice = 0; slice < slices; slice++) {
            int bottomIndex = slice * 2;
            int topIndex = bottomIndex + 1;

            // Bottom circle edge.
            this._tempIndices.Add((uint) bottomIndex);
            this._tempIndices.Add((uint) ((bottomIndex + 2) % (slices * 2)));

            // Top circle edge.
            this._tempIndices.Add((uint) topIndex);
            this._tempIndices.Add((uint) ((topIndex + 2) % (slices * 2)));
        }

        // Center vertices for bottom and top circles.
        int bottomCenterIndex = this._tempVertices.Count;
        this._tempVertices.Add(new ImmediateVertex3D {
            Position = new Vector3(0, -halfHeight, 0),
            Color = finalColor.ToRgbaFloatVec4()
        });

        int topCenterIndex = this._tempVertices.Count;
        this._tempVertices.Add(new ImmediateVertex3D {
            Position = new Vector3(0, halfHeight, 0),
            Color = finalColor.ToRgbaFloatVec4()
        });

        // Generate indices for lines from center to edge for bottom and top circles.
        for (int slice = 0; slice <= slices; slice++) {
            int bottomEdgeIndex = slice * 2;
            int topEdgeIndex = bottomEdgeIndex + 1;

            // Line from center of bottom circle to edge.
            this._tempIndices.Add((uint) bottomCenterIndex);
            this._tempIndices.Add((uint) bottomEdgeIndex);

            // Line from center of top circle to edge.
            this._tempIndices.Add((uint) topCenterIndex);
            this._tempIndices.Add((uint) topEdgeIndex);
        }

        // Generate indices for the vertical edges.
        for (int slice = 0; slice < slices; slice++) {
            int bottomIndex = slice * 2;
            int topIndex = bottomIndex + 1;

            this._tempIndices.Add((uint) bottomIndex);
            this._tempIndices.Add((uint) topIndex);
        }

        this.DrawVertices(commandList, output, transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }

    /// <summary>
    /// Renders a 3D capsule using the specified transformation, dimensions, and visual properties.
    /// </summary>
    /// <param name="commandList">The command list used to issue draw commands.</param>
    /// <param name="output">The output description for the render target.</param>
    /// <param name="transform">The transformation to apply to the capsule, including position, rotation, and scale.</param>
    /// <param name="radius">The radius of the capsule's hemispherical ends and cylindrical body.</param>
    /// <param name="height">The total height of the capsule.</param>
    /// <param name="slices">The number of subdivisions around the circumference of the capsule. Must be 3 or greater.</param>
    /// <param name="color">The color of the capsule. If null, defaults to white.</param>
    public void DrawCapsule(CommandList commandList, OutputDescription output, Transform transform, float radius, float height, int slices, Color? color = null) {
        Color finalColor = color ?? Color.White;
        Texture2D texture = this._currentTexture;
        Rectangle sourceRec = this._currentSourceRec;

        if (slices < 3) {
            slices = 3;
        }

        float halfRadius = radius / 2.0F;
        float halfHeight = height / 2.0F;
        int rings = slices / 2;

        // Calculate source rectangle UVs.
        float uLeft = sourceRec.X / (float)texture.Width;
        float uRight = (sourceRec.X + sourceRec.Width) / (float) texture.Width;
        float vTop = sourceRec.Y / (float) texture.Height;
        float vBottom = (sourceRec.Y + sourceRec.Height) / (float) texture.Height;
    
        // Create top hemisphere vertices.
        for (int ring = 0; ring <= rings; ring++) {
            float theta = ring * MathF.PI / (rings * 2.0F);
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);
    
            for (int slice = 0; slice <= slices; slice++) {
                float phi = slice * 2.0F * MathF.PI / slices;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);
                
                float x = halfRadius * sinTheta * cosPhi;
                float y = halfRadius * cosTheta + halfHeight;
                float z = halfRadius * sinTheta * sinPhi;
    
                // Add vertex.
                this._tempVertices.Add(new ImmediateVertex3D {
                    Position = new Vector3(x, y, z),
                    TexCoords = new Vector2(
                        uLeft + (uRight - uLeft) * slice / slices,
                        vTop + (vBottom - vTop) * ring / rings
                    ),
                    Color = finalColor.ToRgbaFloatVec4()
                });
            }
        }
    
        // Create cylindrical body vertices.
        for (int yStep = 0; yStep <= 1; yStep++) {
            float y = yStep == 0 ? -halfHeight : halfHeight;
            
            for (int slice = 0; slice <= slices; slice++) {
                float phi = slice * 2.0F * MathF.PI / slices;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);
                
                float x = halfRadius * cosPhi;
                float z = halfRadius * sinPhi;
                
                // Add vertex.
                this._tempVertices.Add(new ImmediateVertex3D {
                    Position = new Vector3(x, y, z),
                    TexCoords = new Vector2(
                        uLeft + (uRight - uLeft) * slice / slices,
                        yStep == 0 ? vBottom : vTop 
                    ),
                    Color = finalColor.ToRgbaFloatVec4()
                });
            }
        }
    
        // Create bottom hemisphere vertices.
        for (int ring = 0; ring <= rings; ring++) {
            float theta = ring * MathF.PI / (rings * 2.0F) + MathF.PI;
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);
    
            for (int slice = 0; slice <= slices; slice++) {
                float phi = slice * 2.0F * MathF.PI / slices;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);
                
                float x = halfRadius * sinTheta * cosPhi;
                float y = halfRadius * cosTheta - halfHeight;
                float z = halfRadius * sinTheta * sinPhi;
    
                // Add vertex.
                this._tempVertices.Add(new ImmediateVertex3D {
                    Position = new Vector3(-x, y, -z),
                    TexCoords = new Vector2(
                        uLeft + (uRight - uLeft) * slice / slices,
                        vBottom - (vBottom - vTop) * ring / rings
                    ),
                    Color = finalColor.ToRgbaFloatVec4()
                });
            }
        }
    
        // Generate indices for top hemisphere.
        for (int ring = 0; ring < rings; ring++) {
            for (int slice = 0; slice < slices; slice++) {
                uint first = (uint) (ring * (slices + 1) + slice);
                uint second = first + (uint) (slices + 1);
                
                this._tempIndices.Add(first);
                this._tempIndices.Add(second);
                this._tempIndices.Add(first + 1);
                
                this._tempIndices.Add(second);
                this._tempIndices.Add(second + 1);
                this._tempIndices.Add(first + 1);
            }
        }

        // Generate indices for the cylindrical body.
        int cylinderStartIndex = (rings + 1) * (slices + 1);
        
        for (int step = 0; step < 1; step++) {
            for (int slice = 0; slice < slices; slice++) {
                uint first = (uint) (cylinderStartIndex + step * (slices + 1) + slice);
                uint second = first + (uint) (slices + 1);
                
                this._tempIndices.Add(first);
                this._tempIndices.Add(first + 1);
                this._tempIndices.Add(second);
                
                this._tempIndices.Add(first + 1);
                this._tempIndices.Add(second + 1);
                this._tempIndices.Add(second);
            }
        }
        
        // Generate indices for bottom hemisphere.
        int bottomStartIndex = cylinderStartIndex + 2 * (slices + 1);
        
        for (int ring = 0; ring < rings; ring++) {
            for (int slice = 0; slice < slices; slice++) {
                uint first = (uint) (bottomStartIndex + ring * (slices + 1) + slice);
                uint second = first + (uint) (slices + 1);
    
                this._tempIndices.Add(first);
                this._tempIndices.Add(first + 1);
                this._tempIndices.Add(second);
    
                this._tempIndices.Add(second);
                this._tempIndices.Add(first + 1);
                this._tempIndices.Add(second + 1);
            }
        }
    
        this.DrawVertices(commandList, output, transform, this._tempVertices, this._tempIndices);
    }

    /// <summary>
    /// Draws the wireframe of a capsule based on the specified transform, dimensions, and color.
    /// </summary>
    /// <param name="commandList">The command list used for issuing draw commands.</param>
    /// <param name="output">The output description containing information about the render target and configurations.</param>
    /// <param name="transform">The transformation applied to the capsule, such as position, rotation, and scale.</param>
    /// <param name="radius">The radius of the capsule's hemispherical ends and cylindrical body.</param>
    /// <param name="height">The height of the cylindrical body of the capsule.</param>
    /// <param name="slices">The number of divisions for rendering the capsule's rounded surface. Must be at least 3.</param>
    /// <param name="color">The optional color of the capsule wireframe. Defaults to white if not specified.</param>
    public void DrawCapsuleWires(CommandList commandList, OutputDescription output, Transform transform, float radius, float height, int slices, Color? color = null) {
        Color finalColor = color ?? Color.White;

        if (slices < 3) {
            slices = 3;
        }

        float halfRadius = radius / 2.0F;
        float halfHeight = height / 2.0F;
        int rings = slices / 2;

        // Create top hemisphere vertices.
        for (int ring = 0; ring <= rings; ring++) {
            float theta = ring * MathF.PI / (rings * 2.0F);
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);

            for (int slice = 0; slice <= slices; slice++) {
                float phi = slice * 2.0F * MathF.PI / slices;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);
                
                float x = halfRadius * sinTheta * cosPhi;
                float y = halfRadius * cosTheta + halfHeight;
                float z = halfRadius * sinTheta * sinPhi;
                
                // Add vertex.
                this._tempVertices.Add(new ImmediateVertex3D {
                    Position = new Vector3(x, y, z),
                    Color = finalColor.ToRgbaFloatVec4()
                });
                
                // Connect vertical edges for the hemisphere.
                if (ring < rings) {
                    int current = ring * (slices + 1) + slice;
                    int next = current + slices + 1;
    
                    this._tempIndices.Add((uint) current);
                    this._tempIndices.Add((uint) next);
                }
            }
        }
        
        // Create cylindrical body vertices.
        int cylinderStartIndex = this._tempVertices.Count;
        
        for (int yStep = 0; yStep <= 1; yStep++) {
            float y = yStep == 0 ? -halfHeight : halfHeight;
            
            for (int slice = 0; slice <= slices; slice++) {
                float phi = slice * 2.0F * MathF.PI / slices;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);
                
                float x = halfRadius * cosPhi;
                float z = halfRadius * sinPhi;
                
                // Add vertex.
                this._tempVertices.Add(new ImmediateVertex3D {
                    Position = new Vector3(x, y, z),
                    Color = finalColor.ToRgbaFloatVec4()
                });
                
                // Connect vertical edges for the cylinder.
                if (yStep == 0) {
                    int current = cylinderStartIndex + slice;
                    int next = current + slices + 1;
                    
                    this._tempIndices.Add((uint) current);
                    this._tempIndices.Add((uint) next);
                }
            }
        }
        
        // Create bottom hemisphere vertices.
        int bottomStartIndex = this._tempVertices.Count;
        
        for (int ring = 0; ring <= rings; ring++) {
            float theta = MathF.PI - ring * MathF.PI / (rings * 2.0F);
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);
            
            for (int slice = 0; slice <= slices; slice++) {
                float phi = slice * 2.0F * MathF.PI / slices;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);
                
                float x = halfRadius * sinTheta * cosPhi;
                float y = halfRadius * cosTheta - halfHeight;
                float z = halfRadius * sinTheta * sinPhi;
                
                // Add vertex.
                this._tempVertices.Add(new ImmediateVertex3D {
                    Position = new Vector3(x, y, z),
                    Color = finalColor.ToRgbaFloatVec4()
                });
                
                // Connect vertical edges for the hemisphere.
                if (ring < rings) {
                    int current = bottomStartIndex + ring * (slices + 1) + slice;
                    int next = current + slices + 1;
                    
                    this._tempIndices.Add((uint) current);
                    this._tempIndices.Add((uint) next);
                }
            }
        }
        
        // Connect horizontal edges.
        for (int slice = 0; slice < slices; slice++) {
            
            // First hemisphere.
            for (int ring = 0; ring <= rings; ring++) {
                int current = ring * (slices + 1) + slice;
                this._tempIndices.Add((uint) current);
                this._tempIndices.Add((uint) (current + 1));
            }
            
            // Cylindrical body.
            for (int step = 0; step <= 1; step++) {
                int current = cylinderStartIndex + step * (slices + 1) + slice;
                this._tempIndices.Add((uint) current);
                this._tempIndices.Add((uint) (current + 1));
            }
            
            // Second hemisphere.
            for (int ring = 0; ring <= rings; ring++) {
                int current = bottomStartIndex + ring * (slices + 1) + slice;
                this._tempIndices.Add((uint) current);
                this._tempIndices.Add((uint) (current + 1));
            }
        }
        
        this.DrawVertices(commandList, output, transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }

    /// <summary>
    /// Draws a 3D cone using the specified parameters.
    /// </summary>
    /// <param name="commandList">The command list to record drawing commands.</param>
    /// <param name="output">The output description specifying the target rendering surface.</param>
    /// <param name="transform">The transform specifying the position, rotation, and scale of the cone in world space.</param>
    /// <param name="radius">The radius of the base of the cone.</param>
    /// <param name="height">The height of the cone from its base to its apex.</param>
    /// <param name="slices">The number of slices used to construct the cone's base. Must be at least 3.</param>
    /// <param name="color">An optional parameter to define the color of the cone. Defaults to white if not provided.</param>
    public void DrawCone(CommandList commandList, OutputDescription output, Transform transform, float radius, float height, int slices, Color? color = null) {
        Color finalColor = color ?? Color.White;
        Texture2D texture = this._currentTexture;
        Rectangle sourceRec = this._currentSourceRec;

        if (slices < 3) {
            slices = 3;
        }

        // Calculate source rectangle UVs.
        float uLeft = sourceRec.X / (float) texture.Width;
        float uRight = (sourceRec.X + sourceRec.Width) / (float) texture.Width;
        float vTop = sourceRec.Y / (float) texture.Height;
        float vBottom = (sourceRec.Y + sourceRec.Height) / (float) texture.Height;
        
        float halfHeight = height / 2.0F;
        
        // Calculate the side vertices.
        for (int slice = 0; slice <= slices; slice++) {
            float angle = slice * MathF.Tau / slices;
            float x = MathF.Cos(angle) * (radius / 2.0F);
            float z = MathF.Sin(angle) * (radius / 2.0F);
            float u = uLeft + (uRight - uLeft) * ((float) slice / slices);

            // Bottom vertex.
            this._tempVertices.Add(new ImmediateVertex3D() {
                Position = new Vector3(x, -halfHeight, z),
                TexCoords = new Vector2(u, vBottom),
                Color = finalColor.ToRgbaFloatVec4()
            });

            // Top vertex (tip of the cone).
            this._tempVertices.Add(new ImmediateVertex3D {
                Position = new Vector3(0.0F, halfHeight, 0.0F),
                TexCoords = new Vector2(u, 0.0F),
                Color = finalColor.ToRgbaFloatVec4()
            });
        }
        
        // Calculate the side indices.
        for (int slice = 0; slice < slices; slice++) {
            int baseIndex = slice * 2;
            int nextIndex = (slice + 1) * 2;

            this._tempIndices.Add((uint) (baseIndex + 1));
            this._tempIndices.Add((uint) baseIndex);
            this._tempIndices.Add((uint) nextIndex);
        }
        
        // Calculate the bottom cap.
        int bottomCenterIndex = this._tempVertices.Count;

        this._tempVertices.Add(new ImmediateVertex3D() {
            Position = new Vector3(0, -halfHeight, 0),
            TexCoords = new Vector2((uLeft + uRight) / 2.0F, (vTop + vBottom) / 2.0F),
            Color = finalColor.ToRgbaFloatVec4()
        });

        for (int slice = 0; slice < slices; slice++) {
            int baseIndex = slice * 2;

            this._tempIndices.Add((uint) bottomCenterIndex);
            this._tempIndices.Add((uint) (baseIndex + 2));
            this._tempIndices.Add((uint) baseIndex);
        }
        
        this.DrawVertices(commandList, output, transform, this._tempVertices, this._tempIndices);
    }

    /// <summary>
    /// Draws a wireframe representation of a cone in 3D space.
    /// </summary>
    /// <param name="commandList">The command list used to issue rendering commands.</param>
    /// <param name="output">The output description that specifies render target details.</param>
    /// <param name="transform">The transformation to apply to the cone, including position, rotation, and scale.</param>
    /// <param name="radius">The radius of the cone's base.</param>
    /// <param name="height">The height of the cone from the base to its tip.</param>
    /// <param name="slices">The number of slices used to approximate the circular base. Must be at least 3.</param>
    /// <param name="color">An optional color for the wireframe. Defaults to white if null.</param>
    public void DrawConeWires(CommandList commandList, OutputDescription output, Transform transform, float radius, float height, int slices, Color? color = null) {
        Color finalColor = color ?? Color.White;

        if (slices < 3) {
            slices = 3;
        }

        float halfHeight = height / 2.0F;

        // Generate vertices for the cone.
        for (int slice = 0; slice <= slices; slice++) {
            float angle = slice * MathF.Tau / slices;
            float x = MathF.Cos(angle) * (radius / 2.0F);
            float z = MathF.Sin(angle) * (radius / 2.0F);
    
            // Bottom edge vertex.
            this._tempVertices.Add(new ImmediateVertex3D {
                Position = new Vector3(x, -halfHeight, z),
                Color = finalColor.ToRgbaFloatVec4()
            });
    
            // Top vertex (tip of the cone).
            this._tempVertices.Add(new ImmediateVertex3D {
                Position = new Vector3(0.0F, halfHeight, 0.0F),
                Color = finalColor.ToRgbaFloatVec4()
            });
        }
    
        // Generate indices for the cone edges.
        for (int slice = 0; slice < slices; slice++) {
            int baseIndex = slice * 2;
    
            // Edge from bottom to tip.
            this._tempIndices.Add((uint) baseIndex);
            this._tempIndices.Add((uint) (baseIndex + 1));
    
            // Edge along the base.
            this._tempIndices.Add((uint) baseIndex);
            this._tempIndices.Add((uint) ((baseIndex + 2) % (slices * 2)));
        }
    
        // Center vertex for the bottom cap.
        int bottomCenterIndex = this._tempVertices.Count;
    
        this._tempVertices.Add(new ImmediateVertex3D {
            Position = new Vector3(0.0F, -halfHeight, 0.0F),
            Color = finalColor.ToRgbaFloatVec4()
        });
    
        // Generate indices for the bottom cap.
        for (int slice = 0; slice < slices; slice++) {
            int baseIndex = slice * 2;
    
            this._tempIndices.Add((uint) bottomCenterIndex);
            this._tempIndices.Add((uint) baseIndex);
            this._tempIndices.Add((uint) ((baseIndex + 2) % (slices * 2)));
        }
    
        this.DrawVertices(commandList, output, transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }

    /// <summary>
    /// Renders a torus shape using the specified transformation, dimensions, and color.
    /// </summary>
    /// <param name="commandList">The command list used for issuing rendering commands.</param>
    /// <param name="output">The output description specifying render target details.</param>
    /// <param name="transform">The transformation to apply to the torus, including position, rotation, and scale.</param>
    /// <param name="radius">The radius of the inner circle of the torus.</param>
    /// <param name="size">The thickness of the torus.</param>
    /// <param name="radSeg">The number of segments along the radial direction of the torus. Must be 3 or greater.</param>
    /// <param name="sides">The number of sides to approximate the circular cross-section of the torus. Must be 3 or greater.</param>
    /// <param name="color">The color of the torus. If null, the default color will be white.</param>
    public void DrawTorus(CommandList commandList, OutputDescription output, Transform transform, float radius, float size, int radSeg, int sides, Color? color = null) {
        Color finalColor = color ?? Color.White;
        Texture2D texture = this._currentTexture;
        Rectangle sourceRec = this._currentSourceRec;

        if (radSeg < 3) {
            radSeg = 3;
        }

        if (sides < 3) {
            sides = 3;
        }

        // Calculate source rectangle UVs.
        float uLeft = sourceRec.X / (float) texture.Width;
        float uRight = (sourceRec.X + sourceRec.Width) / (float) texture.Width;
        float vTop = sourceRec.Y / (float) texture.Height;
        float vBottom = (sourceRec.Y + sourceRec.Height) / (float) texture.Height;
        
        float circusStep = MathF.Tau / radSeg;
        float sideStep = MathF.Tau / sides;

        // Calculate the vertices.
        for (int rad = 0; rad <= radSeg; rad++) {
            float radAngle = rad * circusStep;
            float cosRad = MathF.Cos(radAngle);
            float sinRad = MathF.Sin(radAngle);

            for (int side = 0; side <= sides; side++) {
                float sideAngle = side * sideStep;
                float cosSide = MathF.Cos(sideAngle);
                float sinSide = MathF.Sin(sideAngle);

                Vector3 position = new Vector3(cosSide * cosRad, sinSide, cosSide * sinRad) * (size / 4.0F) + new Vector3(cosRad * (radius / 4.0F), 0.0F, sinRad * (radius / 4.0F));
                Vector2 texCoords = new Vector2(
                    uLeft + (uRight - uLeft) * ((float) rad / radSeg),
                    vTop + (vBottom - vTop) * ((float) side / sides)
                );

                this._tempVertices.Add(new ImmediateVertex3D() {
                    Position = position,
                    TexCoords = texCoords,
                    Color = finalColor.ToRgbaFloatVec4()
                });
            }
        }

        // Calculate the indices.
        for (int rad = 0; rad < radSeg; rad++) {
            for (int side = 0; side < sides; side++) {
                int current = rad * (sides + 1) + side;
                int next = current + sides + 1;

                this._tempIndices.Add((uint) current);
                this._tempIndices.Add((uint) next);
                this._tempIndices.Add((uint) (next + 1));
 
                this._tempIndices.Add((uint) current);
                this._tempIndices.Add((uint) (next + 1));
                this._tempIndices.Add((uint) (current + 1));
            }
        }
        
        this.DrawVertices(commandList, output, transform, this._tempVertices, this._tempIndices);
    }

    /// <summary>
    /// Renders a wireframe torus using the specified transformation, dimensions, and color.
    /// </summary>
    /// <param name="commandList">The command list used for rendering the torus wireframe.</param>
    /// <param name="output">The output description used for the rendering pipeline.</param>
    /// <param name="transform">The transformation applied to the torus, including position, rotation, and scale.</param>
    /// <param name="radius">The radius of the inner circle of the torus.</param>
    /// <param name="size">The thickness of the torus.</param>
    /// <param name="radSeg">The number of radial segments. Must be 3 or greater.</param>
    /// <param name="sides">The number of subdivisions around the circular cross-section. Must be 3 or greater.</param>
    /// <param name="color">The optional color used for rendering the torus wireframe. Defaults to white if not specified.</param>
    public void DrawTorusWires(CommandList commandList, OutputDescription output, Transform transform, float radius, float size, int radSeg, int sides, Color? color = null) {
        Color finalColor = color ?? Color.White;

        if (radSeg < 3) {
            radSeg = 3;
        }

        if (sides < 3) {
            sides = 3;
        }

        float circusStep = MathF.Tau / radSeg;
        float sideStep = MathF.Tau / sides;

        // Calculate the vertices for wireframe.
        for (int rad = 0; rad <= radSeg; rad++) {
            float radAngle = rad * circusStep;
            float cosRad = MathF.Cos(radAngle);
            float sinRad = MathF.Sin(radAngle);

            for (int side = 0; side <= sides; side++) {
                float sideAngle = side * sideStep;
                float cosSide = MathF.Cos(sideAngle);
                float sinSide = MathF.Sin(sideAngle);
    
                Vector3 position = new Vector3(cosSide * cosRad, sinSide, cosSide * sinRad) * (size / 4.0F) + new Vector3(cosRad * (radius / 4.0F), 0.0F, sinRad * (radius / 4.0F));
    
                this._tempVertices.Add(new ImmediateVertex3D() {
                    Position = position,
                    Color = finalColor.ToRgbaFloatVec4()
                });
    
                // Add indices for connecting radial and side segments.
                if (rad < radSeg && side < sides) {
                    int current = rad * (sides + 1) + side;
                    int nextSide = current + 1;
                    int nextRad = current + (sides + 1);
    
                    this._tempIndices.Add((uint) current);
                    this._tempIndices.Add((uint) nextSide);
    
                    this._tempIndices.Add((uint) current);
                    this._tempIndices.Add((uint) nextRad);
                }
            }
        }
        
        this.DrawVertices(commandList, output, transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }

    /// <summary>
    /// Draws a knot shape with the specified parameters using a command list and defined properties such as transformations, radii, number of segments, and optional color.
    /// </summary>
    /// <param name="commandList">The command list used for rendering commands.</param>
    /// <param name="output">The output description specifying the rendering context.</param>
    /// <param name="transform">The transformation to apply to the knot during rendering.</param>
    /// <param name="radius">The overall radius of the knot.</param>
    /// <param name="tubeRadius">The radius of the tube forming the knot structure.</param>
    /// <param name="radSeg">The number of radial segments forming the knot. Must be 3 or greater.</param>
    /// <param name="sides">The number of sides of the tube forming the knot. Must be 3 or greater.</param>
    /// <param name="color">An optional color to apply to the knot. Defaults to white when null.</param>
    public void DrawKnot(CommandList commandList, OutputDescription output, Transform transform, float radius, float tubeRadius, int radSeg, int sides, Color? color = null) {
        Color finalColor = color ?? Color.White;
        Texture2D texture = this._currentTexture;
        Rectangle sourceRec = this._currentSourceRec;

        if (radSeg < 3) {
            radSeg = 3;
        }

        if (sides < 3) {
            sides = 3;
        }

        // Calculate source rectangle UVs.
        float uLeft = sourceRec.X / (float)texture.Width;
        float uRight = (sourceRec.X + sourceRec.Width) / (float)texture.Width;
        float vTop = sourceRec.Y / (float) texture.Height;
        float vBottom = (sourceRec.Y + sourceRec.Height) / (float) texture.Height;

        float step = MathF.Tau / radSeg;
        float sideStep = MathF.Tau / sides;
    
        // Calculate the vertices.
        for (int rad = 0; rad <= radSeg; rad++) {
            float t = rad * step;
    
            float x = MathF.Sin(t) + 2.0F * MathF.Sin(2.0F * t);
            float y = MathF.Cos(t) - 2.0F * MathF.Cos(2.0F * t);
            float z = -MathF.Sin(3.0F * t);
    
            Vector3 center = new Vector3(x, y, z) * (radius / 6.0F);
    
            Vector3 tangent = Vector3.Normalize(new Vector3(
                MathF.Cos(t) + 4.0F * MathF.Cos(2.0F * t),
                -MathF.Sin(t) + 4.0F * MathF.Sin(2.0F * t),
                -3.0F * MathF.Cos(3.0F * t)
            ));
            
            Vector3 normal = Vector3.Normalize(new Vector3(-tangent.Y, tangent.X, 0.0F));
            Vector3 binormal = Vector3.Cross(tangent, normal);

            for (int side = 0; side <= sides; side++) {
                float sideAngle = side * sideStep;
                float cosAngle = MathF.Cos(sideAngle);
                float sinAngle = MathF.Sin(sideAngle);
    
                Vector3 offset = normal * cosAngle * (tubeRadius / 6.0F) + binormal * sinAngle * (tubeRadius / 6.0F);
                Vector3 position = center + offset;
                Vector2 texCoords = new Vector2(
                    float.Lerp(uLeft, uRight, (float) rad / radSeg),
                    float.Lerp(vTop, vBottom, (float) side / sides)
                );
    
                this._tempVertices.Add(new ImmediateVertex3D() {
                    Position = position,
                    TexCoords = texCoords,
                    Color = finalColor.ToRgbaFloatVec4()
                });
            }
        }
        
        // Calculate the indices.
        for (int rad = 0; rad < radSeg; rad++) {
            for (int side = 0; side < sides; side++) {
                int current = rad * (sides + 1) + side;
                int next = current + sides + 1;
    
                this._tempIndices.Add((uint) current);
                this._tempIndices.Add((uint) next);
                this._tempIndices.Add((uint) (next + 1));
                
                this._tempIndices.Add((uint) current);
                this._tempIndices.Add((uint) (next + 1));
                this._tempIndices.Add((uint) (current + 1));
            }
        }
        
        this.DrawVertices(commandList, output, transform, this._tempVertices, this._tempIndices);
    }

    /// <summary>
    /// Renders the wireframe of a knot shape using the specified transformation, radii, and segments.
    /// </summary>
    /// <param name="commandList">The command list used for issuing rendering commands.</param>
    /// <param name="output">The output description specifying the rendering target.</param>
    /// <param name="transform">The transformation applied to the knot wireframe.</param>
    /// <param name="radius">The radius of the knot.</param>
    /// <param name="tubeRadius">The radius of the tube comprising the knot's wireframe.</param>
    /// <param name="radSeg">The number of radial segments making up the knot. Minimum value is 3.</param>
    /// <param name="sides">The number of sides for the tube cross-section. Minimum value is 3.</param>
    /// <param name="color">An optional color to use for the wireframe; if null, the default is white.</param>
    public void DrawKnotWires(CommandList commandList, OutputDescription output, Transform transform, float radius, float tubeRadius, int radSeg, int sides, Color? color = null) {
        Color finalColor = color ?? Color.White;

        if (radSeg < 3) {
            radSeg = 3;
        }

        if (sides < 3) {
            sides = 3;
        }

        float step = MathF.Tau / radSeg;
        float sideStep = MathF.Tau / sides;

        // Calculate vertices and edges for the wireframe.
        for (int rad = 0; rad <= radSeg; rad++) {
            float t = rad * step;

            float x = MathF.Sin(t) + 2.0F * MathF.Sin(2.0F * t);
            float y = MathF.Cos(t) - 2.0F * MathF.Cos(2.0F * t);
            float z = -MathF.Sin(3.0F * t);
    
            Vector3 center = new Vector3(x, y, z) * (radius / 6.0F);
    
            Vector3 tangent = Vector3.Normalize(new Vector3(
                MathF.Cos(t) + 4.0F * MathF.Cos(2.0F * t),
                -MathF.Sin(t) + 4.0F * MathF.Sin(2.0F * t),
                -3.0F * MathF.Cos(3.0F * t)
            ));
    
            Vector3 normal = Vector3.Normalize(new Vector3(-tangent.Y, tangent.X, 0.0F));
            Vector3 binormal = Vector3.Cross(tangent, normal);
    
            for (int side = 0; side <= sides; side++) {
                float sideAngle = side * sideStep;
                float cosAngle = MathF.Cos(sideAngle);
                float sinAngle = MathF.Sin(sideAngle);
    
                Vector3 offset = normal * cosAngle * (tubeRadius / 6.0F) + binormal * sinAngle * (tubeRadius / 6.0F);
                Vector3 position = center + offset;
    
                this._tempVertices.Add(new ImmediateVertex3D() {
                    Position = position,
                    Color = finalColor.ToRgbaFloatVec4()
                });
    
                // Connect to next radial segment.
                if (rad < radSeg) {
                    this._tempIndices.Add((uint) (rad * (sides + 1) + side));
                    this._tempIndices.Add((uint) ((rad + 1) * (sides + 1) + side));
                }
    
                // Connect to the next side (wrapping around at the end).
                if (side < sides) {
                    this._tempIndices.Add((uint) (rad * (sides + 1) + side));
                    this._tempIndices.Add((uint) (rad * (sides + 1) + side + 1));
                }
            }
        }
    
        this.DrawVertices(commandList, output, transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }

    /// <summary>
    /// Renders a grid using the specified command list, output description, transformation matrix, and grid parameters.
    /// </summary>
    /// <param name="commandList">The command list used to issue rendering commands.</param>
    /// <param name="output">The output description for the target render resource.</param>
    /// <param name="transform">The transformation matrix applied to the grid.</param>
    /// <param name="slices">The number of grid divisions or slices. Must be greater than or equal to 1.</param>
    /// <param name="spacing">The distance between adjacent grid lines. Must be greater than or equal to 1.</param>
    /// <param name="majorLineSpacing">The interval of major grid lines, which are visually distinct. Must be greater than or equal to 1.</param>
    /// <param name="color">The optional color of the grid lines. Defaults to white if not provided.</param>
    /// <param name="axisColorX">The optional color for the X-axis grid line. Defaults to red if not provided.</param>
    /// <param name="axisColorZ">The optional color for the Z-axis grid line. Defaults to blue if not provided.</param>
    public void DrawGrid(CommandList commandList, OutputDescription output, Transform transform, int slices, int spacing, int majorLineSpacing, Color? color = null, Color? axisColorX = null, Color? axisColorZ = null) {
        Color finalColor = color ?? Color.White;
        Color finalAxisColorX = axisColorX ?? Color.Red;
        Color finalAxisColorZ = axisColorZ ?? Color.Blue;
        
        if (slices < 1) {
            slices = 1;
        }
        
        if (spacing < 1) {
            spacing = 1;
        }
        
        if (majorLineSpacing < 1) {
            majorLineSpacing = 1;
        }
        
        float halfSize = slices * spacing * 0.5F;
        
        for (int i = 0; i <= slices; i++) {
            float offset = -halfSize + i * spacing;
            
            if (i == slices / 2) {
                
                // Draw lines along the X axis.
                this._tempVertices.Add(new ImmediateVertex3D {
                    Position = new Vector3(offset, 0.0F, -halfSize),
                    Color = finalAxisColorX.ToRgbaFloatVec4()
                });
                
                this._tempVertices.Add(new ImmediateVertex3D {
                    Position = new Vector3(offset, 0.0F, halfSize),
                    Color = finalAxisColorX.ToRgbaFloatVec4()
                });
                
                // Draw lines along the Z axis.
                this._tempVertices.Add(new ImmediateVertex3D {
                    Position = new Vector3(-halfSize, 0.0F, offset),
                    Color = finalAxisColorZ.ToRgbaFloatVec4()
                });
                
                this._tempVertices.Add(new ImmediateVertex3D {
                    Position = new Vector3(halfSize, 0.0F, offset),
                    Color = finalAxisColorZ.ToRgbaFloatVec4()
                });
            }
            else if (i % majorLineSpacing == 0) {
                RgbaFloat lightColor = new RgbaFloat(finalColor.R / 255.0F * 1.5F, finalColor.G / 255.0F * 1.5F, finalColor.B / 255.0F * 1.5F, finalColor.A / 255.0F);
                
                // Draw lines along the X axis.
                this._tempVertices.Add(new ImmediateVertex3D {
                    Position = new Vector3(offset, 0.0F, -halfSize),
                    Color = lightColor.ToVector4()
                });
                
                this._tempVertices.Add(new ImmediateVertex3D {
                    Position = new Vector3(offset, 0.0F, halfSize),
                    Color = lightColor.ToVector4()
                });
                
                // Draw lines along the Z axis.
                this._tempVertices.Add(new ImmediateVertex3D {
                    Position = new Vector3(-halfSize, 0.0F, offset),
                    Color = lightColor.ToVector4()
                });
                
                this._tempVertices.Add(new ImmediateVertex3D {
                    Position = new Vector3(halfSize, 0.0F, offset),
                    Color = lightColor.ToVector4()
                });
            }
            else {
                
                // Draw lines along the X axis.
                this._tempVertices.Add(new ImmediateVertex3D {
                    Position = new Vector3(offset, 0.0F, -halfSize),
                    Color = finalColor.ToRgbaFloatVec4()
                });
                
                this._tempVertices.Add(new ImmediateVertex3D {
                    Position = new Vector3(offset, 0.0F, halfSize),
                    Color = finalColor.ToRgbaFloatVec4()
                });
                
                // Draw lines along the Z axis.
                this._tempVertices.Add(new ImmediateVertex3D {
                    Position = new Vector3(-halfSize, 0.0F, offset),
                    Color = finalColor.ToRgbaFloatVec4()
                });
                
                this._tempVertices.Add(new ImmediateVertex3D {
                    Position = new Vector3(halfSize, 0.0F, offset),
                    Color = finalColor.ToRgbaFloatVec4()
                });
            }
            
            // Add indices for line pairs along X and Z.
            this._tempIndices.Add((uint) (i * 4 + 0));
            this._tempIndices.Add((uint) (i * 4 + 1));
            this._tempIndices.Add((uint) (i * 4 + 2));
            this._tempIndices.Add((uint) (i * 4 + 3));
        }
        
        this.DrawVertices(commandList, output, transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }

    /// <summary>
    /// Draws the edges of the specified bounding box using the given transformation and color.
    /// </summary>
    /// <param name="commandList">The command list used for issuing rendering commands.</param>
    /// <param name="output">The output description for the rendering target.</param>
    /// <param name="transform">The transformation to apply to the bounding box before rendering.</param>
    /// <param name="box">The bounding box to be drawn.</param>
    /// <param name="color">The color to use for the bounding box edges. If null, white is used as the default color.</param>
    public void DrawBoundingBox(CommandList commandList, OutputDescription output, Transform transform, BoundingBox box, Color? color = null) {
        Color finalColor = color ?? Color.White;

        for (int i = 0; i < 8; i++) {
            // Calculate the x, y, z coordinates.
            float x = (i & 1) == 0 ? box.Min.X : box.Max.X;
            float y = (i & 2) == 0 ? box.Min.Y : box.Max.Y;
            float z = (i & 4) == 0 ? box.Min.Z : box.Max.Z;
            
            // Add the vertex to the list.
            this._tempVertices.Add(new ImmediateVertex3D {
                Position = new Vector3(x, y, z),
                Color = finalColor.ToRgbaFloatVec4()
            });
            
            // Connect the vertex to its neighbors.
            for (int bit = 0; bit < 3; bit++) {
                if ((i & (1 << bit)) == 0) {
                    int neighbor = i | (1 << bit);
                    this._tempIndices.Add((uint) i);
                    this._tempIndices.Add((uint) neighbor);
                }
            }
        }

        this.DrawVertices(commandList, output, transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }

    /// <summary>
    /// Draws a line between two points in 3D space with a specified optional color.
    /// </summary>
    /// <param name="commandList">The command list used for recording drawing commands.</param>
    /// <param name="output">The output description of the render target.</param>
    /// <param name="startPos">The starting position of the line in 3D space.</param>
    /// <param name="endPos">The ending position of the line in 3D space.</param>
    /// <param name="color">The optional color of the line. If not provided, defaults to white.</param>
    public void DrawLine(CommandList commandList, OutputDescription output, Vector3 startPos, Vector3 endPos, Color? color = null) {
        Color finalColor = color ?? Color.White;

        // Add start vertex.
        this._tempVertices.Add(new ImmediateVertex3D {
            Position = startPos,
            Color = finalColor.ToRgbaFloatVec4()
        });

        // Add end vertex.
        this._tempVertices.Add(new ImmediateVertex3D {
            Position = endPos,
            Color = finalColor.ToRgbaFloatVec4()
        });
        
        // Add indices for the line.
        this._tempIndices.Add(0);
        this._tempIndices.Add(1);
        
        this.DrawVertices(commandList, output, new Transform(), this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }

    /// <summary>
    /// Draws a billboard at the specified position with optional scaling and color parameters.
    /// </summary>
    /// <param name="commandList">The command list used for rendering commands.</param>
    /// <param name="output">The output description that defines rendering target properties.</param>
    /// <param name="position">The 3D position where the billboard will be rendered.</param>
    /// <param name="scale">The optional scale factor for the billboard. Defaults to a scale of 1 if not specified.</param>
    /// <param name="color">The optional color of the billboard. Defaults to white if not specified.</param>
    public void DrawBillboard(CommandList commandList, OutputDescription output, Vector3 position, Vector2? scale = null, Color? color = null) {
        Color finalColor = color ?? Color.White;
        Vector2 finalScale = scale ?? Vector2.One;
        Texture2D texture = this._currentTexture;
        Rectangle sourceRec = this._currentSourceRec;

        Cam3D? cam3D = Cam3D.ActiveCamera;

        if (cam3D == null) {
            return;
        }

        // Calculate the direction from billboard position to camera position.
        Vector3 directionToCamera = -Vector3.Normalize(cam3D.Position - position);

        // Project the camera's forward direction onto the horizontal plane (yaw axis).
        Vector3 cameraForwardFlat = -Vector3.Normalize(new Vector3(cam3D.GetForward().X, 0, cam3D.GetForward().Z));

        // Calculate the rotation angle between the projected camera forward and the direction to camera.
        float angle = (float) Math.Atan2(directionToCamera.X - cameraForwardFlat.X, directionToCamera.Z - cameraForwardFlat.Z);
        
        // Create billboard transform.
        Transform billboardTransform = new Transform {
            Translation = position,
            Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle),
            Scale = new Vector3(finalScale, 0)
        };
        
        // Calculate source rectangle UVs.
        float uLeft = sourceRec.X / (float) texture.Width;
        float uRight = (sourceRec.X + sourceRec.Width) / (float) texture.Width;
        float vTop = sourceRec.Y / (float) texture.Height;
        float vBottom = (sourceRec.Y + sourceRec.Height) / (float) texture.Height;
        
        // Calculate half size.
        Vector3 halfSize = new Vector3(sourceRec.Width / 100.0F, sourceRec.Height / 100.0F, 0.0F) / 2.0F;
        
        // Add vertices.
        this._tempVertices.Add(new ImmediateVertex3D {
            Position = new Vector3(-halfSize.X, -halfSize.Y, 0.0F),
            TexCoords = new Vector2(uRight, vBottom),
            Color = finalColor.ToRgbaFloatVec4()
        });
        
        this._tempVertices.Add(new ImmediateVertex3D {
            Position = new Vector3(halfSize.X, -halfSize.Y, 0.0F),
            TexCoords = new Vector2(uLeft, vBottom),
            Color = finalColor.ToRgbaFloatVec4()
        });
        
        this._tempVertices.Add(new ImmediateVertex3D {
            Position = new Vector3(halfSize.X, halfSize.Y, 0.0F),
            TexCoords = new Vector2(uLeft, vTop),
            Color = finalColor.ToRgbaFloatVec4()
        });
        
        this._tempVertices.Add(new ImmediateVertex3D {
            Position = new Vector3(-halfSize.X, halfSize.Y, 0.0F),
            TexCoords = new Vector2(uRight, vTop),
            Color = finalColor.ToRgbaFloatVec4()
        });
    
        // Add indices for 2 triangles making up the quad.
        this._tempIndices.Add(0);
        this._tempIndices.Add(1);
        this._tempIndices.Add(2);
        this._tempIndices.Add(2);
        this._tempIndices.Add(3);
        this._tempIndices.Add(0);
    
        this.DrawVertices(commandList, output, billboardTransform, this._tempVertices, this._tempIndices);
    }

    /// <summary>
    /// Draws a set of vertices using the specified command list, output description, transformation, vertex data, indices, and primitive topology.
    /// </summary>
    /// <param name="commandList">The command list used for issuing drawing commands.</param>
    /// <param name="output">The output description that defines how the rendered content is processed and displayed.</param>
    /// <param name="transform">The transformation matrix to apply to the vertex data.</param>
    /// <param name="vertices">The collection of vertices to render.</param>
    /// <param name="indices">The collection of indices defining the order in which vertices are connected. Defaults to null.</param>
    /// <param name="topology">The primitive topology that specifies how the vertex data should be interpreted (e.g., triangle list, line strip). Defaults to TriangleList.</param>
    public void DrawVertices(CommandList commandList, OutputDescription output, Transform transform, List<ImmediateVertex3D> vertices, List<uint>? indices = null, PrimitiveTopology topology = PrimitiveTopology.TriangleList) {
        Cam3D? cam3D = Cam3D.ActiveCamera;
        
        if (cam3D == null) {
            // Clear temp data.
            this._tempVertices.Clear();
            this._tempIndices.Clear();
            return;
        }
        
        if (vertices.Count > this.Capacity) {
            Logger.Fatal(new InvalidOperationException($"The number of provided vertices exceeds the capacity! [{vertices.Count} > {this.Capacity}]"));
        }
        
        if (indices != null && indices.Count > this.Capacity * 3) {
            Logger.Fatal(new InvalidOperationException($"The number of provided indices exceeds the capacity! [{indices.Count} > {this.Capacity * 3}]"));
        }
        
        // Add vertices.
        for (int i = 0; i < vertices.Count; i++) {
            this._vertices[i] = vertices[i];
        }
        
        // Add indices.
        if (indices != null) {
            for (int i = 0; i < indices.Count; i++) {
                this._indices[i] = indices[i];
            }
        }
        
        // Set vertices and indices count.
        this._vertexCount = vertices.Count;
        this._indexCount = indices?.Count ?? 0;
        
        // Update matrix buffer.
        this._matrixBuffer.SetValue(0, cam3D.GetProjection());
        this._matrixBuffer.SetValue(1, cam3D.GetView());
        this._matrixBuffer.SetValue(2, transform.GetTransform());
        this._matrixBuffer.UpdateBufferDeferred(commandList);
        
        // Update pipeline description.
        this._pipelineDescription.PrimitiveTopology = topology;
        this._pipelineDescription.Outputs = output;
        
        if (this._indexCount > 0) {
            
            // Update vertex and index buffer.
            commandList.UpdateBuffer(this._vertexBuffer, 0, new ReadOnlySpan<ImmediateVertex3D>(this._vertices, 0, this._vertexCount));
            commandList.UpdateBuffer(this._indexBuffer, 0, new ReadOnlySpan<uint>(this._indices, 0, this._indexCount));
            
            // Set vertex and index buffer.
            commandList.SetVertexBuffer(0, this._vertexBuffer);
            commandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt32);

            // Set pipeline.
            commandList.SetPipeline(this._currentEffect.GetPipeline(this._pipelineDescription).Pipeline);
        
            // Set matrix buffer.
            commandList.SetGraphicsResourceSet(this._currentEffect.GetBufferLayoutSlot("MatrixBuffer"), this._matrixBuffer.GetResourceSet(this._currentEffect.GetBufferLayout("MatrixBuffer")));

            // Set resourceSet of the texture.
            commandList.SetGraphicsResourceSet(this._currentEffect.GetTextureLayoutSlot("fTexture"), this._currentTexture.GetResourceSet(this._currentSampler, this._currentEffect.GetTextureLayout("fTexture")));
            
            // Apply effect.
            this._currentEffect.Apply(commandList);
            
            // Draw.
            commandList.DrawIndexed((uint) this._indexCount);
        }
        else {
            
            // Update vertex buffer.
            commandList.UpdateBuffer(this._vertexBuffer, 0, new ReadOnlySpan<ImmediateVertex3D>(this._vertices, 0, this._vertexCount));
            
            // Set vertex buffer.
            commandList.SetVertexBuffer(0, this._vertexBuffer);

            // Set pipeline.
            commandList.SetPipeline(this._currentEffect.GetPipeline(this._pipelineDescription).Pipeline);
        
            // Set matrix buffer.
            commandList.SetGraphicsResourceSet(this._currentEffect.GetBufferLayoutSlot("MatrixBuffer"), this._matrixBuffer.GetResourceSet(this._currentEffect.GetBufferLayout("MatrixBuffer")));
        
            // Set resourceSet of the texture.
            commandList.SetGraphicsResourceSet(this._currentEffect.GetTextureLayoutSlot("fTexture"), this._currentTexture.GetResourceSet(this._currentSampler, this._currentEffect.GetTextureLayout("fTexture")));
            
            // Apply effect.
            this._currentEffect.Apply(commandList);
            
            // Draw.
            commandList.Draw((uint) this._vertexCount);
        }
        
        // Reset indexer.
        this._vertexCount = 0;
        this._indexCount = 0;
        
        // Clear data.
        Array.Clear(this._vertices);
        Array.Clear(this._indices);
        
        // Clear temp data.
        this._tempVertices.Clear();
        this._tempIndices.Clear();
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._vertexBuffer.Dispose();
            this._indexBuffer.Dispose();
            this._matrixBuffer.Dispose();
        }
    }
}
