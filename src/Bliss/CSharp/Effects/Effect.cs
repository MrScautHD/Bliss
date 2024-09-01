using Bliss.CSharp.Logging;
using Veldrid;
using Veldrid.SPIRV;

namespace Bliss.CSharp.Effects;

// TODO: Add Load from Stream method, in generel by all things like by the model too.
public class Effect : Disposable {

    public readonly (Shader, Shader) Shader;

    public Effect(ResourceFactory resourceFactory, string vertPath, string fragPath) {
        ShaderDescription vertDescription = new ShaderDescription(ShaderStages.Vertex, this.LoadBytecode(vertPath), "main");
        ShaderDescription fragDescription = new ShaderDescription(ShaderStages.Fragment, this.LoadBytecode(fragPath), "main");

        Shader[] shaders = resourceFactory.CreateFromSpirv(vertDescription, fragDescription);

        this.Shader.Item1 = shaders[0];
        this.Shader.Item2 = shaders[1];
    }
    
    private byte[] LoadBytecode(string path) {
        if (!File.Exists(path) || (Path.GetExtension(path) != ".vert" && Path.GetExtension(path) != ".frag")) {
            throw new ApplicationException($"No shader file found in the path: [{path}]");
        }
        
        Logger.Info($"Successfully loaded shader bytes from the path: [{path}]");
        return File.ReadAllBytes(path);
    }

    /// <summary>
    /// Apply the state effect immediately before rendering it.
    /// </summary>
    public virtual void Apply() { }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.Shader.Item1.Dispose();
            this.Shader.Item2.Dispose();
        }
    }
}