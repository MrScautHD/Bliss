/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: MIT License
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

using System.Reflection;
using Bliss.CSharp.Windowing;
using Veldrid;

namespace Bliss.Test;

public struct GameSettings {
    
    public string Title { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public string IconPath { get; private set; }
    public int TargetFps { get; init; }
    public double FixedTimeStep { get; init; }
    public WindowState WindowFlags { get; init; }
    public GraphicsBackend Backend { get; init; }
    public bool VSync { get; init; }
    public TextureSampleCount SampleCount { get; init; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="GameSettings"/> with default values for various game settings such as window size, icon path, log directory, content directory, and more.
    /// </summary>
    public GameSettings() {
        this.Title = Assembly.GetEntryAssembly()?.GetName().Name ?? "Bliss";
        this.Width = 1280;
        this.Height = 720;
        this.IconPath = string.Empty;
        this.TargetFps = 0;
        this.FixedTimeStep = 1.0F / 60.0F;
        this.WindowFlags = WindowState.Resizable;
        this.Backend = Window.GetPlatformDefaultBackend();
        this.VSync = true;
        this.SampleCount = TextureSampleCount.Count1;
    }
}