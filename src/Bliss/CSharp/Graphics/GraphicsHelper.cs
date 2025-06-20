using Veldrid;

namespace Bliss.CSharp.Graphics;

public static class GraphicsHelper {
    
    /// <summary>
    /// Retrieves a Sampler object based on the provided SamplerType.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create the sampler.</param>
    /// <param name="samplerType">The type of sampler to retrieve.</param>
    /// <returns>A Sampler object corresponding to the specified SamplerType.</returns>
    /// <exception cref="ArgumentException">Thrown when an unsupported sampler type is provided.</exception>
    public static Sampler GetSampler(GraphicsDevice graphicsDevice, SamplerType samplerType) {
        return samplerType switch {
            SamplerType.PointClamp => GlobalResource.PointClampSampler,
            SamplerType.PointWrap => graphicsDevice.PointSampler,
            SamplerType.LinearClamp => GlobalResource.LinearClampSampler,
            SamplerType.LinearWrap => graphicsDevice.LinearSampler,
            SamplerType.Aniso4XClamp => GlobalResource.Aniso4XClampSampler,
            SamplerType.Aniso4XWrap => graphicsDevice.Aniso4XSampler,
            _ => throw new ArgumentException($"Unsupported sampler type: {samplerType}", nameof(samplerType))
        };
    }
}