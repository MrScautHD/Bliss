using Veldrith.SPIRV;

namespace Bliss.CSharp.Effects;

public readonly struct EffectVariantKey : IEquatable<EffectVariantKey> {
    
    /// <summary>
    /// The base macro definitions shared by the effect.
    /// </summary>
    private readonly IReadOnlyList<MacroDefinition> _baseMacros;
    
    /// <summary>
    /// The additional macros that define this variant.
    /// </summary>
    private readonly (string Name, string Value)[] _macros;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="EffectVariantKey"/> struct with the specified macro sets.
    /// </summary>
    /// <param name="baseMacros">The base macro definitions shared by the effect.</param>
    /// <param name="macros">The additional macros (name and value pairs) that define the variant.</param>
    public EffectVariantKey(IReadOnlyList<MacroDefinition> baseMacros, (string Name, string Value)[] macros) {
        this._baseMacros = baseMacros;
        this._macros = macros;
    }
    
    /// <summary>
    /// Determines whether this key represents the given macro combination, without creating a new key.
    /// </summary>
    /// <param name="macros">The additional macros (name and value pairs) to compare against.</param>
    /// <returns><see langword="true"/> if the macro names and values are identical and in the same order; otherwise, <see langword="false"/>.</returns>
    public bool Matches(ReadOnlySpan<(string Name, string Value)> macros) {
        return this._macros.AsSpan().SequenceEqual(macros);
    }
    
    /// <summary>
    /// Determines whether the current key is equal to another key by comparing the names and values of the base and additional macros.
    /// </summary>
    /// <param name="other">The key to compare against.</param>
    /// <returns><see langword="true"/> if both keys represent the same macro combination; otherwise, <see langword="false"/>.</returns>
    public bool Equals(EffectVariantKey other) {
        if (this._baseMacros.Count != other._baseMacros.Count || this._macros.Length != other._macros.Length) {
            return false;
        }
        
        for (int i = 0; i < this._baseMacros.Count; i++) {
            if (this._baseMacros[i].Name != other._baseMacros[i].Name || this._baseMacros[i].Value != other._baseMacros[i].Value) {
                return false;
            }
        }
        
        for (int i = 0; i < this._macros.Length; i++) {
            if (this._macros[i].Name != other._macros[i].Name || this._macros[i].Value != other._macros[i].Value) {
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
    /// Returns a hash code for the current key based on the names and values of the base and additional macros.
    /// </summary>
    /// <returns>A hash code suitable for use in hash-based collections.</returns>
    public override int GetHashCode() {
        HashCode hash = new HashCode();
        
        if (this._baseMacros != null) {
            for (int i = 0; i < this._baseMacros.Count; i++) {
                MacroDefinition macro = this._baseMacros[i];
                hash.Add(macro.Name);
                hash.Add(macro.Value);
            }
        }
        
        if (this._macros != null) {
            for (int i = 0; i < this._macros.Length; i++) {
                hash.Add(this._macros[i].Name);
                hash.Add(this._macros[i].Value);
            }
        }
        
        return hash.ToHashCode();
    }
}