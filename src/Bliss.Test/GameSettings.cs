using System.Reflection;
using Veldrid;

namespace Bliss.Test;

public struct GameSettings {
    
    public string Title { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public int TargetFps { get; init; }
    public double FixedTimeStep { get; init; }
    public TextureSampleCount SampleCount { get; init; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="GameSettings"/> with default values for various game settings such as window size, icon path, log directory, content directory, and more.
    /// </summary>
    public GameSettings() {
        this.Title = Assembly.GetEntryAssembly()?.GetName().Name ?? "Bliss";
        this.Width = 1280;
        this.Height = 720;
        this.TargetFps = 0;
        this.FixedTimeStep = 1.0F / 60.0F;
        this.SampleCount = 0;
    }
}