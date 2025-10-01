using System.Numerics;
using MiniAudioEx.Core.StandardAPI;

namespace Bliss.CSharp.Audio;

public static class Vector3FExtensions {
    
    /// <summary>
    /// Converts a <see cref="Vector3f"/> instance to a <see cref="Vector3"/> instance.
    /// </summary>
    /// <param name="v">The <see cref="Vector3f"/> instance to convert.</param>
    /// <returns>A <see cref="Vector3"/> instance corresponding to the given <see cref="Vector3f"/>.</returns>
    public static Vector3 ToVector3(this Vector3f v) {
        return new Vector3(v.x, v.y, v.z);
    }

    /// <summary>
    /// Converts a <see cref="Vector3"/> instance to a <see cref="Vector3f"/> instance.
    /// </summary>
    /// <param name="v">The <see cref="Vector3"/> instance to convert.</param>
    /// <returns>A <see cref="Vector3f"/> instance corresponding to the given <see cref="Vector3"/>.</returns>
    public static Vector3f ToVector3F(this Vector3 v) {
        return new Vector3f(v.X, v.Y, v.Z);
    }
}