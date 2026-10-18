using System.Text.RegularExpressions;

namespace Bliss.CSharp.Effects;

public class ShaderIncludeProcessor {
    
    /// <summary>
    /// Matches a whole include line, such as <c>#include "lighting/pbr.glsl"</c> or <c>#include &lt;common.glsl&gt;</c>, and captures the path in the group <c>path</c>.
    /// </summary>
    private static readonly Regex IncludeRegex = new Regex(@"^[ \t]*#include[ \t]+[<""](?<path>[^>""]+)[>""][ \t]*\r?$", RegexOptions.Compiled | RegexOptions.Multiline);
    
    /// <summary>
    /// Compares file paths the way the file system does, so the same file is not inserted twice because of its spelling.
    /// </summary>
    private static readonly StringComparer PathComparer = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    
    /// <summary>
    /// The root directory that includes are resolved against when they are not found next to the including file.
    /// </summary>
    public readonly string RootDirectory;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ShaderIncludeProcessor"/> class.
    /// </summary>
    /// <param name="rootDirectory">The root directory used to resolve include directives.</param>
    public ShaderIncludeProcessor(string rootDirectory) {
        this.RootDirectory = Path.GetFullPath(rootDirectory);
    }
    
    /// <summary>
    /// Loads a shader file and resolves all of its include directives.
    /// </summary>
    /// <param name="path">The path of the shader file. It is looked up relative to <see cref="RootDirectory"/> first, and then as given (relative to the working directory, or absolute).</param>
    /// <returns>The shader source with all includes inserted.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the shader file or one of its includes cannot be found.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the includes form a cycle.</exception>
    public string ProcessFile(string path) {
        
        // Relative to the shader root first, like includes. An absolute path is returned unchanged by Path.Combine.
        string rootPath = Path.GetFullPath(Path.Combine(this.RootDirectory, path));
        
        // Then as given, so the same path works as with the overload that has no shader root.
        string fullPath = File.Exists(rootPath) ? rootPath : Path.GetFullPath(path);
        
        if (!File.Exists(fullPath)) {
            throw new FileNotFoundException($"Shader file was not found: [{path}]. Searched: [{rootPath}] and [{fullPath}]");
        }
        
        // The set is empty at this point, so the first file is never skipped and the result is never null.
        return this.ProcessFileInternal(fullPath, new Stack<string>(), new HashSet<string>(PathComparer))!;
    }
    
    /// <summary>
    /// Resolves all include directives of shader source that is already in memory.
    /// </summary>
    /// <param name="source">The shader source code.</param>
    /// <returns>The shader source with all includes inserted. Includes are resolved relative to <see cref="RootDirectory"/>.</returns>
    /// <exception cref="FileNotFoundException">Thrown if an include cannot be found.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the includes form a cycle.</exception>
    public string ProcessSource(string source) {
        return this.ProcessSourceInternal(source, this.RootDirectory, new Stack<string>(), new HashSet<string>(PathComparer));
    }
    
    /// <summary>
    /// Reads a file that was already resolved and processes the includes inside of it.
    /// </summary>
    /// <param name="fullPath">The full path of an existing file.</param>
    /// <param name="includeStack">The files that are currently being processed, from the outermost to the innermost. It is used to detect cycles.</param>
    /// <param name="includedFiles">The files that were already inserted into the result. It is used as the include guard.</param>
    /// <returns>The processed source of the file, or <see langword="null"/> if the file was already included.</returns>
    private string? ProcessFileInternal(string fullPath, Stack<string> includeStack, HashSet<string> includedFiles) {
        
        // A file that is already being processed includes itself, directly or through other files.
        if (includeStack.Contains(fullPath, PathComparer)) {
            string chain = string.Join(" -> ", includeStack.Reverse().Append(fullPath));
            throw new InvalidOperationException($"Circular shader include detected: {chain}");
        }
        
        // Include guard: a file that was already inserted is skipped, so shared code never appears twice.
        if (!includedFiles.Add(fullPath)) {
            return null;
        }
        
        string source = File.ReadAllText(fullPath);
        string currentDirectory = Path.GetDirectoryName(fullPath)!;
        
        // Includes inside this file are looked up next to it.
        includeStack.Push(fullPath);
        string result = this.ProcessSourceInternal(source, currentDirectory, includeStack, includedFiles);
        includeStack.Pop();
        
        return result;
    }
    
    /// <summary>
    /// Replaces every include line of the source with the processed content of the included file.
    /// </summary>
    /// <param name="source">The shader source code.</param>
    /// <param name="currentDirectory">The directory that includes are looked up in first.</param>
    /// <param name="includeStack">The files that are currently being processed. It is used to detect cycles.</param>
    /// <param name="includedFiles">The files that were already inserted into the result. It is used as the include guard.</param>
    private string ProcessSourceInternal(string source, string currentDirectory, Stack<string> includeStack, HashSet<string> includedFiles) {
        return IncludeRegex.Replace(source, match => {
            string includePath = match.Groups["path"].Value;
            string resolvedPath = this.ResolvePath(includePath, currentDirectory);
            string? content = this.ProcessFileInternal(resolvedPath, includeStack, includedFiles);
            
            // The file is already part of the result, so only a note is left behind.
            if (content == null) {
                return $"{Environment.NewLine}// Skipped include (already included): {includePath}{Environment.NewLine}";
            }
            
            // The markers show where the code comes from, which helps when reading compiler errors of the merged source.
            return $"{Environment.NewLine}// Begin include: {includePath}{Environment.NewLine}{content}{Environment.NewLine}// End include: {includePath}{Environment.NewLine}";
        });
    }
    
    /// <summary>
    /// Finds the file for an include path.
    /// </summary>
    /// <param name="path">The path as written in the include directive.</param>
    /// <param name="baseDirectory">The directory that is searched first, usually the directory of the including file.</param>
    /// <returns>The full path of the file.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file exists neither in the base directory nor in the root directory.</exception>
    private string ResolvePath(string path, string baseDirectory) {
        
        // Next to the including file.
        string localPath = Path.GetFullPath(Path.Combine(baseDirectory, path));
        
        if (File.Exists(localPath)) {
            return localPath;
        }
        
        // Relative to the shader root.
        string rootPath = Path.GetFullPath(Path.Combine(this.RootDirectory, path));
        
        if (File.Exists(rootPath)) {
            return rootPath;
        }
        
        throw new FileNotFoundException($"Shader include could not be resolved: [{path}]. Searched: [{localPath}] and [{rootPath}]");
    }
}