using System.Numerics;
using Assimp;
using Bliss.CSharp.Geometry.Animation.Keyframes;
using AAnimation = Assimp.Animation;

namespace Bliss.CSharp.Geometry.Animation.Builders;

public class ModelAnimationBuilder {
    
    /// <summary>
    /// The root node of the model’s scene hierarchy.
    /// </summary>
    private Node _rootNode;
    
    /// <summary>
    /// The skeleton containing the bone definitions used for animation.
    /// </summary>
    private Skeleton _skeleton;
    
    /// <summary>
    /// The Assimp animation data being converted into a <see cref="ModelAnimation"/>.
    /// </summary>
    private AAnimation _animation;
    
    /// <summary>
    /// The collection of node animation channels, storing keyframe data per node.
    /// </summary>
    private IReadOnlyList<NodeAnimChannel> _nodeAnimChannels;
    
    /// <summary>
    /// Temporary buffer used to store bone transformations for each frame during animation baking.
    /// </summary>
    private Matrix4x4[] _boneTransformations;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelAnimationBuilder"/> class.
    /// </summary>
    /// <param name="rootNode">The root node of the model hierarchy.</param>
    /// <param name="skeleton">The skeleton associated with the model.</param>
    /// <param name="animation">The raw Assimp animation data to convert.</param>
    public ModelAnimationBuilder(Node rootNode, Skeleton skeleton, AAnimation animation) {
        this._rootNode = rootNode;
        this._skeleton = skeleton;
        this._animation = animation;
        this._nodeAnimChannels = this.CreateNodeAnimChannels();
        this._boneTransformations = new Matrix4x4[this._skeleton.BoneNameToIndex.Count];
    }
    
    /// <summary>
    /// Builds a <see cref="ModelAnimation"/> by processing the animation channels and baking bone transformations for all frames.
    /// </summary>
    /// <returns> A fully constructed <see cref="ModelAnimation"/> containing per-frame bone transformations.</returns>
    public ModelAnimation Build() {
        int frameCount = this.CalculateFrameCount();
        IReadOnlyDictionary<int, Matrix4x4[]> boneFrameTransformations = this.CreateBoneFrameTransformations(frameCount);
        return new ModelAnimation(this._animation.Name, (float) this._animation.DurationInTicks, (float) this._animation.TicksPerSecond, frameCount, this._nodeAnimChannels, boneFrameTransformations);
    }
    
    /// <summary>
    /// Converts raw Assimp animation channels into internal <see cref="NodeAnimChannel"/> objects.
    /// </summary>
    private IReadOnlyList<NodeAnimChannel> CreateNodeAnimChannels() {
        List<NodeAnimChannel> animChannels = new List<NodeAnimChannel>();
            
        foreach (NodeAnimationChannel aChannel in this._animation.NodeAnimationChannels) {
            List<Vector3Key> positions = new List<Vector3Key>();
            List<QuatKey> rotations = new List<QuatKey>();
            List<Vector3Key> scales = new List<Vector3Key>();
                
            // Setup positions.
            foreach (VectorKey aPosition in aChannel.PositionKeys) {
                Vector3Key position = new Vector3Key(aPosition.Time, aPosition.Value);
                positions.Add(position);
            }
                
            // Setup rotations.
            foreach (QuaternionKey aRotation in aChannel.RotationKeys) {
                QuatKey rotation = new QuatKey(aRotation.Time, aRotation.Value);
                rotations.Add(rotation);
            }
                
            // Setup scales.
            foreach (VectorKey aScale in aChannel.ScalingKeys) {
                Vector3Key scale = new Vector3Key(aScale.Time, aScale.Value);
                scales.Add(scale);
            }
                
            NodeAnimChannel channel = new NodeAnimChannel(aChannel.NodeName, positions, rotations, scales);
            animChannels.Add(channel);
        }

        return animChannels;
    }
    
    /// <summary>
    /// Calculates the number of frames in the animation based on ticks and playback rate.
    /// </summary>
    private int CalculateFrameCount() {
        double frameDuration = this._animation.TicksPerSecond > 0.0 ? this._animation.TicksPerSecond : 25.0;
        return (int) (this._animation.DurationInTicks / frameDuration * 60.0);
    }
    
    /// <summary>
    /// Generates a mapping of frame indices to bone transformation arrays.
    /// </summary>
    private IReadOnlyDictionary<int, Matrix4x4[]> CreateBoneFrameTransformations(int frameCount) {
        Dictionary<int, Matrix4x4[]> boneFrameTransformations = new Dictionary<int, Matrix4x4[]>();

        for (int frame = 0; frame < frameCount; frame++) {
            
            // Reset all bone transformations for this frame.
            Array.Clear(this._boneTransformations, 0, this._boneTransformations.Length);
            
            // Update transformations recursively starting from the root node.
            this.UpdateChannel(this._rootNode, frame, Matrix4x4.Identity);
            
            // Create a copy of transformations for this frame.
            var currentFrameTransformations = new Matrix4x4[this._boneTransformations.Length];
            Array.Copy(this._boneTransformations, currentFrameTransformations, this._boneTransformations.Length);
            
            // Store transformations in the dictionary.
            boneFrameTransformations.Add(frame, currentFrameTransformations);
        }
        
        return boneFrameTransformations;
    }
    
    /// <summary>
    /// Attempts to get the animation channel associated with a given node.
    /// </summary>
    private bool GetChannel(Node node, out NodeAnimChannel? channel) {
        foreach (NodeAnimChannel nodeChannel in this._nodeAnimChannels) {
            if (nodeChannel.Name == node.Name) {
                channel = nodeChannel;
                return true;
            }
        }
        
        channel = null;
        return false;
    }
    
    /// <summary>
    /// Recursively updates bone transformations for a given node and its children.
    /// </summary>
    private void UpdateChannel(Node node, int frame, Matrix4x4 parentTransform) {
        Matrix4x4 nodeTransform = Matrix4x4.Transpose(node.Transform);
        
        if (this.GetChannel(node, out NodeAnimChannel? channel)) {
            Matrix4x4 scale = this.InterpolateScale(channel!, frame);
            Matrix4x4 rotation = this.InterpolateRotation(channel!, frame);
            Matrix4x4 translation = this.InterpolateTranslation(channel!, frame);
            
            nodeTransform = scale * rotation * translation;
        }
        
        foreach (var boneEntry in this._skeleton.BoneNameToIndex) {
            if (node.Name == boneEntry.Key) {
                uint boneIndex = boneEntry.Value;
                
                Matrix4x4.Invert(Matrix4x4.Transpose(this._rootNode.Transform), out Matrix4x4 rootInverseTransform);
                Matrix4x4 transformation = this._skeleton.Bones.ElementAt((int) boneIndex).Transformation * nodeTransform * parentTransform * rootInverseTransform;
                this._boneTransformations[boneIndex] = transformation;
            }
        }
        
        foreach (Node childNode in node.Children) {
            this.UpdateChannel(childNode, frame, nodeTransform * parentTransform);
        }
    }

    /// <summary>
    /// Interpolates translation for the given channel and frame.
    /// </summary>
    private Matrix4x4 InterpolateTranslation(NodeAnimChannel channel, int frame) {
        double frameTime = frame / 60.0F * this._animation.TicksPerSecond;
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
    /// Interpolates rotation for the given channel and frame using spherical linear interpolation (slerp).
    /// </summary>
    private Matrix4x4 InterpolateRotation(NodeAnimChannel channel, int frame) {
        double frameTime = frame / 60.0F * this._animation.TicksPerSecond;
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
    /// Interpolates scaling for the given channel and frame.
    /// </summary>
    private Matrix4x4 InterpolateScale(NodeAnimChannel channel, int frame) {
        double frameTime = frame / 60.0F * this._animation.TicksPerSecond;
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
}