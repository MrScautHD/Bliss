using Bliss.CSharp.Geometry.Animations.Bones;
using Bliss.CSharp.Transformations;

namespace Bliss.CSharp.Geometry.Animations;

public class ModelAnimation {
    
    public string Name { get; private set; }

    public int BoneCount { get; private set; }
    public int FrameCount { get; private set; }
    
    public BoneInfo[] BoneInfos { get; private set; }
    public Transform[][] FramePoses { get; private set; }

    public ModelAnimation(string name, BoneInfo[] boneInfos, Transform[][] framePoses) {
        this.Name = name;
        this.BoneCount = boneInfos.Length;
        this.FrameCount = framePoses.Length;
        this.BoneInfos = boneInfos;
        this.FramePoses = framePoses;
    }
}