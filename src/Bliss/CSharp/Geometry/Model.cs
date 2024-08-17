using System.Numerics;
using System.Runtime.InteropServices;
using Assimp;
using Bliss.CSharp.Colors;

namespace Bliss.CSharp.Geometry;

public class Model : Disposable {
    public Mesh[] Meshes { get; private set; }

    private uint _vertexCount;
    private uint _indexCount;

    private bool _hasIndexBuffer;
    
    public Model(Mesh[] meshes) {
        this.Meshes = meshes;
    }

    /// <summary>
    /// Loads a 3D model from a file using the Assimp library and creates a Bliss Model object.
    /// </summary>
    /// <param name="vk">The Vk instance.</param>
    /// <param name="device">The BlissDevice instance.</param>
    /// <param name="path">The file path of the model.</param>
    /// <returns>A new Model object representing the loaded 3D model.</returns>
    /*public static unsafe Model Load(Vk vk, BlissDevice device, string path) {
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
    }*/

    protected override void Dispose(bool disposing) {
        if (disposing) {
            Array.Clear(this.Meshes);
        }
    }
}