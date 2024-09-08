using Bliss.CSharp.Logging;
using Veldrid;
using Veldrid.SPIRV;

namespace Bliss.CSharp.Effects;

public class Effect : Disposable {

    public readonly (Shader, Shader) Shader;
    public readonly VertexLayoutDescription VertexLayout;

    /// <summary>
    /// Initializes a new instance of the <see cref="Effect"/> class by loading and compiling shaders and setting up the vertex layout.
    /// </summary>
    /// <param name="resourceFactory">The resource factory used to create GPU resources.</param>
    /// <param name="vertexLayout">The vertex layout description to be used with this effect.</param>
    /// <param name="vertPath">The file path to the vertex shader source code.</param>
    /// <param name="fragPath">The file path to the fragment shader source code.</param>
    public Effect(ResourceFactory resourceFactory, VertexLayoutDescription vertexLayout, string vertPath, string fragPath) : this(resourceFactory, vertexLayout, LoadBytecode(vertPath), LoadBytecode(fragPath)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Effect"/> class with the specified vertex layout and shader bytecode.
    /// </summary>
    /// <param name="resourceFactory">The resource factory used to create shaders and resources.</param>
    /// <param name="vertexLayout">The layout of the vertex data for this effect.</param>
    /// <param name="vertBytes">A byte array containing the vertex shader bytecode.</param>
    /// <param name="fragBytes">A byte array containing the fragment shader bytecode.</param>
    public Effect(ResourceFactory resourceFactory, VertexLayoutDescription vertexLayout, byte[] vertBytes, byte[] fragBytes) {
        ShaderDescription vertDescription = new ShaderDescription(ShaderStages.Vertex, vertBytes, "main");
        ShaderDescription fragDescription = new ShaderDescription(ShaderStages.Fragment, fragBytes, "main");
        
        Shader[] shaders = resourceFactory.CreateFromSpirv(vertDescription, fragDescription);

        this.Shader.Item1 = shaders[0];
        this.Shader.Item2 = shaders[1];
        
        this.VertexLayout = vertexLayout;
    }

    /// <summary>
    /// Loads the bytecode from the specified shader file path.
    /// </summary>
    /// <param name="path">The file path to the shader source code.</param>
    /// <returns>A byte array containing the bytecode from the shader file.</returns>
    private static byte[] LoadBytecode(string path) {
        if (!File.Exists(path)) {
            throw new Exception($"No shader file found in the path: [{path}]");
        }
        
        if (Path.GetExtension(path) != ".vert" && Path.GetExtension(path) != ".frag") {
            throw new Exception($"This shader type is not supported: [{Path.GetExtension(path)}]");
        }
        
        Logger.Info($"Successfully loaded shader bytes from the path: [{path}]");
        return File.ReadAllBytes(path);
    }

    // TODO: ADD MATERIAL param here.
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