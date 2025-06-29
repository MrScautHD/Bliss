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
    /// Describes the layout of vertex data for a graphics pipeline.
    /// </summary>
    public readonly VertexLayoutDescription VertexLayout;

    /// <summary>
    /// Represents a description of a shader set, including vertex layout details, shader information, and optional specialization constants.
    /// </summary>
    public readonly ShaderSetDescription ShaderSet;

    /// <summary>
    /// A dictionary that maps string keys to <see cref="SimpleTextureLayout"/> instances, used to define and manage buffer configurations for the Effect class.
    /// </summary>
    private Dictionary<string, SimpleBufferLayout> _bufferLayouts;

    /// <summary>
    /// A dictionary that maps texture layout names to their corresponding <see cref="SimpleTextureLayout"/> instances.
    /// Used to store and retrieve layouts for managing texture resources within the Effect.
    /// </summary>
    private Dictionary<string, SimpleTextureLayout> _textureLayouts;
    
    /// <summary>
    /// A cache of pipelines created for specific pipeline descriptions, enabling reuse.
    /// </summary>
    private Dictionary<SimplePipelineDescription, SimplePipeline> _cachedPipelines;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Effect"/> class using shader file paths.
    /// </summary>
    /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/> used for rendering.</param>
    /// <param name="vertexLayout">The <see cref="VertexLayoutDescription"/> defining the vertex structure.</param>
    /// <param name="vertPath">The path to the vertex shader file.</param>
    /// <param name="fragPath">The path to the fragment shader file.</param>
    /// <param name="constants">Optional specialization constants for shader customization.</param>
    public Effect(GraphicsDevice graphicsDevice, VertexLayoutDescription vertexLayout, string vertPath, string fragPath, SpecializationConstant[]? constants = null) : this(graphicsDevice, vertexLayout, LoadBytecode(vertPath), LoadBytecode(fragPath), constants) { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Effect"/> class using shader bytecode.
    /// </summary>
    /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/> used for rendering.</param>
    /// <param name="vertexLayout">The <see cref="VertexLayoutDescription"/> defining the vertex structure.</param>
    /// <param name="vertBytes">The compiled bytecode for the vertex shader.</param>
    /// <param name="fragBytes">The compiled bytecode for the fragment shader.</param>
    /// <param name="constants">Optional specialization constants for shader customization.</param>
    public Effect(GraphicsDevice graphicsDevice, VertexLayoutDescription vertexLayout, byte[] vertBytes, byte[] fragBytes, SpecializationConstant[]? constants = null) {
        this.GraphicsDevice = graphicsDevice;
        
        ShaderDescription vertDescription = new ShaderDescription(ShaderStages.Vertex, vertBytes, "main");
        ShaderDescription fragDescription = new ShaderDescription(ShaderStages.Fragment, fragBytes, "main");
        
        Shader[] shaders = graphicsDevice.ResourceFactory.CreateFromSpirv(vertDescription, fragDescription);

        this.Shader.VertShader = shaders[0];
        this.Shader.FragShader = shaders[1];
        this.VertexLayout = vertexLayout;
        
        this.ShaderSet = new ShaderSetDescription() {
            VertexLayouts = [
                this.VertexLayout
            ],
            Shaders = [
                this.Shader.VertShader,
                this.Shader.FragShader
            ],
            Specializations = constants ?? []
        };

        this._bufferLayouts = new Dictionary<string, SimpleBufferLayout>();
        this._textureLayouts = new Dictionary<string, SimpleTextureLayout>();
        this._cachedPipelines = new Dictionary<SimplePipelineDescription, SimplePipeline>();
    }

    /// <summary>
    /// Loads the bytecode from the specified shader file path.
    /// </summary>
    /// <param name="path">The file path to the shader source code.</param>
    /// <returns>A byte array containing the bytecode from the shader file.</returns>
    private static byte[] LoadBytecode(string path) {
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
    /// Retrieves the collection of keys representing buffer layouts associated with the Effect instance.
    /// </summary>
    /// <returns>
    /// An enumerable collection of string keys corresponding to the buffer layouts.
    /// </returns>
    public IEnumerable<string> GetBufferLayoutKeys() {
        return this._bufferLayouts.Keys;
    }
    
    /// <summary>
    /// Retrieves the collection of buffer layouts defined for this effect.
    /// </summary>
    /// <returns>An enumerable collection of <see cref="SimpleBufferLayout"/> representing the buffer layouts in this effect.</returns>
    public IEnumerable<SimpleBufferLayout> GetBufferLayouts() {
        return this._bufferLayouts.Values;
    }

    /// <summary>
    /// Retrieves a buffer layout associated with the specified name.
    /// </summary>
    /// <param name="name">The name of the buffer layout to retrieve.</param>
    /// <returns>The <see cref="SimpleBufferLayout"/> associated with the given name.</returns>
    public SimpleBufferLayout GetBufferLayout(string name) {
        return this._bufferLayouts[name];
    }

    /// <summary>
    /// Adds a buffer layout to the current effect instance.
    /// </summary>
    /// <param name="bufferLayout">The buffer layout to be added, containing the name and configuration of the buffer.</param>
    public void AddBufferLayout(SimpleBufferLayout bufferLayout) {
        if (!this._bufferLayouts.TryAdd(bufferLayout.Name, bufferLayout)) {
            Logger.Warn($"Failed to add BufferLayout with name [{bufferLayout.Name}]. A buffer layout with this name might already exist.");
        }
    }

    /// <summary>
    /// Retrieves the collection of keys that correspond to the texture layouts defined in this effect.
    /// </summary>
    /// <returns>A collection of strings representing the keys for the texture layouts.</returns>
    public IEnumerable<string> GetTextureLayoutKeys() {
        return this._textureLayouts.Keys;
    }

    /// <summary>
    /// Retrieves the collection of texture layouts associated with the current effect.
    /// </summary>
    /// <returns>An enumerable collection of <see cref="SimpleTextureLayout"/> instances representing the texture layouts.</returns>
    public IEnumerable<SimpleTextureLayout> GetTextureLayouts() {
        return this._textureLayouts.Values;
    }

    /// <summary>
    /// Retrieves a specific texture layout by its name.
    /// </summary>
    /// <param name="name">The name of the texture layout to retrieve.</param>
    /// <returns>The texture layout associated with the specified name.</returns>
    public SimpleTextureLayout GetTextureLayout(string name) {
        return this._textureLayouts[name];
    }

    /// <summary>
    /// Adds a texture layout to the effect.
    /// </summary>
    /// <param name="textureLayout">The texture layout to be added.</param>
    public void AddTextureLayout(SimpleTextureLayout textureLayout) {
        if (!this._textureLayouts.TryAdd(textureLayout.Name, textureLayout)) {
            Logger.Warn($"Failed to add TextureLayout with name [{textureLayout.Name}]. A texture layout with this name might already exist.");
        }
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
        }
    }
}