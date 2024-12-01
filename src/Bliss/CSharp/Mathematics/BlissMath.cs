/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

using System.Numerics;

namespace Bliss.CSharp.Mathematics;

public static class BlissMath {
    
    /// <summary>
    /// Calculates the angle in radians between two vectors.
    /// </summary>
    /// <param name="v1">The first vector.</param>
    /// <param name="v2">The second vector.</param>
    /// <returns>The angle in radians between the two vectors.</returns>
    public static float Vector3Angle(Vector3 v1, Vector3 v2) {
        float dotProduct = Vector3.Dot(v1, v2);
        float lengthsProduct = v1.Length() * v2.Length();
        
        return (float) Math.Acos(dotProduct / lengthsProduct);
    }

    /// <summary>
    /// Rotates a vector around a specified axis by a given angle.
    /// </summary>
    /// <param name="v">The vector to be rotated.</param>
    /// <param name="axis">The axis around which to rotate the vector.</param>
    /// <param name="angle">The angle in radians by which to rotate the vector.</param>
    /// <returns>The rotated vector.</returns>
    public static Vector3 Vector3RotateByAxisAngle(Vector3 v, Vector3 axis, float angle) {
        Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), angle);
        return Vector3.Transform(v, rotation);
    }
}