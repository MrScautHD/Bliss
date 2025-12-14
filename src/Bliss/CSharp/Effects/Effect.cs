using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.Pipelines.Textures;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Materials;
using Veldrid;
using Veldrid.SPIRV;

namespace Bliss.CSharp.Effects;

public class Effect : Disposable {
    
    /// <summary>
    /// The graphics device used for creating and managing graphical resources.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// Represents a pair of shaders consisting of a vertex shader and a fragment shader.
    /// </summary>
    public readonly (Shader VertShader, Shader FragShader) Shader;

    /// <summary>
    /// Describes the layouts of vertex data for a graphics pipeline.
    /// </summary>
    public readonly VertexLayoutDescription[] VertexLayouts;

    /// <summary>
    /// Represents a description of a shader set, including vertex layout details, shader information, and optional specialization constants.
    /// </summary>
    public readonly ShaderSetDescription ShaderSet;

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
    /// Initializes a new <see cref="Effect"/> using a single vertex layout and shader bytecode loaded from file paths.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create shaders and related GPU resources.</param>
    /// <param name="vertexLayout">The vertex layout describing the structure of vertex input data.</param>
    /// <param name="vertPath">The file path to the compiled vertex shader bytecode.</param>
    /// <param name="fragPath">The file path to the compiled fragment shader bytecode.</param>
    /// <param name="constants">Optional specialization constants applied to the shaders.</param>
    public Effect(GraphicsDevice graphicsDevice, VertexLayoutDescription vertexLayout, string vertPath, string fragPath, SpecializationConstant[]? constants = null) : this(graphicsDevice, [vertexLayout], LoadBytecode(vertPath), LoadBytecode(fragPath), constants) { }
    
    /// <summary>
    /// Initializes a new <see cref="Effect"/> using multiple vertex layouts and shader bytecode loaded from file paths.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create shaders and related GPU resources.</param>
    /// <param name="vertexLayouts">The vertex layouts describing the structure of vertex input data.</param>
    /// <param name="vertPath">The file path to the compiled vertex shader bytecode.</param>
    /// <param name="fragPath">The file path to the compiled fragment shader bytecode.</param>
    /// <param name="constants">Optional specialization constants applied to the shaders.</param>
    public Effect(GraphicsDevice graphicsDevice, VertexLayoutDescription[] vertexLayouts, string vertPath, string fragPath, SpecializationConstant[]? constants = null) : this(graphicsDevice, vertexLayouts, LoadBytecode(vertPath), LoadBytecode(fragPath), constants) { }
    
    /// <summary>
    /// Initializes a new <see cref="Effect"/> using a single vertex layout and provided shader bytecode.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create shaders and related GPU resources.</param>
    /// <param name="vertexLayout">The vertex layout describing the structure of vertex input data.</param>
    /// <param name="vertBytes">The compiled vertex shader bytecode.</param>
    /// <param name="fragBytes">The compiled fragment shader bytecode.</param>
    /// <param name="constants">Optional specialization constants applied to the shaders.</param>
    public Effect(GraphicsDevice graphicsDevice, VertexLayoutDescription vertexLayout, byte[] vertBytes, byte[] fragBytes, SpecializationConstant[]? constants = null) : this(graphicsDevice, [vertexLayout], vertBytes, fragBytes, constants) { }
    
    /// <summary>
    /// Initializes a new <see cref="Effect"/> using multiple vertex layouts and provided shader bytecode.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create shaders, pipelines, and related GPU resources.</param>
    /// <param name="vertexLayouts">The vertex layouts describing the structure of vertex input data.</param>
    /// <param name="vertBytes">The compiled vertex shader bytecode.</param>
    /// <param name="fragBytes">The compiled fragment shader bytecode.</param>
    /// <param name="constants">Optional specialization constants applied to the shaders.</param>
    public Effect(GraphicsDevice graphicsDevice, VertexLayoutDescription[] vertexLayouts, byte[] vertBytes, byte[] fragBytes, SpecializationConstant[]? constants = null) { // TODO: Make better support for constants.
        this.GraphicsDevice = graphicsDevice;
        
        ShaderDescription vertDescription = new ShaderDescription(ShaderStages.Vertex, vertBytes, "main");
        ShaderDescription fragDescription = new ShaderDescription(ShaderStages.Fragment, fragBytes, "main");
        
        Shader[] shaders = graphicsDevice.ResourceFactory.CreateFromSpirv(vertDescription, fragDescription);
        
        this.Shader.VertShader = shaders[0];
        this.Shader.FragShader = shaders[1];
        this.VertexLayouts = vertexLayouts;
        
        this.ShaderSet = new ShaderSetDescription() {
            VertexLayouts = this.VertexLayouts,
            Shaders = [
                this.Shader.VertShader,
                this.Shader.FragShader
            ],
            Specializations = constants ?? []
        };
        
        this._bufferLayouts = new Dictionary<uint, SimpleBufferLayout>();
        this._textureLayouts = new Dictionary<uint, SimpleTextureLayout>();
        this._cachedPipelines = new Dictionary<SimplePipelineDescription, SimplePipeline>();
    }

    /// <summary>
    /// Loads the bytecode from the specified shader file path.
    /// </summary>
    /// <param name="path">The file path to the shader source code.</param>
    /// <returns>A byte array containing the bytecode from the shader file.</returns>
    public static byte[] LoadBytecode(string path) {
        if (!File.Exists(path)) {
            throw new Exception($"No shader file found in path: [{path}]");
        }
        
        if (Path.GetExtension(path) != ".vert" && Path.GetExtension(path) != ".frag") {
            throw new Exception($"This shader type is not supported: [{Path.GetExtension(path)}]");
        }
        
        Logger.Info($"Shader bytes loaded successfully from path: [{path}]");
        return File.ReadAllBytes(path);
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
        foreach (var layout in _textureLayouts.Values) {
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
    /// <exception cref="InvalidOperationException">Thrown if a texture layout with the same name already exists.</exception>
    public void AddTextureLayout(string name, uint slot) {
        if (this._textureLayouts.Any(layoutPair => layoutPair.Value.Name == name)) {
            throw new InvalidOperationException($"Failed to add texture layout with name [{name}]. A texture layout with this name might already exist.");
        }
        
        SimpleTextureLayout layout = new SimpleTextureLayout(this.GraphicsDevice, name);
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