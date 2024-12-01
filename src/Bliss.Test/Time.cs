/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: MIT License
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

using System.Diagnostics;

namespace Bliss.Test;

public static class Time {

    /// <summary>
    /// Gets the time elapsed between the current and the previous frame.
    /// </summary>
    public static double Delta { get; private set; }

    /// <summary>
    /// Gets the total elapsed time since the start of the application.
    /// </summary>
    public static double Total => _watch.Elapsed.TotalSeconds;
    
    /// <summary>
    /// A static stopwatch used for measuring elapsed time. This is intended for internal use only.
    /// </summary>
    internal static Stopwatch Timer;
    
    /// <summary>
    /// A private static stopwatch used for measuring the total elapsed time since the start of the application.
    /// </summary>
    private static Stopwatch _watch;

    /// <summary>
    /// Initializes the Time class.
    /// </summary>
    internal static void Init() {
        Timer = Stopwatch.StartNew();
        _watch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Updates the Time class by calculating the time delta and restarting the timer.
    /// </summary>
    internal static void Update() {
        Delta = Timer.Elapsed.TotalSeconds;
        Timer.Restart();
    }
}