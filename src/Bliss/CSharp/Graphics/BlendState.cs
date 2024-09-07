using Bliss.CSharp.Logging;
using Veldrid;

namespace Bliss.CSharp.Graphics;

public class BlendState {

    private static Dictionary<BlendStateDescription, BlendState> _cachedBlendStates = new();
    
    public static BlendState Disabled => FromDescription(BlendStateDescription.SingleDisabled);
    public static BlendState AdditiveBlend => FromDescription(BlendStateDescription.SingleAdditiveBlend);
    public static BlendState AlphaBlend => FromDescription(BlendStateDescription.SingleAlphaBlend);
    public static BlendState OverrideBlend => FromDescription(BlendStateDescription.SingleOverrideBlend);
    
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