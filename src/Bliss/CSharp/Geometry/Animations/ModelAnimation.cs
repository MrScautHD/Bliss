/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

using Assimp;

namespace Bliss.CSharp.Geometry.Animations;

public class ModelAnimation {
    
    public string Name { get; private set; }

    public float DurationInTicks { get; private set; }
    public float DurationInSeconds => (float) this.DurationInTicks / (float) this.TicksPerSecond;
    public float TicksPerSecond  { get; private set; }
    
    public List<NodeAnimationChannel> AnimationChannels { get; private set; } // TODO: Replace NodeAnimationChannel with my own class

    public ModelAnimation(string name, float durationInTicks, float ticksPerSecond, List<NodeAnimationChannel> channels) {
        this.Name = name;
        this.DurationInTicks = durationInTicks;
        this.TicksPerSecond = ticksPerSecond;
        this.AnimationChannels = channels;
    }
}