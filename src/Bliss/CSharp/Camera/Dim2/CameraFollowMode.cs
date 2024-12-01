/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

namespace Bliss.CSharp.Camera.Dim2;

public enum CameraFollowMode {
    
    /// <summary>
    /// Custom follow behavior, allowing manual control of the camera's movement.
    /// </summary>
    Custom,
    
    /// <summary>
    /// Directly follows the target position without any smoothing or delay.
    /// </summary>
    FollowTarget,
    
    /// <summary>
    /// Smoothly follows the target with a gradual transition, creating a smoother effect.
    /// </summary>
    FollowTargetSmooth
}