using System.Reflection;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.Pipelines.Textures;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Materials;
using Veldrith;
using Veldrith.SPIRV;

namespace Bliss.CSharp.Effects;

public class Effect : Disposable {
    
    /// <summary>
    /// The graphics device used for creating and managing graphical resources.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// Indicates whether this effect was compiled as a variant of another effect.
    /// </summary>
    public bool IsVariant { get; private set; }
    
    /// <summary>
    /// Specialization constants applied to the shaders when creating pipelines from this effect.
    /// </summary>
    public IReadOnlyList<SpecializationConstant> Specializations { get; private set; }
    
    /// <summary>
    /// Preprocessor macro definitions used when compiling shader source code.
    /// </summary>
    public IReadOnlyList<MacroDefinition> Macros { get; private set; }
    
    /// <summary>
    /// Represents a pair of shaders consisting of a vertex shader and a fragment shader.
    /// </summary>
    public readonly (Shader VertShader, Shader FragShader) Shader;
    
    /// <summary>
    /// An array of shader objects used within an effect for rendering operations.
    /// </summary>
    public readonly Shader[] Shaders;
    
    /// <summary>
    /// The GLSL vertex shader source code, if this effect was created from source.
    /// </summary>
    private readonly string? _vertText;
    
    /// <summary>
    /// The GLSL fragment shader source code, if this effect was created from source.
    /// </summary>
    private readonly string? _fragText;
    
    /// <summary>
    /// The cross compile options used for shader creation.
    /// </summary>
    private readonly CrossCompileOptions _compileOptions;
    
    /// <summary>
    /// A collection of buffer layout descriptions used to define buffer bindings.
    /// </summary>
    private Dictionary<uint, SimpleBufferLayout> _bufferLayouts;
    
    /// <summary>
    /// A collection of texture layout descriptions used to define how textures bindings.
    /// </summary>
    private Dictionary<uint, SimpleTextureLayout> _textureLayouts;
    
    /// <summary>
    /// A cache of pipelines created for specific pipeline descriptions, enabling reuse.
    /// </summary>
    private Dictionary<SimplePipelineDescription, SimplePipeline> _cachedPipelines;
    
    /// <summary>
    /// A cache of effect variants compiled for specific macro/layout combinations.
    /// </summary>du
    private Dictionary<EffectVariantKey, EffectVariant> _cachedVariants;
    
    /// <summary>
    /// Initializes a new <see cref="Effect"/> using multiple vertex layouts and shader bytecode loaded from file paths.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create shaders and related GPU resources.</param>
    /// <param name="vertPath">The file path to the compiled vertex shader bytecode.</param>
    /// <param name="fragPath">The file path to the compiled fragment shader bytecode.</param>
    /// <param name="compileOptions">Optional cross-compilation options used when creating the shaders.</param>
    public Effect(GraphicsDevice graphicsDevice, string vertPath, string fragPath, CrossCompileOptions compileOptions) : this(graphicsDevice, LoadBytecodeFromFile(vertPath), LoadBytecodeFromFile(fragPath), compileOptions) { }
    
    /// <summary>
    /// Initializes a new <see cref="Effect"/> using shader bytecode read from streams.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create shaders and related GPU resources.</param>
    /// <param name="vertStream">The stream containing the compiled vertex shader bytecode.</param>
    /// <param name="fragStream">The stream containing the compiled fragment shader bytecode.</param>
    /// <param name="compileOptions">Optional cross-compilation options used when creating the shaders.</param>
    public Effect(GraphicsDevice graphicsDevice, Stream vertStream, Stream fragStream, CrossCompileOptions compileOptions) : this(graphicsDevice, LoadBytecodeFromStream(vertStream), LoadBytecodeFromStream(fragStream), compileOptions) { }
    
    /// <summary>
    /// Initializes a new <see cref="Effect"/> using multiple vertex layouts and provided shader bytecode.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create shaders, pipelines, and related GPU resources.</param>
    /// <param name="vertBytes">The compiled vertex shader bytecode.</param>
    /// <param name="fragBytes">The compiled fragment shader bytecode.</param>
    /// <param name="compileOptions">Optional cross-compilation options used when creating the shaders.</param>
    public Effect(GraphicsDevice graphicsDevice, byte[] vertBytes, byte[] fragBytes, CrossCompileOptions compileOptions) {
        this.GraphicsDevice = graphicsDevice;
        this.Specializations = compileOptions.Specializations;
        this.Macros = [];
        this._vertText = null;
        this._fragText = null;
        this._compileOptions = compileOptions;
        
        ShaderDescription vertDescription = new ShaderDescription(ShaderStages.Vertex, vertBytes, "main");
        ShaderDescription fragDescription = new ShaderDescription(ShaderStages.Fragment, fragBytes, "main");
        
        this.Shaders = graphicsDevice.ResourceFactory.CreateFromSpirv(vertDescription, fragDescription, compileOptions);
        this.Shader.VertShader = this.Shaders[0];
        this.Shader.FragShader = this.Shaders[1];
        
        this._bufferLayouts = new Dictionary<uint, SimpleBufferLayout>();
        this._textureLayouts = new Dictionary<uint, SimpleTextureLayout>();
        this._cachedPipelines = new Dictionary<SimplePipelineDescription, SimplePipeline>();
        this._cachedVariants = new Dictionary<EffectVariantKey, EffectVariant>();
    }
    
    /// <summary>
    /// Creates a new <see cref="Effect"/> by compiling GLSL shader source code into SPIR-V.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create GPU resources.</param>
    /// <param name="vertText">The GLSL source code for the vertex shader.</param>
    /// <param name="fragText">The GLSL source code for the fragment shader.</param>
    /// <param name="compileOptions">Optional cross-compilation and specialization options.</param>
    /// <param name="macros">Optional macro definitions injected during shader compilation.</param>
    public Effect(GraphicsDevice graphicsDevice, string vertText, string fragText, CrossCompileOptions compileOptions, MacroDefinition[] macros) {
        this.GraphicsDevice = graphicsDevice;
        this.Specializations = compileOptions.Specializations;
        this.Macros = macros;
        this._vertText = vertText;
        this._fragText = fragText;
        this._compileOptions = compileOptions;
        
        GlslCompileOptions glslOptions = new GlslCompileOptions(true, macros);
        SpirvCompilationResult vertResult = SpirvCompilation.CompileGlslToSpirv(vertText, nameof(ShaderStages.Vertex), ShaderStages.Vertex, glslOptions);
        SpirvCompilationResult fragResult = SpirvCompilation.CompileGlslToSpirv(fragText, nameof(ShaderStages.Fragment), ShaderStages.Fragment, glslOptions);
        
        ShaderDescription vertDescription = new ShaderDescription(ShaderStages.Vertex, vertResult.SpirvBytes, "main");
        ShaderDescription fragDescription = new ShaderDescription(ShaderStages.Fragment, fragResult.SpirvBytes, "main");
        
        this.Shaders = graphicsDevice.ResourceFactory.CreateFromSpirv(vertDescription, fragDescription, compileOptions);
        this.Shader.VertShader = this.Shaders[0];
        this.Shader.FragShader = this.Shaders[1];
        
        this._bufferLayouts = new Dictionary<uint, SimpleBufferLayout>();
        this._textureLayouts = new Dictionary<uint, SimpleTextureLayout>();
        this._cachedPipelines = new Dictionary<SimplePipelineDescription, SimplePipeline>();
        this._cachedVariants = new Dictionary<EffectVariantKey, EffectVariant>();
    }
    
    /// <summary>
    /// Creates a new <see cref="Effect"/> by loading GLSL shader source code from files and compiling it into SPIR-V.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create GPU resources.</param>
    /// <param name="vertPath">The file path to the GLSL vertex shader source (.vert).</param>
    /// <param name="fragPath">The file path to the GLSL fragment shader source (.frag).</param>
    /// <param name="compileOptions">Cross-compilation and specialization options.</param>
    /// <param name="macros">Optional macro definitions injected during shader compilation. If <see langword="null"/>, no macros are defined.</param>
    /// <returns>The compiled effect.</returns>
    /// <exception cref="Exception">Thrown if a shader file does not exist or has an unsupported file type.</exception>
    public static Effect FromFiles(GraphicsDevice graphicsDevice, string vertPath, string fragPath, CrossCompileOptions compileOptions, MacroDefinition[]? macros = null) {
        return new Effect(graphicsDevice, LoadTextCodeFromFile(vertPath), LoadTextCodeFromFile(fragPath), compileOptions, macros ?? []);
    }
    
    /// <summary>
    /// Creates a new <see cref="Effect"/> by loading GLSL shader source code from files, resolving its include directives, and compiling it into SPIR-V.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create GPU resources.</param>
    /// <param name="shaderRoot">The root directory that is used to resolve the include directives.</param>
    /// <param name="vertPath">The file path to the GLSL vertex shader source (.vert).</param>
    /// <param name="fragPath">The file path to the GLSL fragment shader source (.frag).</param>
    /// <param name="compileOptions">Cross-compilation and specialization options.</param>
    /// <param name="macros">Optional macro definitions injected during shader compilation. If <see langword="null"/>, no macros are defined.</param>
    /// <returns>The compiled effect.</returns>
    public static Effect FromFiles(GraphicsDevice graphicsDevice, string shaderRoot, string vertPath, string fragPath, CrossCompileOptions compileOptions, MacroDefinition[]? macros = null) {
        ShaderIncludeProcessor processor = new ShaderIncludeProcessor(shaderRoot);
        
        string vertText = processor.ProcessFile(vertPath);
        string fragText = processor.ProcessFile(fragPath);
        
        return new Effect(graphicsDevice, vertText, fragText, compileOptions, macros ?? []);
    }
    
    /// <summary>
    /// Loads the bytecode from the specified shader file path.
    /// </summary>
    /// <param name="path">The file path to the shader source code.</param>
    /// <returns>A byte array containing the bytecode from the shader file.</returns>
    /// <exception cref="Exception">Thrown if the file does not exist or the file type is not supported.</exception>
    public static byte[] LoadBytecodeFromFile(string path) {
        if (!File.Exists(path)) {
            throw new Exception($"No shader file found in path: [{path}]");
        }
        
        if (Path.GetExtension(path) != ".vert" && Path.GetExtension(path) != ".frag") {
            throw new Exception($"This shader type is not supported: [{Path.GetExtension(path)}]");
        }
        
        byte[] source = File.ReadAllBytes(path);
        
        Logger.Info($"Shader bytes loaded successfully from path: [{path}]");
        return source;
    }
    
    /// <summary>
    /// Loads bytecode from an embedded shader resource.
    /// </summary>
    /// <param name="assembly">The assembly containing the embedded shader resource.</param>
    /// <param name="name">The fully qualified resource name.</param>
    /// <returns>A byte array containing the shader bytecode.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the resource does not exist.</exception>
    /// <exception cref="Exception">Thrown if the shader file type is not supported.</exception>
    public static byte[] LoadBytecodeFromResource(Assembly assembly, string name) {
        string extension = Path.GetExtension(name);
        
        if (extension != ".vert" && extension != ".frag") {
            throw new Exception($"This shader type is not supported: [{extension}]");
        }
        
        byte[] bytes = Resource.LoadBytes(assembly, name);
        
        Logger.Info($"Shader bytecode loaded successfully from resource: [{name}]");
        return bytes;
    }
    
    /// <summary>
    /// Loads bytecode from the specified stream.
    /// </summary>
    /// <param name="stream">The stream containing the shader bytecode.</param>
    /// <returns>A byte array containing the shader bytecode.</returns>
    public static byte[] LoadBytecodeFromStream(Stream stream) {
        using MemoryStream memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        
        byte[] bytes = memoryStream.ToArray();
        
        Logger.Info("Shader bytecode loaded successfully from stream.");
        return bytes;
    }
    
    /// <summary>
    /// Loads shader source text from a file at the specified path.
    /// </summary>
    /// <param name="path">The file path to the shader source file. Must have a supported extension (.vert or .frag).</param>
    /// <returns>The contents of the shader source file as a string.</returns>
    /// <exception cref="Exception">Thrown if the file does not exist or the file type is not supported.</exception>
    public static string LoadTextCodeFromFile(string path) {
        if (!File.Exists(path)) {
            throw new Exception($"No shader file found in path: [{path}]");
        }
        
        if (Path.GetExtension(path) != ".vert" && Path.GetExtension(path) != ".frag") {
            throw new Exception($"This shader type is not supported: [{Path.GetExtension(path)}]");
        }
        
        string source = File.ReadAllText(path);
        
        Logger.Info($"Shader bytes loaded successfully from path: [{path}]");
        return source;
    }
    
    /// <summary>
    /// Loads shader source text from an embedded shader resource.
    /// </summary>
    /// <param name="assembly">The assembly containing the embedded shader resource.</param>
    /// <param name="name">The fully qualified resource name.</param>
    /// <returns>The contents of the embedded shader resource as a string.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the resource does not exist.</exception>
    /// <exception cref="Exception">Thrown if the shader file type is not supported.</exception>
    public static string LoadTextCodeFromResource(Assembly assembly, string name) {
        string extension = Path.GetExtension(name);
        
        if (extension != ".vert" && extension != ".frag") {
            throw new Exception($"This shader type is not supported: [{extension}]");
        }
        
        using Stream stream = Resource.Load(assembly, name);
        using StreamReader reader = new(stream);
        
        string source = reader.ReadToEnd();
        
        Logger.Info($"Shader source loaded successfully from resource: [{name}]");
        return source;
    }
    
    /// <summary>
    /// Loads shader source text from the specified stream.
    /// </summary>
    /// <param name="stream">The stream containing the shader source code.</param>
    /// <returns>The contents of the stream as a string.</returns>
    public static string LoadTextCodeFromStream(Stream stream) {
        using StreamReader reader = new StreamReader(stream, leaveOpen: true);
        
        string source = reader.ReadToEnd();
        
        Logger.Info("Shader source loaded successfully from stream.");
        return source;
    }
    
    /// <summary>
    /// Retrieves an <see cref="EffectVariant"/> for the specified macros, compiling and caching it on the first request.
    /// </summary>
    /// <param name="macros">The additional preprocessor macros (name and value pairs) used to compile the variant. They must not redefine any of the effect's base macros, and the order matters when matching cached variants.</param>
    /// <returns>An <see cref="EffectVariant"/> instance created or retrieved from the cache.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the effect is not based on GLSL source code, or when it is itself a variant (variants cannot create further variants).</exception>
    /// <exception cref="ArgumentException">Thrown when one of the macros redefines a base macro of the effect.</exception>
    public EffectVariant GetEffectVariant(ReadOnlySpan<(string Name, string Value)> macros) {
        if (this.IsVariant) {
            throw new InvalidOperationException("Variants cannot create further variants. Call GetEffectVariant on the root effect with the full macro set.");
        }
        
        if (this._vertText == null || this._fragText == null) {
            throw new InvalidOperationException("Effect variants can only be created from GLSL source-based effects.");
        }
        
        // Check cache.
        foreach ((EffectVariantKey cachedKey, EffectVariant cachedVariant) in this._cachedVariants) {
            if (cachedKey.Matches(macros)) {
                return cachedVariant;
            }
        }
        
        // Reject macros that would redefine a base macro (only reached on a cache miss).
        foreach ((string Name, string Value) macro in macros) {
            for (int i = 0; i < this.Macros.Count; i++) {
                if (this.Macros[i].Name == macro.Name) {
                    throw new ArgumentException($"Macro '{macro.Name}' is already defined by the base effect.", nameof(macros));
                }
            }
        }
        
        // Build compiled effect macros.
        MacroDefinition[] macroDefinitions = new MacroDefinition[this.Macros.Count + macros.Length];
        
        int macroIndex = 0;
        
        foreach (MacroDefinition macroDef in this.Macros) {
            macroDefinitions[macroIndex++] = macroDef;
        }
        
        foreach ((string Name, string Value) macro in macros) {
            macroDefinitions[macroIndex++] = new MacroDefinition(macro.Name, macro.Value);
        }
        
        // Compile effect variant.
        Effect compiledEffect = new Effect(this.GraphicsDevice, this._vertText, this._fragText, this._compileOptions, macroDefinitions);
        compiledEffect.IsVariant = true;
        
        foreach (var bufferLayout in this._bufferLayouts) {
            compiledEffect.AddBufferLayout(bufferLayout.Value.Name, bufferLayout.Key, bufferLayout.Value.BufferType, bufferLayout.Value.ShaderStages);
        }
        
        foreach (var textureLayout in this._textureLayouts) {
            compiledEffect.AddTextureLayout(textureLayout.Value.Name, textureLayout.Key, textureLayout.Value.ShaderStages, textureLayout.Value.ResourceKind);
        }
        
        // Cache and return.
        EffectVariantKey key = new EffectVariantKey(this.Macros, macros.ToArray());
        EffectVariant variant = new EffectVariant(this, compiledEffect);
        this._cachedVariants.Add(key, variant);
        return variant;
    }
    
    /// <summary>
    /// Retrieves a collection of buffer layouts associated with the effect.
    /// </summary>
    /// <returns>A read-only collection of <see cref="SimpleBufferLayout"/> objects representing the buffer layouts.</returns>
    public IReadOnlyCollection<SimpleBufferLayout> GetBufferLayouts() {
        return this._bufferLayouts.Values;
    }
    
    /// <summary>
    /// Retrieves the buffer layout identified by the specified name.
    /// </summary>
    /// <param name="name">The name of the buffer layout to retrieve.</param>
    /// <returns>The <see cref="SimpleBufferLayout"/> matching the specified name.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if no buffer layout with the specified name is found.</exception>
    public SimpleBufferLayout GetBufferLayout(string name) {
        foreach (var layout in _bufferLayouts.Values) {
            if (layout.Name == name) {
                return layout;
            }
        }
        
        throw new KeyNotFoundException($"No buffer layout found with name [{name}]");
    }
    
    /// <summary>
    /// Retrieves the slot index of a buffer layout by its name.
    /// </summary>
    /// <param name="name">The name of the buffer layout whose slot index is to be retrieved.</param>
    /// <returns>The slot index of the specified buffer layout.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the buffer layout with the specified name does not exist.</exception>
    public uint GetBufferLayoutSlot(string name) {
        foreach (var layoutPair in this._bufferLayouts) {
            if (layoutPair.Value.Name == name) {
                return layoutPair.Key;
            }
        }
        
        throw new KeyNotFoundException($"Failed to get the slot for [{name}]. A buffer layout with this name do not exist.");
    }

    /// <summary>
    /// Adds a new buffer layout to the effect with the specified parameters.
    /// </summary>
    /// <param name="name">The name of the buffer layout to add. Must be unique within the effect.</param>
    /// <param name="slot">The slot index at which the buffer layout will be bound.</param>
    /// <param name="bufferType">The type of buffer being added, defined by the <see cref="SimpleBufferType"/> enum.</param>
    /// <param name="stages">The shader stages where the buffer will be accessible, specified by <see cref="ShaderStages"/>.</param>
    /// <exception cref="InvalidOperationException"> Thrown if a buffer layout with the specified name already exists. </exception>
    public void AddBufferLayout(string name, uint slot, SimpleBufferType bufferType, ShaderStages stages) {
        if (this._bufferLayouts.Any(layoutPair => layoutPair.Value.Name == name)) {
            throw new InvalidOperationException($"Failed to add buffer layout with name [{name}]. A buffer layout with this name might already exist.");
        }
        
        SimpleBufferLayout layout = new SimpleBufferLayout(this.GraphicsDevice, name, bufferType, stages);
        this._bufferLayouts.Add(slot, layout);
    }
    
    /// <summary>
    /// Retrieves a collection of texture layouts associated with the effect.
    /// </summary>
    /// <returns>A read-only collection of <see cref="SimpleTextureLayout"/> objects representing the texture layouts.</returns>
    public IReadOnlyCollection<SimpleTextureLayout> GetTextureLayouts() {
        return this._textureLayouts.Values;
    }
    
    /// <summary>
    /// Retrieves a texture layout by its name from the list of available texture layouts.
    /// </summary>
    /// <param name="name">The name of the texture layout to retrieve.</param>
    /// <returns>The <see cref="SimpleTextureLayout"/> matching the specified name.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if no texture layout with the specified name is found.</exception>
    public SimpleTextureLayout GetTextureLayout(string name) {
        foreach (var layout in this._textureLayouts.Values) {
            if (layout.Name == name) {
                return layout;
            }
        }
        
        throw new KeyNotFoundException($"No buffer layout found with name [{name}]");
    }
    
    /// <summary>
    /// Retrieves the texture slot index for the specified texture name in the effect's texture layouts.
    /// </summary>
    /// <param name="name">The name of the texture whose slot index is to be retrieved.</param>
    /// <returns>The zero-based index of the texture slot corresponding to the specified texture name.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the specified texture name does not exist in the texture layouts.</exception>
    public uint GetTextureLayoutSlot(string name) {
        foreach (var layoutPair in this._textureLayouts) {
            if (layoutPair.Value.Name == name) {
                return layoutPair.Key;
            }
        }
        
        throw new KeyNotFoundException($"Failed to get the slot for [{name}]. A texture layout with this name do not exist.");
    }
    
    /// <summary>
    /// Adds a new texture layout to the effect with the specified parameters.
    /// </summary>
    /// <param name="name">The unique name of the texture layout to add.</param>
    /// <param name="slot">The slot index where the texture layout will be bound.</param>
    /// <param name="stages">The shader stages that can access the texture layout.</param>
    /// <param name="kind">The kind of texture resource. Defaults to a read-only texture.</param>
    /// <exception cref="InvalidOperationException">Thrown if a texture layout with the same name already exists.</exception>
    public void AddTextureLayout(string name, uint slot, ShaderStages stages = ShaderStages.Fragment, ResourceKind kind = ResourceKind.TextureReadOnly) {
        if (this._textureLayouts.Any(layoutPair => layoutPair.Value.Name == name)) {
            throw new InvalidOperationException($"Failed to add texture layout with name [{name}]. A texture layout with this name might already exist.");
        }
        
        SimpleTextureLayout layout = new SimpleTextureLayout(this.GraphicsDevice, name, stages, kind);
        this._textureLayouts.Add(slot, layout);
    }
    
    /// <summary>
    /// Retrieves or creates a pipeline for the given pipeline description.
    /// </summary>
    /// <param name="pipelineDescription">The description of the pipeline to retrieve or create.</param>
    /// <returns>A <see cref="SimplePipeline"/> configured with the specified description.</returns>
    public SimplePipeline GetPipeline(SimplePipelineDescription pipelineDescription) {
        if (!this._cachedPipelines.TryGetValue(pipelineDescription, out SimplePipeline? pipeline)) {
            SimplePipeline newPipeline = new SimplePipeline(this.GraphicsDevice, pipelineDescription);
            
            this._cachedPipelines.Add(pipelineDescription, newPipeline);
            return newPipeline;
        }
        
        return pipeline;
    }
    
    /// <summary>
    /// Apply the state effect immediately before rendering it.
    /// </summary>
    public virtual void Apply(CommandList commandList, Material? material = null) { }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            foreach (SimplePipeline pipeline in this._cachedPipelines.Values) {
                pipeline.Dispose();
            }
            
            foreach (EffectVariant variant in this._cachedVariants.Values) {
                variant.Dispose();
            }
            
            this.Shader.VertShader.Dispose();
            this.Shader.FragShader.Dispose();
            
            foreach (SimpleBufferLayout bufferLayout in this._bufferLayouts.Values) {
                bufferLayout.Dispose();
            }
            
            foreach (SimpleTextureLayout textureLayout in this._textureLayouts.Values) {
                textureLayout.Dispose();
            }
        }
    }
}