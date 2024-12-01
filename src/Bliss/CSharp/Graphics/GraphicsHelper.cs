/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

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
            SamplerType.Point => graphicsDevice.PointSampler,
            SamplerType.Linear => graphicsDevice.LinearSampler,
            SamplerType.Aniso4X => graphicsDevice.Aniso4XSampler,
            _ => throw new ArgumentException($"Unsupported sampler type: {samplerType}", nameof(samplerType))
        };
    }
}