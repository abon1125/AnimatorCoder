// Author: Small Hedge Games
// Date: 05/04/2024

namespace SHG.AnimatorCoder
{
    /// <summary>
    /// Complete list of all Animator state short names used by AnimatorCoder.
    ///
    /// Important:
    /// - These enum names should match the Animator state's short name EXACTLY (case-sensitive).
    ///   Example: A state named "IDLE" should map to Animations.IDLE.
    /// - Keep <see cref="RESET"/> as a sentinel value used by AnimatorCoder to fall back to DefaultAnimation().
    /// </summary>
    public enum Animations
    {
        // Change the list below to your animation state names (short names).
        IDLE,
        RUN,
        ATTACK1,
        ATTACK2,
        HIT,
        JUMP,
        FALL,
        /// <summary> Sentinel used to indicate "play default animation logic". </summary>
        RESET
    }

    /// <summary>
    /// Complete list of internal flags used by AnimatorCoder (NOT Unity Animator parameters).
    /// Used by behaviours such as <see cref="OnParameter"/>.
    /// </summary>
    public enum Parameters
    {
        // Change the list below to whatever boolean flags your gameplay code needs.
        GROUNDED,
        FALLING
    }
}


