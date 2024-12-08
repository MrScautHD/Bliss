using Assimp;
using Bliss.CSharp.Geometry.Animations.Bones;
using Bliss.CSharp.Geometry.Conversions;
using AMatrix4x4 = Assimp.Matrix4x4;
using Matrix4x4 = System.Numerics.Matrix4x4;
using AQuaternion = Assimp.Quaternion;

namespace Bliss.CSharp.Geometry.Animations;

public class MeshAmateurBuilder {

    private Node _rootNode;
    private ModelAnimation[] _animations;
    
    private Dictionary<uint, Bone> _bonesByName;
    private Matrix4x4[] _boneTransformations;

    public MeshAmateurBuilder(Node rootNode, ModelAnimation[] animations) {
        this._rootNode = rootNode;
        this._animations = animations;
    }

    public Dictionary<string, Dictionary<int, BoneInfo[]>> Build(Dictionary<uint, Bone> bonesByName) {
        this._bonesByName = bonesByName;
        this._boneTransformations = new Matrix4x4[bonesByName.Count];
        
        Dictionary<string, Dictionary<int, BoneInfo[]>> boneInfos = new Dictionary<string, Dictionary<int, BoneInfo[]>>();
        
        foreach (ModelAnimation animation in this._animations) {
            boneInfos.Add(animation.Name, this.SetupBoneInfos(animation));
        }

        return boneInfos;
    }

    private Dictionary<int, BoneInfo[]> SetupBoneInfos(ModelAnimation animation) {
        Dictionary<int, BoneInfo[]> bones = new Dictionary<int, BoneInfo[]>();
        
        double frameDuration = animation.TicksPerSecond > 0 ? animation.TicksPerSecond : 25.0;
        int frameCount = (int) (animation.DurationInTicks / frameDuration * 60);

        for (int i = 0; i < frameCount; i++) {
            List<BoneInfo> boneInfos = new List<BoneInfo>();
            
            this.UpdateChannel(this._rootNode, animation, i, AMatrix4x4.Identity);

            for (uint boneId = 0; boneId < this._boneTransformations.Length; boneId++) {
                BoneInfo boneInfo = new BoneInfo(this._bonesByName[boneId].Name, boneId, this._boneTransformations[boneId]);
                boneInfos.Add(boneInfo);
            }
            
            bones.Add(i, boneInfos.ToArray());
        }

        return bones;
    }

    private void UpdateChannel(Node node, ModelAnimation animation, int frame, AMatrix4x4 parentTransform) {
        AMatrix4x4 nodeTransformation = AMatrix4x4.Identity;

        if (GetChannel(node, animation, out NodeAnimationChannel? channel)) {
            AMatrix4x4 scale = this.InterpolateScale(channel!, frame);
            AMatrix4x4 rotation = this.InterpolateRotation(channel!, frame);
            AMatrix4x4 translation = this.InterpolateTranslation(channel!, frame);

            nodeTransformation = scale * rotation * translation;
        }

        foreach (uint boneId in this._bonesByName.Keys) {
            Bone bone = this._bonesByName[boneId];
            
            if (node.Name == bone.Name) {
                AMatrix4x4 rootInverseTransform = this._rootNode.Transform;
                rootInverseTransform.Inverse();
                
                AMatrix4x4 transformation = bone.OffsetMatrix * nodeTransformation * parentTransform * rootInverseTransform;
                this._boneTransformations[boneId] = Matrix4x4.Transpose(ModelConversion.FromAMatrix4X4(transformation)); // TODO: By doing here Matrix4x4.Identity, it will be normal, there is just something wrong with any matrix i think translation
            }
        }

        foreach (Node childNode in node.Children) {
            this.UpdateChannel(childNode, animation, frame, nodeTransformation * parentTransform);
        }
    }

    private AMatrix4x4 InterpolateTranslation(NodeAnimationChannel channel, int frame) {
        Vector3D position;

        if (channel.PositionKeyCount == 1) {
            position = channel.PositionKeys[0].Value;
        }
        else {
            uint frameIndex = 0;
            for (uint i = 0; i < channel.PositionKeyCount - 1; i++) {
                if (frame < channel.PositionKeys[(int)(i + 1)].Time) {
                    frameIndex = i;
                    break;
                }
            }

            VectorKey currentFrame = channel.PositionKeys[(int)frameIndex];
            VectorKey nextFrame = channel.PositionKeys[(int)((frameIndex + 1) % channel.PositionKeyCount)];

            double delta = (frame - (float)currentFrame.Time) / (float)(nextFrame.Time - currentFrame.Time);

            Vector3D start = currentFrame.Value;
            Vector3D end = nextFrame.Value;
            position = (start + (float)delta * (end - start));
        }

        return AMatrix4x4.FromTranslation(position);
    }
    
    private AMatrix4x4 InterpolateRotation(NodeAnimationChannel channel, int frame) {
        AQuaternion rotation;

        if (channel.RotationKeyCount == 1) {
            rotation = channel.RotationKeys[0].Value;
        }
        else
        {
            uint frameIndex = 0;
            for (uint i = 0; i < channel.RotationKeyCount - 1; i++) {
                if (frame < channel.RotationKeys[(int)(i + 1)].Time) {
                    frameIndex = i;
                    break;
                }
            }

            QuaternionKey currentFrame = channel.RotationKeys[(int)frameIndex];
            QuaternionKey nextFrame = channel.RotationKeys[(int)((frameIndex + 1) % channel.RotationKeyCount)];

            double delta = (frame - (float)currentFrame.Time) / (float)(nextFrame.Time - currentFrame.Time);

            AQuaternion start = currentFrame.Value;
            AQuaternion end = nextFrame.Value;
            rotation = AQuaternion.Slerp(start, end, (float)delta);
            rotation.Normalize();
        }

        return rotation.GetMatrix();
    }
    
    private AMatrix4x4 InterpolateScale(NodeAnimationChannel channel, int frame) {
        Vector3D scale;

        if (channel.ScalingKeyCount == 1) {
            scale = channel.ScalingKeys[0].Value;
        }
        else {
            uint frameIndex = 0;
        
            for (uint i = 0; i < channel.ScalingKeyCount - 1; i++) {
                if (frame < channel.ScalingKeys[(int)(i + 1)].Time) {
                    frameIndex = i;
                    break;
                }
            }

            VectorKey currentFrame = channel.ScalingKeys[(int)frameIndex];
            VectorKey nextFrame = channel.ScalingKeys[(int)((frameIndex + 1) % channel.ScalingKeyCount)];

            double delta = (frame - (float)currentFrame.Time) / (float)(nextFrame.Time - currentFrame.Time);

            Vector3D start = currentFrame.Value;
            Vector3D end = nextFrame.Value;

            scale = (start + (float)delta * (end - start));
        }

        return AMatrix4x4.FromScaling(scale);
    }
    
    private bool GetChannel(Node node, ModelAnimation animation, out NodeAnimationChannel? channel) {
        foreach (NodeAnimationChannel nodeChannel in animation.AnimationChannels) {
            if (nodeChannel.NodeName == node.Name) {
                channel = nodeChannel;
                return true;
            }
        }

        channel = null;
        return false;
    }
}