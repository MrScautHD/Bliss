using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.Pipelines.Textures;
using Veldrid;

namespace Bliss.CSharp.Graphics.Pipelines;

public struct SimplePipelineDescription : IEquatable<SimplePipelineDescription> {

    /// <summary>
    /// Defines the blend state configuration, controlling how colors are blended in the pipeline.
    /// </summary>
    public BlendStateDescription BlendState;

    /// <summary>
    /// Defines the depth and stencil state configuration, controlling depth testing and stencil operations in the pipeline.
    /// </summary>
    public DepthStencilStateDescription DepthStencilState;

    /// <summary>
    /// Defines the rasterizer state configuration, which determines how primitives are rasterized (e.g., culling mode, fill mode).
    /// </summary>
    public RasterizerStateDescription RasterizerState;

    /// <summary>
    /// Specifies the primitive topology, which defines how the input vertices are interpreted (e.g., triangles, lines).
    /// </summary>
    public PrimitiveTopology PrimitiveTopology;

    /// <summary>
    /// An array of buffers that are bound to the pipeline, containing vertex data and possibly other information.
    /// </summary>
    public SimpleBufferLayout[] BufferLayouts;

    /// <summary>
    /// An array of texture layouts, describing how textures are arranged and accessed in the pipeline.
    /// </summary>
    public SimpleTextureLayout[] TextureLayouts;

    /// <summary>
    /// Describes the shader set, including the vertex and fragment shaders used by the pipeline.
    /// </summary>
    public ShaderSetDescription ShaderSet;

    /// <summary>
    /// Defines the output configuration, specifying render targets and the depth-stencil buffer for the pipeline.
    /// </summary>
    public OutputDescription Outputs;

    /// <summary>
    /// An optional resource binding model that maps resources such as textures and buffers to the pipeline.
    /// </summary>
    public ResourceBindingModel? ResourceBindingModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimplePipelineDescription"/> struct with the provided pipeline configuration details.
    /// </summary>
    /// <param name="blendState">The blend state configuration of the pipeline.</param>
    /// <param name="depthStencilState">The depth and stencil state configuration of the pipeline.</param>
    /// <param name="rasterizerState">The rasterizer state configuration of the pipeline.</param>
    /// <param name="primitiveTopology">The primitive topology that defines how vertex data is interpreted.</param>
    /// <param name="bufferLayouts">An array of buffer layouts to be used by the pipeline.</param>
    /// <param name="textureLayouts">An array of texture layouts to be used in the pipeline.</param>
    /// <param name="shaderSet">The shader set description that defines the vertex and fragment shaders.</param>
    /// <param name="outputs">The output configuration of the pipeline, specifying the render targets and depth-stencil buffer.</param>
    public SimplePipelineDescription(BlendStateDescription blendState, DepthStencilStateDescription depthStencilState, RasterizerStateDescription rasterizerState, PrimitiveTopology primitiveTopology, SimpleBufferLayout[] bufferLayouts, SimpleTextureLayout[] textureLayouts, ShaderSetDescription shaderSet, OutputDescription outputs) {
        this.BlendState = blendState;
        this.DepthStencilState = depthStencilState;
        this.RasterizerState = rasterizerState;
        this.PrimitiveTopology = primitiveTopology;
        this.BufferLayouts = bufferLayouts;
        this.TextureLayouts = textureLayouts;
        this.ShaderSet = shaderSet;
        this.Outputs = outputs;
        this.ResourceBindingModel = null;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SimplePipelineDescription"/> struct.
    /// </summary>
    public SimplePipelineDescription() {
        this.BufferLayouts = [];
        this.TextureLayouts = [];
    }
    
    /// <summary>
    /// Determines whether two <see cref="SimplePipelineDescription"/> instances are equal based on their properties.
    /// </summary>
    /// <param name="left">The first <see cref="SimplePipelineDescription"/> instance to compare.</param>
    /// <param name="right">The second <see cref="SimplePipelineDescription"/> instance to compare.</param>
    /// <returns><c>true</c> if the instances are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(SimplePipelineDescription left, SimplePipelineDescription right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="SimplePipelineDescription"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="SimplePipelineDescription"/> to compare.</param>
    /// <param name="right">The second <see cref="SimplePipelineDescription"/> to compare.</param>
    /// <returns>True if both instances are equal; otherwise, false.</returns>
    public static bool operator !=(SimplePipelineDescription left, SimplePipelineDescription right) => !left.Equals(right);

    /// <summary>
    /// Determines whether the current <see cref="SimplePipelineDescription"/> instance is equal to another specified instance.
    /// </summary>
    /// <param name="other">The other <see cref="SimplePipelineDescription"/> instance to compare with the current instance.</param>
    /// <returns>A boolean value indicating whether the two instances are equal.</returns>
    public bool Equals(SimplePipelineDescription other) {
        return this.BlendState.Equals(other.BlendState) &&
               this.DepthStencilState.Equals(other.DepthStencilState) &&
               this.RasterizerState.Equals(other.RasterizerState) &&
               this.PrimitiveTopology == other.PrimitiveTopology &&
               this.BufferLayouts.Select(layout => layout.Name).SequenceEqual(other.BufferLayouts.Select(layout => layout.Name)) &&
               this.TextureLayouts.Select(layout => layout.Name).SequenceEqual(other.TextureLayouts.Select(layout => layout.Name)) &&
               this.ShaderSet.Equals(other.ShaderSet) &&
               this.Outputs.Equals(other.Outputs) &&
               this.ResourceBindingModel == other.ResourceBindingModel;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="SimplePipelineDescription"/> instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns>A boolean value indicating whether the specified object is equal to the current instance.</returns>
    public override bool Equals(object? obj) {
        return obj is SimplePipelineDescription other && this.Equals(other);
    }

    /// <summary>
    /// Returns a hash code for this instance of <see cref="SimplePipelineDescription"/>.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code that is representative of the object's current state and its members.</returns>
    public override int GetHashCode() {
        HashCode hashCode = new HashCode();
        hashCode.Add(this.BlendState);
        hashCode.Add(this.DepthStencilState);
        hashCode.Add(this.RasterizerState);
        hashCode.Add((int) this.PrimitiveTopology);

        foreach (SimpleBufferLayout buffer in this.BufferLayouts) {
            hashCode.Add(buffer.Name);
        }
        
        foreach (SimpleTextureLayout layout in this.TextureLayouts) {
            hashCode.Add(layout.Name);
        }
        
        hashCode.Add(this.ShaderSet);
        hashCode.Add(this.Outputs);
        hashCode.Add(this.ResourceBindingModel);
        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Returns a string representation of the <see cref="SimplePipelineDescription"/> instance, detailing its configuration and properties.
    /// </summary>
    /// <returns>A string that includes the blend state, depth-stencil state, rasterizer state, primitive topology, buffers, texture layouts, shader set, outputs, and resource binding model of the pipeline description.</returns>
    public override string ToString() {
        return $"SimplePipelineDescription: \n" +
               $"\t> BlendState = {this.BlendState}, \n" +
               $"\t> DepthStencilState = {this.DepthStencilState}, \n" +
               $"\t> RasterizerState = {this.RasterizerState}, \n" +
               $"\t> PrimitiveTopology = {this.PrimitiveTopology}, \n" +
               $"\t> Buffers = [{string.Join(", ", this.BufferLayouts.Select(layout => layout.Name))}], \n" +
               $"\t> TextureLayouts = [{string.Join(", ", this.TextureLayouts.Select(layout => layout.Name))}], \n" +
               $"\t> ShaderSet = {this.ShaderSet}, \n" +
               $"\t> Outputs = {this.Outputs}, \n" +
               $"\t> ResourceBindingModel = {(this.ResourceBindingModel.HasValue ? this.ResourceBindingModel.Value : "NULL")}";
    }
}