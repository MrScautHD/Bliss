using System.Numerics;
using Bliss.CSharp.Textures.Cubemaps;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Sky.Environment;

public interface IEnvironmentProvider : IDisposable {
    
    /// <summary>
    /// The source cubemap used to bake environment lighting.
    /// </summary>
    Cubemap SourceEnvironmentCubemap { get; }
    
    /// <summary>
    /// Color applied to environment lighting.
    /// </summary>
    Vector4 EnvironmentColor { get; }
}