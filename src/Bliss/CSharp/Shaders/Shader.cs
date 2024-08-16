using Bliss.CSharp.Logging;
using Bliss.CSharp.Vulkan;
using Silk.NET.Vulkan;

namespace Bliss.CSharp.Shaders;

public class Shader : Disposable {
    
    public readonly Vk Vk;
    public readonly BlissDevice Device;

    public readonly byte[] VertBytes;
    public readonly byte[] FragBytes;
    
    public readonly ShaderModule VertShaderModule;
    public readonly ShaderModule FragShaderModule;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Shader"/> class with specified vertex and fragment shader paths.
    /// </summary>
    /// <param name="vk">The Vulkan instance.</param>
    /// <param name="device">The Bliss device.</param>
    /// <param name="vertPath">The path to the vertex shader file.</param>
    /// <param name="fragPath">The path to the fragment shader file.</param>
    public Shader(Vk vk, BlissDevice device, string vertPath, string fragPath) {
        this.Vk = vk;
        this.Device = device;
        this.VertBytes = this.GetShaderBytes(vertPath);
        this.FragBytes = this.GetShaderBytes(fragPath);
        this.VertShaderModule = this.CreateShaderModule(this.VertBytes);
        this.FragShaderModule = this.CreateShaderModule(this.FragBytes);
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
    
    /// <summary>
    /// Creates a shader module from the given byte code.
    /// </summary>
    /// <param name="code">The byte array containing the shader code.</param>
    /// <returns>The created shader module.</returns>
    private unsafe ShaderModule CreateShaderModule(byte[] code) {
        fixed (byte* codePtr = code) {
            ShaderModuleCreateInfo createInfo = new() {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint) code.Length,
                PCode = (uint*) codePtr
            };
            
            if (this.Vk.CreateShaderModule(this.Device.GetVkDevice(), createInfo, null, out ShaderModule shaderModule) == Result.Success) {
                return shaderModule;
            }
        }

        throw new Exception("An error occurred while loading the shader module.");
    }

    protected override unsafe void Dispose(bool disposing) {
        if (disposing) {
            this.Vk.DestroyShaderModule(this.Device.GetVkDevice(), this.VertShaderModule, null);
            this.Vk.DestroyShaderModule(this.Device.GetVkDevice(), this.FragShaderModule, null);
        }
    }
}