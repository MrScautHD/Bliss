using Veldrid;

namespace Bliss.CSharp.Graphics.Pipelines;

public struct VertexFormat : IEquatable<VertexFormat> {
    
    /// <summary>
    /// Gets the name of the vertex format.
    /// </summary>
    public string Name;
    
    /// <summary>
    /// Gets the vertex layout descriptions that define this format.
    /// </summary>
    public VertexLayoutDescription[] Layouts;
    
    /// <summary>
    /// Gets or sets a value indicating whether this vertex format uses skinning data.
    /// </summary>
    public bool IsSkinned;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="VertexFormat"/> struct.
    /// </summary>
    /// <param name="name">The name of the vertex format.</param>
    /// <param name="layouts">The vertex layout descriptions that make up the format.</param>
    public VertexFormat(string name, params VertexLayoutDescription[] layouts) {
        this.Name = name;
        this.Layouts = layouts;
    }
    
    /// <summary>
    /// Determines whether two <see cref="VertexFormat"/> values are equal.
    /// </summary>
    /// <param name="left">The first vertex format to compare.</param>
    /// <param name="right">The second vertex format to compare.</param>
    /// <returns><see langword="true"/> if the formats are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(VertexFormat left, VertexFormat right) => left.Equals(right);
    
    /// <summary>
    /// Determines whether two <see cref="VertexFormat"/> values are not equal.
    /// </summary>
    /// <param name="left">The first vertex format to compare.</param>
    /// <param name="right">The second vertex format to compare.</param>
    /// <returns><see langword="true"/> if the formats are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(VertexFormat left, VertexFormat right) => !left.Equals(right);
    
    /// <summary>
    /// Determines whether the current instance is equal to another <see cref="VertexFormat"/>.
    /// </summary>
    /// <param name="other">The vertex format to compare with the current instance.</param>
    /// <returns><see langword="true"/> if the specified vertex format is equal to the current instance; otherwise, <see langword="false"/>.</returns>
    public bool Equals(VertexFormat other) {
        return this.Name == other.Name &&
               this.IsSkinned == other.IsSkinned &&
               this.Layouts.SequenceEqual(other.Layouts);
    }
    
    /// <summary>
    /// Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns><see langword="true"/> if the specified object is a <see cref="VertexFormat"/> and is equal to the current instance; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj) {
        return obj is VertexFormat other && this.Equals(other);
    }
    
    /// <summary>
    /// Returns a hash code for the current <see cref="VertexFormat"/>.
    /// </summary>
    /// <returns>A hash code that represents the current vertex format.</returns>
    public override int GetHashCode() {
        HashCode hashCode = new HashCode();
        hashCode.Add(this.Name);
        hashCode.Add(this.IsSkinned);
        
        foreach (VertexLayoutDescription layout in this.Layouts) {
            hashCode.Add(layout);
        }
        
        return hashCode.ToHashCode();
    }
}