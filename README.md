# AnimatorCoder

Unity C# helper for driving Animator states with strongly-typed enums, with optional layer locking and simple animation chaining.

Originally by Small Hedge Games (08/04/2024).

## Features

- Play Animator states via `Animations` enum (state short names).
- Per-layer locking to prevent unwanted interruptions.
- Simple chaining via `AnimationData.nextAnimation`.
- Optional state-machine hook via `OnParameter` (triggers chains off AnimatorCoder internal flags).

## Install

Copy these scripts into your Unity project (e.g. `Assets/Scripts/AnimatorCoder/`):

- `Runtime/AnimatorCoder.cs`
- `Runtime/AnimatorValues.cs`
- `Runtime/OnParameter.cs`

## Setup

1. Edit `AnimatorValues.cs` and update:
   - `Animations` to match your Animator **state short names** exactly (case-sensitive).
   - `Parameters` to match the boolean flags you want to drive from gameplay code.
2. In your scripts, use the namespace `SHG.AnimatorCoder`.
3. Create a component that inherits `AnimatorCoder`.
4. Implement `DefaultAnimation(int layer)` in your component.
5. Call `Initialize()` once (typically in `Start()`).

## Quick start

```csharp
using UnityEngine;
using SHG.AnimatorCoder;

public class PlayerAnimator : AnimatorCoder
{
    private void Start()
    {
        Initialize();
    }

    public override void DefaultAnimation(int layer)
    {
        // Example default logic
        Play(new AnimationData(Animations.IDLE, lockLayer: false, nextAnimation: null, crossfade: 0.1f), layer);
    }

    private void Update()
    {
        // Example input-driven play
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var attack2 = new AnimationData(Animations.ATTACK2, lockLayer: false, nextAnimation: null, crossfade: 0.05f);
            var attack1 = new AnimationData(Animations.ATTACK1, lockLayer: true, nextAnimation: attack2, crossfade: 0.05f);
            Play(attack1, layer: 0);
        }
    }
}
```

## OnParameter (Animator StateMachineBehaviour)

`OnParameter` is intended to be added as a behaviour on an Animator state. It checks an `AnimatorCoder` internal flag:

- When `AnimatorCoder.GetBool(parameter) == target` becomes true, it triggers the configured `nextAnimations` chain.
- It uses AnimatorCoder **internal flags**, not Unity Animator parameters.

Drive it from gameplay code like this:

```csharp
SetBool(Parameters.GROUNDED, true);
```

## Notes / pitfalls

- **RESET behavior**: Passing `Animations.RESET` to `Play()` calls `DefaultAnimation(layer)` and returns `false`.
- **Crossfade & RESET**: Crossfade is not applied when playing the RESET animation (because RESET routes to `DefaultAnimation()`).
- **State name hashing**: `Animations` enum names are expected to match Animator state's **short name** (not full path).
- **Layer chaining**: Chaining coroutines are managed per-layer.

## Public API (AnimatorCoder)

- `void Initialize()`
- `bool Play(AnimationData data, int layer = 0)`
- `Animations GetCurrentAnimation(int layer)`
- `void SetLocked(bool lockLayer, int layer)`
- `bool IsLocked(int layer)`
- `void SetBool(Parameters id, bool value)` (internal flags)
- `bool GetBool(Parameters id)` (internal flags)

## Tutorial

- [YouTube tutorial](https://youtu.be/9tvDtS1vYuM)

## License

CC0 1.0 Universal. See `LICENSE`.
