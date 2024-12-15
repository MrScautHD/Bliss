/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

using System.Numerics;
using Assimp;
using Bliss.CSharp.Colors;
using AQuaternion = Assimp.Quaternion;
using AMatrix4x4 = Assimp.Matrix4x4;
using Matrix4x4 = System.Numerics.Matrix4x4;
using Quaternion = System.Numerics.Quaternion;

namespace Bliss.CSharp.Geometry.Conversions;

public static class ModelConversion {
    
    /// <summary>
    /// Converts a Bliss.CSharp.Colors.Color to an Assimp.Color4D.
    /// </summary>
    /// <param name="color">The color in Bliss.CSharp.Colors.Color to be converted.</param>
    /// <returns>Returns a new instance of Assimp.Color4D with the same R, G, B, and A values.</returns>
    public static Color4D ToVector4D(Color color) {
        return new Color4D(color.R, color.G, color.B, color.A);
    }
    
    /// <summary>
    /// Converts a System.Numerics.Vector3 to an Assimp.Vector3D.
    /// </summary>
    /// <param name="vector3">The vector in System.Numerics.Vector3 to be converted.</param>
    /// <returns>Returns a new instance of Assimp.Vector3D with the same X, Y, and Z values.</returns>
    public static Vector3D ToVector3D(Vector3 vector3) {
        return new Vector3D(vector3.X, vector3.Y, vector3.Z);
    }

    /// <summary>
    /// Converts a System.Numerics.Quaternion to an Assimp.Quaternion.
    /// </summary>
    /// <param name="quaternion">The quaternion in System.Numerics.Quaternion to be converted.</param>
    /// <returns>Returns a new instance of Assimp.Quaternion with the same X, Y, Z, and W values.</returns>
    public static AQuaternion ToAQuaternion(Quaternion quaternion) {
        return new AQuaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
    }

    /// <summary>
    /// Converts a System.Numerics.Matrix4x4 to an Assimp.Matrix4x4.
    /// </summary>
    /// <param name="matrix">The matrix in System.Numerics.Matrix4x4 to be converted.</param>
    /// <returns>Returns a new instance of Assimp.Matrix4x4 with corresponding matrix elements.</returns>
    public static AMatrix4x4 ToAMatrix4X4(Matrix4x4 matrix) {
        AMatrix4x4 aMatrix = AMatrix4x4.Identity;
        aMatrix.A1 = matrix.M11;
        aMatrix.A2 = matrix.M12;
        aMatrix.A3 = matrix.M13;
        aMatrix.A4 = matrix.M14;

        aMatrix.B1 = matrix.M21;
        aMatrix.B2 = matrix.M22;
        aMatrix.B3 = matrix.M23;
        aMatrix.B4 = matrix.M24;

        aMatrix.C1 = matrix.M31;
        aMatrix.C2 = matrix.M32;
        aMatrix.C3 = matrix.M33;
        aMatrix.C4 = matrix.M33;
        
        aMatrix.D1 = matrix.M41;
        aMatrix.D2 = matrix.M42;
        aMatrix.D3 = matrix.M43;
        aMatrix.D4 = matrix.M44;

        return aMatrix;
    }

    /// <summary>
    /// Converts a System.Numerics.Matrix4x4 to an Assimp.Matrix4x4 with the matrix transposed.
    /// </summary>
    /// <param name="matrix">The matrix in System.Numerics.Matrix4x4 to be converted and transposed.</param>
    /// <returns>Returns a new instance of Assimp.Matrix4x4 with corresponding transposed matrix elements.</returns>
    public static AMatrix4x4 ToAMatrix4X4Transposed(Matrix4x4 matrix) {
        return ToAMatrix4X4(Matrix4x4.Transpose(matrix));
    }

    /// <summary>
    /// Converts an Assimp.Color4D to a Bliss.CSharp.Colors.Color.
    /// </summary>
    /// <param name="color4D">The color in Assimp.Color4D to be converted.</param>
    /// <returns>Returns a new instance of Bliss.CSharp.Colors.Color with the same R, G, B, and A values.</returns>
    public static Color FromColor4D(Color4D color4D) {
        return new Color((byte) color4D.R, (byte) color4D.G, (byte) color4D.B, (byte) color4D.A);
    }

    /// <summary>
    /// Converts an Assimp.Vector3D to a System.Numerics.Vector3.
    /// </summary>
    /// <param name="aVector">The vector in Assimp.Vector3D to be converted.</param>
    /// <returns>Returns a new instance of System.Numerics.Vector3 with the same X, Y, and Z values.</returns>
    public static Vector3 FromVector3D(Vector3D aVector) {
        return new Vector3(aVector.X, aVector.Y, aVector.Z);
    }

    /// <summary>
    /// Converts an Assimp.Quaternion to a System.Numerics.Quaternion.
    /// </summary>
    /// <param name="aQuaternion">The quaternion in Assimp.Quaternion to be converted.</param>
    /// <returns>Returns a new instance of System.Numerics.Quaternion with the same X, Y, Z, and W values.</returns>
    public static Quaternion FromAQuaternion(AQuaternion aQuaternion) {
        return new Quaternion(aQuaternion.X, aQuaternion.Y, aQuaternion.Z, aQuaternion.W);
    }

    /// <summary>
    /// Converts an Assimp.Matrix4x4 to a System.Numerics.Matrix4x4.
    /// </summary>
    /// <param name="aMatrix">The matrix in Assimp.Matrix4x4 to be converted.</param>
    /// <returns>Returns a new instance of System.Numerics.Matrix4x4 with corresponding matrix elements.</returns>
    public static Matrix4x4 FromAMatrix4X4(AMatrix4x4 aMatrix) {
        Matrix4x4 matrix = Matrix4x4.Identity;
        matrix.M11 = aMatrix.A1;
        matrix.M12 = aMatrix.A2;
        matrix.M13 = aMatrix.A3;
        matrix.M14 = aMatrix.A4;

        matrix.M21 = aMatrix.B1;
        matrix.M22 = aMatrix.B2;
        matrix.M23 = aMatrix.B3;
        matrix.M24 = aMatrix.B4;

        matrix.M31 = aMatrix.C1;
        matrix.M32 = aMatrix.C2;
        matrix.M33 = aMatrix.C3;
        matrix.M34 = aMatrix.C4;
        
        matrix.M41 = aMatrix.D1;
        matrix.M42 = aMatrix.D2;
        matrix.M43 = aMatrix.D3;
        matrix.M44 = aMatrix.D4;

        return matrix;
    }

    /// <summary>
    /// Converts an Assimp.Matrix4x4 to a transposed System.Numerics.Matrix4x4.
    /// </summary>
    /// <param name="aMatrix">The matrix in Assimp.Matrix4x4 to be converted and transposed.</param>
    /// <returns>Returns a new instance of System.Numerics.Matrix4x4 with corresponding matrix elements, transposed.</returns>
    public static Matrix4x4 FromAMatrix4X4Transposed(AMatrix4x4 aMatrix) {
        return Matrix4x4.Transpose(FromAMatrix4X4(aMatrix));
    }
}