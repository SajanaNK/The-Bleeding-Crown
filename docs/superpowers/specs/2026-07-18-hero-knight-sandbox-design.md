# Hero Knight Movement & Combat Sandbox — Design

## Context

The project is a Unity 2D Platformer Microgame template (Unity 2022.3.41f1, URP) targeting **mobile**. A separate pixel-art asset pack ("Hero Knight - Pixel Art") has been added to `Assets/`, containing sprites, animations, an animator controller, and a self-contained demo script (`Assets/Hero Knight - Pixel Art/Demo/HeroKnight.cs`) built for keyboard/mouse input.

The goal of this project is a **movement/combat sandbox**: get the Hero Knight character controller feeling right — full moveset, mobile touch controls, no level design or enemies yet. This is a first step; a real level/game structure is a separate future design.

## Goals

- Rebuild the Hero Knight character as a clean state machine (not a port of the single-`Update()` demo script, and not integrated into the template's `Platformer.*` / `Simulation` event framework — this is an intentionally separate system).
- Support the full moveset the sprite/animations provide: run, jump, wall-slide, roll, block, 3-hit attack combo, and **ledge-grab** (currently unimplemented — see below).
- Mobile touch controls: on-screen virtual joystick (movement) + action buttons (jump/attack/block/roll).
- A dedicated sandbox scene with terrain to exercise every move, landscape orientation.
- No enemies/combat targets in this pass — attacks/block play their animations and effects but have nothing to hit yet.

## Non-goals

- Level design, game structure, save/progression systems.
- Enemy AI or combat resolution (damage, hit detection, death).
- Final art for touch controls (placeholder graphics for now).
- Integrating with the template's `Platformer.Core.Simulation` event framework.
- Automated tests (no test framework is set up in this project; validation is manual, in-editor).

## Architecture & file layout

New code lives in its own namespace, `HeroKnightSandbox`, under the folder `Assets/Scripts/HeroKnight/` — kept separate from the template's `Platformer.*` namespaces since it intentionally doesn't use the `Simulation` event framework. The namespace can't simply be `HeroKnight`: the asset pack's own demo script declares a global-scope `class HeroKnight`, and a C# namespace can't share a name with a type already occupying that slot in the same scope (`CS0101`) — discovered when Task 1's implementer hit this collision against `Assets/Hero Knight - Pixel Art/Demo/HeroKnight.cs`.

```text
Assets/Scripts/HeroKnight/
  HeroKnightController.cs      // MonoBehaviour: owns Rigidbody2D, Animator, sensors, runs the state machine
  HeroKnightContext.cs         // shared mutable data passed between states (facing dir, combo count, timers, sensor refs, input ref)
  States/
    PlayerState.cs             // abstract base: Enter(), Tick(), FixedTick(), Exit()
    IdleState.cs, RunState.cs, JumpState.cs, FallState.cs,
    WallSlideState.cs, LedgeGrabState.cs, RollState.cs,
    BlockState.cs, AttackState.cs
  Input/
    IHeroKnightInput.cs         // MoveX, JumpPressed, AttackPressed, BlockHeld, RollPressed
    TouchHeroKnightInput.cs     // reads from the on-screen joystick + buttons
  Sensors/
    Sensor_HeroKnight.cs        // moved from the asset pack's Demo folder, reused as-is
    LedgeSensor.cs              // new: detects a grabbable ledge
  UI/
    VirtualJoystick.cs          // drag-based, outputs normalized Vector2
    TouchButton.cs              // press/hold UI button for Jump/Attack/Block/Roll
```

`HeroKnightController` drives a small explicit state machine (`ChangeState(PlayerState next)` calling `Exit()`/`Enter()`), each state reading `HeroKnightContext` for shared data instead of everything living in one `Update()` like the original demo script.

`HeroKnightController` and every state read input only through `IHeroKnightInput` — never `Input.*` or UI components directly — so a keyboard/gamepad implementation can be added later without touching movement logic.

## States & transitions

| State | Entered from | Behavior | Exits to |
|---|---|---|---|
| **Idle** | Run, Fall (landed), Roll/Attack/Block end | No horizontal input | Run (input), Jump (jump pressed), Fall (walks off ledge), Attack, Block, Roll |
| **Run** | Idle | Applies `MoveX * speed` to velocity, flips sprite | Idle (no input), Jump, Fall, Attack, Block, Roll |
| **Jump** | Idle/Run (jump pressed + grounded) | Sets upward velocity once, plays Jump anim | Fall (once ascent ends / `velocity.y <= 0`) |
| **Fall** | Jump, Run/Idle (walked off edge) | Gravity only; horizontal air control retained | Idle/Run (grounded via `Sensor_HeroKnight`), WallSlide (wall sensor active), LedgeGrab (ledge detected) |
| **WallSlide** | Fall (wall sensors both active) | Clamps fall speed, faces wall | Fall (wall sensor clears), LedgeGrab, Idle/Run (grounded) |
| **LedgeGrab** | Fall/WallSlide (ledge sensor condition) | Freezes physics, snaps to ledge anchor, plays hang pose | Idle (Jump button = climb up), Fall (Roll button = drop) |
| **Roll** | Idle/Run/Fall (roll pressed, not already rolling) | Fixed-duration dash in facing direction, ignores move input, cannot be interrupted | Idle/Run (grounded) or Fall (duration ends mid-air) |
| **Block** | Idle/Run (block held) | Zero horizontal velocity, block-idle loop while held | Idle/Run (block released) |
| **Attack** | Idle/Run (attack pressed) | Plays Attack1/2/3 by combo counter in `HeroKnightContext`; a new press within the combo window advances/re-enters Attack, otherwise combo resets | Idle/Run (animation completes, no combo follow-up) |

Attack/Block/Roll are mutually exclusive (can't roll while attacking, etc.). Combo/roll timing thresholds are preserved from `HeroKnight.cs`'s tuned values: 0.25s minimum between combo hits, 1.0s combo reset window, ~0.57s (8/14s) roll duration.

## Touch input

- **Canvas**: Screen Space – Overlay, Canvas Scaler set to "Scale With Screen Size" (reference resolution landscape, e.g. 1920×1080), so controls stay proportional across phone sizes.
- **VirtualJoystick** (left side): a background circle + draggable handle `UI.Image`, implementing `IPointerDownHandler`/`IDragHandler`/`IPointerUpHandler`, clamped to a radius, outputting a normalized `Vector2` (only `.x` is used for `MoveX`, matching the original single-axis movement).
- **TouchButton** (right side, one per action): Jump/Attack/Roll are one-shot presses (`IPointerDownHandler` → sets the corresponding `*Pressed` flag true for one frame, consumed by `HeroKnightController`); Block is press-and-hold (`IPointerDownHandler`/`IPointerUpHandler` toggling `BlockHeld`).
- `TouchHeroKnightInput` implements `IHeroKnightInput` and is the only thing wired to these UI elements.
- Placeholder art: plain semi-transparent circles/squares via `UI.Image` with no sprite — not blocking on final art per project decision.

## Ledge-grab (new logic)

The asset pack ships a `HeroKnight_LedgeGrab.anim` animation, but it is wired into neither the demo script nor the animator controller's transitions — ledge-grab does not currently exist as a working feature and is being built from scratch here.

- **Detection**: add a `LedgeSensor` positioned above each existing wall sensor pair (`WallSensor_R2`/`WallSensor_L2`). While in **Fall** or **WallSlide**, if a wall sensor is active (touching wall) **and** the corresponding ledge sensor is *not* active (nothing above the wall at that height — the wall ends here), that's a grabbable ledge → transition to **LedgeGrab**.
- **On entry**: snap the character's position so the sensor pair aligns with the ledge corner, zero out velocity, and freeze physics (`Rigidbody2D.bodyType = Kinematic` for the duration, restored on exit) so gravity doesn't pull the character off. Play `HeroKnight_LedgeGrab` as a held hang pose.
- **Exit — climb up** (Jump button, reusing the existing Jump input rather than adding a new control): since the pack ships only the one hang-pose animation (no dedicated climb-up animation), reposition the character to standing-on-ledge height in code and transition straight to **Idle** — a snap, not an animated climb-over. Confirmed acceptable for this sandbox pass; a proper climb transition is a future follow-up once there's a climb animation.
- **Exit — drop** (Roll button, reusing the existing Roll input rather than adding a new control): restore dynamic physics and transition to **Fall**.
- A short cooldown on `LedgeSensor` (mirroring `Sensor_HeroKnight.Disable()`) prevents immediately re-grabbing the same ledge after dropping.

## Sandbox scene & camera

- New scene `Assets/Scenes/HeroKnightSandbox.unity`, built with the Hero Knight pack's own `EnvironmentTiles_*` tile assets and `Walls_noFriction` physics material (so wall-slide isn't fighting friction).
- Terrain covers every move: a flat run of ground, a raised platform requiring a jump, a tall wall for wall-slide, a wall segment topped by a ledge (wall ends mid-air, matching the `LedgeSensor` detection above) for ledge-grab, and open space for roll.
- Camera: the project already depends on `com.unity.cinemachine` (2.10.1) — use a `CinemachineVirtualCamera` following the player on X/Y with a `CinemachineConfiner2D` bounded to the sandbox level, rather than a custom follow script.
- Project Player Settings default orientation set to Landscape for this build target.

## Testing approach

- No Unity Test Framework tests — this is feel-driven character-controller work, consistent with the project having no automated tests currently. Validated by playing it, not by writing NUnit tests.
- Validate in the Unity Editor Game view using mouse clicks on the on-screen joystick/buttons (simulated touch) — no on-device build required for this pass.
- Each state/transition in the table above gets manually exercised once implemented: run, jump, wall-slide, ledge-grab (both climb-up and drop), roll, block, and the full 3-hit attack combo (including combo reset after the 1.0s window).
