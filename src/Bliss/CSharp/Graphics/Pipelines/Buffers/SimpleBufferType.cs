namespace Bliss.CSharp.Graphics.Pipelines.Buffers;

public enum SimpleBufferType {
    
    /// <summary>
    /// A buffer type that holds uniform data.
    /// </summary>
    Uniform,
    
    /// <summary>
    /// A read-only buffer that supports structured data access.
    /// </summary>
    StructuredReadOnly,
    
    /// <summary>
    /// A read-write buffer that supports structured data access.
    /// </summary>
    StructuredReadWrite
}