namespace Bliss.CSharp.Effects;

public class ComputeEffectVariant : Disposable {
    
    /// <summary>
    /// The parent effect that owns this variant.
    /// </summary>
    public ComputeEffect ParentEffect { get; private set; }
    
    /// <summary>
    /// The compiled effect used by this variant.
    /// </summary>
    public ComputeEffect Effect { get; private set; }
    
    /// <summary>
    /// Initializes a new <see cref="ComputeEffectVariant"/> class.
    /// </summary>
    /// <param name="parentEffect">The effect that owns this variant.</param>
    /// <param name="effect">The compiled effect used by this variant.</param>
    internal ComputeEffectVariant(ComputeEffect parentEffect, ComputeEffect effect) {
        this.ParentEffect = parentEffect;
        this.Effect = effect;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.Effect.Dispose();
        }
    }
}