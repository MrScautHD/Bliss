using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.Pipelines.Textures;
using Bliss.CSharp.Materials;
using Veldrid;

namespace Bliss.CSharp.Effects;

public class EffectVariant : Disposable {
    
    /// <summary>
    /// The parent effect that owns this variant.
    /// </summary>
    public Effect ParentEffect { get; private set; }
    
    /// <summary>
    /// The compiled effect used by this variant.
    /// </summary>
    public Effect Effect { get; private set; }
    
    /// <summary>
    /// The normalized cache key for this effect variant.
    /// </summary>
    public string Key { get; private set; }
    
    /// <summary>
    /// Initializes a new <see cref="EffectVariant"/> with the specified parent effect, compiled effect, and cache key.
    /// </summary>
    /// <param name="parentEffect">The effect that owns this variant.</param>
    /// <param name="effect">The compiled effect used by this variant.</param>
    /// <param name="key">The normalized cache key for this effect variant.</param>
    internal EffectVariant(Effect parentEffect, Effect effect, string key) {
        this.ParentEffect = parentEffect;
        this.Effect = effect;
        this.Key = key;
    }
    
    /// <summary>
    /// Retrieves a collection of buffer layouts associated with the variant.
    /// </summary>
    public IReadOnlyCollection<SimpleBufferLayout> GetBufferLayouts() {
        return this.Effect.GetBufferLayouts();
    }
    
    /// <summary>
    /// Retrieves the buffer layout identified by the specified name.
    /// </summary>
    public SimpleBufferLayout GetBufferLayout(string name) {
        return this.Effect.GetBufferLayout(name);
    }
    
    /// <summary>
    /// Retrieves the slot index of a buffer layout by its name.
    /// </summary>
    public uint GetBufferLayoutSlot(string name) {
        return this.Effect.GetBufferLayoutSlot(name);
    }
    
    /// <summary>
    /// Retrieves a collection of texture layouts associated with the variant.
    /// </summary>
    public IReadOnlyCollection<SimpleTextureLayout> GetTextureLayouts() {
        return this.Effect.GetTextureLayouts();
    }
    
    /// <summary>
    /// Retrieves the texture layout identified by the specified name.
    /// </summary>
    public SimpleTextureLayout GetTextureLayout(string name) {
        return this.Effect.GetTextureLayout(name);
    }
    
    /// <summary>
    /// Retrieves the slot index of a texture layout by its name.
    /// </summary>
    public uint GetTextureLayoutSlot(string name) {
        return this.Effect.GetTextureLayoutSlot(name);
    }
    
    /// <summary>
    /// Retrieves or creates a pipeline for the given pipeline description.
    /// </summary>
    public SimplePipeline GetPipeline(SimplePipelineDescription pipelineDescription) {
        return this.Effect.GetPipeline(pipelineDescription);
    }
    
    /// <summary>
    /// Apply the state effect immediately before rendering it.
    /// </summary>
    public void Apply(CommandList commandList, Material? material = null) {
        this.Effect.Apply(commandList, material);
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.Effect.Dispose();
        }
    }
}