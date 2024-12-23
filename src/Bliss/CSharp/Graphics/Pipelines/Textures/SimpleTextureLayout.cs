/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

using Veldrid;

namespace Bliss.CSharp.Graphics.Pipelines.Textures;

public class SimpleTextureLayout : Disposable {
    
    /// <summary>
    /// The graphics device used to create the resource layout for textures and samplers.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }

    /// <summary>
    /// The name used for the texture and sampler resources.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// The resource layout associated with textures and samplers.
    /// </summary>
    public ResourceLayout Layout { get; private set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleTextureLayout"/> class with the specified graphics device and texture name.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create the resource layout.</param>
    /// <param name="name">The name used for the texture and sampler resources.</param>
    public SimpleTextureLayout(GraphicsDevice graphicsDevice, string name) {
        this.GraphicsDevice = graphicsDevice;
        this.Name = name;
        
        this.Layout = this.GraphicsDevice.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription() {
            Elements = [
                new ResourceLayoutElementDescription(name, ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription($"{name}Sampler", ResourceKind.Sampler, ShaderStages.Fragment)
            ]
        });
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.Layout.Dispose();
        }
    }
}