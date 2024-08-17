using Bliss.CSharp.Logging;
using Veldrid;
using Veldrid.SPIRV;

namespace Bliss.CSharp.Shaders;

public static class ShaderHelper {
    
    /// <summary>
    /// Loads vertex and fragment shaders from the provided file paths and creates a shader resource from them.
    /// </summary>
    /// <param name="resourceFactory">The resource factory to use for creating the shader.</param>
    /// <param name="vertPath">The file path of the vertex shader.</param>
    /// <param name="fragPath">The file path of the fragment shader.</param>
    /// <returns>A tuple containing the vertex shader and fragment shader as Shader objects.</returns>
    public static (Shader, Shader) Load(ResourceFactory resourceFactory, string vertPath, string fragPath) {
        ShaderDescription vertDescription = new ShaderDescription(ShaderStages.Vertex, LoadBytecode(vertPath), "main");
        ShaderDescription fragDescription = new ShaderDescription(ShaderStages.Fragment, LoadBytecode(fragPath), "main");

        Shader[] shaders = resourceFactory.CreateFromSpirv(vertDescription, fragDescription);
        return (shaders[0], shaders[1]);
    }

    /// <summary>
    /// Loads the bytecode of a shader from the provided file path.
    /// </summary>
    /// <param name="path">The file path of the shader.</param>
    /// <returns>The bytecode of the shader as a byte array.</returns>
    public static byte[] LoadBytecode(string path) {
        if (!File.Exists(path) || (Path.GetExtension(path) != ".vert" && Path.GetExtension(path) != ".frag")) {
            throw new ApplicationException($"No shader file found in the path: [{path}]");
        }
        
        Logger.Info($"Successfully loaded shader bytes from the path: [{path}]");
        return File.ReadAllBytes(path);
    }
}