using System.Reflection;

namespace Bliss.CSharp;

public static class Resource {
    
    /// <summary>
    /// The assembly containing the embedded resources of this class.
    /// </summary>
    public static readonly Assembly BlissAssembly = typeof(Resource).Assembly;
    
    /// <summary>
    /// Opens an embedded resource from the Bliss assembly.
    /// </summary>
    /// <param name="name">The fully qualified resource name.</param>
    /// <returns>A stream containing the resource data.</returns>
    public static Stream Load(string name) {
        return Load(BlissAssembly, name);
    }
    
    /// <summary>
    /// Loads an embedded resource from the Bliss assembly into a byte array.
    /// </summary>
    /// <param name="name">The fully qualified resource name.</param>
    /// <returns>A byte array containing the resource data.</returns>
    public static byte[] LoadBytes(string name) {
        return LoadBytes(BlissAssembly, name);
    }
    
    /// <summary>
    /// Opens an embedded resource from the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly containing the resource.</param>
    /// <param name="name">The fully qualified resource name.</param>
    /// <returns>A stream containing the resource data.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the resource does not exist.</exception>
    public static Stream Load(Assembly assembly, string name) {
        return assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{name}") ?? throw new FileNotFoundException($"Failed to find embedded resource! [{name}]");
    }
    
    /// <summary>
    /// Loads an embedded resource into a byte array.
    /// </summary>
    /// <param name="assembly">The assembly containing the resource.</param>
    /// <param name="name">The fully qualified resource name.</param>
    /// <returns>The resource data.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the resource does not exist.</exception>
    public static byte[] LoadBytes(Assembly assembly, string name) {
        using Stream stream = Load(assembly, name);
        using MemoryStream memoryStream = new MemoryStream();
        
        stream.CopyTo(memoryStream);
        
        return memoryStream.ToArray();
    }
}