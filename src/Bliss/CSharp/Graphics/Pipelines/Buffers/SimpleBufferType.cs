/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

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