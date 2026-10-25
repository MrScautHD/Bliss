using System.Numerics;
using Bliss.CSharp.Textures.Cubemaps;

namespace Bliss.CSharp.Graphics.Rendering.Renderers.Sky.Environment;

public class CubemapEnvironment : Disposable, IEnvironmentProvider {
    
    /// <summary>
    /// Gets the cubemap skybox used to provide the environment.
    /// </summary>
    public CubemapSkybox Skybox { get; private set; }
    
    /// <summary>
    /// Gets the cubemap texture used as the source environment.
    /// </summary>
    public Cubemap SourceEnvironmentCubemap => this.Skybox.Cubemap;
    
    /// <summary>
    /// Gets the environment color with the skybox exposure applied to its alpha component.
    /// </summary>
    public Vector4 EnvironmentColor {
        get {
            Vector4 color = this.Skybox.Color.ToRgbaFloatVec4();
            return new Vector4(color.X, color.Y, color.Z, this.Skybox.Exposure);
        }
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CubemapEnvironment"/> class.
    /// </summary>
    /// <param name="skybox">The cubemap skybox used to provide the environment.</param>
    public CubemapEnvironment(CubemapSkybox skybox) {
        this.Skybox = skybox;
    }
    
    protected override void Dispose(bool disposing) { }
}