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
    /// A collection of buffer layout descriptions used to define buffer bindings.
    /// </summary>
    private List<SimpleBufferLayout> _bufferLayouts;

    /// <summary>
    /// A collection of texture layout descriptions used to define how textures bindings.
    /// </summary>
    private List<SimpleTextureLayout> _textureLayouts;
    
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
        
        this._bufferLayouts = new List<SimpleBufferLayout>();
        this._textureLayouts = new List<SimpleTextureLayout>();
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
    /// Retrieves the list of simple buffer layouts associated with the effect.
    /// </summary>
    /// <returns>A list of <see cref="SimpleBufferLayout"/> objects representing the buffer layouts.</returns>
    public IReadOnlyList<SimpleBufferLayout> GetBufferLayouts() {
        return this._bufferLayouts;
    }

    /// <summary>
    /// Retrieves the buffer layout identified by the specified name.
    /// </summary>
    /// <param name="name">The name of the buffer layout to retrieve.</param>
    /// <returns>The <see cref="SimpleBufferLayout"/> matching the specified name.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if no buffer layout with the specified name is found.</exception>
    public SimpleBufferLayout GetBufferLayout(string name) {
        foreach (var layout in _bufferLayouts) {
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
        uint index = 0;
        
        foreach (SimpleBufferLayout layout in this._bufferLayouts) {
            if (layout.Name == name) {
                return index;
            }
            
            index++;
        }
        
        throw new KeyNotFoundException($"Failed to get the slot for [{name}]. A buffer layout with this name do not exist.");
    }

    /// <summary>
    /// Adds a buffer layout to the effect.
    /// </summary>
    /// <param name="name">The name of the buffer layout to add.</param>
    /// <param name="bufferType">The <see cref="SimpleBufferType"/> specifying the type of buffer being added.</param>
    /// <param name="stages">The <see cref="ShaderStages"/> indicating the shader stages where the buffer will be used.</param>
    public void AddBufferLayout(string name, SimpleBufferType bufferType, ShaderStages stages) {
        if (this._bufferLayouts.Any(layout => layout.Name == name)) {
            throw new InvalidOperationException($"Failed to add buffer layout with name [{name}]. A buffer layout with this name might already exist.");
        }
        
        SimpleBufferLayout layout = new SimpleBufferLayout(this.GraphicsDevice, name, bufferType, stages);
        this._bufferLayouts.Add(layout);
    }

    /// <summary>
    /// Retrieves the list of texture layouts used by the effect.
    /// </summary>
    /// <returns>A list of <see cref="SimpleTextureLayout"/> objects representing the texture layouts associated with the effect.</returns>
    public IReadOnlyList<SimpleTextureLayout> GetTextureLayouts() {
        return this._textureLayouts;
    }

    /// <summary>
    /// Retrieves a texture layout by its name from the list of available texture layouts.
    /// </summary>
    /// <param name="name">The name of the texture layout to retrieve.</param>
    /// <returns>The <see cref="SimpleTextureLayout"/> matching the specified name.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if no texture layout with the specified name is found.</exception>
    public SimpleTextureLayout GetTextureLayout(string name) {
        foreach (var layout in _textureLayouts) {
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
        uint index = (uint) this._bufferLayouts.Count;
        
        foreach (SimpleTextureLayout layout in this._textureLayouts) {
            if (layout.Name == name) {
                return index;
            }
            
            index++;
        }
        
        throw new KeyNotFoundException($"Failed to get the slot for [{name}]. A texture layout with this name do not exist.");
    }

    /// <summary>
    /// Adds a texture layout to the effect.
    /// </summary>
    /// <param name="name">The name of the texture layout to be added.</param>
    public void AddTextureLayout(string name) {
        if (this._textureLayouts.Any(layout => layout.Name == name)) {
            throw new InvalidOperationException($"Failed to add texture layout with name [{name}]. A texture layout with this name might already exist.");
        }
        
        SimpleTextureLayout layout = new SimpleTextureLayout(this.GraphicsDevice, name);
        this._textureLayouts.Add(layout);
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
            
            foreach (SimpleBufferLayout bufferLayout in this._bufferLayouts) {
                bufferLayout.Dispose();
            }
            
            foreach (SimpleTextureLayout textureLayout in this._textureLayouts) {
                textureLayout.Dispose();
            }
        }
    }
}