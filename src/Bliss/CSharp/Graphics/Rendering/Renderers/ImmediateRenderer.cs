using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Effects;
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
    /// Gets the output description used for rendering.
    /// </summary>
    public OutputDescription Output { get; private set; }
    
    /// <summary>
    /// Gets the effect (shader) used for rendering.
    /// </summary>
    public Effect Effect { get; private set; }
    
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
    /// The currently bound texture.
    /// </summary>
    private Texture2D _currentTexture;
    
    /// <summary>
    /// The currently active sampler.
    /// </summary>
    private Sampler _currentSampler;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ImmediateRenderer"/> class with the specified graphics device, output, effect, and capacity.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering.</param>
    /// <param name="output">The output description for rendering.</param>
    /// <param name="effect">An optional effect (shader) to use; if null, the default immediate renderer effect is used.</param>
    /// <param name="capacity">The maximum number of vertices that can be batched. Defaults to 30720.</param>
    public ImmediateRenderer(GraphicsDevice graphicsDevice, OutputDescription output, Effect? effect = null, uint capacity = 30720) {
        this.GraphicsDevice = graphicsDevice;
        this.Output = output;
        this.Effect = effect ?? GlobalResource.ImmediateRendererEffect;
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
    }

    /// <summary>
    /// Begins the rendering process by setting up the command list and pipeline state.
    /// </summary>
    /// <param name="commandList">The command list used for recording rendering commands.</param>
    /// <param name="blendState">An optional blend state; if null, the default alpha blend state is used.</param>
    /// <exception cref="Exception">Thrown if the renderer has already begun.</exception>
    public void Begin(CommandList commandList, BlendState? blendState = null) {
        if (this._begun) {
            throw new Exception("The ImmediateRenderer has already begun!");
        }
        
        this._begun = true;
        this._currentCommandList = commandList;
        
        // Update BlendState.
        this._pipelineDescription.BlendState = blendState?.Description ?? BlendState.AlphaBlend.Description;
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
    /// Sets the current texture and sampler to be used for rendering.
    /// </summary>
    /// <param name="texture">The texture to use; if null, the default immediate renderer texture is used.</param>
    /// <param name="sampler">The sampler to use; if null, a default point sampler is used.</param>
    public void SetTexture(Texture2D? texture, Sampler? sampler = null) {
        this._currentTexture = texture ?? GlobalResource.DefaultImmediateRendererTexture;
        this._currentSampler = sampler ?? GraphicsHelper.GetSampler(this.GraphicsDevice, SamplerType.Point);
    }
    
    /// <summary>
    /// Draws a cube with the specified transformation, size, and optional color.
    /// </summary>
    /// <param name="transform">The transformation to be applied to the cube.</param>
    /// <param name="size">The size of the cube in 3D space.</param>
    /// <param name="color">An optional color for the cube; if null, white is used.</param>
    public void DrawCube(Transform transform, Vector3 size, Color? color = null) {
        Color finalColor = color ?? Color.White;
        
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
                    + ((corner == 0 || corner == 3) ? -tangent : tangent)
                    + ((corner == 0 || corner == 1) ? -bitangent : bitangent);

                // Assign texture coordinates for the current corner.
                Vector2 texCoord = corner switch {
                    0 => new Vector2(0.0F, 0.0F),
                    1 => new Vector2(1.0F, 0.0F),
                    2 => new Vector2(1.0F, 1.0F),
                    3 => new Vector2(0.0F, 1.0F),
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
        
        if (rings < 3) {
            rings = 3;
        }
        
        if (slices < 3) {
            slices = 3;
        }
    
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
                    TexCoords = new Vector2(slice / (float) slices, ring / (float) rings),
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
        
        if (rings < 3) {
            rings = 3;
        }
        
        if (slices < 3) {
            slices = 3;
        }
        
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
                        0.5F + cosPhi * sinTheta * 0.5F,
                        0.5F + sinPhi * sinTheta * 0.5F
                    ),
                    Color = finalColor.ToRgbaFloatVec4()
                });
            }
        }

        int baseCenterIndex = this._tempVertices.Count;

        // Add center vertex for the circular base.
        this._tempVertices.Add(new ImmediateVertex3D() {
            Position = new Vector3(0.0F, -halfHeight, 0.0F),
            TexCoords = new Vector2(0.5F, 0.5F),
            Color = finalColor.ToRgbaFloatVec4()
        });

        // Generate vertices for the base circle.
        for (int slice = 0; slice <= slices; slice++) {
            float sliceAngle = MathF.PI * 2.0F * slice / slices;

            float x = MathF.Cos(sliceAngle) * (radius / 2.0F);
            float z = MathF.Sin(sliceAngle) * (radius / 2.0F);

            this._tempVertices.Add(new ImmediateVertex3D() {
                Position = new Vector3(x, -halfHeight, z),
                TexCoords = new Vector2(0.5F + x / radius, 0.5F + z / radius),
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
    
    public void DrawCylinder() {
        throw new NotImplementedException("DrawCylinder is not implemented yet.");
    }
    
    public void DrawCylinderWires() {
        throw new NotImplementedException("DrawCylinderWires is not implemented yet.");
    }
    
    public void DrawCapsule() {
        throw new NotImplementedException("DrawCapsule is not implemented yet.");
    }
    
    public void DrawCapsuleWires() {
        throw new NotImplementedException("DrawCapsuleWires is not implemented yet.");
    }
    
    public void DrawBoundingBox() {
        throw new NotImplementedException("DrawBoundingBox is not implemented yet.");
    }
    
    public void DrawBillboard() { // TODO: Do for the texture a Rectangle: source (For example if you want to play a GIF yu will need that.)
        throw new NotImplementedException("DrawBillboard is not implemented yet.");
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

        // Set transform.
        Transform transform = new Transform() {
            Translation = Vector3.Zero
        };
        
        this.DrawVertices(transform, this._tempVertices, this._tempIndices, PrimitiveTopology.LineList);
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
            this._currentCommandList.SetPipeline(this.Effect.GetPipeline(this._pipelineDescription).Pipeline);
        
            // Set matrix buffer.
            this._currentCommandList.SetGraphicsResourceSet(0, this._matrixBuffer.GetResourceSet(this.Effect.GetBufferLayout("MatrixBuffer")));

            // Set resourceSet of the texture.
            this._currentCommandList.SetGraphicsResourceSet(1, this._currentTexture.GetResourceSet(this._currentSampler, this.Effect.GetTextureLayout("fTexture")));
            
            // Draw.
            this._currentCommandList.DrawIndexed((uint) this._indexCount);
        }
        else {
            
            // Update vertex buffer.
            this._currentCommandList.UpdateBuffer(this._vertexBuffer, 0, new ReadOnlySpan<ImmediateVertex3D>(this._vertices, 0, this._vertexCount));
            
            // Set vertex buffer.
            this._currentCommandList.SetVertexBuffer(0, this._vertexBuffer);

            // Set pipeline.
            this._currentCommandList.SetPipeline(this.Effect.GetPipeline(this._pipelineDescription).Pipeline);
        
            // Set matrix buffer.
            this._currentCommandList.SetGraphicsResourceSet(0, this._matrixBuffer.GetResourceSet(this.Effect.GetBufferLayout("MatrixBuffer")));
        
            // Set resourceSet of the texture.
            this._currentCommandList.SetGraphicsResourceSet(1, this._currentTexture.GetResourceSet(this._currentSampler, this.Effect.GetTextureLayout("fTexture")));
            
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
            },
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