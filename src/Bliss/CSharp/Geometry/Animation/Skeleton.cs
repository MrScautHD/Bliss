using System.Collections.ObjectModel;

namespace Bliss.CSharp.Geometry.Animation;

public class Skeleton {
    
    /// <summary>
    /// The list of bones that make up the skeleton.
    /// </summary>
    public IReadOnlyList<BoneInfo> Bones { get; private set; }
    
    /// <summary>
    /// A dictionary mapping bone names to their corresponding indices in the <see cref="Bones"/> list.
    /// </summary>
    public IReadOnlyDictionary<string, uint> BoneNameToIndex { get; private set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Skeleton"/> class.
    /// </summary>
    /// <param name="bones">The list of bones that define the skeleton.</param>
    public Skeleton(IReadOnlyList<BoneInfo> bones) {
        this.Bones = bones;
        this.BoneNameToIndex = new ReadOnlyDictionary<string, uint>(bones.Select((bone, index) => new KeyValuePair<string, uint>(bone.Name, (uint) index)).ToDictionary(pair => pair.Key, pair => pair.Value));
    }
}