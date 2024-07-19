using System.Reflection;

namespace Bliss.CSharp.Shaders;

public class Pipeline {

    public Pipeline(string vertPath, string fragPath) {
        this.Create(vertPath, fragPath);
    }

    private void Create(string vertPath, string fragPath) {
        byte[] vertSource = this.GetBytes(vertPath);
        byte[] fragSource = this.GetBytes(fragPath);

        Console.WriteLine($"shader bytes are {vertSource.Length} and {fragSource.Length}");
    }

    private byte[] GetBytes(string path) {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(s => s.EndsWith(path))!;
        
        if (resourceName == null) {
            throw new ApplicationException($"No shader file found in the Path [{path}]!");
        }

        using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
        using MemoryStream ms = new MemoryStream();
        
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}