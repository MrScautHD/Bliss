using System.Numerics;
using Assimp;
using Bliss.CSharp.Geometry.Animations.Bones;
using Bliss.CSharp.Geometry.Conversions;
using Bliss.CSharp.Logging;
using Matrix4x4 = System.Numerics.Matrix4x4;
using Quaternion = System.Numerics.Quaternion;

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
            
            this.UpdateChannel(this._rootNode, animation, i, Matrix4x4.Identity);

            for (uint boneId = 0; boneId < this._boneTransformations.Length; boneId++) {
                BoneInfo boneInfo = new BoneInfo(this._bonesByName[boneId].Name, boneId, this._boneTransformations[boneId]);
                boneInfos.Add(boneInfo);
            }
            
            bones.Add(i, boneInfos.ToArray());
        }

        return bones;
    }

    private void UpdateChannel(Node node, ModelAnimation animation, int frame, Matrix4x4 parentTransform) {
        Matrix4x4 nodeTransformation = Matrix4x4.Identity;

        if (GetChannel(node, animation, out NodeAnimationChannel? channel)) {
            Matrix4x4 scale = this.InterpolateScale(channel!, frame);
            Matrix4x4 rotation = this.InterpolateRotation(channel!, frame);
            Matrix4x4 translation = this.InterpolateTranslation(channel!, frame);

            nodeTransformation = scale * rotation * translation;
        }

        foreach (uint boneId in this._bonesByName.Keys) {
            Bone bone = this._bonesByName[boneId];
            
            if (node.Name == bone.Name) {
                Assimp.Matrix4x4 rootInverseTransform = this._rootNode.Transform;
                rootInverseTransform.Inverse();
                
                Matrix4x4 transformation = Matrix4x4.Transpose(ModelConversion.FromAMatrix4X4(bone.OffsetMatrix)) * nodeTransformation * parentTransform * Matrix4x4.Transpose(ModelConversion.FromAMatrix4X4(rootInverseTransform));
                this._boneTransformations[boneId] = transformation; // TODO: By doing here Matrix4x4.Identity, it will be normal, there is just something wrong with any matrix i think translation
            }
        }

        foreach (Node childNode in node.Children) {
            this.UpdateChannel(childNode, animation, frame, nodeTransformation * parentTransform);
        }
    }

    private Matrix4x4 InterpolateTranslation(NodeAnimationChannel channel, int frame) {
        if (channel.PositionKeys.Count == 0) {
            return Matrix4x4.Identity;
        }

        Vector3 startPosition = ModelConversion.FromVector3D(channel.PositionKeys[0].Value);
        Vector3 endPosition = startPosition;

        for (int i = 1; i < channel.PositionKeys.Count; i++) {
            if (channel.PositionKeys[i].Time > frame) {
                float t = (float)(frame - channel.PositionKeys[i - 1].Time) / (float)(channel.PositionKeys[i].Time - channel.PositionKeys[i - 1].Time);
                startPosition = ModelConversion.FromVector3D(channel.PositionKeys[i - 1].Value);
                endPosition = ModelConversion.FromVector3D(channel.PositionKeys[i].Value);
                return Matrix4x4.CreateTranslation(Vector3.Lerp(startPosition, endPosition, t));
            }
        }

        return Matrix4x4.CreateTranslation(endPosition);
    }
    
    private Matrix4x4 InterpolateRotation(NodeAnimationChannel channel, int frame) {
        if (channel.RotationKeys.Count == 0) {
            return Matrix4x4.Identity;
        }
        
        Quaternion startRotation = ModelConversion.FromAQuaternion(channel.RotationKeys[0].Value);
        Quaternion endRotation = startRotation;
        
        for (int i = 1; i < channel.RotationKeys.Count; i++) {
            if (channel.RotationKeys[i].Time > frame) {
                float t = (float)(frame - channel.RotationKeys[i - 1].Time) / (float)(channel.RotationKeys[i].Time - channel.RotationKeys[i - 1].Time);
                startRotation = ModelConversion.FromAQuaternion(channel.RotationKeys[i - 1].Value);
                endRotation = ModelConversion.FromAQuaternion(channel.RotationKeys[i].Value);
                return Matrix4x4.CreateFromQuaternion(Quaternion.Slerp(startRotation, endRotation, t));
            }
        }
        
        return Matrix4x4.CreateFromQuaternion(endRotation);
    }
    
    private Matrix4x4 InterpolateScale(NodeAnimationChannel channel, int frame) {
        if (channel.ScalingKeys.Count == 0) {
            return Matrix4x4.Identity;
        }

        Vector3 startScale = ModelConversion.FromVector3D(channel.ScalingKeys[0].Value);
        Vector3 endScale = startScale;
        
        for (int i = 1; i < channel.ScalingKeys.Count; i++) {
            if (channel.ScalingKeys[i].Time > frame) {
                float t = (float)(frame - channel.ScalingKeys[i - 1].Time) / (float)(channel.ScalingKeys[i].Time - channel.ScalingKeys[i - 1].Time);
                startScale = ModelConversion.FromVector3D(channel.ScalingKeys[i - 1].Value);
                endScale = ModelConversion.FromVector3D(channel.ScalingKeys[i].Value);
                return Matrix4x4.CreateScale(Vector3.Lerp(startScale, endScale, t));
            }
        }
        
        return Matrix4x4.CreateScale(endScale);
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