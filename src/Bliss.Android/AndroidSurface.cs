using Android.Content;
using Android.Runtime;
using Org.Libsdl.App;

namespace Bliss.Android;

public class AndroidSurface : SDLSurface {
    
    protected AndroidSurface(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {
        
    }

    public AndroidSurface(Context? p0) : base(p0) {
    }
}