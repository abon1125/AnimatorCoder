// Author: Small Hedge Games
// Date: 08/04/2024

using System.Collections;
using UnityEngine;
using System;

namespace SHG.AnimatorCoder
{
    /// <summary>
    /// Lightweight Animator state controller that uses <see cref="Animations"/> / <see cref="Parameters"/> enums
    /// as strongly-typed IDs.
    ///
    /// Usage:
    /// 1) Create a MonoBehaviour that inherits <see cref="AnimatorCoder"/>
    /// 2) Implement <see cref="DefaultAnimation(int)"/> per layer
    /// 3) Call <see cref="Initialize"/> once (typically in Start)
    /// 4) Call <see cref="Play(AnimationData,int)"/> to request animations
    ///
    /// Notes:
    /// - This does NOT set Unity Animator parameters; <see cref="SetBool"/> / <see cref="GetBool"/> are internal flags
    ///   intended for behaviours like <see cref="OnParameter"/>.
    /// - Enum names in <see cref="Animations"/> should match Animator state's short name exactly (e.g. "IDLE").
    /// </summary>
    public abstract class AnimatorCoder : MonoBehaviour
    {
        /// <summary> The baseline animation logic on a specific layer </summary>
        public abstract void DefaultAnimation(int layer);
        private Animator animator;
        private Animations[] currentAnimation;
        private bool[] layerLocked;
        // Debug-friendly view of the internal flag store in the Inspector.
        // Initialized from the Parameters enum; user edits will be overwritten on Initialize().
        [SerializeField] private ParameterDisplay[] parameters;
        private Coroutine[] currentCoroutines;
        private bool isInitialized = false;

        /// <summary> True after <see cref="Initialize"/> has successfully run. </summary>
        public bool IsInitialized => isInitialized;

        /// <summary> Sets up the Animator Brain </summary>
        public void Initialize()
        {
            AnimatorValues.Initialize();

            animator = GetComponent<Animator>();
            if (animator == null)
            {
                LogError("Missing Animator component");
                isInitialized = false;
                return;
            }

            layerLocked = new bool[animator.layerCount];
            currentAnimation = new Animations[animator.layerCount];
            currentCoroutines = new Coroutine[animator.layerCount];

            for (int i = 0; i < animator.layerCount; ++i)
            {
                layerLocked[i] = false;
                currentAnimation[i] = Animations.RESET;
                currentCoroutines[i] = null;

                // We compare against shortNameHash so enum names can simply be "IDLE", "RUN", ...
                // (fullPathHash usually includes layer/path and will not match Animator.StringToHash("IDLE").)
                int hash = animator.GetCurrentAnimatorStateInfo(i).shortNameHash;
                for (int k = 0; k < AnimatorValues.Animations.Length; ++k)
                {
                    if (hash == AnimatorValues.Animations[k])
                    {
                        currentAnimation[i] = (Animations)k;
                        break;
                    }
                }
            }

            string[] names = Enum.GetNames(typeof(Parameters));
            parameters = new ParameterDisplay[names.Length];
            for (int i = 0; i < names.Length; ++i)
            {
                parameters[i].name = names[i];
                parameters[i].value = false;
            }

            isInitialized = true;
        }

        /// <summary> Returns the current animation that is playing </summary>
        public Animations GetCurrentAnimation(int layer)
        {
            if (!IsValidLayer(layer))
            {
                LogError("Can't retrieve Current Animation. Fix: Initialize() in Start() and don't exceed number of animator layers");
                return Animations.RESET;
            }

            return currentAnimation[layer];
        }

        /// <summary> Sets the whole layer to be locked or unlocked </summary>
        public void SetLocked(bool lockLayer, int layer)
        {
            if (!IsValidLayer(layer))
            {
                LogError("Can't set layer lock. Fix: Initialize() in Start() and don't exceed number of animator layers");
                return;
            }

            layerLocked[layer] = lockLayer;
        }

        public bool IsLocked(int layer)
        {
            if (!IsValidLayer(layer))
            {
                LogError("Can't retrieve layer lock state. Fix: Initialize() in Start() and don't exceed number of animator layers");
                return false;
            }

            return layerLocked[layer];
        }

        /// <summary>
        /// Sets an internal flag (NOT a Unity Animator parameter).
        /// Used by <see cref="OnParameter"/> to drive animation chains.
        /// </summary>
        public void SetBool(Parameters id, bool value)
        {
            if (!isInitialized || parameters == null)
            {
                LogError("Please Initialize() in Start()");
                return;
            }

            int index = (int)id;
            if (index < 0 || index >= parameters.Length)
            {
                LogError("Parameter index out of range (update Parameters enum / Initialize)");
                return;
            }

            parameters[index].value = value;
        }

        /// <summary> Returns an internal flag (NOT a Unity Animator parameter). </summary>
        public bool GetBool(Parameters id)
        {
            if (!isInitialized || parameters == null)
            {
                LogError("Please Initialize() in Start()");
                return false;
            }

            int index = (int)id;
            if (index < 0 || index >= parameters.Length)
            {
                LogError("Parameter index out of range (update Parameters enum / Initialize)");
                return false;
            }

            return parameters[index].value;
        }

        /// <summary> Takes in the animation details and the animation layer, then attempts to play the animation </summary>
        public bool Play(AnimationData data, int layer = 0)
        {
            if (!isInitialized || animator == null || currentAnimation == null || layerLocked == null || currentCoroutines == null)
            {
                LogError("Please Initialize() in Start()");
                return false;
            }

            if (data == null)
            {
                LogError("AnimationData is null");
                return false;
            }

            if (!IsValidLayer(layer))
            {
                LogError("Layer index out of range");
                return false;
            }

            if (data.animation == Animations.RESET)
            {
                DefaultAnimation(layer);
                return false;
            }

            if (layerLocked[layer] || currentAnimation[layer] == data.animation) return false;

            if (currentCoroutines[layer] != null) StopCoroutine(currentCoroutines[layer]);
            layerLocked[layer] = data.lockLayer;
            currentAnimation[layer] = data.animation;

            animator.CrossFade(AnimatorValues.GetHash(currentAnimation[layer]), data.crossfade, layer);

            if (data.nextAnimation != null)
            {
                currentCoroutines[layer] = StartCoroutine(WaitForThenPlayNext(data, layer));
            }

            return true;
        }

        private IEnumerator WaitForThenPlayNext(AnimationData data, int layer)
        {
            // Wait one frame so the Animator can evaluate transitions / next state.
            // This is more reliable than reading GetNextAnimatorStateInfo immediately after CrossFade.
            yield return null;

            animator.Update(0);

            float length = animator.GetNextAnimatorStateInfo(layer).length;
            if (data.crossfade == 0) length = animator.GetCurrentAnimatorStateInfo(layer).length;

            // If nextAnimation.crossfade exceeds the clip length, clamp to 0 to avoid negative waits.
            float waitSeconds = Mathf.Max(0f, length - data.nextAnimation.crossfade);
            yield return new WaitForSeconds(waitSeconds);

            SetLocked(false, layer);
            Play(data.nextAnimation, layer);
        }

        private void LogError(string message)
        {
            Debug.LogError("AnimatorCoder Error: " + message);
        }

        private bool IsValidLayer(int layer)
        {
            return isInitialized && animator != null && layerLocked != null && currentAnimation != null && layer >= 0 && layer < animator.layerCount;
        }

        private void OnDisable()
        {
            if (currentCoroutines == null) return;
            for (int i = 0; i < currentCoroutines.Length; ++i)
            {
                if (currentCoroutines[i] != null) StopCoroutine(currentCoroutines[i]);
                currentCoroutines[i] = null;
            }
        }
    }

    /// <summary> Holds all data about an animation </summary>
    [Serializable]
    public class AnimationData
    {
        public Animations animation;
        /// <summary> Should the layer lock for this animation? </summary>
        public bool lockLayer;
        /// <summary> Should an animation play immediately after? </summary>
        public AnimationData nextAnimation;
        /// <summary> Should there be a transition time into this animation? </summary>
        public float crossfade = 0;

        /// <summary> Sets the animation data </summary>
        public AnimationData(Animations animation = Animations.RESET, bool lockLayer = false, AnimationData nextAnimation = null, float crossfade = 0)
        {
            this.animation = animation;
            this.lockLayer = lockLayer;
            this.nextAnimation = nextAnimation;
            this.crossfade = crossfade;
        }
    }

    /// <summary> Maintains cached hashes for <see cref="Animations"/> to avoid repeated hashing. </summary>
    public static class AnimatorValues
    {
        /// <summary> Returns the animation hash array </summary>
        public static int[] Animations { get { return animations; } }

        private static int[] animations;
        private static bool initialized = false;

        /// <summary> Initializes the animator state names </summary>
        public static void Initialize()
        {
            if (initialized) return;
            initialized = true;

            string[] names = Enum.GetNames(typeof(Animations));

            animations = new int[names.Length];
            for (int i = 0; i < names.Length; i++)
                animations[i] = Animator.StringToHash(names[i]);
        }

        /// <summary> Gets the animator hash value of an animation </summary>
        public static int GetHash(Animations animation)
        {
            return animations[(int)animation];
        }
    }

    /// <summary> Allows the animation parameters to be shown in debug inspector </summary>
    [Serializable]
    public struct ParameterDisplay
    {
        public string name;
        public bool value;
    }
}
