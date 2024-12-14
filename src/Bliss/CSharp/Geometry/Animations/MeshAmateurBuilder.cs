using Assimp;
using Bliss.CSharp.Geometry.Animations.Bones;
using Bliss.CSharp.Geometry.Conversions;
using Bliss.CSharp.Logging;
using AMatrix4x4 = Assimp.Matrix4x4;
using Matrix4x4 = System.Numerics.Matrix4x4;
using AQuaternion = Assimp.Quaternion;

namespace Bliss.CSharp.Geometry.Animations;

public class MeshAmateurBuilder {

    private Node _rootNode;
    private ModelAnimation[] _animations;
    
    private Dictionary<uint, Bone> _bonesByName;
    private Matrix4x4[] _boneTransformations;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="MeshAmateurBuilder"/> class with a root node and animations.
    /// </summary>
    /// <param name="rootNode">The root <see cref="Node"/> representing the hierarchical structure of the mesh.</param>
    /// <param name="animations">An array of <see cref="ModelAnimation"/> objects defining animations for the mesh.</param>
    public MeshAmateurBuilder(Node rootNode, ModelAnimation[] animations) {
        this._rootNode = rootNode;
        this._animations = animations;
    }

    /// <summary>
    /// Constructs a dictionary containing bone information mapped by animation name and frame index.
    /// </summary>
    /// <param name="bonesByName">A dictionary mapping bone identifiers to bone objects.</param>
    /// <returns>A dictionary where keys are animation names, and values are dictionaries that map frame indices to arrays of bone information.</returns>
    public Dictionary<string, Dictionary<int, BoneInfo[]>> Build(Dictionary<uint, Bone> bonesByName) {
        this._bonesByName = bonesByName;
        this._boneTransformations = new Matrix4x4[bonesByName.Count];
        
        Dictionary<string, Dictionary<int, BoneInfo[]>> boneInfos = new Dictionary<string, Dictionary<int, BoneInfo[]>>();
        
        foreach (ModelAnimation animation in this._animations) {
            boneInfos.Add(animation.Name, this.SetupBoneInfos(animation));
        }

        return boneInfos;
    }

    /// <summary>
    /// Generates a dictionary that maps frame indices to arrays of <see cref="BoneInfo"/> for a given animation.
    /// </summary>
    /// <param name="animation">The animation for which bone information is being set up.</param>
    /// <returns>A dictionary with frame indices as keys and arrays of <see cref="BoneInfo"/> as values.</returns>
    private Dictionary<int, BoneInfo[]> SetupBoneInfos(ModelAnimation animation) {
        Dictionary<int, BoneInfo[]> bones = new Dictionary<int, BoneInfo[]>();
        
        for (int i = 0; i < animation.FrameCount; i++) {
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

    /// <summary>
    /// Updates the transformation channel of a specified node based on the animation data and current frame index.
    /// </summary>
    /// <param name="node">The node whose transformation channel is to be updated.</param>
    /// <param name="animation">The animation containing the transformation data.</param>
    /// <param name="frame">The current frame index being processed.</param>
    /// <param name="parentTransform">The transformation matrix of the parent node.</param>
    private void UpdateChannel(Node node, ModelAnimation animation, int frame, AMatrix4x4 parentTransform) {
        AMatrix4x4 nodeTransformation = AMatrix4x4.Identity;

        if (this.GetChannel(node, animation, out NodeAnimationChannel? channel)) {
            AMatrix4x4 scale = this.InterpolateScale(channel!, animation, frame);
            AMatrix4x4 rotation = this.InterpolateRotation(channel!, animation, frame);
            AMatrix4x4 translation = this.InterpolateTranslation(channel!, animation, frame);
            
            nodeTransformation = scale * rotation * translation;
        }

        foreach (uint boneId in this._bonesByName.Keys) {
            Bone bone = this._bonesByName[boneId];
            
            if (node.Name == bone.Name) {
                AMatrix4x4 rootInverseTransform = this._rootNode.Transform;
                rootInverseTransform.Inverse();
                
                AMatrix4x4 transformation = bone.OffsetMatrix * nodeTransformation * parentTransform * rootInverseTransform;
                this._boneTransformations[boneId] = Matrix4x4.Transpose(ModelConversion.FromAMatrix4X4(transformation));
            }
        }

        foreach (Node childNode in node.Children) {
            this.UpdateChannel(childNode, animation, frame, nodeTransformation * parentTransform);
        }
    }

    /// <summary>
    /// Interpolates the translation transformation at a specific frame using the provided animation channel and animation data.
    /// </summary>
    /// <param name="channel">The <see cref="NodeAnimationChannel"/> containing position keys for the node.</param>
    /// <param name="animation">The <see cref="ModelAnimation"/> containing the animation data, including timing information.</param>
    /// <param name="frame">The current frame for which the translation is being interpolated.</param>
    /// <returns>An <see cref="Assimp.Matrix4x4"/> representing the interpolated translation transformation for the given frame.</returns>
    private AMatrix4x4 InterpolateTranslation(NodeAnimationChannel channel, ModelAnimation animation, int frame) {
        double frameTime = frame / 60.0F * animation.TicksPerSecond;
        Vector3D position;
        
        if (channel.PositionKeyCount == 1) {
            position = channel.PositionKeys[0].Value;
        }
        else {
            uint frameIndex = 0;
            for (uint i = 0; i < channel.PositionKeyCount - 1; i++) {
                if (frameTime < channel.PositionKeys[(int) (i + 1)].Time) {
                    frameIndex = i;
                    break;
                }
            }

            VectorKey currentFrame = channel.PositionKeys[(int) frameIndex];
            VectorKey nextFrame = channel.PositionKeys[(int) ((frameIndex + 1) % channel.PositionKeyCount)];

            double delta = (frameTime - currentFrame.Time) / (nextFrame.Time - currentFrame.Time);

            Vector3D start = currentFrame.Value;
            Vector3D end = nextFrame.Value;
            position = start + (float) delta * (end - start);
        }
        
        return AMatrix4x4.FromTranslation(position);
    }

    /// <summary>
    /// Interpolates the rotation transformation for a given node animation channel at a specified frame.
    /// </summary>
    /// <param name="channel">The <see cref="NodeAnimationChannel"/> containing the rotation keyframes for the node.</param>
    /// <param name="animation">The <see cref="ModelAnimation"/> object defining the overall animation data.</param>
    /// <param name="frame">The current frame for which the rotation needs to be interpolated.</param>
    /// <returns>An <see cref="AMatrix4x4"/> representing the interpolated rotation transformation matrix for the specified frame.</returns>
    private AMatrix4x4 InterpolateRotation(NodeAnimationChannel channel, ModelAnimation animation, int frame) {
        double frameTime = frame / 60.0F * animation.TicksPerSecond;
        AQuaternion rotation;

        if (channel.RotationKeyCount == 1) {
            rotation = channel.RotationKeys[0].Value;
        }
        else {
            uint frameIndex = 0;
            for (uint i = 0; i < channel.RotationKeyCount - 1; i++) {
                if (frameTime < channel.RotationKeys[(int) (i + 1)].Time) {
                    frameIndex = i;
                    break;
                }
            }

            QuaternionKey currentFrame = channel.RotationKeys[(int) frameIndex];
            QuaternionKey nextFrame = channel.RotationKeys[(int) ((frameIndex + 1) % channel.RotationKeyCount)];

            double delta = (frameTime - currentFrame.Time) / (nextFrame.Time - currentFrame.Time);

            AQuaternion start = currentFrame.Value;
            AQuaternion end = nextFrame.Value;
            rotation = AQuaternion.Slerp(start, end, (float) delta);
            rotation.Normalize();
        }
        
        return rotation.GetMatrix();
    }

    /// <summary>
    /// Computes the interpolated scale transformation for a given animation channel at a specific frame.
    /// </summary>
    /// <param name="channel">The <see cref="NodeAnimationChannel"/> containing scaling keyframes.</param>
    /// <param name="animation">The <see cref="ModelAnimation"/> associated with the animation data.</param>
    /// <param name="frame">The current frame of the animation for which the scale transformation is calculated.</param>
    /// <returns>An <see cref="AMatrix4x4"/> representing the interpolated scale transformation at the specified frame.</returns>
    private AMatrix4x4 InterpolateScale(NodeAnimationChannel channel, ModelAnimation animation, int frame) {
        double frameTime = frame / 60.0F * animation.TicksPerSecond;
        Vector3D scale;

        if (channel.ScalingKeyCount == 1) {
            scale = channel.ScalingKeys[0].Value;
        }
        else {
            uint frameIndex = 0;
            for (uint i = 0; i < channel.ScalingKeyCount - 1; i++) {
                if (frameTime < channel.ScalingKeys[(int) (i + 1)].Time) {
                    frameIndex = i;
                    break;
                }
            }

            VectorKey currentFrame = channel.ScalingKeys[(int)frameIndex];
            VectorKey nextFrame = channel.ScalingKeys[(int)((frameIndex + 1) % channel.ScalingKeyCount)];

            double delta = (frameTime - currentFrame.Time) / (nextFrame.Time - currentFrame.Time);

            Vector3D start = currentFrame.Value;
            Vector3D end = nextFrame.Value;

            scale = start + (float) delta * (end - start);
        }
        
        return AMatrix4x4.FromScaling(scale);
    }

    /// <summary>
    /// Retrieves the animation channel for a given node from the specified animation.
    /// </summary>
    /// <param name="node">The node for which the animation channel should be retrieved.</param>
    /// <param name="animation">The animation containing the channels to check.</param>
    /// <param name="channel">The output parameter that will hold the retrieved node animation channel if found; otherwise, null.</param>
    /// <returns>True if the channel for the specified node is found in the animation; otherwise, false.</returns>
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