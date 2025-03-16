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
    /// Gets the maximum number of vertices that can be batched.
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
    private SimpleBuffer<Matrix4x4> _matrixBuffer;

    /// <summary>
    /// The pipeline description used to configure the graphics pipeline for rendering.
    /// </summary>
    private SimplePipelineDescription _pipelineDescription;
    
    /// <summary>
    /// Indicates whether the rendering process has begun.
    /// </summary>
    private bool _begun;
    
    /// <summary>
    /// The current command list used for recording rendering commands.
    /// </summary>
    private CommandList _currentCommandList;

    /// <summary>
    /// The currently bound effect.
    /// </summary>
    private Effect _currentEffect;
    
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
    /// <param name="capacity">The maximum number of vertices that can be batched. Defaults to 30720.</param>
    public ImmediateRenderer(GraphicsDevice graphicsDevice, uint capacity = 30720) {
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
        this._matrixBuffer = new SimpleBuffer<Matrix4x4>(graphicsDevice, 3, SimpleBufferType.Uniform, ShaderStages.Vertex);

        // Create pipeline description.
        this._pipelineDescription = this.CreatePipelineDescription();
        
        // Set default texture and sampler.
        this._currentTexture = GlobalResource.DefaultImmediateRendererTexture;
        this._currentSampler = GraphicsHelper.GetSampler(this.GraphicsDevice, SamplerType.Point);
        this._currentSourceRec = new Rectangle(0, 0, (int) this._currentTexture.Width, (int) this._currentTexture.Height);
    }

    /// <summary>
    /// Begins the rendering process by configuring the command list, output, effect, and blend state for the pipeline.
    /// </summary>
    /// <param name="commandList">The command list used to record rendering commands.</param>
    /// <param name="output">The output description that specifies the rendering target details.</param>
    /// <param name="effect">An optional effect instance; if null, the default immediate renderer effect is used.</param>
    /// <param name="blendState">An optional blend state instance; if null, the default alpha blend state is used.</param>
    /// <exception cref="Exception">Thrown if the rendering process has already been initiated.</exception>
    public void Begin(CommandList commandList, OutputDescription output, Effect? effect = null, BlendState? blendState = null) {
        if (this._begun) {
            throw new Exception("The ImmediateRenderer has already begun!");
        }

        this._begun = true;
        this._currentCommandList = commandList;
        this._currentEffect = effect ?? GlobalResource.DefaultImmediateRendererEffect;

        // Update pipeline description.
        this._pipelineDescription.BlendState = blendState?.Description ?? BlendState.AlphaBlend.Description;
        this._pipelineDescription.BufferLayouts = this._currentEffect.GetBufferLayouts();
        this._pipelineDescription.TextureLayouts = this._currentEffect.GetTextureLayouts();
        this._pipelineDescription.ShaderSet = this._currentEffect.ShaderSet;
        this._pipelineDescription.Outputs = output;
    }

    /// <summary>
    /// Ends the rendering process, flushing any remaining batched geometry.
    /// </summary>
    /// <exception cref="Exception">Thrown if the renderer has not begun rendering.</exception>
    public void End() {
        if (!this._begun) {
            throw new Exception("The ImmediateRenderer has not begun yet!");
        }
        
        this._begun = false;
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
        this._currentSampler = sampler ?? GraphicsHelper.GetSampler(this.GraphicsDevice, SamplerType.Point);
        this._currentSourceRec = sourceRect ?? new Rectangle(0, 0, (int) this._currentTexture.Width, (int) this._currentTexture.Height);
    }
    
    /// <summary>
    /// Draws a cube with the specified transformation, size, and optional color.
    /// </summary>
    /// <param name="transform">The transformation to be applied to the cube.</param>
    /// <param name="size">The size of the cube in 3D space.</param>
    /// <param name="color">An optional color for the cube; if null, white is used.</param>
    public void DrawCube(Transform transform, Vector3 size, Color? color = null) {
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

        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.TriangleList);
    }

    /// <summary>
    /// Draws the wireframe of a cube with the specified transform, size, and optional color.
    /// </summary>
    /// <param name="transform">The transformation to apply to the cube, which includes position, rotation, and scale.</param>
    /// <param name="size">The dimensions of the cube to draw.</param>
    /// <param name="color">An optional color for the wireframe. If null, the default color is white.</param>
    public void DrawCubeWires(Transform transform, Vector3 size, Color? color = null) {
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

        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }
    
    /// <summary>
    /// Draws a sphere with the specified transformation, radius, number of rings, slices, and optional color.
    /// </summary>
    /// <param name="transform">The transformation to be applied to the sphere.</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <param name="rings">The number of horizontal subdivisions of the sphere.</param>
    /// <param name="slices">The number of vertical subdivisions of the sphere.</param>
    /// <param name="color">An optional color for the sphere; defaults to white.</param>
    public void DrawSphere(Transform transform, float radius, int rings, int slices, Color? color = null) {
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
        
        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.TriangleList);
    }
    
    /// <summary>
    /// Draws the wireframe of a sphere with the given transform, radius, number of rings, slices, and optional color.
    /// </summary>
    /// <param name="transform">The transformation to apply to the sphere (position, rotation, and scaling).</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <param name="rings">The number of horizontal subdivisions of the sphere.</param>
    /// <param name="slices">The number of vertical subdivisions of the sphere.</param>
    /// <param name="color">An optional color for the wireframe; defaults to white.</param>
    public void DrawSphereWires(Transform transform, float radius, int rings, int slices, Color? color = null) {
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
    
        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }

    /// <summary>
    /// Renders a 3D hemisphere with the specified transformation, radius, rings, slices, and optional color.
    /// </summary>
    /// <param name="transform">The transformation to apply to the hemisphere.</param>
    /// <param name="radius">The radius of the hemisphere.</param>
    /// <param name="rings">The number of rings used to construct the hemisphere. Must be 3 or greater.</param>
    /// <param name="slices">The number of slices used to construct the hemisphere. Must be 3 or greater.</param>
    /// <param name="color">An optional color to apply to the hemisphere. If null, the default color will be white.</param>
    public void DrawHemisphere(Transform transform, float radius, int rings, int slices, Color? color = null) {
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

        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.TriangleList);
    }

    /// <summary>
    /// Draws the wireframe outline of a hemisphere using the specified transformation, radius, number of rings, and slices.
    /// </summary>
    /// <param name="transform">The transformation to apply to the hemisphere.</param>
    /// <param name="radius">The radius of the hemisphere.</param>
    /// <param name="rings">The number of horizontal subdivisions (rings) for the hemisphere.</param>
    /// <param name="slices">The number of vertical subdivisions (slices) for the hemisphere.</param>
    /// <param name="color">An optional color for the hemisphere. If null, the color defaults to white.</param>
    public void DrawHemisphereWires(Transform transform, float radius, int rings, int slices, Color? color = null) {
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
    
        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }

    /// <summary>
    /// Renders a cylinder using the specified transform, dimensions, slice count, and optional color.
    /// </summary>
    /// <param name="transform">The transform used to position and orient the cylinder in 3D space.</param>
    /// <param name="radius">The radius of the cylinder's top and bottom caps.</param>
    /// <param name="height">The height of the cylinder from bottom to top cap.</param>
    /// <param name="slices">The number of segments used to approximate the cylindrical surface. Minimum value is 3.</param>
    /// <param name="color">The optional color of the cylinder. If null, a default white color is applied.</param>
    public void DrawCylinder(Transform transform, float radius, float height, int slices, Color? color = null) {
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
    
        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.TriangleList);
    }

    /// <summary>
    /// Draws the wireframe representation of a cylinder using the specified transform, radius, height, and number of slices.
    /// </summary>
    /// <param name="transform">The transformation to apply to the cylinder, defining its position, rotation, and scale in the scene.</param>
    /// <param name="radius">The radius of the cylinder.</param>
    /// <param name="height">The height of the cylinder.</param>
    /// <param name="slices">The number of subdivisions around the cylinder's circumference. Must be at least 3.</param>
    /// <param name="color">The color of the cylinder's wires. If null, the default color is white.</param>
    public void DrawCylinderWires(Transform transform, float radius, float height, int slices, Color? color = null) {
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

        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }

    /// <summary>
    /// Renders a 3D capsule using the specified transformation, dimensions, and visual properties.
    /// </summary>
    /// <param name="transform">The transformation to apply to the capsule, including position, rotation, and scaling.</param>
    /// <param name="radius">The radius of the capsule's hemispherical ends and cylindrical body.</param>
    /// <param name="height">The total height of the capsule.</param>
    /// <param name="slices">The number of subdivisions around the circumference of the capsule. Minimum value is 3.</param>
    /// <param name="color">The color of the capsule. If null, defaults to white.</param>
    public void DrawCapsule(Transform transform, float radius, float height, int slices, Color? color = null) {
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
        float uLeft = sourceRec.X / (float) texture.Width;
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
    
        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.TriangleList);
    }

    /// <summary>
    /// Draws the wireframe of a capsule based on the specified transform, dimensions, and color.
    /// </summary>
    /// <param name="transform">The transformation applied to the capsule, such as position, rotation, and scale.</param>
    /// <param name="radius">The radius of the capsule's hemispherical ends and cylindrical body.</param>
    /// <param name="height">The height of the cylindrical body of the capsule.</param>
    /// <param name="slices">The number of divisions for rendering the capsule's rounded surface. Must be at least 3.</param>
    /// <param name="color">The optional color of the capsule wireframe. Defaults to white if not specified.</param>
    public void DrawCapsuleWires(Transform transform, float radius, float height, int slices, Color? color = null) {
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
        
        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }

    /// <summary>
    /// Draws a 3D cone using the specified transform, radius, height, and slices.
    /// </summary>
    /// <param name="transform">The transform applied to the cone in the world space.</param>
    /// <param name="radius">The radius of the base of the cone.</param>
    /// <param name="height">The height of the cone from base to apex.</param>
    /// <param name="slices">The number of slices (segments around the base) for the cone. Must be at least 3.</param>
    /// <param name="color">Optional color of the cone. Defaults to white if not specified.</param>
    public void DrawCone(Transform transform, float radius, float height, int slices, Color? color = null) {
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
        
        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.TriangleList);
    }

    /// <summary>
    /// Draws a wireframe representation of a cone in 3D space.
    /// </summary>
    /// <param name="transform">The transformation to apply to the cone, including position, rotation, and scale.</param>
    /// <param name="radius">The radius of the cone's base.</param>
    /// <param name="height">The height of the cone from the base to its tip.</param>
    /// <param name="slices">The number of slices used to approximate the circular base. Must be at least 3.</param>
    /// <param name="color">An optional color for the wireframe. Defaults to white if null.</param>
    public void DrawConeWires(Transform transform, float radius, float height, int slices, Color? color = null) {
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
    
        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }

    /// <summary>
    /// Renders a torus shape using the specified transformation, dimensions, and color.
    /// </summary>
    /// <param name="transform">The transformation to apply to the torus, including position, rotation, and scale.</param>
    /// <param name="radius">The radius of the inner circle of the torus.</param>
    /// <param name="size">The thickness of the torus.</param>
    /// <param name="radSeg">The number of segments along the radial direction of the torus. Must be 3 or greater.</param>
    /// <param name="sides">The number of sides to approximate the circular cross-section of the torus. Must be 3 or greater.</param>
    /// <param name="color">The color of the torus. If null, the default color will be white.</param>
    public void DrawTorus(Transform transform, float radius, float size, int radSeg, int sides, Color? color = null) {
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
        
        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.TriangleList);
    }

    /// <summary>
    /// Renders a wireframe torus using the specified transformation, dimensions, and color.
    /// </summary>
    /// <param name="transform">The transformation applied to the torus, including position, rotation, and scale.</param>
    /// <param name="radius">The radius of the inner circle of the torus.</param>
    /// <param name="size">The thickness of the torus.</param>
    /// <param name="radSeg">The number of segments in the radial direction. Minimum value is 3.</param>
    /// <param name="sides">The number of subdivisions of the circular cross-section. Minimum value is 3.</param>
    /// <param name="color">The optional color for rendering the torus wireframe.</param>
    public void DrawTorusWires(Transform transform, float radius, float size, int radSeg, int sides, Color? color = null) {
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
    
        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }

    /// <summary>
    /// Draws a knot shape with the specified parameters using transformations, radii, segments, and color.
    /// </summary>
    /// <param name="transform">The transformation applied to the knot.</param>
    /// <param name="radius">The overall radius of the knot.</param>
    /// <param name="tubeRadius">The radius of the tube forming the knot.</param>
    /// <param name="radSeg">The number of segments along the radial direction. Minimum value is 3.</param>
    /// <param name="sides">The number of sides of the tube forming the knot. Minimum value is 3.</param>
    /// <param name="color">An optional color for the knot. Defaults to white if null.</param>
    public void DrawKnot(Transform transform, float radius, float tubeRadius, int radSeg, int sides, Color? color = null) {
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
        
        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.TriangleList);
    }

    /// <summary>
    /// Renders the wireframe of a knot shape using the specified transformation, radii, and segments.
    /// </summary>
    /// <param name="transform">The transformation applied to the knot wireframe.</param>
    /// <param name="radius">The radius of the knot.</param>
    /// <param name="tubeRadius">The radius of the tube comprising the knot's wireframe.</param>
    /// <param name="radSeg">The number of radial segments making up the knot. Minimum value is 3.</param>
    /// <param name="sides">The number of sides for the tube cross-section. Minimum value is 3.</param>
    /// <param name="color">An optional color to use for the wireframe; if null, the default is white.</param>
    public void DrawKnotWires(Transform transform, float radius, float tubeRadius, int radSeg, int sides, Color? color = null) {
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
    
        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }
    
    /// <summary>
    /// Renders a grid using the specified transformation matrix, slice count, spacing, and optional color.
    /// </summary>
    /// <param name="transform">The transformation applied to the grid.</param>
    /// <param name="slices">The number of divisions (slices) in the grid. Must be greater than or equal to 1.</param>
    /// <param name="spacing">The distance (spacing) between each grid line. Must be greater than or equal to 1.</param>
    /// <param name="color">An optional color for the grid lines. Defaults to white if not specified.</param>
    public void DrawGird(Transform transform, int slices, int spacing, Color? color = null) {
        Color finalColor = color ?? Color.White;
        
        if (spacing < 1) {
            spacing = 1;
        }
        
        if (slices < 1) {
            slices = 1;
        }
        
        float halfSize = slices * spacing * 0.5f;
    
        for (int i = 0; i <= slices; i++) {
            float offset = -halfSize + i * spacing;
            
            // Draw lines along the X axis.
            this._tempVertices.Add(new ImmediateVertex3D {
                Position = new Vector3(offset, 0, -halfSize),
                Color = finalColor.ToRgbaFloatVec4()
            });
            
            this._tempVertices.Add(new ImmediateVertex3D {
                Position = new Vector3(offset, 0, halfSize),
                Color = finalColor.ToRgbaFloatVec4()
            });
            
            // Draw lines along the Z axis.
            this._tempVertices.Add(new ImmediateVertex3D {
                Position = new Vector3(-halfSize, 0, offset),
                Color = finalColor.ToRgbaFloatVec4()
            });
            
            this._tempVertices.Add(new ImmediateVertex3D {
                Position = new Vector3(halfSize, 0, offset),
                Color = finalColor.ToRgbaFloatVec4()
            });
            
            // Add indices for line pairs along X and Z.
            this._tempIndices.Add((uint) (i * 4 + 0));
            this._tempIndices.Add((uint) (i * 4 + 1));
            this._tempIndices.Add((uint) (i * 4 + 2));
            this._tempIndices.Add((uint) (i * 4 + 3));
        }
        
        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }

    /// <summary>
    /// Draws the edges of the specified bounding box using the given transformation and color.
    /// </summary>
    /// <param name="transform">The transformation to apply to the bounding box before rendering.</param>
    /// <param name="box">The bounding box to be drawn.</param>
    /// <param name="color">The color to use for the bounding box edges. If null, white is used as the default color.</param>
    public void DrawBoundingBox(Transform transform, BoundingBox box, Color? color = null) {
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

        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }

    /// <summary>
    /// Draws a line between two points in 3D space with an optional color.
    /// </summary>
    /// <param name="startPos">The starting position of the line in 3D space.</param>
    /// <param name="endPos">The ending position of the line in 3D space.</param>
    /// <param name="color">The optional color of the line. If null, the default color is white.</param>
    public void DrawLine(Vector3 startPos, Vector3 endPos, Color? color = null) {
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
        
        this.DrawVertices(new Transform(), this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
    }

    /// <summary>
    /// Renders a billboard at the specified position with optional scaling and color.
    /// </summary>
    /// <param name="position">The world position where the billboard will be rendered.</param>
    /// <param name="scale">The optional scaling factor for the billboard's size. Defaults to a uniform scale of 1 if not provided.</param>
    /// <param name="color">The optional color to be applied to the billboard. Defaults to white if not provided.</param>
    public void DrawBillboard(Vector3 position, Vector2? scale = null, Color? color = null) {
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
    
        this.DrawVertices(billboardTransform, this._tempVertices, this._tempIndices, PrimitiveTopology.TriangleList);
    }
    
    /// <summary>
    /// Draws a set of vertices using the specified transformation, vertex data, indices, and topology.
    /// </summary>
    /// <param name="transform">The transformation matrix to apply to the vertices.</param>
    /// <param name="vertices">The list of vertices to be rendered.</param>
    /// <param name="indices">The list of indices defining the order in which the vertices are connected.</param>
    /// <param name="topology">The primitive topology specifying how the vertices are interpreted (e.g., triangle list, line strip).</param>
    public void DrawVertices(Transform transform, List<ImmediateVertex3D> vertices, List<uint> indices, PrimitiveTopology topology) {
        if (!this._begun) {
            throw new Exception("You must begin the ImmediateRenderer before calling draw methods!");
        }
        
        Cam3D? cam3D = Cam3D.ActiveCamera;

        if (cam3D == null) {
            // Clear temp data.
            this._tempVertices.Clear();
            this._tempIndices.Clear();
            return;
        }
        
        if (vertices.Count > this.Capacity) {
            Logger.Fatal(new InvalidOperationException($"The number of provided vertices exceeds the maximum batch size! [{vertices.Count} > {this.Capacity}]"));
        }

        if (indices.Count > this.Capacity * 3) {
            Logger.Fatal(new InvalidOperationException($"The number of provided indices exceeds the maximum batch size! [{indices.Count} > {this.Capacity * 3}]"));
        }
        
        // Add vertices.
        for (int i = 0; i < vertices.Count; i++) {
            this._vertices[i] = vertices[i];
        }
        
        // Add indices.
        for (int i = 0; i < indices.Count; i++) {
            this._indices[i] = indices[i];
        }

        // Set vertices and indices count.
        this._vertexCount = vertices.Count;
        this._indexCount = indices.Count;
        
        // Update matrix buffer.
        this._matrixBuffer.SetValue(0, cam3D.GetProjection());
        this._matrixBuffer.SetValue(1, cam3D.GetView());
        this._matrixBuffer.SetValue(2, transform.GetTransform());
        this._matrixBuffer.UpdateBuffer(this._currentCommandList);
        
        // Update topology.
        this._pipelineDescription.PrimitiveTopology = topology;
        
        if (this._indexCount > 0) {
            
            // Update vertex and index buffer.
            this._currentCommandList.UpdateBuffer(this._vertexBuffer, 0, new ReadOnlySpan<ImmediateVertex3D>(this._vertices, 0, this._vertexCount));
            this._currentCommandList.UpdateBuffer(this._indexBuffer, 0, new ReadOnlySpan<uint>(this._indices, 0, this._indexCount));
            
            // Set vertex and index buffer.
            this._currentCommandList.SetVertexBuffer(0, this._vertexBuffer);
            this._currentCommandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt32);

            // Set pipeline.
            this._currentCommandList.SetPipeline(this._currentEffect.GetPipeline(this._pipelineDescription).Pipeline);
        
            // Set matrix buffer.
            this._currentCommandList.SetGraphicsResourceSet(0, this._matrixBuffer.GetResourceSet(this._currentEffect.GetBufferLayout("MatrixBuffer")));

            // Set resourceSet of the texture.
            this._currentCommandList.SetGraphicsResourceSet(1, this._currentTexture.GetResourceSet(this._currentSampler, this._currentEffect.GetTextureLayout("fTexture")));
            
            // Apply effect.
            this._currentEffect.Apply();
            
            // Draw.
            this._currentCommandList.DrawIndexed((uint) this._indexCount);
        }
        else {
            
            // Update vertex buffer.
            this._currentCommandList.UpdateBuffer(this._vertexBuffer, 0, new ReadOnlySpan<ImmediateVertex3D>(this._vertices, 0, this._vertexCount));
            
            // Set vertex buffer.
            this._currentCommandList.SetVertexBuffer(0, this._vertexBuffer);

            // Set pipeline.
            this._currentCommandList.SetPipeline(this._currentEffect.GetPipeline(this._pipelineDescription).Pipeline);
        
            // Set matrix buffer.
            this._currentCommandList.SetGraphicsResourceSet(0, this._matrixBuffer.GetResourceSet(this._currentEffect.GetBufferLayout("MatrixBuffer")));
        
            // Set resourceSet of the texture.
            this._currentCommandList.SetGraphicsResourceSet(1, this._currentTexture.GetResourceSet(this._currentSampler, this._currentEffect.GetTextureLayout("fTexture")));
            
            // Apply effect.
            this._currentEffect.Apply();
            
            // Draw.
            this._currentCommandList.Draw((uint) this._vertexCount);
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
    
    /// <summary>
    /// Creates a new pipeline description used for configuring the graphics pipeline.
    /// </summary>
    /// <returns>A <see cref="SimplePipelineDescription"/> configured with depth/stencil, rasterizer, topology, and shader settings.</returns>
    private SimplePipelineDescription CreatePipelineDescription() {
        return new SimplePipelineDescription() {
            DepthStencilState = new DepthStencilStateDescription(true, true, ComparisonKind.LessEqual),
            RasterizerState = new RasterizerStateDescription() {
                CullMode = FaceCullMode.Back,
                FillMode = PolygonFillMode.Solid,
                FrontFace = FrontFace.Clockwise,
                DepthClipEnabled = true,
                ScissorTestEnabled = false
            }
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