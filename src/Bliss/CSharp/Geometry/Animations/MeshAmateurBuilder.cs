using System.Numerics;
using Assimp;
using Bliss.CSharp.Geometry.Animations.Keyframes;

namespace Bliss.CSharp.Geometry.Animations;

public class MeshAmateurBuilder {

    /// <summary>
    /// The root node of the model. This node typically contains the root transformations for the mesh and references to other child nodes.
    /// </summary>
    private Node _rootNode;
    
    /// <summary>
    /// An array of <see cref="ModelAnimation"/> objects that represent animations applied to the model.
    /// </summary>
    private ModelAnimation[] _animations;
    
    /// <summary>
    /// A dictionary mapping bone IDs to the corresponding <see cref="Bone"/> objects. This allows efficient look-up of bones by their ID or name.
    /// </summary>
    private Dictionary<uint, Bone> _bonesByName;
    
    /// <summary>
    /// An array of transformation matrices for the bones, used for skeletal animation. Each bone has a corresponding transformation matrix.
    /// </summary>
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
            
            this.UpdateChannel(this._rootNode, animation, i, Matrix4x4.Identity);

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
    private void UpdateChannel(Node node, ModelAnimation animation, int frame, Matrix4x4 parentTransform) {
        Matrix4x4 nodeTransformation = Matrix4x4.Transpose(node.Transform);
        
        if (this.GetChannel(node, animation, out NodeAnimChannel? channel)) {
            Matrix4x4 scale = this.InterpolateScale(channel!, animation, frame);
            Matrix4x4 rotation = this.InterpolateRotation(channel!, animation, frame);
            Matrix4x4 translation = this.InterpolateTranslation(channel!, animation, frame);
            
            nodeTransformation = scale * rotation * translation;
        }

        foreach (uint boneId in this._bonesByName.Keys) {
            Bone bone = this._bonesByName[boneId];
            
            if (node.Name == bone.Name) {
                Matrix4x4.Invert(Matrix4x4.Transpose(this._rootNode.Transform), out Matrix4x4 rootInverseTransform);
                
                Matrix4x4 transformation = Matrix4x4.Transpose(bone.OffsetMatrix) * nodeTransformation * parentTransform * rootInverseTransform;
                this._boneTransformations[boneId] = transformation;
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
    /// <returns>An <see cref="Matrix4x4"/> representing the interpolated translation transformation for the given frame.</returns>
    private Matrix4x4 InterpolateTranslation(NodeAnimChannel channel, ModelAnimation animation, int frame) {
        double frameTime = frame / 60.0F * animation.TicksPerSecond;
        Vector3 position;
        
        if (channel.Positions.Count == 1) {
            position = channel.Positions[0].Value;
        }
        else {
            uint frameIndex = 0;
            for (uint i = 0; i < channel.Positions.Count - 1; i++) {
                if (frameTime < channel.Positions[(int) (i + 1)].Time) {
                    frameIndex = i;
                    break;
                }
            }

            Vector3Key currentFrame = channel.Positions[(int) frameIndex];
            Vector3Key nextFrame = channel.Positions[(int) ((frameIndex + 1) % channel.Positions.Count)];

            double delta = (frameTime - currentFrame.Time) / (nextFrame.Time - currentFrame.Time);

            Vector3 start = currentFrame.Value;
            Vector3 end = nextFrame.Value;
            position = start + (float) Math.Clamp(delta, 0.0F, 1.0F) * (end - start);
        }
        
        return Matrix4x4.CreateTranslation(position);
    }

    /// <summary>
    /// Interpolates the rotation transformation for a given node animation channel at a specified frame.
    /// </summary>
    /// <param name="channel">The <see cref="NodeAnimationChannel"/> containing the rotation keyframes for the node.</param>
    /// <param name="animation">The <see cref="ModelAnimation"/> object defining the overall animation data.</param>
    /// <param name="frame">The current frame for which the rotation needs to be interpolated.</param>
    /// <returns>An <see cref="Matrix4x4"/> representing the interpolated rotation transformation matrix for the specified frame.</returns>
    private Matrix4x4 InterpolateRotation(NodeAnimChannel channel, ModelAnimation animation, int frame) {
        double frameTime = frame / 60.0F * animation.TicksPerSecond;
        Quaternion rotation;

        if (channel.Rotations.Count == 1) {
            rotation = channel.Rotations[0].Value;
        }
        else {
            uint frameIndex = 0;
            for (uint i = 0; i < channel.Rotations.Count - 1; i++) {
                if (frameTime < channel.Rotations[(int) (i + 1)].Time) {
                    frameIndex = i;
                    break;
                }
            }

            QuatKey currentFrame = channel.Rotations[(int) frameIndex];
            QuatKey nextFrame = channel.Rotations[(int) ((frameIndex + 1) % channel.Rotations.Count)];

            double delta = (frameTime - currentFrame.Time) / (nextFrame.Time - currentFrame.Time);

            Quaternion start = currentFrame.Value;
            Quaternion end = nextFrame.Value;
            rotation = Quaternion.Normalize(Quaternion.Slerp(start, end, (float) Math.Clamp(delta, 0.0F, 1.0F)));
        }
        
        return Matrix4x4.CreateFromQuaternion(rotation);
    }

    /// <summary>
    /// Computes the interpolated scale transformation for a given animation channel at a specific frame.
    /// </summary>
    /// <param name="channel">The <see cref="NodeAnimationChannel"/> containing scaling keyframes.</param>
    /// <param name="animation">The <see cref="ModelAnimation"/> associated with the animation data.</param>
    /// <param name="frame">The current frame of the animation for which the scale transformation is calculated.</param>
    /// <returns>An <see cref="Matrix4x4"/> representing the interpolated scale transformation at the specified frame.</returns>
    private Matrix4x4 InterpolateScale(NodeAnimChannel channel, ModelAnimation animation, int frame) {
        double frameTime = frame / 60.0F * animation.TicksPerSecond;
        Vector3 scale;

        if (channel.Scales.Count == 1) {
            scale = channel.Scales[0].Value;
        }
        else {
            uint frameIndex = 0;
            for (uint i = 0; i < channel.Scales.Count - 1; i++) {
                if (frameTime < channel.Scales[(int) (i + 1)].Time) {
                    frameIndex = i;
                    break;
                }
            }

            Vector3Key currentFrame = channel.Scales[(int) frameIndex];
            Vector3Key nextFrame = channel.Scales[(int) ((frameIndex + 1) % channel.Scales.Count)];

            double delta = (frameTime - currentFrame.Time) / (nextFrame.Time - currentFrame.Time);

            Vector3 start = currentFrame.Value;
            Vector3 end = nextFrame.Value;

            scale = start + (float) Math.Clamp(delta, 0.0F, 1.0F) * (end - start);
        }

        return Matrix4x4.CreateScale(scale);
    }

    /// <summary>
    /// Retrieves the animation channel for a given node from the specified animation.
    /// </summary>
    /// <param name="node">The node for which the animation channel should be retrieved.</param>
    /// <param name="animation">The animation containing the channels to check.</param>
    /// <param name="channel">The output parameter that will hold the retrieved node animation channel if found; otherwise, null.</param>
    /// <returns>True if the channel for the specified node is found in the animation; otherwise, false.</returns>
    private bool GetChannel(Node node, ModelAnimation animation, out NodeAnimChannel? channel) {
        foreach (NodeAnimChannel nodeChannel in animation.AnimationChannels) {
            if (nodeChannel.Name == node.Name) {
                channel = nodeChannel;
                return true;
            }
        }

        channel = null;
        return false;
    }
}