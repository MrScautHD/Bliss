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

    /// <summary>
    /// Creates a <see cref="Quaternion"/> from Euler angles specified in degrees.
    /// </summary>
    /// <param name="pitch">The pitch angle in degrees (rotation around the X axis).</param>
    /// <param name="yaw">The yaw angle in degrees (rotation around the Y axis).</param>
    /// <param name="roll">The roll angle in degrees (rotation around the Z axis).</param>
    /// <returns>A <see cref="Quaternion"/> representing the combined rotation.</returns>
    public static Quaternion EulerDegreesToQuaternion(float pitch, float yaw, float roll) {
        return Quaternion.CreateFromYawPitchRoll(float.DegreesToRadians(yaw), float.DegreesToRadians(pitch), float.DegreesToRadians(roll));
    }

    /// <summary>
    /// Creates a delta <see cref="Quaternion"/> rotation from Euler angles in degrees, scaled by a time step.
    /// </summary>
    /// <param name="pitch">The pitch angle in degrees (rotation around the X axis).</param>
    /// <param name="yaw">The yaw angle in degrees (rotation around the Y axis).</param>
    /// <param name="roll">The roll angle in degrees (rotation around the Z axis).</param>
    /// <param name="deltaTime">The elapsed time step to scale the rotation by.</param>
    /// <returns>A <see cref="Quaternion"/> representing the incremental rotation.</returns>
    public static Quaternion EulerDegreesToQuaternion(float pitch, float yaw, float roll, float deltaTime) {
        return Quaternion.CreateFromYawPitchRoll(float.DegreesToRadians(yaw * deltaTime), float.DegreesToRadians(pitch * deltaTime), float.DegreesToRadians(roll * deltaTime));
    }
}
