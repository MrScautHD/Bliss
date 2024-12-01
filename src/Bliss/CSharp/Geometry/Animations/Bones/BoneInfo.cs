/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

using System.Numerics;

namespace Bliss.CSharp.Geometry.Animations.Bones;

public class BoneInfo {

    public string Name;
    
    public uint Id;

    public Matrix4x4 Transformation;

    public BoneInfo(string name, uint id, Matrix4x4 transformation) {
        this.Name = name;
        this.Id = id;
        this.Transformation = transformation;
    }
}