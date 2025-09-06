using System.Numerics;

namespace Bliss.CSharp.Geometry.Animation;

public class ModelAnimation {
    
    /// <summary>
    /// The name of the animation.
    /// </summary>
    public string Name { get; private set; }
    
    /// <summary>
    /// The total duration of the animation in ticks.
    /// </summary>
    public float DurationInTicks { get; private set; }
    
    /// <summary>
    /// The number of ticks per second for the animation, determining its playback speed.
    /// </summary>
    public float TicksPerSecond { get; private set; }
    
    /// <summary>
    /// The total number of frames in the animation, calculated based on duration and ticks per second.
    /// </summary>
    public int FrameCount { get; private set; }
    
    /// <summary>
    /// A list of animation channels that define the per-node transformations during the animation.
    /// </summary>
    public IReadOnlyList<NodeAnimChannel> AnimationChannels { get; private set; }

    /// <summary>
    /// A dictionary containing the precomputed transformations for each bone at each animation frame.
    /// </summary>
    public IReadOnlyDictionary<int, Matrix4x4[]> BoneFrameTransformations { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelAnimation"/> class with the specified properties and animation channels.
    /// </summary>
    /// <param name="name">The name of the animation.</param>
    /// <param name="durationInTicks">The total duration of the animation in ticks.</param>
    /// <param name="ticksPerSecond">The number of ticks per second, determining playback speed.</param>
    /// <param name="frameCount">The total number of frames in the animation.</param>
    /// <param name="channels">The list of animation channels associated with this animation.</param>
    /// <param name="boneFrameTransformations">The dictionary containing the precomputed transformations for each bone at each animation frame.</param>
    public ModelAnimation(string name, float durationInTicks, float ticksPerSecond, int frameCount, IReadOnlyList<NodeAnimChannel> channels, IReadOnlyDictionary<int, Matrix4x4[]> boneFrameTransformations) {
        this.Name = name;
        this.DurationInTicks = durationInTicks;
        this.TicksPerSecond = ticksPerSecond;
        this.FrameCount = frameCount;
        this.AnimationChannels = channels;
        this.BoneFrameTransformations = boneFrameTransformations;
    }
}