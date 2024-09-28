namespace Bliss.CSharp.Interact.Mice.Cursors;

public interface ICursor : IDisposable {
    
    /// <summary>
    /// Retrieves the handle of the current cursor.
    /// </summary>
    /// <returns>A pointer to the cursor handle.</returns>
    nint GetHandle();
}