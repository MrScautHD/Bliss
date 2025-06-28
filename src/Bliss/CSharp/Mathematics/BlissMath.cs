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

    /// <summary> This creates a rotation from Euler angles in degrees.</summary>
    /// <usage> This should be used when you want to apply an absolute rotation.</usage>
    /// <param name="pitch">The pitch angle in degrees.</param>
    /// <param name="yaw">The yaw angle in degrees.</param>
    /// <param name="roll"> The roll angle in degrees.</param>
    /// <returns> A <see cref="Quaternion"/> representing the absolute rotation given the angles provided.</returns>
    public static Quaternion RotationEulerDegrees(float pitch, float yaw, float roll)
    {
        return Quaternion.CreateFromYawPitchRoll(float.DegreesToRadians(yaw), float.DegreesToRadians(pitch), float.DegreesToRadians(roll));
    }
    
    /// <summary> This gives a delta rotation given the rotation angles in degrees.</summary>
    /// <usage> This should be used when you want to apply a rotation based on the change in angles over time, such as in an update loop.</usage>
    /// <param name="pitch">The pitch angle in degrees.</param>
    /// <param name="yaw">The yaw angle in degrees.</param>
    /// <param name="roll"> The roll angle in degrees.</param>
    /// <returns> A <see cref="Quaternion"/> representing the rotation delta between frames given the angles provided.</returns>
    public static Quaternion RotationDeltaEulerDegrees(float pitch, float yaw, float roll, float deltaTime)
    {
        return Quaternion.CreateFromYawPitchRoll(float.DegreesToRadians(yaw * deltaTime), float.DegreesToRadians(pitch * deltaTime), float.DegreesToRadians(roll * deltaTime));
    }
}