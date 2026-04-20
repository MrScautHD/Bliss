using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Geometry.Meshes.Data;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Images;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Materials;
using Veldrid;
using Color = Bliss.CSharp.Colors.Color;
using Material = Bliss.CSharp.Materials.Material;

namespace Bliss.CSharp.Geometry.Meshes;

public class Mesh<T> : Disposable, IMesh where T : unmanaged, IVertexType {
    
    /// <summary>
    /// Gets the graphics device used by this mesh for GPU resource creation and updates.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// Gets or sets the material used to render this mesh.
    /// </summary>
    public Material Material { get; set; }
    
    /// <summary>
    /// Gets the mesh data used to populate the vertex and index buffers.
    /// </summary>
    public IMeshData<T> MeshData { get; private set; }
    
    /// <summary>
    /// Gets the vertex format describing the layout of the mesh vertices.
    /// </summary>
    public VertexFormat VertexFormat => this.MeshData.VertexFormat;
    
    /// <summary>
    /// Gets the number of vertices contained in this mesh.
    /// </summary>
    public uint VertexCount => this.MeshData.VertexCount;
    
    /// <summary>
    /// Gets the number of indices contained in this mesh.
    /// </summary>
    public uint IndexCount => this.MeshData.IndexCount;

    /// <summary>
    /// Gets the number of bones associated with this mesh.
    /// </summary>
    public uint BoneCount => this.MeshData.BoneCount;
    
    /// <summary>
    /// Gets a value indicating whether this mesh uses skinning data.
    /// </summary>
    public bool IsSkinned => this.MeshData.VertexFormat.IsSkinned;
    
    /// <summary>
    /// Gets the GPU vertex buffer containing this mesh's vertex data.
    /// </summary>
    public DeviceBuffer VertexBuffer { get; private set; }
    
    /// <summary>
    /// Gets the GPU index buffer containing this mesh's index data, if one exists.
    /// </summary>
    public DeviceBuffer? IndexBuffer { get; private set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Mesh{T}"/> class with the specified graphics device, material, and mesh data.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create and update GPU buffers.</param>
    /// <param name="material">The material used to render the mesh.</param>
    /// <param name="meshData">The mesh data containing vertices, indices, and vertex format information.</param>
    public Mesh(GraphicsDevice graphicsDevice, Material material, IMeshData<T> meshData) {
        this.GraphicsDevice = graphicsDevice;
        this.Material = material;
        this.MeshData = meshData;
        
        // Create vertex buffer.
        this.VertexBuffer = this.MeshData.CreateVertexBuffer(graphicsDevice);
        
        // Create index buffer (if their indices).
        if (this.MeshData.IndexCount > 0) {
            this.IndexBuffer = this.MeshData.CreateIndexBuffer(graphicsDevice);
        }
    }
    
    /// <summary>
    /// Creates a new quad mesh with the specified width and height.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to generate the quad mesh.</param>
    /// <param name="width">The width of the quad.</param>
    /// <param name="height">The height of the quad.</param>
    /// <returns>A new mesh representing a quadrilateral.</returns>
    public static Mesh<Vertex3D> GenQuad(GraphicsDevice graphicsDevice, float width, float height) {
        float halfWidth = width / 2.0F;
        float halfHeight = height / 2.0F;
        
        Vertex3D[] vertices = [
            new Vertex3D() {
                Position = new Vector3(-halfWidth, -halfHeight, 0.0F),
                Normal = Vector3.UnitZ,
                TexCoords = new Vector2(0.0F, 1.0F)
            },
            new Vertex3D() {
                Position = new Vector3(halfWidth, -halfHeight, 0.0F),
                Normal = Vector3.UnitZ,
                TexCoords = new Vector2(1.0F, 1.0F)
            },
            new Vertex3D() {
                Position = new Vector3(halfWidth, halfHeight, 0.0F),
                Normal = Vector3.UnitZ,
                TexCoords = new Vector2(1.0F, 0.0F)
            },
            new Vertex3D() {
                Position = new Vector3(-halfWidth, halfHeight, 0.0F),
                Normal = Vector3.UnitZ,
                TexCoords = new Vector2(0.0F, 0.0F)
            }
        ];
        
        uint[] indices = [
            0, 1, 2,
            2, 3, 0
        ];
        
        Material material = new Material(GlobalResource.DefaultModelEffect);
        
        material.AddMaterialMap(MaterialMapType.Albedo, new MaterialMap {
            Texture = GlobalResource.DefaultModelTexture,
            Color = Color.White
        });
        
        return new Mesh<Vertex3D>(graphicsDevice, material, new BasicMeshData(vertices, indices));
    }
    
    /// <summary>
    /// Generates a 3D polygon mesh with the specified number of sides and radius.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to manage GPU resources for the generated mesh.</param>
    /// <param name="sides">The number of sides of the polygon. Must be at least 3.</param>
    /// <param name="radius">The radius of the polygon.</param>
    /// <returns>A new instance of the <see cref="Mesh{Vertex3D}"/> class representing the 3D polygon.</returns>
    public static Mesh<Vertex3D> GenPoly(GraphicsDevice graphicsDevice, int sides, float radius) {
        if (sides < 3) {
            sides = 3;
            Logger.Warn("The number of sides must be at least 3. The value is now set to 3.");
        }
        
        List<Vertex3D> vertices = new List<Vertex3D>();
        List<uint> indices = new List<uint>();
        
        // Center vertex.
        vertices.Add(new Vertex3D {
            Position = new Vector3(0.0F, 0.0F, 0.0F),
            Normal = Vector3.UnitY,
            TexCoords = new Vector2(0.5F, 0.5F)
        });
        
        // Generate vertices for the outer circle.
        for (int i = 0; i < sides; i++) {
            float angle = i * MathF.Tau / sides;
            float x = MathF.Cos(angle) * radius / 2.0F;
            float z = MathF.Sin(angle) * radius / 2.0F;
    
            vertices.Add(new Vertex3D {
                Position = new Vector3(x, 0.0F, z),
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
        
        Material material = new Material(GlobalResource.DefaultModelEffect);
        
        material.AddMaterialMap(MaterialMapType.Albedo, new MaterialMap {
            Texture = GlobalResource.DefaultModelTexture,
            Color = Color.White
        });
        
        return new Mesh<Vertex3D>(graphicsDevice, material, new BasicMeshData(vertices.ToArray(), indices.ToArray()));
    }

    /// <summary>
    /// Generates a cuboid mesh with the specified dimensions and initializes it with default materials and texture settings.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device that manages GPU resources for the mesh.</param>
    /// <param name="width">The width of the cuboid.</param>
    /// <param name="height">The height of the cuboid.</param>
    /// <param name="length">The length of the cuboid.</param>
    /// <returns>A new instance of the <see cref="Mesh{Vertex3D}"/> class representing the cuboid.</returns>
    public static Mesh<Vertex3D> GenCube(GraphicsDevice graphicsDevice, float width, float height, float length) {
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
        
        Material material = new Material(GlobalResource.DefaultModelEffect);
        
        material.AddMaterialMap(MaterialMapType.Albedo, new MaterialMap() {
            Texture = GlobalResource.DefaultModelTexture,
            Color = Color.White
        });
        
        return new Mesh<Vertex3D>(graphicsDevice, material, new BasicMeshData(vertices, indices));
    }

    /// <summary>
    /// Generates a sphere mesh with the specified radius, number of rings, and number of slices.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create and manage GPU resources for the mesh.</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <param name="rings">The number of horizontal segments dividing the sphere. Must be greater than or equal to 3.</param>
    /// <param name="slices">The number of vertical segments dividing the sphere. Must be greater than or equal to 3.</param>
    /// <returns>A new sphere mesh represented as an instance of the <see cref="Mesh{Vertex3D}"/> class.</returns>
    public static Mesh<Vertex3D> GenSphere(GraphicsDevice graphicsDevice, float radius, int rings, int slices) {
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

        Material material = new Material(GlobalResource.DefaultModelEffect);

        material.AddMaterialMap(MaterialMapType.Albedo, new MaterialMap() {
            Texture = GlobalResource.DefaultModelTexture,
            Color = Color.White
        });

        return new Mesh<Vertex3D>(graphicsDevice, material, new BasicMeshData(vertices, indices));
    }

    /// <summary>
    /// Generates a 3D hemisphere mesh with the specified radius, number of rings, and slices.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for allocating GPU resources needed for the mesh.</param>
    /// <param name="radius">The radius of the hemisphere.</param>
    /// <param name="rings">The number of vertical subdivisions along the Y-axis direction of the hemisphere.</param>
    /// <param name="slices">The number of horizontal subdivisions around the XZ-plane direction of the hemisphere.</param>
    /// <returns>A new mesh representing the generated hemisphere.</returns>
    public static Mesh<Vertex3D> GenHemisphere(GraphicsDevice graphicsDevice, float radius, int rings, int slices) {
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
    
        Material material = new Material(GlobalResource.DefaultModelEffect);
    
        material.AddMaterialMap(MaterialMapType.Albedo, new MaterialMap() {
            Texture = GlobalResource.DefaultModelTexture,
            Color = Color.White
        });
    
        return new Mesh<Vertex3D>(graphicsDevice, material, new BasicMeshData(vertices.ToArray(), indices.ToArray()));
    }

    /// <summary>
    /// Generates a cylindrical mesh with the specified dimensions and resolution.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device responsible for managing GPU resources.</param>
    /// <param name="radius">The radius of the cylinder's base and top.</param>
    /// <param name="height">The height of the cylinder.</param>
    /// <param name="slices">The number of slices (or segments) used to approximate the cylinder. Must be at least 3.</param>
    /// <returns>A new mesh representing the generated cylindrical geometry.</returns>
    public static Mesh<Vertex3D> GenCylinder(GraphicsDevice graphicsDevice, float radius, float height, int slices) {
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
    
        Material material = new Material(GlobalResource.DefaultModelEffect);
        
        material.AddMaterialMap(MaterialMapType.Albedo, new MaterialMap {
            Texture = GlobalResource.DefaultModelTexture,
            Color = Color.White
        });
    
        return new Mesh<Vertex3D>(graphicsDevice, material, new BasicMeshData(vertices.ToArray(), indices.ToArray()));
    }

    /// <summary>
    /// Generates a capsule mesh with the specified radius, height, and number of slices.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create and manage the mesh's GPU resources.</param>
    /// <param name="radius">The radius of the capsule.</param>
    /// <param name="height">The height of the cylindrical midsection of the capsule.</param>
    /// <param name="slices">The number of slices used to approximate the capsule. Must be at least 3.</param>
    /// <returns>A mesh representing a capsule with the specified dimensions and configuration.</returns>
    public static Mesh<Vertex3D> GenCapsule(GraphicsDevice graphicsDevice, float radius, float height, int slices) {
        if (slices < 3) {
            slices = 3;
            Logger.Warn("The number of slices must be at least 3. The value is now set to 3.");
        }
        
        List<Vertex3D> vertices = new List<Vertex3D>();
        List<uint> indices = new List<uint>();
        
        float halfRadius = radius / 2.0F;
        float halfHeight = height / 2.0F;
        int rings = slices / 2;
        
        // Create top hemisphere vertices.
        for (int ring = 0; ring <= rings; ring++) {
            float theta = ring * MathF.PI / (rings * 2.0F);
            float cosTheta = MathF.Cos(theta);
            float sinTheta = MathF.Sin(theta);
            
            for (int slice = 0; slice <= slices; slice++) {
                float phi = slice * 2.0F * MathF.PI / slices;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);
                
                float x = halfRadius * sinTheta * cosPhi;
                float y = halfRadius * cosTheta + halfHeight;
                float z = halfRadius * sinTheta * sinPhi;
                
                // Add vertex.
                vertices.Add(new Vertex3D {
                    Position = new Vector3(x, y, z),
                    TexCoords = new Vector2((float) slice / slices, (float) ring / rings),
                    Normal = Vector3.Normalize(new Vector3(x, y - halfHeight, z))
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
                vertices.Add(new Vertex3D {
                    Position = new Vector3(x, y, z),
                    TexCoords = new Vector2((float) slice / slices, -yStep),
                    Normal = Vector3.Normalize(new Vector3(x, 0, z))
                });
            }
        }
        
        // Create bottom hemisphere vertices.
        for (int ring = 0; ring <= rings; ring++) {
            float theta = ring * MathF.PI / (rings * 2.0F) + MathF.PI;
            float cosTheta = MathF.Cos(theta);
            float sinTheta = MathF.Sin(theta);
            
            for (int slice = 0; slice <= slices; slice++) {
                float phi = slice * 2.0F * MathF.PI / slices;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);
                
                float x = halfRadius * sinTheta * cosPhi;
                float y = halfRadius * cosTheta - halfHeight;
                float z = halfRadius * sinTheta * sinPhi;
                
                // Add vertex.
                vertices.Add(new Vertex3D {
                    Position = new Vector3(-x, y, -z),
                    TexCoords = new Vector2((float) slice / slices, 1.0F - (float) ring / rings),
                    Normal = Vector3.Normalize(new Vector3(-x, y + halfHeight, -z))
                });
            }
        }
        
        // Generate indices for top hemisphere.
        for (int ring = 0; ring < rings; ring++) {
            for (int slice = 0; slice < slices; slice++) {
                uint first = (uint) (ring * (slices + 1) + slice);
                uint second = first + (uint) (slices + 1);
                
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
                uint first = (uint) (cylinderStartIndex + step * (slices + 1) + slice);
                uint second = first + (uint) (slices + 1);
                
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
                uint first = (uint) (bottomStartIndex + ring * (slices + 1) + slice);
                uint second = first + (uint) (slices + 1);
    
                indices.Add(first);
                indices.Add(first + 1);
                indices.Add(second);
    
                indices.Add(second);
                indices.Add(first + 1);
                indices.Add(second + 1);
            }
        }
        
        Material material = new Material(GlobalResource.DefaultModelEffect);
        
        material.AddMaterialMap(MaterialMapType.Albedo, new MaterialMap {
            Texture = GlobalResource.DefaultModelTexture,
            Color = Color.White
        });
        
        return new Mesh<Vertex3D>(graphicsDevice, material, new BasicMeshData(vertices.ToArray(), indices.ToArray()));
    }
    
    /// <summary>
    /// Generates a 3D cone mesh with the specified radius, height, and number of slices.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to manage GPU resources for the mesh.</param>
    /// <param name="radius">The radius of the cone's base.</param>
    /// <param name="height">The height of the cone from base to tip.</param>
    /// <param name="slices">The number of slices dividing the circular base. Must be at least 3.</param>
    /// <returns>A new mesh representing the generated 3D cone.</returns>
    public static Mesh<Vertex3D> GenCone(GraphicsDevice graphicsDevice, float radius, float height, int slices) {
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

        Material material = new Material(GlobalResource.DefaultModelEffect);

        material.AddMaterialMap(MaterialMapType.Albedo, new MaterialMap {
            Texture = GlobalResource.DefaultModelTexture,
            Color = Color.White
        });

        return new Mesh<Vertex3D>(graphicsDevice, material, new BasicMeshData(vertices.ToArray(), indices.ToArray()));
    }
    
    /// <summary>
    /// Generates a torus-shaped mesh with specified dimensions and level of detail.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for managing GPU resources while creating the mesh.</param>
    /// <param name="radius">The distance from the center of the torus to the center of its circular tube.</param>
    /// <param name="size">The thickness of the torus tube's circular cross-section.</param>
    /// <param name="radSeg">The number of radial segments dividing the torus along its circumference.</param>
    /// <param name="sides">The number of segments dividing the torus tube's circular cross-section.</param>
    /// <returns>A new mesh object representing the generated torus.</returns>
    public static Mesh<Vertex3D> GenTorus(GraphicsDevice graphicsDevice, float radius, float size, int radSeg, int sides) {
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

        Material material = new Material(GlobalResource.DefaultModelEffect);
        
        material.AddMaterialMap(MaterialMapType.Albedo, new MaterialMap {
            Texture = GlobalResource.DefaultModelTexture,
            Color = Color.White
        });

        return new Mesh<Vertex3D>(graphicsDevice, material, new BasicMeshData(vertices.ToArray(), indices.ToArray()));
    }

    /// <summary>
    /// Generates a torus knot mesh with the specified parameters.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to manage GPU resources for the mesh.</param>
    /// <param name="radius">The radius of the torus knot.</param>
    /// <param name="tubeRadius">The thickness of the tube forming the torus knot.</param>
    /// <param name="radSeg">The number of radial segments for the torus knot. The minimum value is 3.</param>
    /// <param name="sides">The number of sides forming the cross-section of the tube. The minimum value is 3.</param>
    /// <returns>A mesh representing the torus knot with the specified parameters.</returns>
    public static Mesh<Vertex3D> GenKnot(GraphicsDevice graphicsDevice, float radius, float tubeRadius, int radSeg, int sides) {
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
        
        Material material = new Material(GlobalResource.DefaultModelEffect);
        
        material.AddMaterialMap(MaterialMapType.Albedo, new MaterialMap {
            Texture = GlobalResource.DefaultModelTexture,
            Color = Color.White
        });
        
        return new Mesh<Vertex3D>(graphicsDevice, material, new BasicMeshData(vertices.ToArray(), indices.ToArray()));
    }

    /// <summary>
    /// Generates a heightmap-based 3D mesh from the specified heightmap image and dimensions.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to manage GPU resources and render the resulting mesh.</param>
    /// <param name="heightmap">The heightmap image defining the height for each vertex in the mesh.</param>
    /// <param name="size">The size of the resulting mesh in world units, where X and Z represent the width and depth, and Y indicates the height scale.</param>
    /// <returns>A new mesh constructed from the given heightmap and dimensions.</returns>
    public static Mesh<Vertex3D> GenHeightmap(GraphicsDevice graphicsDevice, Image heightmap, Vector3 size) {
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
        
        Material material = new Material(GlobalResource.DefaultModelEffect);
        
        material.AddMaterialMap(MaterialMapType.Albedo, new MaterialMap {
            Texture = GlobalResource.DefaultModelTexture,
            Color = Color.White
        });
        
        return new Mesh<Vertex3D>(graphicsDevice, material, new BasicMeshData(vertices.ToArray(), indices.ToArray()));
    }
    
    /// <summary>
    /// Generates a bounding box that encloses the mesh geometry.
    /// </summary>
    /// <returns>A bounding box covering the mesh's vertices.</returns>
    public BoundingBox GenBoundingBox() {
        return this.MeshData.GenBoundingBox();
    }
    
    /// <summary>
    /// Generates tangent vectors for the mesh vertices.
    /// </summary>
    public void GenTangents() {
        this.MeshData.GenTangents();
    }
    
    /// <summary>
    /// Updates the value of a vertex in the mesh data.
    /// </summary>
    /// <param name="index">The index of the vertex to update.</param>
    /// <param name="value">The new vertex value.</param>
    public void SetVertexValue(int index, T value) {
        this.MeshData.Vertices[index] = value;
    }
    
    /// <summary>
    /// Updates the value of a vertex in the mesh data and uploads the change to the GPU immediately.
    /// </summary>
    /// <param name="index">The index of the vertex to update.</param>
    /// <param name="value">The new vertex value.</param>
    public void SetVertexValueImmediate(int index, T value) {
        this.MeshData.Vertices[index] = value;
        this.GraphicsDevice.UpdateBuffer(this.VertexBuffer, (uint) (index * Marshal.SizeOf<T>()), this.MeshData.Vertices[index]);
    }
    
    /// <summary>
    /// Updates the value of a vertex in the mesh data and schedules the change on the provided command list.
    /// </summary>
    /// <param name="commandList">The command list used to update the GPU buffer.</param>
    /// <param name="index">The index of the vertex to update.</param>
    /// <param name="value">The new vertex value.</param>
    public void SetVertexValueDeferred(CommandList commandList, int index, T value) {
        this.MeshData.Vertices[index] = value;
        commandList.UpdateBuffer(this.VertexBuffer, (uint) (index * Marshal.SizeOf<T>()), this.MeshData.Vertices[index]);
    }
    
    /// <summary>
    /// Uploads the entire vertex array to the GPU immediately.
    /// </summary>
    public void UpdateVertexBufferImmediate() {
        this.GraphicsDevice.UpdateBuffer(this.VertexBuffer, 0, this.MeshData.Vertices);
    }
    
    /// <summary>
    /// Uploads the entire vertex array to the GPU using the provided command list.
    /// </summary>
    /// <param name="commandList">The command list used to update the GPU buffer.</param>
    public void UpdateVertexBuffer(CommandList commandList) {
        commandList.UpdateBuffer(this.VertexBuffer, 0, this.MeshData.Vertices);
    }
    
    /// <summary>
    /// Updates the value of an index in the mesh data.
    /// </summary>
    /// <param name="index">The index of the index entry to update.</param>
    /// <param name="value">The new index value.</param>
    public void SetIndexValue(int index, uint value) {
        this.MeshData.Indices[index] = value;
    }
    
    /// <summary>
    /// Updates the value of an index in the mesh data and uploads the change to the GPU immediately.
    /// </summary>
    /// <param name="index">The index of the index entry to update.</param>
    /// <param name="value">The new index value.</param>
    public void SetIndexValueImmediate(int index, uint value) {
        this.MeshData.Indices[index] = value;
        this.GraphicsDevice.UpdateBuffer(this.IndexBuffer, (uint) index * sizeof(uint), this.MeshData.Indices[index]);
    }
    
    /// <summary>
    /// Updates the value of an index in the mesh data and schedules the change on the provided command list.
    /// </summary>
    /// <param name="commandList">The command list used to update the GPU buffer.</param>
    /// <param name="index">The index of the index entry to update.</param>
    /// <param name="value">The new index value.</param>
    public void SetIndexValueDeferred(CommandList commandList, int index, uint value) {
        this.MeshData.Indices[index] = value;
        commandList.UpdateBuffer(this.IndexBuffer, (uint) index * sizeof(uint), this.MeshData.Indices[index]);
    }
    
    /// <summary>
    /// Uploads the entire index array to the GPU immediately.
    /// </summary>
    public void UpdateIndexBufferImmediate() {
        this.GraphicsDevice.UpdateBuffer(this.IndexBuffer, 0, this.MeshData.Indices);
    }
    
    /// <summary>
    /// Uploads the entire index array to the GPU using the provided command list.
    /// </summary>
    /// <param name="commandList">The command list used to update the GPU buffer.</param>
    public void UpdateIndexBuffer(CommandList commandList) {
        commandList.UpdateBuffer(this.IndexBuffer, 0, this.MeshData.Indices);
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.VertexBuffer.Dispose();
            this.IndexBuffer?.Dispose();
        }
    }
}