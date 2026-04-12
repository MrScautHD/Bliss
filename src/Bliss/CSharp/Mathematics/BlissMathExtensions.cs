using System.Numerics;

namespace Bliss.CSharp.Mathematics;

public static class BlissMathExtensions {
    
    /// <summary>
    /// Provides extension methods for working with <see cref="Vector3"/>.
    /// </summary>
    extension(Vector3) {
        
        /// <summary>
        /// Calculates the angle in radians between two vectors.
        /// </summary>
        /// <param name="v1">The first vector.</param>
        /// <param name="v2">The second vector.</param>
        /// <returns>The angle in radians between the two vectors.</returns>
        public static float AngleBetween(Vector3 v1, Vector3 v2) {
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
        public static Vector3 RotateByAxisAngle(Vector3 v, Vector3 axis, float angle) {
            Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), angle);
            return Vector3.Transform(v, rotation);
        }
    }
    
    /// <summary>
    /// Provides extension methods for working with <see cref="Quaternion"/>.
    /// </summary>
    extension(Quaternion) {
        
        /// <summary>
        /// Creates a <see cref="Quaternion"/> from Euler angles specified in degrees.
        /// </summary>
        /// <param name="pitch">The pitch angle in degrees (rotation around the X axis).</param>
        /// <param name="yaw">The yaw angle in degrees (rotation around the Y axis).</param>
        /// <param name="roll">The roll angle in degrees (rotation around the Z axis).</param>
        /// <returns>A <see cref="Quaternion"/> representing the combined rotation.</returns>
        public static Quaternion CreateFromYawPitchRollInDegrees(float pitch, float yaw, float roll) {
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
        public static Quaternion CreateFromYawPitchRollInDegrees(float pitch, float yaw, float roll, float deltaTime) {
            return Quaternion.CreateFromYawPitchRoll(float.DegreesToRadians(yaw * deltaTime), float.DegreesToRadians(pitch * deltaTime), float.DegreesToRadians(roll * deltaTime));
        }
        
        /// <summary>
        /// Converts a <see cref="Quaternion"/> to Euler angles in radians.
        /// </summary>
        /// <returns>A <see cref="Vector3"/> containing pitch (X), yaw (Y), and roll (Z).</returns>
        public static Vector3 ToEulerAngles(Quaternion rotation) {
            float xx = rotation.X * rotation.X;
            float yy = rotation.Y * rotation.Y;
            float zz = rotation.Z * rotation.Z;
            
            float m31 = 2.0F * rotation.X * rotation.Z + 2.0F * rotation.Y * rotation.W;
            float m32 = 2.0F * rotation.Y * rotation.Z - 2.0F * rotation.X * rotation.W;
            float m33 = 1.0F - 2.0F * xx - 2.0F * yy;
            
            float cy = MathF.Sqrt(m33 * m33 + m31 * m31);
            float cx = MathF.Atan2(-m32, cy);
            
            if (cy > 16.0F * float.Epsilon) {
                float m12 = 2.0F * rotation.X * rotation.Y + 2.0F * rotation.Z * rotation.W;
                float m22 = 1.0F - 2.0F * xx - 2.0F * zz;
                
                return new Vector3(cx, MathF.Atan2(m31, m33), MathF.Atan2(m12, m22));
            }
            else {
                float m11 = 1.0F - 2.0F * yy - 2.0F * zz;
                float m21 = 2.0F * rotation.X * rotation.Y - 2.0F * rotation.Z * rotation.W;

                return new Vector3(cx, 0.0F, MathF.Atan2(-m21, m11));
            }
        }
    }
    
    /// <summary>
    /// Provides extension methods for working with <see cref="Matrix4x4"/>.
    /// </summary>
    extension(Matrix4x4) {
        
        /// <summary>
        /// Linearly interpolates between two transformation matrices (scale, rotation, translation) by a specified amount.
        /// </summary>
        /// <param name="matrix1">The first transformation matrix to interpolate from.</param>
        /// <param name="matrix2">The second transformation matrix to interpolate to.</param>
        /// <param name="amount">The amount of interpolation, where 0.0F represents the first matrix and 1.0F represents the second matrix.</param>
        /// <returns>A new <see cref="Matrix4x4"/> that represents the interpolated transformation.</returns>
        public static Matrix4x4 LerpSrt(Matrix4x4 matrix1, Matrix4x4 matrix2, float amount) {
            Matrix4x4.Decompose(matrix1, out Vector3 scale1, out Quaternion r1, out Vector3 t1);
            Matrix4x4.Decompose(matrix2, out Vector3 scale2, out Quaternion r2, out Vector3 t2);
            
            // Shortest path rotation.
            if (Quaternion.Dot(r1, r2) < 0.0F) {
                r2 = -r2;
            }
            
            Vector3 finalScale = Vector3.Lerp(scale1, scale2, amount);
            Quaternion finalRotation = Quaternion.Slerp(r1, r2, amount);
            Vector3 finalTranslation = Vector3.Lerp(t1, t2, amount);
            
            return Matrix4x4.CreateScale(finalScale) * Matrix4x4.CreateFromQuaternion(finalRotation) * Matrix4x4.CreateTranslation(finalTranslation);
        }
    }
}