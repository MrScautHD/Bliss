using Bliss.CSharp.Geometry.Animations.Keyframes;

namespace Bliss.CSharp.Geometry.Animations;

public class NodeAnimChannel {
    
    /// <summary>
    /// The name of the node being animated.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// A list of <see cref="Vector3Key"/> representing the position keyframes for the node.
    /// </summary>
    public IReadOnlyList<Vector3Key> Positions { get; private set; }
    
    /// <summary>
    /// A list of <see cref="QuatKey"/> representing the rotation keyframes (quaternions) for the node.
    /// </summary>
    public IReadOnlyList<QuatKey> Rotations { get; private set; }
    
    /// <summary>
    /// A list of <see cref="Vector3Key"/> representing the scale keyframes for the node.
    /// </summary>
    public IReadOnlyList<Vector3Key> Scales { get; private set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="NodeAnimChannel"/> class with the specified name, positions, rotations, and scales.
    /// </summary>
    /// <param name="name">The name of the node being animated.</param>
    /// <param name="positions">A list of <see cref="Vector3Key"/> representing position keyframes.</param>
    /// <param name="rotations">A list of <see cref="QuatKey"/> representing rotation keyframes.</param>
    /// <param name="scales">A list of <see cref="Vector3Key"/> representing scale keyframes.</param>
    public NodeAnimChannel(string name, IReadOnlyList<Vector3Key> positions, IReadOnlyList<QuatKey> rotations, IReadOnlyList<Vector3Key> scales) {
        this.Name = name;
        this.Positions = positions;
        this.Rotations = rotations;
        this.Scales = scales;
    }
}