// Author: Small Hedge Games
// Date: 05/04/2024

using UnityEngine;

namespace SHG.AnimatorCoder
{
    /// <summary>
    /// Animator StateMachineBehaviour that triggers an animation chain when an AnimatorCoder flag matches a target.
    ///
    /// Important:
    /// - This checks <see cref="AnimatorCoder.GetBool"/> (internal flags), NOT Unity Animator parameters.
    /// - It triggers on the rising edge (when the condition becomes true) to avoid re-firing every frame.
    /// - It clones the configured chain at runtime so we don't mutate serialized <see cref="AnimationData"/> instances.
    /// </summary>
    public class OnParameter : StateMachineBehaviour
    {
        [SerializeField, Tooltip("Parameter to test")] private Parameters parameter;
        [SerializeField, Tooltip("Specify whether it should be on or off")] private bool target;
        [SerializeField, Tooltip("Chain of animations to play when condition is met")] private AnimationData[] nextAnimations;

        private AnimatorCoder animatorBrain;
        private bool wasConditionMet;

        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animatorBrain = animator.GetComponent<AnimatorCoder>();
            wasConditionMet = false;
        }

        override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (animatorBrain == null || !animatorBrain.IsInitialized) return;
            if (nextAnimations == null || nextAnimations.Length == 0) return;

            bool conditionMet = animatorBrain.GetBool(parameter) == target;
            if (!conditionMet)
            {
                wasConditionMet = false;
                return;
            }

            // Trigger only once while the condition remains true.
            if (wasConditionMet) return;
            wasConditionMet = true;

            animatorBrain.SetLocked(false, layerIndex);

            AnimationData chain = BuildRuntimeChain(nextAnimations);
            if (chain != null) animatorBrain.Play(chain, layerIndex);
        }

        private static AnimationData BuildRuntimeChain(AnimationData[] template)
        {
            // Clone to avoid modifying serialized data at runtime.
            AnimationData head = null;
            AnimationData prev = null;

            for (int i = 0; i < template.Length; ++i)
            {
                AnimationData src = template[i];
                if (src == null) continue;

                AnimationData node = new AnimationData(src.animation, src.lockLayer, null, src.crossfade);
                if (head == null) head = node;
                if (prev != null) prev.nextAnimation = node;
                prev = node;
            }

            return head;
        }
    }
}

