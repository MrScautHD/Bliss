/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

using System.Numerics;

namespace Bliss.CSharp.Transformations;

public struct Transform {
    
    /// <summary>
    /// Represents the position of an object in 3D space.
    /// </summary>
    public Vector3 Translation;

    /// <summary>
    /// Represents the rotation of an object in 3D space.
    /// </summary>
    public Quaternion Rotation;

    /// <summary>
    /// Represents the scale of an object in 3D space.
    /// </summary>
    public Vector3 Scale;

    /// <summary>
    /// Initializes a new instance of the <see cref="Transform"/> class with default values.
    /// </summary>
    public Transform() {
        this.Translation = Vector3.Zero;
        this.Rotation = Quaternion.Identity;
        this.Scale = Vector3.One;
    }

    /// <summary>
    /// Returns the transformation matrix for the current Transform object.
    /// </summary>
    /// <returns>The transformation matrix.</returns>
    public Matrix4x4 GetTransform() {
        Matrix4x4 matTranslation = Matrix4x4.CreateTranslation(this.Translation);
        Matrix4x4 matRotation = Matrix4x4.CreateFromQuaternion(this.Rotation);
        Matrix4x4 matScale = Matrix4x4.CreateScale(this.Scale);
        
        return matTranslation * matRotation * matScale;
    }
}