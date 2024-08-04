using System.Numerics;
using Silk.NET.Vulkan;

namespace Bliss.CSharp.Camera.Dim3;

public class Cam3D : ICam {

    public Viewport Viewport { get; private set; }
    
    public Cam3D(Vector3 pos) {
        
    }
}