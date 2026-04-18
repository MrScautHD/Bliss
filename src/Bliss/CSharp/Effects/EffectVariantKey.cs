using Veldrid.SPIRV;

namespace Bliss.CSharp.Effects;

public readonly struct EffectVariantKey : IEquatable<EffectVariantKey> {
    
    /// <summary>
    /// The base macro definitions shared by the effect.
    /// </summary>
    private readonly IReadOnlyList<MacroDefinition> _baseMacros;
    
    /// <summary>
    /// The additional macro names that define this variant.
    /// </summary>
    private readonly string[] _macros;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="EffectVariantKey"/> struct with the specified macro sets.
    /// </summary>
    /// <param name="baseMacros">The base macro definitions shared by the effect.</param>
    /// <param name="macros">The additional macro names that define the variant.</param>
    public EffectVariantKey(IReadOnlyList<MacroDefinition> baseMacros, string[] macros) {
        this._baseMacros = baseMacros;
        this._macros = macros;
    }
    
    /// <summary>
    /// Determines whether the current key is equal to another key by comparing macro names.
    /// </summary>
    /// <param name="other">The key to compare against.</param>
    /// <returns><see langword="true"/> if both keys represent the same macro combination; otherwise, <see langword="false"/>.</returns>
    public bool Equals(EffectVariantKey other) {
        for (int i = 0; i < this._baseMacros.Count; i++) {
            if (this._baseMacros[i].Name != other._baseMacros[i].Name) {
                return false;
            }
        }
        
        for (int i = 0; i < this._macros.Length; i++) {
            if (this._macros[i] != other._macros[i]) {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Determines whether the specified object is equal to the current key.
    /// </summary>
    /// <param name="obj">The object to compare with the current key.</param>
    /// <returns><see langword="true"/> if the object is an <see cref="EffectVariantKey"/> with the same macro combination; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj) {
        return obj is EffectVariantKey other && this.Equals(other);
    }
    
    /// <summary>
    /// Returns a hash code for the current key based on its macro names.
    /// </summary>
    /// <returns>A hash code suitable for use in hash-based collections.</returns>
    public override int GetHashCode() {
        HashCode hash = new HashCode();
        
        foreach (var baseMacro in this._baseMacros) {
            hash.Add(baseMacro.Name);
        }

        foreach (var macro in this._macros) {
            hash.Add(macro);
        }

        return hash.ToHashCode();
    }
}