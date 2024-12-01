/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

namespace Bliss.CSharp.Camera.Dim3;

public enum CameraMode {
    
    /// <summary>
    /// Custom mode, allowing for user-defined camera behavior and controls.
    /// </summary>
    Custom,
    
    /// <summary>
    /// Free mode, where the camera can move freely in 3D space.
    /// </summary>
    Free,
    
    /// <summary>
    /// Orbital mode, where the camera orbits around a target point.
    /// </summary>
    Orbital,
    
    /// <summary>
    /// First-person mode, where the camera simulates the view of a character.
    /// </summary>
    FirstPerson,
    
    /// <summary>
    /// Third-person mode, where the camera follows a character from a distance.
    /// </summary>
    ThirdPerson
}