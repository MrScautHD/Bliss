/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Logging;
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
    public readonly (Shader, Shader) Shader;

    /// <summary>
    /// Describes the layout of vertex data for a graphics pipeline.
    /// </summary>
    public readonly VertexLayoutDescription VertexLayout;

    /// <summary>
    /// A cache of pipelines created for specific pipeline descriptions, enabling reuse.
    /// </summary>
    private Dictionary<SimplePipelineDescription, SimplePipeline> _cachedPipelines;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Effect"/> class by loading shaders from file paths.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for creating resources.</param>
    /// <param name="vertexLayout">The vertex layout description for the pipeline.</param>
    /// <param name="vertPath">The file path to the vertex shader source code.</param>
    /// <param name="fragPath">The file path to the fragment shader source code.</param>
    public Effect(GraphicsDevice graphicsDevice, VertexLayoutDescription vertexLayout, string vertPath, string fragPath) : this(graphicsDevice, vertexLayout, LoadBytecode(vertPath), LoadBytecode(fragPath)) { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Effect"/> class with precompiled shader bytecode.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for creating resources.</param>
    /// <param name="vertexLayout">The vertex layout description for the pipeline.</param>
    /// <param name="vertBytes">The bytecode for the vertex shader.</param>
    /// <param name="fragBytes">The bytecode for the fragment shader.</param>
    public Effect(GraphicsDevice graphicsDevice, VertexLayoutDescription vertexLayout, byte[] vertBytes, byte[] fragBytes) {
        this.GraphicsDevice = graphicsDevice;
        
        ShaderDescription vertDescription = new ShaderDescription(ShaderStages.Vertex, vertBytes, "main");
        ShaderDescription fragDescription = new ShaderDescription(ShaderStages.Fragment, fragBytes, "main");
        
        Shader[] shaders = graphicsDevice.ResourceFactory.CreateFromSpirv(vertDescription, fragDescription);

        this.Shader.Item1 = shaders[0];
        this.Shader.Item2 = shaders[1];
        
        this.VertexLayout = vertexLayout;
        
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
    /// Retrieves or creates a pipeline for the given pipeline description.
    /// </summary>
    /// <param name="pipelineDescription">The description of the pipeline to retrieve or create.</param>
    /// <returns>A <see cref="SimplePipeline"/> configured with the specified description.</returns>
    public SimplePipeline GetPipeline(SimplePipelineDescription pipelineDescription) {
        if (!this._cachedPipelines.TryGetValue(pipelineDescription, out SimplePipeline? pipeline)) {
            SimplePipeline newPipeline = new SimplePipeline(this.GraphicsDevice, pipelineDescription);
            
            Logger.Error(pipelineDescription.ToString());
            
            this._cachedPipelines.Add(pipelineDescription, newPipeline);
            return newPipeline;
        }

        return pipeline;
    }
    
    // TODO: Adding Location system, for Material(Texture, Color) and in generel for buffers...
    // TODO: ADD MATERIAL param here.
    /// <summary>
    /// Apply the state effect immediately before rendering it.
    /// </summary>
    public virtual void Apply() { }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.Shader.Item1.Dispose();
            this.Shader.Item2.Dispose();

            foreach (SimplePipeline pipeline in this._cachedPipelines.Values) {
                pipeline.Dispose();
            }
        }
    }
}