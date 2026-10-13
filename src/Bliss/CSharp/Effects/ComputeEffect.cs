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
    /// The resource layouts used by the compute pipeline.
    /// </summary>
    public ResourceLayout[] ResourceLayouts { get; private set; }
    
    /// <summary>
    /// The compute pipeline.
    /// </summary>
    public Pipeline Pipeline { get; private set; }
    
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
    /// A cache of compute effect variants compiled for specific macro combinations.
    /// </summary>
    private Dictionary<ComputeEffectVariantKey, ComputeEffectVariant> _cachedVariants;
    
    /// <summary>
    /// Creates a new <see cref="ComputeEffect"/> from a compute shader bytecode file path.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create GPU resources.</param>
    /// <param name="computePath">The path to the compiled compute shader bytecode.</param>
    /// <param name="resourceLayouts">The resource layouts used by the compute pipeline.</param>
    /// <param name="compileOptions">The cross compile options.</param>
    /// <param name="threadGroupSizeX">The X thread group size.</param>
    /// <param name="threadGroupSizeY">The Y thread group size.</param>
    /// <param name="threadGroupSizeZ">The Z thread group size.</param>
    public ComputeEffect(GraphicsDevice graphicsDevice, string computePath, ResourceLayout[] resourceLayouts, CrossCompileOptions compileOptions, uint threadGroupSizeX, uint threadGroupSizeY, uint threadGroupSizeZ) : this(graphicsDevice, LoadBytecodeFromFile(computePath), resourceLayouts, compileOptions, threadGroupSizeX, threadGroupSizeY, threadGroupSizeZ) { }
    
    /// <summary>
    /// Creates a new <see cref="ComputeEffect"/> from compute shader bytecode.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create GPU resources.</param>
    /// <param name="computeBytes">The compiled compute shader bytecode.</param>
    /// <param name="resourceLayouts">The resource layouts used by the compute pipeline.</param>
    /// <param name="compileOptions">The cross compile options.</param>
    /// <param name="threadGroupSizeX">The X thread group size.</param>
    /// <param name="threadGroupSizeY">The Y thread group size.</param>
    /// <param name="threadGroupSizeZ">The Z thread group size.</param>
    public ComputeEffect(GraphicsDevice graphicsDevice, byte[] computeBytes, ResourceLayout[] resourceLayouts, CrossCompileOptions compileOptions, uint threadGroupSizeX, uint threadGroupSizeY, uint threadGroupSizeZ) {
        this.GraphicsDevice = graphicsDevice;
        this.Specializations = compileOptions.Specializations;
        this.Macros = [];
        this.ResourceLayouts = resourceLayouts;
        this.ThreadGroupSizeX = threadGroupSizeX;
        this.ThreadGroupSizeY = threadGroupSizeY;
        this.ThreadGroupSizeZ = threadGroupSizeZ;
        this._computeText = null;
        this._compileOptions = compileOptions;
        
        ShaderDescription shaderDescription = new ShaderDescription(ShaderStages.Compute, computeBytes, "main");
        
        this.Shader = graphicsDevice.ResourceFactory.CreateFromSpirv(shaderDescription, compileOptions);
        this.Pipeline = graphicsDevice.ResourceFactory.CreateComputePipeline(new ComputePipelineDescription(this.Shader, this.ResourceLayouts, this.ThreadGroupSizeX, this.ThreadGroupSizeY, this.ThreadGroupSizeZ));
        
        this._cachedVariants = new Dictionary<ComputeEffectVariantKey, ComputeEffectVariant>();
    }
    
    /// <summary>
    /// Creates a new <see cref="ComputeEffect"/> from GLSL compute shader source code.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create GPU resources.</param>
    /// <param name="computeText">The GLSL compute shader source code.</param>
    /// <param name="resourceLayouts">The resource layouts used by the compute pipeline.</param>
    /// <param name="compileOptions">The cross compile options.</param>
    /// <param name="macros">The shader macro definitions.</param>
    /// <param name="threadGroupSizeX">The X thread group size.</param>
    /// <param name="threadGroupSizeY">The Y thread group size.</param>
    /// <param name="threadGroupSizeZ">The Z thread group size.</param>
    public ComputeEffect(GraphicsDevice graphicsDevice, string computeText, ResourceLayout[] resourceLayouts, CrossCompileOptions compileOptions, MacroDefinition[] macros, uint threadGroupSizeX, uint threadGroupSizeY, uint threadGroupSizeZ) {
        this.GraphicsDevice = graphicsDevice;
        this.Specializations = compileOptions.Specializations;
        this.Macros = macros;
        this.ResourceLayouts = resourceLayouts;
        this.ThreadGroupSizeX = threadGroupSizeX;
        this.ThreadGroupSizeY = threadGroupSizeY;
        this.ThreadGroupSizeZ = threadGroupSizeZ;
        this._computeText = computeText;
        this._compileOptions = compileOptions;
        
        GlslCompileOptions glslOptions = new GlslCompileOptions(true, macros);
        SpirvCompilationResult computeResult = SpirvCompilation.CompileGlslToSpirv(computeText, nameof(ShaderStages.Compute), ShaderStages.Compute, glslOptions);
        
        ShaderDescription shaderDescription = new ShaderDescription(ShaderStages.Compute, computeResult.SpirvBytes, "main");
        
        this.Shader = graphicsDevice.ResourceFactory.CreateFromSpirv(shaderDescription, compileOptions);
        this.Pipeline = graphicsDevice.ResourceFactory.CreateComputePipeline(new ComputePipelineDescription(this.Shader, this.ResourceLayouts, this.ThreadGroupSizeX, this.ThreadGroupSizeY, this.ThreadGroupSizeZ));
        
        this._cachedVariants = new Dictionary<ComputeEffectVariantKey, ComputeEffectVariant>();
    }
    
    /// <summary>
    /// Creates a new <see cref="ComputeEffect"/> from a compute shader source file.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create GPU resources.</param>
    /// <param name="computePath">The path to the GLSL compute shader source file.</param>
    /// <param name="resourceLayouts">The resource layouts used by the compute pipeline.</param>
    /// <param name="compileOptions">The cross compile options.</param>
    /// <param name="macros">The shader macro definitions.</param>
    /// <param name="threadGroupSizeX">The X thread group size.</param>
    /// <param name="threadGroupSizeY">The Y thread group size.</param>
    /// <param name="threadGroupSizeZ">The Z thread group size.</param>
    /// <returns>A compute effect created from source code.</returns>
    public static ComputeEffect FromSourceFile(GraphicsDevice graphicsDevice, string computePath, ResourceLayout[] resourceLayouts, CrossCompileOptions compileOptions, MacroDefinition[] macros, uint threadGroupSizeX, uint threadGroupSizeY, uint threadGroupSizeZ) {
        return new ComputeEffect(graphicsDevice, LoadTextCodeFromFile(computePath), resourceLayouts, compileOptions, macros, threadGroupSizeX, threadGroupSizeY, threadGroupSizeZ);
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
        ComputeEffect compiledEffect = new ComputeEffect(this.GraphicsDevice, this._computeText, this.ResourceLayouts, this._compileOptions, macroDefinitions, this.ThreadGroupSizeX, this.ThreadGroupSizeY, this.ThreadGroupSizeZ);
        
        // Cache and return.
        ComputeEffectVariantKey key = new ComputeEffectVariantKey(this.Macros, macros.ToArray());
        ComputeEffectVariant variant = new ComputeEffectVariant(this, compiledEffect);
        this._cachedVariants.Add(key, variant);
        return variant;
    }
    
    /// <summary>
    /// Dispatches this compute effect.
    /// </summary>
    /// <param name="commandList">The command list.</param>
    /// <param name="resourceSets">The resource sets to bind.</param>
    /// <param name="groupCountX">The X dispatch group count.</param>
    /// <param name="groupCountY">The Y dispatch group count.</param>
    /// <param name="groupCountZ">The Z dispatch group count.</param>
    public void Dispatch(CommandList commandList, ResourceSet[] resourceSets, uint groupCountX, uint groupCountY, uint groupCountZ) {
        commandList.SetPipeline(this.Pipeline);
        
        for (uint i = 0; i < resourceSets.Length; i++) {
            commandList.SetComputeResourceSet(i, resourceSets[i]);
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
    public void DispatchInvocations(CommandList commandList, ResourceSet[] resourceSets, uint invocationCountX, uint invocationCountY, uint invocationCountZ) {
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
            foreach (ComputeEffectVariant variant in this._cachedVariants.Values) {
                variant.Dispose();
            }
            
            this.Pipeline.Dispose();
            this.Shader.Dispose();
        }
    }
}