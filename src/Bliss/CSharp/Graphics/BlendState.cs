using Veldrid;

namespace Bliss.CSharp.Graphics;

public class BlendState {
    
    /// <summary>
    /// A dictionary that caches instances of <see cref="BlendState"/> based on their <see cref="BlendStateDescription"/>.
    /// This allows for quick retrieval of blend states without needing to recreate them, thereby improving performance.
    /// </summary>
    private static Dictionary<BlendStateDescription, BlendState> _cachedBlendStates = new();
    
    /// <summary>
    /// Describes a blend state in which a single color target is blended with Disabled.
    /// </summary>
    public static BlendState Disabled => FromDescription(BlendStateDescription.SingleDisabled);
    
    /// <summary>
    /// Describes a blend state in which a single color target is blended with AdditiveBlend.
    /// </summary>
    public static BlendState AdditiveBlend => FromDescription(BlendStateDescription.SingleAdditiveBlend);
    
    /// <summary>
    /// Describes a blend state in which a single color target is blended with AlphaBlend.
    /// </summary>
    public static BlendState AlphaBlend => FromDescription(BlendStateDescription.SingleAlphaBlend);
    
    /// <summary>
    /// Describes a blend state in which a single color target is blended with OverrideBlend.
    /// </summary>
    public static BlendState OverrideBlend => FromDescription(BlendStateDescription.SingleOverrideBlend);
    
    /// <summary>
    /// Defines the blend state for rendering operations, including how blending is handled between source and destination pixels.
    /// </summary>
    public readonly BlendStateDescription Description;

    /// <summary>
    /// Represents a state for blending operations in graphics rendering. It provides predefined blend states such as
    /// Empty, Disabled, AdditiveBlend, AlphaBlend, and OverrideBlend. This class allows for the creation or retrieval
    /// of blend states based on a given description.
    /// </summary>
    private BlendState(BlendStateDescription description) {
        this.Description = description;
    }

    /// <summary>
    /// Creates a new or retrieves an existing <see cref="BlendState"/> instance based on the provided
    /// <see cref="BlendStateDescription"/>.
    /// </summary>
    /// <param name="description">The description of the blend state to create or retrieve.</param>
    /// <returns>The <see cref="BlendState"/> instance corresponding to the provided description.</returns>
    public static BlendState FromDescription(BlendStateDescription description) {
        if (!_cachedBlendStates.TryGetValue(description, out BlendState? state)) {
            BlendState blendState = new BlendState(description);
            
            _cachedBlendStates.Add(description, blendState);
            return blendState;
        }

        return state;
    }
}