using Bliss.CSharp.Logging;

namespace Bliss.CSharp.Shaders;

public class Shader : Disposable {
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Shader"/> class with specified vertex and fragment shader paths.
    /// </summary>
    /// <param name="vk">The Vulkan instance.</param>
    /// <param name="device">The Bliss device.</param>
    /// <param name="vertPath">The path to the vertex shader file.</param>
    /// <param name="fragPath">The path to the fragment shader file.</param>
    public Shader(string vertPath, string fragPath) {
    }
    
    /// <summary>
    /// Retrieves the byte code of a shader from a specified file path.
    /// </summary>
    /// <param name="path">The path to the shader file.</param>
    /// <returns>The byte array containing the shader code.</returns>
    private byte[] GetShaderBytes(string path) {
        string finalPath = $"{path}.spv";
        
        if (!File.Exists(finalPath) || (Path.GetExtension(path) != ".frag" && Path.GetExtension(path) != ".vert")) {
            throw new ApplicationException($"No shader file found in the path: [{path}]");
        }
        
        Logger.Info($"Successfully loaded shader bytes from the path: [{finalPath}]");
        return File.ReadAllBytes(finalPath);
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            
        }
    }
}