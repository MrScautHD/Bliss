/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

namespace Bliss.CSharp.Interact.Mice.Cursors;

public interface ICursor : IDisposable {
    
    /// <summary>
    /// Retrieves the handle of the current cursor.
    /// </summary>
    /// <returns>A pointer to the cursor handle.</returns>
    nint GetHandle();
}