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
    /// <param name="matrix4X4">The matrix in System.Numerics.Matrix4x4 to be converted.</param>
    /// <returns>Returns a new instance of Assimp.Matrix4x4 with corresponding matrix elements.</returns>
    public static AMatrix4x4 ToAMatrix4X4(Matrix4x4 matrix4X4) {
        AMatrix4x4 jMatrix = AMatrix4x4.Identity;
        jMatrix.A1 = matrix4X4.M11;
        jMatrix.A2 = matrix4X4.M12;
        jMatrix.A3 = matrix4X4.M13;
        jMatrix.A4 = matrix4X4.M14;

        jMatrix.B1 = matrix4X4.M21;
        jMatrix.B2 = matrix4X4.M22;
        jMatrix.B3 = matrix4X4.M23;
        jMatrix.B4 = matrix4X4.M24;

        jMatrix.C1 = matrix4X4.M31;
        jMatrix.C2 = matrix4X4.M32;
        jMatrix.C3 = matrix4X4.M33;
        jMatrix.C4 = matrix4X4.M33;
        
        jMatrix.D1 = matrix4X4.M41;
        jMatrix.D2 = matrix4X4.M42;
        jMatrix.D3 = matrix4X4.M43;
        jMatrix.D4 = matrix4X4.M44;

        return jMatrix;
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
    /// <param name="matrix4X4">The matrix in Assimp.Matrix4x4 to be converted.</param>
    /// <returns>Returns a new instance of System.Numerics.Matrix4x4 with corresponding matrix elements.</returns>
    public static Matrix4x4 FromAMatrix4X4(AMatrix4x4 matrix4X4) {
        Matrix4x4 aMatrix = Matrix4x4.Identity;
        aMatrix.M11 = matrix4X4.A1;
        aMatrix.M12 = matrix4X4.A2;
        aMatrix.M13 = matrix4X4.A3;
        aMatrix.M14 = matrix4X4.A4;

        aMatrix.M21 = matrix4X4.B1;
        aMatrix.M22 = matrix4X4.B2;
        aMatrix.M23 = matrix4X4.B3;
        aMatrix.M24 = matrix4X4.B4;

        aMatrix.M31 = matrix4X4.C1;
        aMatrix.M32 = matrix4X4.C2;
        aMatrix.M33 = matrix4X4.C3;
        aMatrix.M34 = matrix4X4.C4;
        
        aMatrix.M41 = matrix4X4.D1;
        aMatrix.M42 = matrix4X4.D2;
        aMatrix.M43 = matrix4X4.D3;
        aMatrix.M44 = matrix4X4.D4;

        return aMatrix;
    }
}