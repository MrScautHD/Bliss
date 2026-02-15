using Bliss.CSharp.Logging;

namespace Bliss.CSharp;

public abstract class Disposable : IDisposable {
    
    public bool HasDisposed => this._hasDisposed != 0;
    
    private volatile uint _hasDisposed;
    
    protected Disposable() {
        this._hasDisposed = 0;
    }
    
    ~Disposable() {
        if (Interlocked.Exchange(ref this._hasDisposed, 1) == 0) {
            this.Dispose(false);
        }
    }
    
    public void Dispose() {
        if (Interlocked.Exchange(ref this._hasDisposed, 1) == 0) {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        else {
            Logger.Warn($"This object of type [{this.GetType().Name}] has already been disposed.");
        }
    }
    
    protected abstract void Dispose(bool disposing);
    
    protected void ThrowIfDisposed() {
        if (this._hasDisposed != 0) {
            throw new ObjectDisposedException(this.GetType().Name);
        }
    }
}