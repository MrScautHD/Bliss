using Bliss.CSharp.Logging;

namespace Bliss.CSharp;

public abstract class Disposable : IDisposable {
    
    /// <summary>
    /// Gets a value indicating whether this instance has already been disposed.
    /// </summary>
    public bool HasDisposed => this._hasDisposed != 0;
    
    /// <summary>
    /// Internal flag used to ensure disposal happens only once.
    /// </summary>
    private volatile uint _hasDisposed;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Disposable"/> class.
    /// </summary>
    protected Disposable() {
        this._hasDisposed = 0;
    }
    
    /// <summary>
    /// Finalizer that ensures unmanaged resources are released if <see cref="Dispose()"/> was not called explicitly.
    /// </summary>
    ~Disposable() {
        if (Interlocked.Exchange(ref this._hasDisposed, 1) == 0) {
            this.Dispose(false);
        }
    }
    
    /// <summary>
    /// Releases all resources used by this instance.
    /// </summary>
    public void Dispose() {
        if (Interlocked.Exchange(ref this._hasDisposed, 1) == 0) {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        else {
            Logger.Warn($"This object of type [{this.GetType().Name}] has already been disposed.");
        }
    }
    
    /// <summary>
    /// Releases the unmanaged resources used by this instance and optionally releases managed resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> if called from <see cref="Dispose()"/>; <c>false</c> if called from the finalizer.</param>
    protected abstract void Dispose(bool disposing);
    
    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if this instance has already been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the object has already been disposed.</exception>
    protected void ThrowIfDisposed() {
        if (this._hasDisposed != 0) {
            throw new ObjectDisposedException(this.GetType().Name);
        }
    }
}