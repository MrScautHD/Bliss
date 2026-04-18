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
    /// Initializes a new <see cref="EffectVariant"/> with the specified parent effect, compiled effect, and cache key.
    /// </summary>
    /// <param name="parentEffect">The effect that owns this variant.</param>
    /// <param name="effect">The compiled effect used by this variant.</param>
    internal EffectVariant(Effect parentEffect, Effect effect) {
        this.ParentEffect = parentEffect;
        this.Effect = effect;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.Effect.Dispose();
        }
    }
}