namespace Bliss.CSharp.Windowing.Events;

public struct DragDropEvent {
    
    /// <summary>
    /// Represents the X-coordinate position where a drag and drop event occurs.
    /// </summary>
    public int X;

    /// <summary>
    /// Represents the Y-coordinate position where a drag and drop event occurs.
    /// </summary>
    public int Y;
    
    /// <summary>
    /// Represents the file system path involved in a drag and drop event.
    /// </summary>
    public string Path;

    /// <summary>
    /// Initializes a new instance of the <see cref="DragDropEvent"/> class with the specified coordinates and file path.
    /// </summary>
    /// <param name="x">The X-coordinate where the drag-and-drop event occurred.</param>
    /// <param name="y">The Y-coordinate where the drag-and-drop event occurred.</param>
    /// <param name="path">The file path associated with the drag-and-drop event.</param>
    public DragDropEvent(int x, int y, string path) {
        this.X = x;
        this.Y = y;
        this.Path = path;
    }
}