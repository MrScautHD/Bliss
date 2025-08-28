using System.Numerics;
using System.Runtime.InteropServices;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Forward;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct MaterialData {
    
    /// <summary>
    /// Specifies the maximum number of material maps supported.
    /// </summary>
    private const int MaxMaterialMapCount = 8;
    
    /// <summary>
    /// The render mode of the material (corresponds to <see cref="RenderMode"/> enum).
    /// </summary>
    public int RenderMode;

    /// <summary>
    /// A private padding field used for memory alignment.
    /// </summary>
    private Vector3 _padding;
    
    /// <summary>
    /// Array storing the colors of the material maps. Each map uses 4 floats (RGBA).
    /// </summary>
    public fixed float Colors[4 * MaxMaterialMapCount];
    
    /// <summary>
    /// Array storing scalar values for the material maps.
    /// </summary>
    public fixed float Values[MaxMaterialMapCount];
    
    /// <summary>
    /// Gets the color of a material map at the specified index.
    /// </summary>
    /// <param name="index">The index of the material map (0-based, max 7).</param>
    /// <returns>A <see cref="Vector4"/> representing RGBA values.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown if index is out of range.</exception>
    public Vector4 GetColor(uint index) {
        if (index >= MaxMaterialMapCount) {
            throw new IndexOutOfRangeException($"Index {index} is out of the valid range for Colors.");
        }
        
        return new Vector4(this.Colors[index * 4], this.Colors[index * 4 + 1], this.Colors[index * 4 + 2], this.Colors[index * 4 + 3]);
    }
    
    /// <summary>
    /// Sets the color of a material map at the specified index.
    /// </summary>
    /// <param name="index">The index of the material map (0-based, max 7).</param>
    /// <param name="color">The RGBA color to set.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown if index is out of range.</exception>
    public void SetColor(uint index, Vector4 color) {
        if (index >= MaxMaterialMapCount) {
            throw new IndexOutOfRangeException($"Index {index} is out of the valid range for Colors.");
        }
        
        this.Colors[index * 4] = color.X;
        this.Colors[index * 4 + 1] = color.Y;
        this.Colors[index * 4 + 2] = color.Z;
        this.Colors[index * 4 + 3] = color.W;
    }
    
    /// <summary>
    /// Gets the scalar value of a material map at the specified index.
    /// </summary>
    /// <param name="index">The index of the material map (0-based, max 7).</param>
    /// <returns>The scalar value.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown if index is out of range.</exception>
    public float GetValue(uint index) {
        if (index >= MaxMaterialMapCount) {
            throw new IndexOutOfRangeException($"Index {index} is out of the valid range for Values.");
        }
        
        return this.Values[index];
    }
    
    /// <summary>
    /// Sets the scalar value of a material map at the specified index.
    /// </summary>
    /// <param name="index">The index of the material map (0-based, max 7).</param>
    /// <param name="value">The scalar value to set.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown if index is out of range.</exception>
    public void SetValue(uint index, float value) {
        if (index >= MaxMaterialMapCount) {
            throw new IndexOutOfRangeException($"Index {index} is out of the valid range for Values.");
        }
        
        this.Values[index] = value;
    }
}