using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Rendering.Vulkan;
using Silk.NET.Assimp;
using Silk.NET.Vulkan;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Bliss.CSharp.Geometry;

public class Model : Disposable {
    
    public readonly Vk Vk;
    public readonly BlissDevice Device;
    
    public Mesh[] Meshes { get; private set; }

    private BlissBuffer _vertexBuffer;
    private BlissBuffer _indexBuffer;

    private uint _vertexCount;
    private uint _indexCount;

    private bool _hasIndexBuffer;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Model"/> class with the specified Vulkan instance, device, and meshes.
    /// </summary>
    /// <param name="vk">The Vulkan instance.</param>
    /// <param name="device">The Bliss device.</param>
    /// <param name="meshes">An array of <see cref="Mesh"/> objects.</param>
    public Model(Vk vk, BlissDevice device, Mesh[] meshes) {
        this.Vk = vk;
        this.Device = device;
        this.Meshes = meshes;
        
        this.CreateVertexBuffer();
        this.CreateIndexBuffer();
    }

    /// <summary>
    /// Loads a 3D model from a file using the Assimp library and creates a Bliss Model object.
    /// </summary>
    /// <param name="vk">The Vk instance.</param>
    /// <param name="device">The BlissDevice instance.</param>
    /// <param name="path">The file path of the model.</param>
    /// <returns>A new Model object representing the loaded 3D model.</returns>
    public static unsafe Model Load(Vk vk, BlissDevice device, string path) {
        Assimp assimp = Assimp.GetApi();
        Scene* scene = assimp.ImportFile(path, (uint) PostProcessPreset.TargetRealTimeMaximumQuality);

        List<Mesh> meshes = new List<Mesh>();

        for (int i = 0; i < scene->MNumMeshes; i++) {
            AssimpMesh* mesh = scene->MMeshes[i];
            
            Vertex[] vertices = new Vertex[mesh->MNumVertices];
            uint[] indices = new uint[mesh->MNumFaces * 3];
            
            for (int j = 0; j < mesh->MNumVertices; j++) {
                vertices[j] = new Vertex {
                    Position = mesh->MVertices[j]
                };
                
                if (mesh->MTextureCoords[0] != null) {
                    Vector3 texCoords = mesh->MTextureCoords[0][i];
                    vertices[j].TexCoords = new Vector2(texCoords.X, texCoords.Y);
                }
                
                if (mesh->MTextureCoords[1] != null) {
                    Vector3 texCoords2 = mesh->MTextureCoords[1][i];
                    vertices[j].TexCoords2 = new Vector2(texCoords2.X, texCoords2.Y);
                }
                
                if (mesh->MNormals != null) {
                    vertices[j].Normal = mesh->MNormals[j];
                }
                
                if (mesh->MTangents != null) {
                    vertices[j].Tangent = mesh->MTangents[j];
                }
                
                //if (mesh->MColors[0][j] != null) {
                //    //Color color = new Color()
                //    vertices[j].Color = mesh->MColors[0][j];
                //}
                vertices[j].Color = Color.White;
            }

            for (int j = 0; j < mesh->MNumFaces; j++) {
                Face face = mesh->MFaces[j];
                
                for (int k = 0; k < 3; k++) {
                    indices[j * 3 + k] = face.MIndices[k];
                }
            }

            meshes.Add(new Mesh(vertices, indices));
        }
        
        return new Model(vk, device, meshes.ToArray());
    }

    /// <summary>
    /// Creates a Vulkan vertex buffer for the model's vertices.
    /// </summary>
    private void CreateVertexBuffer() {
        List<Vertex> vertices = new List<Vertex>();

        foreach (Mesh mesh in this.Meshes) {
            foreach (Vertex vertex in mesh.Vertices) {
                vertices.Add(vertex);
            }
        }

        this._vertexCount = (uint) vertices.Count;
        
        ulong instanceSize = (ulong) Marshal.SizeOf<Vertex>();
        ulong bufferSize = instanceSize * (ulong) vertices.Count;
        
        BlissBuffer stagingBuffer = new(this.Vk, this.Device, instanceSize, (uint) vertices.Count, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        stagingBuffer.Map();
        stagingBuffer.WriteToBuffer(vertices.ToArray());

        this._vertexBuffer = new(this.Vk, this.Device, instanceSize, (uint) vertices.Count, BufferUsageFlags.VertexBufferBit | BufferUsageFlags.TransferDstBit, MemoryPropertyFlags.DeviceLocalBit);
        this.Device.CopyBuffer(stagingBuffer.VkBuffer, this._vertexBuffer.VkBuffer, bufferSize);
    }

    /// <summary>
    /// Creates a Vulkan index buffer for the model's indices.
    /// </summary>
    private void CreateIndexBuffer() {
        List<uint> indices = new List<uint>();

        foreach (Mesh mesh in this.Meshes) {
            foreach (uint index in mesh.Indices) {
                indices.Add(index);
            }
        }

        if (indices.Count == 0) {
            return;
        }

        this._indexCount = (uint) indices.Count;
        this._hasIndexBuffer = true;
        
        var instanceSize = (ulong) Marshal.SizeOf<uint>();
        ulong bufferSize = instanceSize * (ulong) indices.Count;

        BlissBuffer stagingBuffer = new(this.Vk, this.Device, instanceSize, (uint) indices.Count, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        stagingBuffer.Map();
        stagingBuffer.WriteToBuffer(indices.ToArray());

        this._indexBuffer = new(this.Vk, this.Device, instanceSize, (uint) indices.Count, BufferUsageFlags.IndexBufferBit | BufferUsageFlags.TransferDstBit, MemoryPropertyFlags.DeviceLocalBit);
        this.Device.CopyBuffer(stagingBuffer.VkBuffer, this._indexBuffer.VkBuffer, bufferSize);
    }

    /// <summary>
    /// Binds the vertex and index buffers for rendering the model to the specified command buffer.
    /// </summary>
    /// <param name="commandBuffer">The command buffer to bind the buffers to.</param>
    public unsafe void Bind(CommandBuffer commandBuffer) {
        Buffer[] vertexBuffers = new Buffer[] {
            this._vertexBuffer.VkBuffer
        };
        
        ulong[] offsets = new ulong[] {
            0
        };

        fixed (ulong* offsetsPtr = offsets) {
            fixed (Buffer* vertexBuffersPtr = vertexBuffers) {
                this.Vk.CmdBindVertexBuffers(commandBuffer, 0, 1, vertexBuffersPtr, offsetsPtr);
            }
        }

        if (this._hasIndexBuffer) {
            this.Vk.CmdBindIndexBuffer(commandBuffer, this._indexBuffer.VkBuffer, 0, IndexType.Uint32);
        }
    }

    /// <summary>
    /// Renders the model by binding the vertex and index buffers to the specified command buffer and issuing draw commands.
    /// </summary>
    /// <param name="commandBuffer">The command buffer to render the model to.</param>
    public void Draw(CommandBuffer commandBuffer) {
        if (this._hasIndexBuffer) {
            this.Vk.CmdDrawIndexed(commandBuffer, this._indexCount, 1, 0, 0, 0);
        }
        else {
            this.Vk.CmdDraw(commandBuffer, this._vertexCount, 1, 0, 0);
        }
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._vertexBuffer.Dispose();
            
            if (this._hasIndexBuffer) {
                this._indexBuffer.Dispose();
            }
            
            Array.Clear(this.Meshes);
        }
    }
}