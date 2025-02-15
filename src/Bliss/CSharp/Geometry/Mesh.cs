using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Geometry.Animations;
using Bliss.CSharp.Graphics;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Images;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Materials;
using Bliss.CSharp.Transformations;
using Veldrid;
using Color = Bliss.CSharp.Colors.Color;
using Material = Bliss.CSharp.Materials.Material;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace Bliss.CSharp.Geometry;

public class Mesh : Disposable {
    
    /// <summary>
    /// Represents the graphics device used for rendering operations.
    /// This property provides access to the underlying GraphicsDevice instance responsible for managing GPU resources and executing rendering commands.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }

    /// <summary>
    /// Represents the material properties used for rendering the mesh.
    /// This may include shaders (effects), texture mappings, blending states, and other rendering parameters.
    /// The Material controls how the mesh is rendered within the graphics pipeline.
    /// </summary>
    public Material Material { get; private set; }
    
    /// <summary>
    /// An array of Vertex3D structures that define the geometric points of a mesh.
    /// Each vertex contains attributes such as position, texture coordinates, normal, and optional color.
    /// Vertices are used to construct the shape and appearance of a 3D model.
    /// </summary>
    public Vertex3D[] Vertices { get; private set; }

    /// <summary>
    /// An array of indices that define the order in which vertices are drawn.
    /// Indices are used in conjunction with the vertex array to form geometric shapes
    /// such as triangles in a mesh. This allows for efficient reuse of vertex data.
    /// </summary>
    public uint[] Indices { get; private set; }

    /// <summary>
    /// An array containing information about each bone in a mesh, used for skeletal animation.
    /// Each element provides details such as the bone's name, its identifier, and its transformation matrix.
    /// </summary>
    public Dictionary<string, Dictionary<int, BoneInfo[]>> BoneInfos { get; private set; }

    /// <summary>
    /// The axis-aligned bounding box (AABB) for the mesh.
    /// This bounding box is calculated based on the vertices of the mesh and represents
    /// the minimum and maximum coordinates that encompass the entire mesh.
    /// </summary>
    public BoundingBox BoundingBox { get; private set; }

    /// <summary>
    /// The total count of vertices present in the mesh.
    /// This value determines the number of vertices available for rendering within the mesh.
    /// </summary>
    public uint VertexCount { get; private set; }

    /// <summary>
    /// The number of indices in the mesh used for rendering.
    /// </summary>
    public uint IndexCount { get; private set; }

    /// <summary>
    /// A buffer that stores vertex data used for rendering in the graphics pipeline.
    /// </summary>
    private DeviceBuffer _vertexBuffer;

    /// <summary>
    /// A buffer that stores index data used for indexed drawing in the graphics pipeline.
    /// </summary>
    private DeviceBuffer _indexBuffer;

    /// <summary>
    /// A buffer that stores model matrix data for shader usage in rendering.
    /// </summary>
    private SimpleBuffer<Matrix4x4> _modelMatrixBuffer;

    /// <summary>
    /// A buffer that stores bone transformation data used for skeletal animation.
    /// </summary>
    private SimpleBuffer<Matrix4x4> _boneBuffer;

    /// <summary>
    /// A buffer that stores color data as a collection of 4D vectors (Vector4) for rendering purposes.
    /// </summary>
    private SimpleBuffer<Vector4> _colorBuffer;

    /// <summary>
    /// Represents a GPU buffer that stores floating-point values for use in rendering or animation systems.
    /// </summary>
    private SimpleBuffer<float> _valueBuffer;
    
    /// <summary>
    /// Defines the characteristics of the rendering pipeline used by the mesh.
    /// This field specifies the pipeline configurations such as blending, depth stencil, rasterizer state,
    /// primitive topology, associated buffers, texture layouts, shader set, and output descriptions.
    /// </summary>
    private SimplePipelineDescription _pipelineDescription;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Mesh"/> class with the specified properties.
    /// </summary>
    /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/> used for managing GPU resources.</param>
    /// <param name="material">The material applied to the mesh, which defines its appearance (textures, shaders, etc.).</param>
    /// <param name="vertices">An optional array of <see cref="Vertex3D"/> instances representing the mesh's vertices.</param>
    /// <param name="indices">An optional array of indices used for defining the mesh's primitive topology.</param>
    /// <param name="boneInfos">An optional dictionary containing bone information used for skeletal animation, where the key is a bone name and the value is a dictionary of bone indices mapped to bone data.</param>
    public Mesh(GraphicsDevice graphicsDevice, Material material, Vertex3D[]? vertices = null, uint[]? indices = null, Dictionary<string, Dictionary<int, BoneInfo[]>>? boneInfos = null) {
        this.GraphicsDevice = graphicsDevice;
        this.Material = material;
        this.Vertices = vertices ?? [];
        this.Indices = indices ?? [];
        this.BoneInfos = boneInfos ?? new Dictionary<string, Dictionary<int, BoneInfo[]>>();
        this.BoundingBox = this.GenerateBoundingBox();

        this.VertexCount = (uint) this.Vertices.Length;
        this.IndexCount = (uint) this.Indices.Length;
        
        uint vertexBufferSize = this.VertexCount * (uint) Marshal.SizeOf<Vertex3D>();
        uint indexBufferSize = this.IndexCount * sizeof(uint);
        
        // Create vertex buffer.
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        graphicsDevice.UpdateBuffer(this._vertexBuffer, 0, this.Vertices);

        // Create index buffer.
        this._indexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(indexBufferSize, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        graphicsDevice.UpdateBuffer(this._indexBuffer, 0, this.Indices);
        
        // Create model matrix buffer.
        this._modelMatrixBuffer = new SimpleBuffer<Matrix4x4>(graphicsDevice, 3, SimpleBufferType.Uniform, ShaderStages.Vertex);
        
        // Create bone buffer.
        this._boneBuffer = new SimpleBuffer<Matrix4x4>(graphicsDevice, 128, SimpleBufferType.Uniform, ShaderStages.Vertex);

        for (int i = 0; i < 128; i++) {
            this._boneBuffer.SetValue(i, Matrix4x4.Identity);
        }
        
        this._boneBuffer.UpdateBufferImmediate();
        
        // Create color buffer.
        this._colorBuffer = new SimpleBuffer<Vector4>(graphicsDevice, 7, SimpleBufferType.Uniform, ShaderStages.Fragment);

        for (int i = 0; i < 7; i++) {
            this._colorBuffer.SetValue(i, Color.White.ToRgbaFloatVec4());
        }
        
        this._colorBuffer.UpdateBufferImmediate();
        
        // Create value buffer.
        this._valueBuffer = new SimpleBuffer<float>(graphicsDevice, 7, SimpleBufferType.Uniform, ShaderStages.Fragment);

        // Create pipeline description.
        this._pipelineDescription = this.CreatePipelineDescription();
    }
    
    /// <summary>
    /// Generates a 3D polygon mesh with the specified number of sides and radius.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to manage GPU resources for the generated mesh.</param>
    /// <param name="sides">The number of sides of the polygon. Must be at least 3.</param>
    /// <param name="radius">The radius of the polygon.</param>
    /// <returns>A new instance of the <see cref="Mesh"/> class representing the 3D polygon.</returns>
    public static Mesh GenPoly(GraphicsDevice graphicsDevice, int sides, float radius) {
        if (sides < 3) {
            sides = 3;
            Logger.Warn("The number of sides must be at least 3. The value is now set to 3.");
        }
    
        List<Vertex3D> vertices = new List<Vertex3D>();
        List<uint> indices = new List<uint>();
    
        // Center vertex.
        vertices.Add(new Vertex3D {
            Position = new Vector3(0, 0, 0),
            Normal = Vector3.UnitY,
            TexCoords = new Vector2(0.5F, 0.5F)
        });
        
        // Generate vertices for the outer circle.
        for (int i = 0; i < sides; i++) {
            float angle = i * MathF.Tau / sides;
            float x = MathF.Cos(angle) * radius / 2.0F;
            float z = MathF.Sin(angle) * radius / 2.0F;
    
            vertices.Add(new Vertex3D {
                Position = new Vector3(x, 0, z),
                Normal = Vector3.UnitY,
                TexCoords = new Vector2(x / radius + 0.5F, z / radius + 0.5F)
            });
        }
        
        // Generate indices.
        for (uint i = 1; i <= sides; i++) {
            indices.Add(0);
            indices.Add(i);
            indices.Add((uint) (i % sides + 1));
        }
    
        Material material = new Material(graphicsDevice, GlobalResource.DefaultModelEffect);
    
        material.AddMaterialMap(MaterialMapType.Albedo.GetName(), new MaterialMap {
            Texture = GlobalResource.DefaultModelTexture,
            Color = Color.White
        });
    
        return new Mesh(graphicsDevice, material, vertices.ToArray(), indices.ToArray());
    }

    /// <summary>
    /// Generates a cuboid mesh with the specified dimensions and initializes it with default material and texture settings.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device that manages GPU resources for the mesh.</param>
    /// <param name="width">The width of the cuboid.</param>
    /// <param name="height">The height of the cuboid.</param>
    /// <param name="length">The length of the cuboid.</param>
    /// <returns>A new instance of the <see cref="Mesh"/> class representing the cuboid.</returns>
    public static Mesh GenCube(GraphicsDevice graphicsDevice, float width, float height, float length) {
        Vector3[] normals = [
            new Vector3(0.0F, 0.0F, -1.0F), new Vector3(0.0F, 0.0F, 1.0F),
            new Vector3(-1.0F, 0.0F, 0.0F), new Vector3(1.0F, 0.0F, 0.0F),
            new Vector3(0.0F, 1.0F, 0.0F), new Vector3(0.0F, -1.0F, 0.0F)
        ];
    
        Vector2[] texCoords = [
            new Vector2(0.0F, 1.0F), new Vector2(1.0F, 1.0F),
            new Vector2(1.0F, 0.0F), new Vector2(0.0F, 0.0F)
        ];
    
        Vector3[] positions = [
            // Front face
            new Vector3(-1.0F, -1.0F, -1.0F), new Vector3(1.0F, -1.0F, -1.0F), new Vector3(1.0F, 1.0F, -1.0F), new Vector3(-1.0F, 1.0F, -1.0F),
            // Back face
            new Vector3(1.0F, -1.0F, 1.0F), new Vector3(-1.0F, -1.0F, 1.0F), new Vector3(-1.0F, 1.0F, 1.0F), new Vector3(1.0F, 1.0F, 1.0F),
            // Left face
            new Vector3(-1.0F, -1.0F, 1.0F), new Vector3(-1.0F, -1.0F, -1.0F), new Vector3(-1.0F, 1.0F, -1.0F), new Vector3(-1.0F, 1.0F, 1.0F),
            // Right face
            new Vector3(1.0F, -1.0F, -1.0F), new Vector3(1.0F, -1.0F, 1.0F), new Vector3(1.0F, 1.0F, 1.0F), new Vector3(1.0F, 1.0F, -1.0F),
            // Top face
            new Vector3(-1.0F, 1.0F, -1.0F), new Vector3(1.0F, 1.0F, -1.0F), new Vector3(1.0F, 1.0F, 1.0F), new Vector3(-1.0F, 1.0F, 1.0F),
            // Bottom face
            new Vector3(-1.0F, -1.0F, 1.0F), new Vector3(1.0F, -1.0F, 1.0F), new Vector3(1.0F, -1.0F, -1.0F), new Vector3(-1.0F, -1.0F, -1.0F)
        ];
        
        Vertex3D[] vertices = new Vertex3D[24];
        
        for (int i = 0; i < 6; i++) {
            for (int j = 0; j < 4; j++) {
                int index = i * 4 + j;
                vertices[index] = new Vertex3D() {
                    Position = positions[index] * new Vector3(width / 2.0F, height / 2.0F, length / 2.0F),
                    TexCoords = texCoords[j],
                    Normal = normals[i]
                };
            }
        }
    
        uint[] indices = [
            // Front face
            0, 1, 2,
            2, 3, 0,
    
            // Back face
            4, 5, 6,
            6, 7, 4,
    
            // Left face
            8, 9, 10,
            10, 11, 8,
    
            // Right face
            12, 13, 14,
            14, 15, 12,
    
            // Top face
            16, 17, 18,
            18, 19, 16,
    
            // Bottom face
            20, 21, 22,
            22, 23, 20
        ];
    
        Material material = new Material(graphicsDevice, GlobalResource.DefaultModelEffect);
    
        material.AddMaterialMap(MaterialMapType.Albedo.GetName(), new MaterialMap() {
            Texture = GlobalResource.DefaultModelTexture,
            Color = Color.White
        });
    
        return new Mesh(graphicsDevice, material, vertices, indices);
    }

    /// <summary>
    /// Generates a sphere mesh with the specified radius, rings, and slices.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create and manage the GPU resources needed for the mesh.</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <param name="rings">The number of horizontal segments dividing the sphere.</param>
    /// <param name="slices">The number of vertical segments dividing the sphere.</param>
    /// <returns>A new instance of the <see cref="Mesh"/> class representing the generated sphere.</returns>
    public static Mesh GenSphere(GraphicsDevice graphicsDevice, float radius, int rings, int slices) {
        if (rings < 3) {
            rings = 3;
            Logger.Warn("The number of rings must be at least 3. The value is now set to 3.");
        }
        
        if (slices < 3) {
            slices = 3;
            Logger.Warn("The number of slices must be at least 3. The value is now set to 3.");
        }
        
        Vector3[] positions = new Vector3[(rings + 1) * (slices + 1)];
        Vector2[] texCoords = new Vector2[(rings + 1) * (slices + 1)];
        Vector3[] normals = new Vector3[(rings + 1) * (slices + 1)];
        Vertex3D[] vertices = new Vertex3D[(rings + 1) * (slices + 1)];
        uint[] indices = new uint[rings * slices * 6];

        for (int ring = 0; ring <= rings; ring++) {
            float theta = ring * MathF.PI / rings;
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);

            for (int slice = 0; slice <= slices; slice++) {
                float phi = slice * 2.0F * MathF.PI / slices;
                float sinPhi = MathF.Sin(phi);
                float cosPhi = MathF.Cos(phi);

                int index = ring * (slices + 1) + slice;

                Vector3 position = new Vector3(
                    radius / 2.0F * sinTheta * cosPhi,
                    radius / 2.0F * cosTheta,
                    radius / 2.0F * sinTheta * sinPhi
                );

                positions[index] = position;
                texCoords[index] = new Vector2((float) slice / slices, (float) ring / rings);
                normals[index] = Vector3.Normalize(position);

                vertices[index] = new Vertex3D() {
                    Position = positions[index],
                    TexCoords = texCoords[index],
                    Normal = normals[index]
                };
            }
        }

        int counter = 0;

        for (int ring = 0; ring < rings; ring++) {
            for (int slice = 0; slice < slices; slice++) {
                int first = ring * (slices + 1) + slice;
                int second = first + slices + 1;

                indices[counter++] = (uint) first;
                indices[counter++] = (uint) second;
                indices[counter++] = (uint) (first + 1);
 
                indices[counter++] = (uint) second;
                indices[counter++] = (uint) (second + 1);
                indices[counter++] = (uint) (first + 1);
            }
        }

        Material material = new Material(graphicsDevice, GlobalResource.DefaultModelEffect);

        material.AddMaterialMap(MaterialMapType.Albedo.GetName(), new MaterialMap() {
            Texture = GlobalResource.DefaultModelTexture,
            Color = Color.White
        });

        return new Mesh(graphicsDevice, material, vertices, indices);
    }

    /// <summary>
    /// Generates a 3D hemisphere mesh with a specified radius, number of rings, and slices.
    /// </summary>
    /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/> instance used to allocate GPU resources for the mesh.</param>
    /// <param name="radius">The radius of the hemisphere.</param>
    /// <param name="rings">The number of subdivisions along the vertical (Y-axis) direction of the hemisphere.</param>
    /// <param name="slices">The number of subdivisions around the horizontal (XZ-plane) direction of the hemisphere.</param>
    /// <returns>A new instance of the <see cref="Mesh"/> class representing the generated hemisphere model.</returns>
    public static Mesh GenHemisphere(GraphicsDevice graphicsDevice, float radius, int rings, int slices) {
        if (rings < 3) {
            rings = 3;
            Logger.Warn("The number of rings must be at least 3. The value is now set to 3.");
        }
        
        if (slices < 3) {
            slices = 3;
            Logger.Warn("The number of slices must be at least 3. The value is now set to 3.");
        }
    
        List<Vertex3D> vertices = new List<Vertex3D>();
        List<uint> indices = new List<uint>();
        
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

                vertices.Add(new Vertex3D() {
                    Position = position,
                    TexCoords = new Vector2(0.5F + cosPhi * sinTheta * 0.5F, 0.5F + sinPhi * sinTheta * 0.5F),
                    Normal = Vector3.Normalize(position)
                });
            }
        }
    
        // Generate indices for the hemisphere.
        for (int ring = 0; ring < rings / 2; ring++) {
            for (int slice = 0; slice < slices; slice++) {
                int first = ring * (slices + 1) + slice;
                int second = first + slices + 1;
    
                indices.Add((uint) first);
                indices.Add((uint) second);
                indices.Add((uint) (first + 1));
                
                indices.Add((uint) second);
                indices.Add((uint) (second + 1));
                indices.Add((uint) (first + 1));
            }
        }
    
        // Add center point for the circle.
        int centerIndex = vertices.Count;
        vertices.Add(new Vertex3D() {
            Position = new Vector3(0.0F, -halfHeight, 0.0F),
            TexCoords = new Vector2(0.5F, 0.5F),
            Normal = Vector3.UnitY
        });
        
        // Add circle vertices.
        for (int slice = 0; slice <= slices; slice++) {
            float phi = slice * 2.0F * MathF.PI / slices;
            float sinPhi = MathF.Sin(phi);
            float cosPhi = MathF.Cos(phi);
            
            vertices.Add(new Vertex3D() {
                Position = new Vector3(radius / 2.0F * cosPhi, -halfHeight, radius / 2.0F * sinPhi),
                TexCoords = new Vector2(0.5F + cosPhi * 0.5F, 0.5F + sinPhi * 0.5F),
                Normal = Vector3.UnitY
            });
        }
    
        // Generate indices for the circle
        for (int slice = 0; slice < slices; slice++) {
            indices.Add((uint) centerIndex);
            indices.Add((uint) (centerIndex + slice + 2));
            indices.Add((uint) (centerIndex + slice + 1));
        }
    
        Material material = new Material(graphicsDevice, GlobalResource.DefaultModelEffect);
    
        material.AddMaterialMap(MaterialMapType.Albedo.GetName(), new MaterialMap() {
            Texture = GlobalResource.DefaultModelTexture,
            Color = Color.White
        });
    
        return new Mesh(graphicsDevice, material, vertices.ToArray(), indices.ToArray());
    }

    /// <summary>
    /// Generates a cylindrical mesh with specified dimensions and resolution.
    /// </summary>
    /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/> responsible for managing GPU resources.</param>
    /// <param name="radius">The radius of the cylinder's base and top.</param>
    /// <param name="height">The height of the cylinder.</param>
    /// <param name="slices">The number of slices (or segments) used to approximate the cylinder. Must be at least 3.</param>
    /// <returns>A new instance of <see cref="Mesh"/> representing the generated cylindrical geometry.</returns>
    public static Mesh GenCylinder(GraphicsDevice graphicsDevice, float radius, float height, int slices) {
        if (slices < 3) {
            slices = 3;
            Logger.Warn("The number of slices must be at least 3. The value is now set to 3.");
        }
    
        List<Vertex3D> vertices = new List<Vertex3D>();
        List<uint> indices = new List<uint>();
    
        float halfHeight = height / 2.0F;
    
        // Generate the side vertices.
        for (int slice = 0; slice <= slices; slice++) {
            float angle = slice * MathF.Tau / slices;
            float x = MathF.Cos(angle) * (radius / 2.0F);
            float z = MathF.Sin(angle) * (radius / 2.0F);
            float u = (float) slice / slices;
    
            // Bottom vertex.
            vertices.Add(new Vertex3D {
                Position = new Vector3(x, -halfHeight, z),
                Normal = Vector3.Normalize(new Vector3(x, 0.0F, z)),
                TexCoords = new Vector2(u, 1.0F)
            });
    
            // Top vertex.
            vertices.Add(new Vertex3D {
                Position = new Vector3(x, halfHeight, z),
                Normal = Vector3.Normalize(new Vector3(x, 0.0F, z)),
                TexCoords = new Vector2(u, 0.0F)
            });
        }
    
        // Generate the side indices.
        for (int slice = 0; slice < slices; slice++) {
            int baseIndex = slice * 2;
            int nextIndex = (slice + 1) * 2;
            
            indices.Add((uint) (baseIndex + 1));
            indices.Add((uint) baseIndex);
            indices.Add((uint) nextIndex);
            
            indices.Add((uint) (baseIndex + 1));
            indices.Add((uint) nextIndex);
            indices.Add((uint) (nextIndex + 1));
        }
    
        // Generate the bottom cap.
        int bottomCenterIndex = vertices.Count;
        
        vertices.Add(new Vertex3D {
            Position = new Vector3(0, -halfHeight, 0),
            Normal = -Vector3.UnitY,
            TexCoords = new Vector2(0.5F, 0.5F)
        });
    
        for (int slice = 0; slice < slices; slice++) {
            int baseIndex = slice * 2;
    
            indices.Add((uint) bottomCenterIndex);
            indices.Add((uint) (baseIndex + 2));
            indices.Add((uint) baseIndex);
        }
    
        // Generate the top cap.
        int topCenterIndex = vertices.Count;
        
        vertices.Add(new Vertex3D {
            Position = new Vector3(0.0F, halfHeight, 0.0F),
            Normal = Vector3.UnitY,
            TexCoords = new Vector2(0.5F, 0.5F)
        });
    
        for (int slice = 0; slice < slices; slice++) {
            int baseIndex = slice * 2 + 1;
    
            indices.Add((uint) topCenterIndex);
            indices.Add((uint) baseIndex);
            indices.Add((uint) (baseIndex + 2));
        }
    
        Material material = new Material(graphicsDevice, GlobalResource.DefaultModelEffect);
        
        material.AddMaterialMap(MaterialMapType.Albedo.GetName(), new MaterialMap {
            Texture = GlobalResource.DefaultModelTexture,
            Color = Color.White
        });
    
        return new Mesh(graphicsDevice, material, vertices.ToArray(), indices.ToArray());
    }

    /// <summary>
    /// Generates a capsule mesh with the specified radius, height, and number of slices.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create and manage the mesh's GPU resources.</param>
    /// <param name="radius">The radius of the capsule.</param>
    /// <param name="height">The cylindrical midsection's height of the capsule.</param>
    /// <param name="slices">The number of slices used to approximate the capsule. Must be at least 3.</param>
    /// <returns>A <see cref="Mesh"/> representing a capsule with the given parameters.</returns>
    public static Mesh GenCapsule(GraphicsDevice graphicsDevice, float radius, float height, int slices) {
        if (slices < 3) {
            slices = 3;
            Logger.Warn("The number of slices must be at least 3. The value is now set to 3.");
        }
        
        List<Vertex3D> vertices = new List<Vertex3D>();
        List<uint> indices = new List<uint>();
        
        float halfRadius = radius / 2.0f;
        float halfHeight = height / 2.0f; // Center the capsule by using height / 2
        int rings = slices / 2;
        
        // Create top hemisphere vertices.
        for (int ring = 0; ring <= rings; ring++) {
            float theta = ring * MathF.PI / (rings * 2);
            float cosTheta = MathF.Cos(theta);
            float sinTheta = MathF.Sin(theta);
            
            for (int slice = 0; slice <= slices; slice++) {
                float phi = slice * 2 * MathF.PI / slices;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);
                
                float x = halfRadius * sinTheta * cosPhi;
                float y = halfRadius * cosTheta + halfHeight; // Adjusted to center vertically
                float z = halfRadius * sinTheta * sinPhi;
                
                vertices.Add(new Vertex3D {
                    Position = new Vector3(x, y, z),
                    TexCoords = new Vector2((float)slice / slices, (float)ring / rings),
                    Normal = Vector3.Normalize(new Vector3(x, y - halfHeight, z))
                });
            }
        }
        
        // Create cylindrical body vertices.
        for (int yStep = 0; yStep <= 1; yStep++) {
            float y = yStep == 0 ? -halfHeight : halfHeight;
            
            for (int slice = 0; slice <= slices; slice++) {
                float phi = slice * 2 * MathF.PI / slices;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);
                
                float x = halfRadius * cosPhi;
                float z = halfRadius * sinPhi;
                
                // Add vertex
                vertices.Add(new Vertex3D {
                    Position = new Vector3(x, y, z),
                    TexCoords = new Vector2((float)slice / slices, yStep),
                    Normal = Vector3.Normalize(new Vector3(x, 0, z))
                });
            }
        }
        
        // Create bottom hemisphere vertices.
        for (int ring = 0; ring <= rings; ring++) {
            float theta = ring * MathF.PI / (rings * 2) + MathF.PI;
            float cosTheta = MathF.Cos(theta);
            float sinTheta = MathF.Sin(theta);
            
            for (int slice = 0; slice <= slices; slice++) {
                float phi = slice * 2 * MathF.PI / slices;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);
                
                float x = halfRadius * sinTheta * cosPhi;
                float y = halfRadius * cosTheta - halfHeight; // Adjusted to center vertically
                float z = halfRadius * sinTheta * sinPhi;
                
                // Add vertex
                vertices.Add(new Vertex3D {
                    Position = new Vector3(-x, y, -z),
                    TexCoords = new Vector2((float)slice / slices, (float)ring / rings),
                    Normal = Vector3.Normalize(new Vector3(-x, y + halfHeight, -z))
                });
            }
        }
        
        // Generate indices for top hemisphere.
        for (int ring = 0; ring < rings; ring++) {
            for (int slice = 0; slice < slices; slice++) {
                uint first = (uint)(ring * (slices + 1) + slice);
                uint second = first + (uint)(slices + 1);
                
                indices.Add(first);
                indices.Add(second);
                indices.Add(first + 1);
                
                indices.Add(second);
                indices.Add(second + 1);
                indices.Add(first + 1);
            }
        }

        // Generate indices for the cylindrical body.
        int cylinderStartIndex = (rings + 1) * (slices + 1);
        for (int step = 0; step < 1; step++) {
            for (int slice = 0; slice < slices; slice++) {
                uint first = (uint)(cylinderStartIndex + step * (slices + 1) + slice);
                uint second = first + (uint)(slices + 1);
                
                indices.Add(first);
                indices.Add(first + 1);
                indices.Add(second);
                
                indices.Add(first + 1);
                indices.Add(second + 1);
                indices.Add(second);
            }
        }
        
        // Generate indices for bottom hemisphere.
        int bottomStartIndex = cylinderStartIndex + 2 * (slices + 1);
        for (int ring = 0; ring < rings; ring++) {
            for (int slice = 0; slice < slices; slice++) {
                uint first = (uint)(bottomStartIndex + ring * (slices + 1) + slice);
                uint second = first + (uint)(slices + 1);
    
                indices.Add(first);
                indices.Add(first + 1);
                indices.Add(second);
    
                indices.Add(second);
                indices.Add(first + 1);
                indices.Add(second + 1);
            }
        }
        
        Material material = new Material(graphicsDevice, GlobalResource.DefaultModelEffect);
        
        material.AddMaterialMap(MaterialMapType.Albedo.GetName(), new MaterialMap {
            Texture = GlobalResource.DefaultModelTexture,
            Color = Color.White
        });
        
        return new Mesh(graphicsDevice, material, vertices.ToArray(), indices.ToArray());
    }

    /// <summary>
    /// Generates a 3D cone mesh with a specified radius, height, and number of slices.
    /// </summary>
    /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/> used to manage GPU resources for the mesh.</param>
    /// <param name="radius">The radius of the cone's base.</param>
    /// <param name="height">The height of the cone from base to tip.</param>
    /// <param name="slices">The number of slices dividing the circular base. Must be at least 3.</param>
    /// <returns>A new instance of the <see cref="Mesh"/> class representing the generated cone.</returns>
    public static Mesh GenCone(GraphicsDevice graphicsDevice, float radius, float height, int slices) {
        if (slices < 3) {
            slices = 3;
            Logger.Warn("The number of slices must be at least 3. The value is now set to 3.");
        }
        
        float halfHeight = height / 2.0F;
        
        List<Vertex3D> vertices = new List<Vertex3D>();
        List<uint> indices = new List<uint>();

        // Generate the side vertices.
        for (int slice = 0; slice <= slices; slice++) {
            float angle = slice * MathF.Tau / slices;
            float x = MathF.Cos(angle) * (radius / 2.0F);
            float z = MathF.Sin(angle) * (radius / 2.0F);
            float u = (float) slice / slices;

            // Bottom vertex.
            vertices.Add(new Vertex3D {
                Position = new Vector3(x, -halfHeight, z),
                Normal = Vector3.Normalize(new Vector3(x, radius / 2.0F, z)),
                TexCoords = new Vector2(u, 1.0F)
            });

            // Top vertex (tip of the cone).
            vertices.Add(new Vertex3D {
                Position = new Vector3(0.0F, halfHeight, 0.0F),
                Normal = Vector3.Normalize(new Vector3(x, radius / 2.0F, z)),
                TexCoords = new Vector2(u, 0.0F)
            });
        }

        // Generate the side indices.
        for (int slice = 0; slice < slices; slice++) {
            int baseIndex = slice * 2;
            int nextIndex = (slice + 1) * 2;

            indices.Add((uint) (baseIndex + 1));
            indices.Add((uint) baseIndex);
            indices.Add((uint) nextIndex);
        }

        // Generate the bottom cap.
        int bottomCenterIndex = vertices.Count;

        vertices.Add(new Vertex3D {
            Position = new Vector3(0, -halfHeight, 0),
            Normal = Vector3.UnitY,
            TexCoords = new Vector2(0.5F, 0.5F)
        });

        for (int slice = 0; slice < slices; slice++) {
            int baseIndex = slice * 2;

            indices.Add((uint) bottomCenterIndex);
            indices.Add((uint) (baseIndex + 2));
            indices.Add((uint) baseIndex);
        }

        Material material = new Material(graphicsDevice, GlobalResource.DefaultModelEffect);

        material.AddMaterialMap(MaterialMapType.Albedo.GetName(), new MaterialMap {
            Texture = GlobalResource.DefaultModelTexture,
            Color = Color.White
        });

        return new Mesh(graphicsDevice, material, vertices.ToArray(), indices.ToArray());
    }

    /// <summary>
    /// Generates a torus-shaped mesh with specified dimensions and detail.
    /// </summary>
    /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/> used for managing GPU resources.</param>
    /// <param name="radius">The radius of the torus from the center to the middle of the tube.</param>
    /// <param name="size">The thickness of the torus tube.</param>
    /// <param name="radSeg">The number of radial segments in the torus.</param>
    /// <param name="sides">The number of segments around the tube's circular cross-section.</param>
    /// <returns>A new instance of <see cref="Mesh"/> representing the generated torus.</returns>
    public static Mesh GenTorus(GraphicsDevice graphicsDevice, float radius, float size, int radSeg, int sides) {
        if (radSeg < 3) {
            radSeg = 3;
            Logger.Warn("The number of radial segments must be at least 3. The value is now set to 3.");
        }
        
        if (sides < 3) {
            sides = 3;
            Logger.Warn("The number of sides must be at least 3. The value is now set to 3.");
        }

        List<Vertex3D> vertices = new List<Vertex3D>();
        List<uint> indices = new List<uint>();

        float circusStep = MathF.Tau / radSeg;
        float sideStep = MathF.Tau / sides;

        // Generate the vertices.
        for (int rad = 0; rad <= radSeg; rad++) {
            float radAngle = rad * circusStep;
            float cosRad = MathF.Cos(radAngle);
            float sinRad = MathF.Sin(radAngle);

            for (int side = 0; side <= sides; side++) {
                float sideAngle = side * sideStep;
                float cosSide = MathF.Cos(sideAngle);
                float sinSide = MathF.Sin(sideAngle);

                Vector3 normal = new Vector3(cosSide * cosRad, sinSide, cosSide * sinRad);
                Vector3 position = normal * (size / 4) + new Vector3(cosRad * (radius / 4), 0, sinRad * (radius / 4));
                Vector2 texCoords = new Vector2((float) rad / radSeg, (float) side / sides);

                vertices.Add(new Vertex3D {
                    Position = position,
                    Normal = Vector3.Normalize(normal),
                    TexCoords = texCoords
                });
            }
        }

        // Generate the indices.
        for (int rad = 0; rad < radSeg; rad++) {
            for (int side = 0; side < sides; side++) {
                int current = rad * (sides + 1) + side;
                int next = current + sides + 1;

                indices.Add((uint) current);
                indices.Add((uint) next);
                indices.Add((uint) (next + 1));
 
                indices.Add((uint) current);
                indices.Add((uint) (next + 1));
                indices.Add((uint) (current + 1));
            }
        }

        Material material = new Material(graphicsDevice, GlobalResource.DefaultModelEffect);
        
        material.AddMaterialMap(MaterialMapType.Albedo.GetName(), new MaterialMap {
            Texture = GlobalResource.DefaultModelTexture,
            Color = Color.White
        });

        return new Mesh(graphicsDevice, material, vertices.ToArray(), indices.ToArray());
    }

    /// <summary>
    /// Generates a torus knot mesh with the specified parameters.
    /// </summary>
    /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/> used to manage GPU resources for the mesh.</param>
    /// <param name="radius">The radius of the torus knot.</param>
    /// <param name="tubeRadius">The thickness of the tube forming the torus knot.</param>
    /// <param name="radSeg">The number of radial segments for the torus knot. The minimum value is 3.</param>
    /// <param name="sides">The number of sides forming the cross-section of the tube. The minimum value is 3.</param>
    /// <returns>A <see cref="Mesh"/> representing the torus knot with the specified parameters.</returns>
    public static Mesh GenKnot(GraphicsDevice graphicsDevice, float radius, float tubeRadius, int radSeg, int sides) {
        if (radSeg < 3) {
            radSeg = 3;
            Logger.Warn("The number of radial segments must be at least 3. The value is now set to 3.");
        }
    
        if (sides < 3) {
            sides = 3;
            Logger.Warn("The number of sides must be at least 3. The value is now set to 3.");
        }
    
        List<Vertex3D> vertices = new List<Vertex3D>();
        List<uint> indices = new List<uint>();
    
        float step = MathF.Tau / radSeg;
        float sideStep = MathF.Tau / sides;
    
        // Generate the vertices.
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
    
                Vector3 vertexNormal = Vector3.Normalize(offset);
                Vector2 texCoords = new Vector2((float) rad / radSeg, (float) side / sides);
    
                vertices.Add(new Vertex3D {
                    Position = position,
                    Normal = vertexNormal,
                    TexCoords = texCoords
                });
            }
        }
        
        // Generate the indices.
        for (int rad = 0; rad < radSeg; rad++) {
            for (int side = 0; side < sides; side++) {
                int current = rad * (sides + 1) + side;
                int next = current + sides + 1;
    
                indices.Add((uint) current);
                indices.Add((uint) next);
                indices.Add((uint) (next + 1));
                
                indices.Add((uint) current);
                indices.Add((uint) (next + 1));
                indices.Add((uint) (current + 1));
            }
        }
        
        Material material = new Material(graphicsDevice, GlobalResource.DefaultModelEffect);
        
        material.AddMaterialMap(MaterialMapType.Albedo.GetName(), new MaterialMap {
            Texture = GlobalResource.DefaultModelTexture,
            Color = Color.White
        });
        
        return new Mesh(graphicsDevice, material, vertices.ToArray(), indices.ToArray());
    }

    /// <summary>
    /// Generates a heightmap-based 3D mesh from the given heightmap image and dimensions.
    /// </summary>
    /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/> used for managing GPU resources and rendering the generated mesh.</param>
    /// <param name="heightmap">The heightmap image used to determine the height of each vertex in the mesh.</param>
    /// <param name="size">The 3D size of the heightmap mesh in world units, where X and Z represent the width and depth, and Y represents the height scale.</param>
    /// <returns>A new <see cref="Mesh"/> instance representing the generated heightmap mesh based on the input parameters.</returns>
    public static Mesh GenHeightmap(GraphicsDevice graphicsDevice, Image heightmap, Vector3 size) {
        float xStep = size.X / (heightmap.Width - 1.0F);
        float zStep = size.Z / (heightmap.Height - 1.0F);
        float heightScale = size.Y / 255.0F;

        List<Vertex3D> vertices = new List<Vertex3D>();
        List<uint> indices = new List<uint>();

        // Generate the vertices.
        for (int y = 0; y < heightmap.Height; y++) {
            for (int x = 0; x < heightmap.Width; x++) {
                int pixelIndex = (y * heightmap.Width + x) * 4;
                byte redChannel = heightmap.Data[pixelIndex];
                float height = (redChannel * heightScale) - (size.Y / 2);
                
                Vector3 position = new Vector3(x * xStep - size.X / 2, height, y * zStep - size.Z / 2);
                Vector3 normal = Vector3.UnitY;
                Vector2 texCoords = new Vector2(x / (heightmap.Width - 1.0F), y / (heightmap.Height - 1.0F));

                vertices.Add(new Vertex3D {
                    Position = position,
                    Normal = normal,
                    TexCoords = texCoords
                });
            }
        }

        // Generate the indices.
        for (int z = 0; z < heightmap.Height - 1; z++) {
            for (int x = 0; x < heightmap.Width - 1; x++) {
                uint topLeft = (uint) (z * heightmap.Width + x);
                uint topRight = (uint) (z * heightmap.Width + x + 1);
                uint bottomLeft = (uint) ((z + 1) * heightmap.Width + x);
                uint bottomRight = (uint) ((z + 1) * heightmap.Width + x + 1);

                indices.Add(topLeft);
                indices.Add(topRight);
                indices.Add(bottomLeft);

                indices.Add(bottomLeft);
                indices.Add(topRight);
                indices.Add(bottomRight);
            }
        }
        
        Material material = new Material(graphicsDevice, GlobalResource.DefaultModelEffect);

        material.AddMaterialMap(MaterialMapType.Albedo.GetName(), new MaterialMap {
            Texture = GlobalResource.DefaultModelTexture,
            Color = Color.White
        });

        return new Mesh(graphicsDevice, material, vertices.ToArray(), indices.ToArray());
    }

    // TODO: FIX first Cubemap class for it.
    //public static Mesh GenCubemap(GraphicsDevice graphicsDevice, Texture2D cubemap, Vector3 size) {
    //    
    //}
    
    // TODO: Check if it works correct.
    /// <summary>
    /// Generates tangent vectors for the mesh's vertices based on the provided geometric and UV coordinate data.
    /// </summary>
    public void GenerateTangents() {
        if (this.Vertices.Length < 3 || this.Indices.Length < 3) {
            return;
        }

        Vector3[] tan1 = new Vector3[this.Vertices.Length];
        Vector3[] tan2 = new Vector3[this.Vertices.Length];

        for (int i = 0; i < this.Indices.Length; i += 3) {
            int i1 = (int) this.Indices[i];
            int i2 = (int) this.Indices[i + 1];
            int i3 = (int) this.Indices[i + 2];

            Vertex3D v1 = this.Vertices[i1];
            Vertex3D v2 = this.Vertices[i2];
            Vertex3D v3 = this.Vertices[i3];

            Vector3 p1 = v1.Position;
            Vector3 p2 = v2.Position;
            Vector3 p3 = v3.Position;

            Vector3 w1 = new Vector3(v1.TexCoords.X, v1.TexCoords.Y, 1.0f);
            Vector3 w2 = new Vector3(v2.TexCoords.X, v2.TexCoords.Y, 1.0f);
            Vector3 w3 = new Vector3(v3.TexCoords.X, v3.TexCoords.Y, 1.0f);

            Vector3 q1 = p2 - p1;
            Vector3 q2 = p3 - p1;

            Vector3 sdir = new Vector3(
                w2.Y * q1.X - w1.Y * q2.X,
                w2.Y * q1.Y - w1.Y * q2.Y,
                w2.Y * q1.Z - w1.Y * q2.Z
            );
            
            Vector3 tdir = new Vector3(
                w1.X * q2.X - w2.X * q1.X,
                w1.X * q2.Y - w2.X * q1.Y,
                w1.X * q2.Z - w2.X * q1.Z
            );

            tan1[i1] += sdir;
            tan1[i2] += sdir;
            tan1[i3] += sdir;

            tan2[i1] += tdir;
            tan2[i2] += tdir;
            tan2[i3] += tdir;
        }

        for (int i = 0; i < this.Vertices.Length; i++) {
            Vertex3D vertex = this.Vertices[i];
            Vector3 n = vertex.Normal;
            Vector3 t = tan1[i];

            Vector3 tangent = Vector3.Normalize(t - (n * Vector3.Dot(n, t)));
            this.Vertices[i].Tangent = tangent;
        }
    }
    
    /// <summary>
    /// Updates the transformation matrices of the animation bones for a specific frame using the provided command list and animation data.
    /// </summary>
    /// <param name="commandList">The command list used to issue rendering commands.</param>
    /// <param name="animation">The animation data containing the bone transformations.</param>
    /// <param name="frame">The specific frame of the animation to update the bone transformations.</param>
    public void UpdateAnimationBones(CommandList commandList, ModelAnimation animation, int frame) {
        if (this.BoneInfos.Count > 0) {
            for (int boneId = 0; boneId < this.BoneInfos[animation.Name][frame].Length; boneId++) {
                this._boneBuffer.SetValue(boneId, this.BoneInfos[animation.Name][frame][boneId].Transformation);
            }
            
            this._boneBuffer.UpdateBuffer(commandList);
        }
    }

    /// <summary>
    /// Resets the bone transformation matrices to their identity state and updates the buffer on the GPU using the provided command list.
    /// </summary>
    /// <param name="commandList">The command list used to record the buffer update command, ensuring the changes are applied to the GPU.</param>
    public void ResetAnimationBones(CommandList commandList) {
        for (int i = 0; i < 128; i++) {
            this._boneBuffer.SetValue(i, Matrix4x4.Identity);
        }
        
        this._boneBuffer.UpdateBuffer(commandList);
    }

    /// <summary>
    /// Renders the mesh using the provided command list and transformation, with customizable rendering options.
    /// </summary>
    /// <param name="commandList">The <see cref="CommandList"/> used to issue draw calls and update resources in the rendering process.</param>
    /// <param name="transform">The <see cref="Transform"/> representing the position, rotation, and scale for rendering the mesh.</param>
    /// <param name="output">The <see cref="OutputDescription"/> specifying the render target and depth buffer setup for the rendering pipeline.</param>
    /// <param name="sampler">An optional <see cref="Sampler"/> instance defining how textures are sampled. If null, a default sampler will be used.</param>
    /// <param name="wires">A boolean indicating whether to render the mesh in wireframe mode (true) or solid mode (false).</param>
    /// <param name="color">An optional <see cref="Color"/> to override the default material color used for rendering. If null, the default material color is applied.</param>
    public void Draw(CommandList commandList, Transform transform, OutputDescription output, Sampler? sampler = null, bool wires = false, Color? color = null) {
        Cam3D? cam3D = Cam3D.ActiveCamera;

        if (cam3D == null) {
            return;
        }
        
        // Set optional color.
        Color cachedColor = this.Material.GetMapColor(MaterialMapType.Albedo.GetName()) ?? Color.White;
        this.Material.SetMapColor(MaterialMapType.Albedo.GetName(), color ?? cachedColor);
        
        // Update matrix buffer.
        this._modelMatrixBuffer.SetValue(0, cam3D.GetProjection());
        this._modelMatrixBuffer.SetValue(1, cam3D.GetView());
        this._modelMatrixBuffer.SetValue(2, transform.GetTransform());
        this._modelMatrixBuffer.UpdateBuffer(commandList);
        
        // Update color buffer.
        for (int i = 0; i < this.Material.GetMaterialMaps().Length; i++) {
            Color? mapColor = this.Material.GetMapColor(((MaterialMapType) i).GetName());

            if (mapColor.HasValue) {
                this._colorBuffer.SetValue(i, mapColor.Value.ToRgbaFloatVec4());
            }
        }

        this._colorBuffer.UpdateBuffer(commandList);
        
        // Update value buffer.
        for (int i = 0; i < this.Material.GetMaterialMaps().Length; i++) {
            this._valueBuffer.SetValue(i, this.Material.GetMapValue(((MaterialMapType) i).GetName()));
        }
        
        this._valueBuffer.UpdateBuffer(commandList);

        // Update pipeline description.
        this._pipelineDescription.BlendState = this.Material.BlendState.Description;
        this._pipelineDescription.RasterizerState.CullMode = wires ? FaceCullMode.None : FaceCullMode.Back;
        this._pipelineDescription.RasterizerState.FillMode = wires ? PolygonFillMode.Wireframe : PolygonFillMode.Solid;
        this._pipelineDescription.Outputs = output;
        
        if (this.IndexCount > 0) {
            
            // Set vertex and index buffer.
            commandList.SetVertexBuffer(0, this._vertexBuffer);
            commandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt32);
            
            // Set pipeline.
            commandList.SetPipeline(this.Material.Effect.GetPipeline(this._pipelineDescription).Pipeline);
            
            // Set projection view buffer.
            commandList.SetGraphicsResourceSet(0, this._modelMatrixBuffer.GetResourceSet(this.Material.Effect.GetBufferLayout("MatrixBuffer")));
            
            // Set bone buffer.
            commandList.SetGraphicsResourceSet(1, this._boneBuffer.GetResourceSet(this.Material.Effect.GetBufferLayout("BoneBuffer")));
            
            // Set color buffer.
            commandList.SetGraphicsResourceSet(2, this._colorBuffer.GetResourceSet(this.Material.Effect.GetBufferLayout("ColorBuffer")));
            
            // Set value buffer.
            commandList.SetGraphicsResourceSet(3, this._valueBuffer.GetResourceSet(this.Material.Effect.GetBufferLayout("ValueBuffer")));
            
            // Set material texture.
            for (int i = 0; i < this.Material.Effect.GetTextureLayoutKeys().Length; i++) {
                string key = this.Material.Effect.GetTextureLayoutKeys()[i];
                ResourceSet? resourceSet = this.Material.GetResourceSet(sampler ?? GraphicsHelper.GetSampler(this.GraphicsDevice, SamplerType.Point), this.Material.Effect.GetTextureLayout(key), key);

                if (resourceSet != null) {
                    commandList.SetGraphicsResourceSet((uint) i + 4, resourceSet);
                }
            }
            
            // Draw.
            commandList.DrawIndexed(this.IndexCount);
        }
        else {
            
            // Set vertex buffer.
            commandList.SetVertexBuffer(0, this._vertexBuffer);
            
            // Set pipeline.
            commandList.SetPipeline(this.Material.Effect.GetPipeline(this._pipelineDescription).Pipeline);
            
            // Set projection view buffer.
            commandList.SetGraphicsResourceSet(0, this._modelMatrixBuffer.GetResourceSet(this.Material.Effect.GetBufferLayout("MatrixBuffer")));
            
            // Set bone buffer.
            commandList.SetGraphicsResourceSet(1, this._boneBuffer.GetResourceSet(this.Material.Effect.GetBufferLayout("BoneBuffer")));
            
            // Set color buffer.
            commandList.SetGraphicsResourceSet(2, this._colorBuffer.GetResourceSet(this.Material.Effect.GetBufferLayout("ColorBuffer")));
            
            // Set value buffer.
            commandList.SetGraphicsResourceSet(3, this._valueBuffer.GetResourceSet(this.Material.Effect.GetBufferLayout("ValueBuffer")));
            
            // Set material texture.
            for (int i = 0; i < this.Material.Effect.GetTextureLayoutKeys().Length; i++) {
                string key = this.Material.Effect.GetTextureLayoutKeys()[i];
                ResourceSet? resourceSet = this.Material.GetResourceSet(sampler ?? GraphicsHelper.GetSampler(this.GraphicsDevice, SamplerType.Point), this.Material.Effect.GetTextureLayout(key), key);

                if (resourceSet != null) {
                    commandList.SetGraphicsResourceSet((uint) i + 4, resourceSet);
                }
            }
            
            // Draw.
            commandList.Draw(this.VertexCount);
        }
        
        // Reset albedo material color.
        this.Material.SetMapColor(MaterialMapType.Albedo.GetName(), cachedColor);
    }

    /// <summary>
    /// Creates and returns a configured instance of <see cref="SimplePipelineDescription"/> for rendering the mesh.
    /// </summary>
    /// <returns>A <see cref="SimplePipelineDescription"/> containing settings for blend state, depth-stencil state, rasterizer state,
    /// primitive topology, buffer bindings, texture layouts, and shader set configuration.</returns>
    private SimplePipelineDescription CreatePipelineDescription() {
        return new SimplePipelineDescription() {
            BlendState = this.Material.BlendState.Description,
            DepthStencilState = new DepthStencilStateDescription(true, true, ComparisonKind.LessEqual),
            RasterizerState = new RasterizerStateDescription() {
                CullMode = FaceCullMode.Back,
                FillMode = PolygonFillMode.Solid,
                FrontFace = FrontFace.Clockwise,
                DepthClipEnabled = true,
                ScissorTestEnabled = false
            },
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            BufferLayouts = this.Material.Effect.GetBufferLayouts(),
            TextureLayouts = this.Material.Effect.GetTextureLayouts(),
            ShaderSet = new ShaderSetDescription() {
                VertexLayouts = [
                    this.Material.Effect.VertexLayout
                ],
                Shaders = [
                    this.Material.Effect.Shader.Item1,
                    this.Material.Effect.Shader.Item2
                ]
            }
        };
    }
    
    /// <summary>
    /// Calculates the bounding box for the current mesh based on its vertices.
    /// </summary>
    /// <returns>A BoundingBox object that encompasses all vertices of the mesh.</returns>
    private BoundingBox GenerateBoundingBox() {
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (Vertex3D vertex in this.Vertices) {
            min = Vector3.Min(min, vertex.Position);
            max = Vector3.Max(max, vertex.Position);
        }

        return new BoundingBox(min, max);
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._vertexBuffer.Dispose();
            this._indexBuffer.Dispose();
            this._modelMatrixBuffer.Dispose();
            this._boneBuffer.Dispose();
            this._colorBuffer.Dispose();
            this._valueBuffer.Dispose();
        }
    }
}