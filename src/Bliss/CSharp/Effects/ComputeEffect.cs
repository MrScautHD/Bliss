using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.Pipelines.Textures;
using Bliss.CSharp.Logging;
using Veldrith;
using Veldrith.SPIRV;

namespace Bliss.CSharp.Effects;

public class ComputeEffect : Disposable {
    
    /// <summary>
    /// The graphics device used for creating and managing GPU resources.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// Specialization constants applied to the compute shader.
    /// </summary>
    public IReadOnlyList<SpecializationConstant> Specializations { get; private set; }
    
    /// <summary>
    /// Preprocessor macro definitions used when compiling compute shader source code.
    /// </summary>
    public IReadOnlyList<MacroDefinition> Macros { get; private set; }
    
    /// <summary>
    /// The compute shader used by this effect.
    /// </summary>
    public Shader Shader { get; private set; }
    
    /// <summary>
    /// The compute pipeline.
    /// </summary>
    public Pipeline? Pipeline { get; private set; }
    
    /// <summary>
    /// The X thread group size used by the compute pipeline.
    /// </summary>
    public uint ThreadGroupSizeX { get; private set; }
    
    /// <summary>
    /// The Y thread group size used by the compute pipeline.
    /// </summary>
    public uint ThreadGroupSizeY { get; private set; }
    
    /// <summary>
    /// The Z thread group size used by the compute pipeline.
    /// </summary>
    public uint ThreadGroupSizeZ { get; private set; }
    
    /// <summary>
    /// The GLSL compute shader source code, if this effect was created from source.
    /// </summary>
    private readonly string? _computeText;
    
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
    /// A cache of compute effect variants compiled for specific macro combinations.
    /// </summary>
    private Dictionary<ComputeEffectVariantKey, ComputeEffectVariant> _cachedVariants;
    
    /// <summary>
    /// Creates a new <see cref="ComputeEffect"/> from a compute shader bytecode file path.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create GPU resources.</param>
    /// <param name="computePath">The path to the compiled compute shader bytecode.</param>
    /// <param name="compileOptions">The cross compile options.</param>
    /// <param name="threadGroupSizeX">The X thread group size.</param>
    /// <param name="threadGroupSizeY">The Y thread group size.</param>
    /// <param name="threadGroupSizeZ">The Z thread group size.</param>
    public ComputeEffect(GraphicsDevice graphicsDevice, string computePath, CrossCompileOptions compileOptions, uint threadGroupSizeX, uint threadGroupSizeY, uint threadGroupSizeZ) : this(graphicsDevice, LoadBytecodeFromFile(computePath), compileOptions, threadGroupSizeX, threadGroupSizeY, threadGroupSizeZ) { }
    
    /// <summary>
    /// Creates a new <see cref="ComputeEffect"/> from compute shader bytecode.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create GPU resources.</param>
    /// <param name="computeBytes">The compiled compute shader bytecode.</param>
    /// <param name="compileOptions">The cross compile options.</param>
    /// <param name="threadGroupSizeX">The X thread group size.</param>
    /// <param name="threadGroupSizeY">The Y thread group size.</param>
    /// <param name="threadGroupSizeZ">The Z thread group size.</param>
    public ComputeEffect(GraphicsDevice graphicsDevice, byte[] computeBytes, CrossCompileOptions compileOptions, uint threadGroupSizeX, uint threadGroupSizeY, uint threadGroupSizeZ) {
        this.GraphicsDevice = graphicsDevice;
        this.Specializations = compileOptions.Specializations;
        this.Macros = [];
        this.ThreadGroupSizeX = threadGroupSizeX;
        this.ThreadGroupSizeY = threadGroupSizeY;
        this.ThreadGroupSizeZ = threadGroupSizeZ;
        this._computeText = null;
        this._compileOptions = compileOptions;
        
        ShaderDescription shaderDescription = new ShaderDescription(ShaderStages.Compute, computeBytes, "main");
        this.Shader = graphicsDevice.ResourceFactory.CreateFromSpirv(shaderDescription, compileOptions);
        
        this._bufferLayouts = new Dictionary<uint, SimpleBufferLayout>();
        this._textureLayouts = new Dictionary<uint, SimpleTextureLayout>();
        this._cachedVariants = new Dictionary<ComputeEffectVariantKey, ComputeEffectVariant>();
    }
    
    /// <summary>
    /// Creates a new <see cref="ComputeEffect"/> from GLSL compute shader source code.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create GPU resources.</param>
    /// <param name="computeText">The GLSL compute shader source code.</param>
    /// <param name="compileOptions">The cross compile options.</param>
    /// <param name="macros">The shader macro definitions.</param>
    /// <param name="threadGroupSizeX">The X thread group size.</param>
    /// <param name="threadGroupSizeY">The Y thread group size.</param>
    /// <param name="threadGroupSizeZ">The Z thread group size.</param>
    public ComputeEffect(GraphicsDevice graphicsDevice, string computeText, CrossCompileOptions compileOptions, MacroDefinition[] macros, uint threadGroupSizeX, uint threadGroupSizeY, uint threadGroupSizeZ) {
        this.GraphicsDevice = graphicsDevice;
        this.Specializations = compileOptions.Specializations;
        this.Macros = macros;
        this.ThreadGroupSizeX = threadGroupSizeX;
        this.ThreadGroupSizeY = threadGroupSizeY;
        this.ThreadGroupSizeZ = threadGroupSizeZ;
        this._computeText = computeText;
        this._compileOptions = compileOptions;
        
        GlslCompileOptions glslOptions = new GlslCompileOptions(true, macros);
        SpirvCompilationResult computeResult = SpirvCompilation.CompileGlslToSpirv(computeText, nameof(ShaderStages.Compute), ShaderStages.Compute, glslOptions);
        
        ShaderDescription shaderDescription = new ShaderDescription(ShaderStages.Compute, computeResult.SpirvBytes, "main");
        this.Shader = graphicsDevice.ResourceFactory.CreateFromSpirv(shaderDescription, compileOptions);
        
        this._bufferLayouts = new Dictionary<uint, SimpleBufferLayout>();
        this._textureLayouts = new Dictionary<uint, SimpleTextureLayout>();
        this._cachedVariants = new Dictionary<ComputeEffectVariantKey, ComputeEffectVariant>();
    }
    
    /// <summary>
    /// Creates a new <see cref="ComputeEffect"/> by loading GLSL compute shader source code from a file and compiling it into SPIR-V.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create GPU resources.</param>
    /// <param name="computePath">The file path to the GLSL compute shader source.</param>
    /// <param name="compileOptions">The cross compile options.</param>
    /// <param name="macros">The shader macro definitions.</param>
    /// <param name="threadGroupSizeX">The X thread group size. It has to match the local size of the shader.</param>
    /// <param name="threadGroupSizeY">The Y thread group size. It has to match the local size of the shader.</param>
    /// <param name="threadGroupSizeZ">The Z thread group size. It has to match the local size of the shader.</param>
    /// <returns>The compiled compute effect.</returns>
    /// <exception cref="Exception">Thrown if the shader file does not exist or has an unsupported file type.</exception>
    public static ComputeEffect FromFiles(GraphicsDevice graphicsDevice, string computePath, CrossCompileOptions compileOptions, MacroDefinition[] macros, uint threadGroupSizeX, uint threadGroupSizeY, uint threadGroupSizeZ) {
        return new ComputeEffect(graphicsDevice, LoadTextCodeFromFile(computePath), compileOptions, macros, threadGroupSizeX, threadGroupSizeY, threadGroupSizeZ);
    }
    
    /// <summary>
    /// Creates a new <see cref="ComputeEffect"/> by loading GLSL compute shader source code from a file, resolving its include directives, and compiling it into SPIR-V.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create GPU resources.</param>
    /// <param name="shaderRoot">The root directory that is used to resolve the include directives.</param>
    /// <param name="computePath">The file path to the GLSL compute shader source.</param>
    /// <param name="compileOptions">The cross compile options.</param>
    /// <param name="macros">The shader macro definitions.</param>
    /// <param name="threadGroupSizeX">The X thread group size. It has to match the local size of the shader.</param>
    /// <param name="threadGroupSizeY">The Y thread group size. It has to match the local size of the shader.</param>
    /// <param name="threadGroupSizeZ">The Z thread group size. It has to match the local size of the shader.</param>
    /// <returns>The compiled compute effect.</returns>
    public static ComputeEffect FromFiles(GraphicsDevice graphicsDevice, string shaderRoot, string computePath, CrossCompileOptions compileOptions, MacroDefinition[] macros, uint threadGroupSizeX, uint threadGroupSizeY, uint threadGroupSizeZ) {
        ShaderIncludeProcessor processor = new ShaderIncludeProcessor(shaderRoot);
        
        string computeText = processor.ProcessFile(computePath);
        
        return new ComputeEffect(graphicsDevice, computeText, compileOptions, macros, threadGroupSizeX, threadGroupSizeY, threadGroupSizeZ);
    }
    
    /// <summary>
    /// Loads compute shader bytecode from the specified file path.
    /// </summary>
    /// <param name="path">The file path to the compute shader bytecode.</param>
    /// <returns>A byte array containing the compute shader bytecode.</returns>
    /// <exception cref="Exception">Thrown if the file does not exist or the file extension is not supported.</exception>
    public static byte[] LoadBytecodeFromFile(string path) {
        if (!File.Exists(path)) {
            throw new Exception($"No compute shader file found in path: [{path}]");
        }
        
        if (Path.GetExtension(path).ToLowerInvariant() != ".comp") {
            throw new Exception($"This compute shader type is not supported: [{Path.GetExtension(path)}]");
        }
        
        Logger.Info($"Compute shader bytes loaded successfully from path: [{path}]");
        return File.ReadAllBytes(path);
    }
    
    /// <summary>
    /// Loads compute shader source text from a file at the specified path.
    /// </summary>
    /// <param name="path">The file path to the compute shader source file.</param>
    /// <returns>The contents of the compute shader source file as a string.</returns>
    /// <exception cref="Exception">Thrown if the file does not exist or the file extension is not supported.</exception>
    public static string LoadTextCodeFromFile(string path) {
        if (!File.Exists(path)) {
            throw new Exception($"No compute shader file found in path: [{path}]");
        }
        
        if (Path.GetExtension(path).ToLowerInvariant() != ".comp") {
            throw new Exception($"This compute shader type is not supported: [{Path.GetExtension(path)}]");
        }
        
        Logger.Info($"Compute shader code loaded successfully from path: [{path}]");
        return File.ReadAllText(path);
    }
    
    /// <summary>
    /// Creates or retrieves a cached variant of this compute effect with additional macros.
    /// Variants can only be created when this compute effect was created from GLSL source code.
    /// </summary>
    /// <param name="macros">The additional macro names.</param>
    /// <returns>A compute effect variant.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this compute effect was not created from source code.</exception>
    public ComputeEffectVariant GetEffectVariant(ReadOnlySpan<string> macros) {
        if (this._computeText == null) {
            throw new InvalidOperationException("Compute effect variants can only be created from GLSL source-based compute effects.");
        }
        
        // Check cache.
        foreach ((ComputeEffectVariantKey cachedKey, ComputeEffectVariant cachedVariant) in this._cachedVariants) {
            if (cachedKey.Matches(macros)) {
                return cachedVariant;
            }
        }
        
        // Build compiled effect macros.
        MacroDefinition[] macroDefinitions = new MacroDefinition[this.Macros.Count + macros.Length];
        
        int macroIndex = 0;
        
        foreach (MacroDefinition macroDef in this.Macros) {
            macroDefinitions[macroIndex++] = macroDef;
        }
        
        foreach (string macro in macros) {
            macroDefinitions[macroIndex++] = new MacroDefinition(macro, "1");
        }
        
        // Compile effect variant.
        ComputeEffect compiledEffect = new ComputeEffect(this.GraphicsDevice, this._computeText, this._compileOptions, macroDefinitions, this.ThreadGroupSizeX, this.ThreadGroupSizeY, this.ThreadGroupSizeZ);
        
        foreach (var bufferLayout in this._bufferLayouts) {
            compiledEffect.AddBufferLayout(bufferLayout.Value.Name, bufferLayout.Key, bufferLayout.Value.BufferType, bufferLayout.Value.ShaderStages);
        }
        
        foreach (var textureLayout in this._textureLayouts) {
            compiledEffect.AddTextureLayout(textureLayout.Value.Name, textureLayout.Key, textureLayout.Value.ShaderStages, textureLayout.Value.ResourceKind);
        }
        
        // Cache and return.
        ComputeEffectVariantKey key = new ComputeEffectVariantKey(this.Macros, macros.ToArray());
        ComputeEffectVariant variant = new ComputeEffectVariant(this, compiledEffect);
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
    /// Builds the compute pipeline using the registered buffer and texture layouts.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the pipeline has already been built.</exception>
    public void BuildPipeline() {
        if (this.Pipeline != null) {
            throw new InvalidOperationException("The pipeline has already been built.");
        }
        
        ResourceLayout[] resourceLayouts = new ResourceLayout[this._bufferLayouts.Count + this._textureLayouts.Count];
        
        foreach ((uint slot, SimpleBufferLayout layout) in this._bufferLayouts) {
            resourceLayouts[slot] = layout.Layout;
        }
        
        foreach ((uint slot, SimpleTextureLayout layout) in this._textureLayouts) {
            resourceLayouts[slot] = layout.Layout;
        }
        
        this.Pipeline = this.GraphicsDevice.ResourceFactory.CreateComputePipeline(new ComputePipelineDescription(this.Shader, resourceLayouts, this.ThreadGroupSizeX, this.ThreadGroupSizeY, this.ThreadGroupSizeZ));
    }
    
    /// <summary>
    /// Dispatches this compute effect.
    /// </summary>
    /// <param name="commandList">The command list.</param>
    /// <param name="resourceSets">The resource sets to bind.</param>
    /// <param name="groupCountX">The X dispatch group count.</param>
    /// <param name="groupCountY">The Y dispatch group count.</param>
    /// <param name="groupCountZ">The Z dispatch group count.</param>
    public void Dispatch(CommandList commandList, ReadOnlySpan<(uint Slot, ResourceSet ResourceSet)> resourceSets, uint groupCountX, uint groupCountY, uint groupCountZ) {
        if (this.Pipeline == null) {
            throw new InvalidOperationException("The compute effect must be built before it can be dispatched.");
        }
        
        commandList.SetPipeline(this.Pipeline);
        
        foreach ((uint slot, ResourceSet resourceSet) in resourceSets) {
            commandList.SetComputeResourceSet(slot, resourceSet);
        }
        
        commandList.Dispatch(groupCountX, groupCountY, groupCountZ);
    }
    
    /// <summary>
    /// Dispatches this compute effect and calculates group counts from the provided total invocation counts.
    /// </summary>
    /// <param name="commandList">The command list.</param>
    /// <param name="resourceSets">The resource sets to bind.</param>
    /// <param name="invocationCountX">The total X invocation count.</param>
    /// <param name="invocationCountY">The total Y invocation count.</param>
    /// <param name="invocationCountZ">The total Z invocation count.</param>
    public void DispatchInvocations(CommandList commandList, ReadOnlySpan<(uint Slot, ResourceSet ResourceSet)> resourceSets, uint invocationCountX, uint invocationCountY, uint invocationCountZ) {
        uint groupCountX = this.DivideRoundUp(invocationCountX, this.ThreadGroupSizeX);
        uint groupCountY = this.DivideRoundUp(invocationCountY, this.ThreadGroupSizeY);
        uint groupCountZ = this.DivideRoundUp(invocationCountZ, this.ThreadGroupSizeZ);
        
        this.Dispatch(commandList, resourceSets, groupCountX, groupCountY, groupCountZ);
    }
    
    /// <summary>
    /// Divides and rounds up to the next integer.
    /// </summary>
    /// <param name="value">The value to divide.</param>
    /// <param name="divisor">The divisor.</param>
    /// <returns>The rounded-up division result.</returns>
    private uint DivideRoundUp(uint value, uint divisor) {
        if (divisor == 0) {
            throw new DivideByZeroException("The divisor cannot be zero.");
        }
        
        return (value + divisor - 1) / divisor;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.Pipeline?.Dispose();
            
            foreach (ComputeEffectVariant variant in this._cachedVariants.Values) {
                variant.Dispose();
            }
            
            this.Shader.Dispose();
            
            foreach (SimpleBufferLayout bufferLayout in this._bufferLayouts.Values) {
                bufferLayout.Dispose();
            }
            
            foreach (SimpleTextureLayout textureLayout in this._textureLayouts.Values) {
                textureLayout.Dispose();
            }
        }
    }
}