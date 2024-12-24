namespace Bliss.CSharp.Geometry.Animations;

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
    /// Initializes a new instance of the <see cref="ModelAnimation"/> class with the specified properties and animation channels.
    /// </summary>
    /// <param name="name">The name of the animation.</param>
    /// <param name="durationInTicks">The total duration of the animation in ticks.</param>
    /// <param name="ticksPerSecond">The number of ticks per second, determining playback speed.</param>
    /// <param name="channels">The list of animation channels associated with this animation.</param>
    public ModelAnimation(string name, float durationInTicks, float ticksPerSecond, IReadOnlyList<NodeAnimChannel> channels) {
        this.Name = name;
        this.DurationInTicks = durationInTicks;
        this.TicksPerSecond = ticksPerSecond;
        this.FrameCount = this.CalculateFrameCount();
        this.AnimationChannels = channels;
    }

    /// <summary>
    /// Calculates the total number of frames for the animation based on its duration and ticks per second.
    /// </summary>
    /// <returns>Returns the computed frame count as an integer.</returns>
    private int CalculateFrameCount() {
        double frameDuration = this.TicksPerSecond > 0 ? this.TicksPerSecond : 25.0;
        return (int) (this.DurationInTicks / frameDuration * 60);
    }
}