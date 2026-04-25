namespace Bliss.CSharp.Materials;

public readonly struct MaterialMapKey : IEquatable<MaterialMapKey> {
    
    /// <summary>
    /// The shader-facing name this key resolves to.
    /// </summary>
    public readonly string Name;
    
    /// <summary>
    /// Initializes a <see cref="MaterialMapKey"/> from a <see cref="MaterialMapType"/> enum value.
    /// The key name is resolved via <see cref="MaterialMapTypeExtensions.GetName"/>.
    /// </summary>
    public MaterialMapKey(MaterialMapType type) {
        this.Name = type.GetName();
    }
    
    /// <summary>
    /// Initializes a <see cref="MaterialMapKey"/> from a raw string name.
    /// </summary>
    /// <param name="name">The shader-facing name of the material map slot.</param>
    public MaterialMapKey(string name) {
        this.Name = name;
    }
    
    /// <summary>
    /// Implicitly converts a <see cref="MaterialMapType"/> enum value to a <see cref="MaterialMapKey"/>.
    /// </summary>
    /// <param name="type">The material map type to convert.</param>
    public static implicit operator MaterialMapKey(MaterialMapType type) => new MaterialMapKey(type);
    
    /// <summary>
    /// Implicitly converts a plain string to a <see cref="MaterialMapKey"/>.
    /// </summary>
    /// <param name="name">The shader-facing name to convert.</param>
    public static implicit operator MaterialMapKey(string name) => new MaterialMapKey(name);
    
    /// <summary>
    /// Determines whether two <see cref="MaterialMapKey"/> instances are equal.
    /// </summary>
    /// <param name="left">The left-hand side key.</param>
    /// <param name="right">The right-hand side key.</param>
    public static bool operator ==(MaterialMapKey left, MaterialMapKey right) => left.Equals(right);
    
    /// <summary>
    /// Determines whether two <see cref="MaterialMapKey"/> instances are not equal.
    /// </summary>
    /// <param name="left">The left-hand side key.</param>
    /// <param name="right">The right-hand side key.</param>
    public static bool operator !=(MaterialMapKey left, MaterialMapKey right) => !left.Equals(right);
    
    /// <summary>
    /// Determines whether this <see cref="MaterialMapKey"/> is equal to another by performing an ordinal string comparison on their names.
    /// </summary>
    /// <param name="other">The other <see cref="MaterialMapKey"/> to compare against.</param>
    /// <returns>True if both keys resolve to the same shader-facing name; otherwise false.</returns>
    public bool Equals(MaterialMapKey other) {
        return string.Equals(this.Name, other.Name, StringComparison.Ordinal);
    }
    
    /// <summary>
    /// Determines whether this <see cref="MaterialMapKey"/> is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns>True if the object is a <see cref="MaterialMapKey"/> with the same name; otherwise false.</returns>
    public override bool Equals(object? obj) {
        return obj is MaterialMapKey other && this.Equals(other);
    }
    
    /// <summary>
    /// Returns a hash code for this <see cref="MaterialMapKey"/> based on its name using ordinal comparison.
    /// </summary>
    /// <returns>A hash code derived from the shader-facing name.</returns>
    public override int GetHashCode() {
        return StringComparer.Ordinal.GetHashCode(this.Name);
    }
    
    /// <summary>
    /// Returns the shader-facing name this key resolves to.
    /// </summary>
    /// <returns>The name of this <see cref="MaterialMapKey"/>.</returns>
    public override string ToString() {
        return this.Name;
    }
}