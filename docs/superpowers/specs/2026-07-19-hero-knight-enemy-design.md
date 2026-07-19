# Hero Knight Sandbox — Enemy Design

## Purpose

Add a basic melee enemy to the Hero Knight Sandbox so the player's combat moveset (Attack combo, Block, Roll) can be exercised against a real opponent instead of empty air. This is the first enemy pass for the sandbox — one enemy type (Light Bandit), ground-only patrol/attack AI, real sprite art and Animator-driven states. Follow-up passes (the Heavy Bandit variant, chase/aggro behavior, more enemy variety, player death/respawn) are explicitly out of scope and left for future specs.

## Context

The Hero Knight Sandbox character controller (`HeroKnightSandbox.*`, see [`2026-07-18-hero-knight-sandbox-design.md`](2026-07-18-hero-knight-sandbox-design.md)) is a standalone, self-contained state machine deliberately built outside the 2D Platformer Microgame template's `Platformer.Core.Simulation` event framework. The template does already have its own enemy system (`Platformer.Mechanics.EnemyController`, `Platformer.Gameplay.EnemyDeath`/`PlayerEnemyCollision`, driven through `Simulation.Schedule<T>()`), but it assumes stomp-to-kill mechanics using the "alien" sprite set in `Assets/Character/Sprites` — a different art style and a different combat model than Hero Knight's combo/block/roll melee.

An earlier revision of this spec, written before any matching enemy art existed in the project, planned a generated placeholder capsule sprite with `SpriteRenderer` color tints standing in for state feedback. Since then, `Assets/Bandits - Pixel Art/` was added to the project: a matching-style asset pack with two enemy variants (Light Bandit, Heavy Bandit) sharing one Animator Controller (`Assets/Bandits - Pixel Art/Animations/Light Bandit/LightBandit_AnimController.controller` — Heavy Bandit's is an `AnimatorOverrideController` on top of the same one, swapping clips only). This spec now uses the Light Bandit variant's real sprite and Animator instead of a placeholder; Heavy Bandit is deferred (it needs no new code, only a second prefab/override-controller pairing, once this pass is proven out).

The vendor demo (`Assets/Bandits - Pixel Art/Demo/Bandit.cs`, `Sensor_Bandit.cs`, `LightBandit.prefab`) is a fully self-contained, already-wired keyboard/mouse-driven demo — every state (`Idle`/`CombatIdle`/`Run`/`Jump`/`Attack`/`Hurt`/`Death`/`Recover`) and its Animator transitions already work out of the box, unlike Hero Knight's own pack where the ledge-grab clip existed but had no transitions wired yet. This design reuses the prefab and Animator wholesale and replaces only `Bandit.cs`'s input-driven `Update()` with our own AI-driven `EnemyState` machine — no Animator Controller edits are needed this time.

The demo's Animator parameters: `AnimState` (int: 0=Idle, 1=CombatIdle, 2=Run), `Grounded` (bool), `AirSpeed` (float), and `Attack`/`Hurt`/`Death`/`Recover`/`Jump` (triggers). This enemy patrols a fixed ground range and never leaves the ground (no chase, no jump — see Non-goals), so `Grounded`/`AirSpeed`/`Jump`/`Recover` are never touched; only `AnimState` and the `Attack`/`Hurt`/`Death` triggers are driven. Real clip lengths (read from the `.anim` assets), used to tune state timing below: `LightBandit_Attack` 0.8s, `LightBandit_Hurt` 0.25s, `LightBandit_Death` 1.0s.

The vendor demo flips facing via `transform.localScale.x` sign (not `SpriteRenderer.flipX`, which is how the Hero Knight player controller does it) — this design follows the vendor's own convention for the enemy rather than unifying the two, since it's the convention already proven to work with this sprite/pivot.

The player controller currently has no health/damage-taking concept at all — `HeroKnightContext` has no HP field, and nothing calls into the player's state machine from outside it. This design adds the minimum needed to make combat feel real (HP, a Hurt state, taking damage) without building out death/respawn, which is left for a separate future spec.

## Architecture

Two new self-contained pieces, both following the same "data-only context + `MonoBehaviour` owner + one class per state" pattern the player controller already established:

1. **Enemy system** — new folder `Assets/Scripts/HeroKnight/Enemy/`, namespace `HeroKnightSandbox.Enemy`. Not integrated with `Simulation`, `Platformer.Mechanics.EnemyController`, or `Platformer.Mechanics.Health`. Two pieces of the existing project are reused: `Platformer.Mechanics.PatrolPath` (a plain position-oscillator `MonoBehaviour` with zero `Simulation` coupling), and the vendor `LightBandit.prefab`/Animator Controller (visuals only — no vendor script is kept).
2. **Player combat additions** — extends the existing `HeroKnightSandbox` namespace: new fields on `HeroKnightContext`, a new `HurtState`, and a new `TakeDamage()` entry point on `HeroKnightController`.

A lightweight static `EnemyRegistry` (in the `HeroKnightSandbox.Enemy` namespace) bridges the two: enemies register themselves on `OnEnable`/deregister on `OnDisable`, and the player's `AttackState` queries it once per attack swing instead of using `FindObjectsOfType` every frame.

**`PatrolPath` placement:** `PatrolPath.Mover.Position` is computed as a *local* offset from `path.transform`, re-evaluated every call (see `Assets/Scripts/Mechanics/PatrolPath.Mover.cs`). If `PatrolPath` sat on the enemy's own (moving) transform, that reference frame would shift every tick the enemy's position is assigned from the mover, and the patrol would drift instead of oscillating between two fixed points. `Platformer.Mechanics.EnemyController` (the template's own, unrelated enemy) avoids exactly this by keeping `path` as a separate serialized reference rather than a component on the moving object — this design follows that same, already-working pattern: `PatrolPath` lives on a separate, stationary anchor `GameObject`, and `EnemyController` holds a serialized reference to it, not a `[RequireComponent]`.

## Components

### `EnemyContext` (data-only)

`Assets/Scripts/HeroKnight/Enemy/EnemyContext.cs`. Mirrors `HeroKnightContext`'s role: mutable data shared across enemy states, owned and populated by `EnemyController`.

Fields:
- `Transform Transform`
- `Animator Animator`
- `Platformer.Mechanics.PatrolPath PatrolPath` — reference to the separate anchor object described above, not a component on this enemy.
- `HeroKnightController Player` — cached reference to the player controller, assigned at scene-build time (see "Scene setup" below), used for range checks and calling `TakeDamage()`.
- `int MaxHP = 3`
- `int CurrentHP` — initialized to `MaxHP` in `Awake()`.
- `float MoveSpeed = 2.0f` — patrol speed, passed to `PatrolPath.CreateMover(MoveSpeed)`.
- `float AttackRange = 1.0f` — distance from enemy to player, in world units, that triggers Patrol → Attack.
- `int AttackDamage = 1`
- `float AttackWindup = 0.4f` — seconds from entering Attack to the damage-application instant. Combined with `AttackCooldown`, totals the real `LightBandit_Attack` clip length (0.8s) so the hit lands at the visual midpoint of the swing.
- `float AttackCooldown = 0.4f` — seconds after the damage instant before returning to Patrol.
- `float HurtDuration = 0.25f` — matches the `LightBandit_Hurt` clip length.
- `float DeathDuration = 1.0f` — matches the `LightBandit_Death` clip length; how long `DeadState` waits before destroying the `GameObject`.

### `EnemyController` (MonoBehaviour)

`Assets/Scripts/HeroKnight/Enemy/EnemyController.cs`. Owns an `EnemyContext` and the current `EnemyState`, same shape as `HeroKnightController`:

```csharp
using UnityEngine;

namespace HeroKnightSandbox.Enemy
{
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private HeroKnightController player;
        [SerializeField] private Platformer.Mechanics.PatrolPath patrolPath;

        private EnemyContext context;
        private EnemyState currentState;

        public PatrolState Patrol { get; private set; }
        public AttackState Attack { get; private set; }
        public HurtState Hurt { get; private set; }
        public DeadState Dead { get; private set; }

        private void Awake()
        {
            context = new EnemyContext
            {
                Transform = transform,
                Animator = GetComponent<Animator>(),
                PatrolPath = patrolPath,
                Player = player,
            };
            context.CurrentHP = context.MaxHP;

            Patrol = new PatrolState(this, context);
            Attack = new AttackState(this, context);
            Hurt = new HurtState(this, context);
            Dead = new DeadState(this, context);
        }

        private void OnEnable()
        {
            EnemyRegistry.Register(this);
        }

        private void OnDisable()
        {
            EnemyRegistry.Unregister(this);
        }

        private void Start()
        {
            ChangeState(Patrol);
        }

        private void Update()
        {
            currentState.Tick();
        }

        public void ChangeState(EnemyState next)
        {
            currentState?.Exit();
            currentState = next;
            currentState.Enter();
        }

        public void TakeDamage(int amount)
        {
            if (currentState == Dead || currentState == Hurt)
            {
                return;
            }

            context.CurrentHP -= amount;
            if (context.CurrentHP <= 0)
            {
                ChangeState(Dead);
            }
            else
            {
                ChangeState(Hurt);
            }
        }

        public Vector3 Position => context.Transform.position;
    }
}
```

No `[RequireComponent]` for `Animator`/`PatrolPath` — the former comes from the vendor `LightBandit.prefab` (already present on its root), and `PatrolPath` deliberately lives on a separate object (see Architecture).

Note: unlike the player's `TakeDamage`, the enemy's version has no invulnerability window — the `Hurt`-state guard above already prevents a single attack's multi-frame check from double-hitting (the attack only calls `TakeDamage` once per swing, see `AttackState` below), and re-entering Hurt from Hurt is blocked outright. This is deliberately simpler than the player's version since the enemy doesn't need to survive a flurry the way the player does.

### `EnemyState` (abstract base)

`Assets/Scripts/HeroKnight/Enemy/States/EnemyState.cs`. Mirrors `Assets/Scripts/HeroKnight/States/PlayerState.cs`'s shape exactly:

```csharp
namespace HeroKnightSandbox.Enemy
{
    public abstract class EnemyState
    {
        protected readonly EnemyController Controller;
        protected readonly EnemyContext Context;

        protected EnemyState(EnemyController controller, EnemyContext context)
        {
            Controller = controller;
            Context = context;
        }

        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void Tick() { }
    }
}
```

### `PatrolState`

`Assets/Scripts/HeroKnight/Enemy/States/PatrolState.cs`. On `Enter()`, creates a `PatrolPath.Mover` via `Context.PatrolPath.CreateMover(Context.MoveSpeed)` and sets `Context.Animator.SetInteger("AnimState", 2)` (Run). Each `Tick()`: reads `mover.Position`; if it differs from the current position, flips `Context.Transform.localScale.x` to match the vendor's own convention (`Bandit.cs`: moving right → `localScale.x = -1`; moving left → `localScale.x = 1`) based on the sign of `mover.Position.x - Context.Transform.position.x`; then assigns `Context.Transform.position` from the mover (same direct-assignment approach as the original placeholder-sprite revision of this design — the mover is already time-parameterized at the given speed, so no separate lerp/move-toward step is needed). Each `Tick()`, computes `Vector2.Distance(Context.Transform.position, Context.Player.transform.position)`; if less than `Context.AttackRange`, calls `Controller.ChangeState(Controller.Attack)`.

### `AttackState`

`Assets/Scripts/HeroKnight/Enemy/States/AttackState.cs`. On `Enter()`: resets an internal timer to 0, resets a "has dealt damage" flag, calls `Context.Animator.SetTrigger("Attack")`. Each `Tick()`, increments the timer by `Time.deltaTime`:
- When the timer crosses `Context.AttackWindup` (once, guarded by the flag so it only fires once per attack): if the player is still within `Context.AttackRange`, call `Context.Player.TakeDamage(Context.AttackDamage)`.
- When the timer crosses `Context.AttackWindup + Context.AttackCooldown`: `Controller.ChangeState(Controller.Patrol)` (which sets `AnimState` back to Run on `Enter()`).

No `AnimState` change on `Attack.Enter()` — the vendor Animator's `Attack` state is reached via its own pre-wired Any-State trigger transition, independent of `AnimState`, and its own exit transition returns to whatever `AnimState` reads once the clip finishes (which by then is already `Patrol`'s `2`, since our C# timer and the clip length are tuned to match — see `EnemyContext.AttackWindup`/`AttackCooldown`).

### `HurtState`

`Assets/Scripts/HeroKnight/Enemy/States/HurtState.cs`. On `Enter()`: calls `Context.Animator.SetTrigger("Hurt")`, resets a timer to 0. Each `Tick()`: once the timer exceeds `Context.HurtDuration`, calls `Controller.ChangeState(Controller.Patrol)`.

### `DeadState`

`Assets/Scripts/HeroKnight/Enemy/States/DeadState.cs`. On `Enter()`: calls `Context.Animator.SetTrigger("Death")`, resets a timer to 0. Each `Tick()`: once the timer exceeds `Context.DeathDuration`, calls `Object.Destroy(Controller.gameObject)`. No manual sprite fade — the real death pose is the visual, unlike the earlier placeholder-sprite revision of this design.

### `EnemyRegistry` (static)

`Assets/Scripts/HeroKnight/Enemy/EnemyRegistry.cs`:

```csharp
using System.Collections.Generic;

namespace HeroKnightSandbox.Enemy
{
    public static class EnemyRegistry
    {
        private static readonly List<EnemyController> enemies = new List<EnemyController>();

        public static void Register(EnemyController enemy) => enemies.Add(enemy);
        public static void Unregister(EnemyController enemy) => enemies.Remove(enemy);
        public static IReadOnlyList<EnemyController> All => enemies;
    }
}
```

Plain static list — no `Simulation.InstanceRegister<T>` involved (that type lives in `Platformer.Core`, part of the framework this design deliberately stays out of); this is a much smaller, single-purpose analog scoped to just enemies.

## Player combat additions

Unchanged from the original revision of this design — none of it depends on which enemy art is used.

### `HeroKnightContext` new fields

Added to `Assets/Scripts/HeroKnight/HeroKnightContext.cs`:

```csharp
public int MaxHP = 5;
public int CurrentHP;
public int AttackDamage = 1;
public float AttackHitRadius = 1.0f;
public float InvulnerabilityDuration = 0.5f;
public float HurtDuration = 0.3f;
public float InvulnerabilityTimer = 0f;

public bool IsInvulnerable => InvulnerabilityTimer > 0f;
```

`CurrentHP` is initialized to `MaxHP` in `HeroKnightController.Awake()`, same place the rest of the context is populated. `InvulnerabilityTimer` counts down in `HeroKnightController.Update()` (`if (InvulnerabilityTimer > 0f) InvulnerabilityTimer -= Time.deltaTime;`), mirroring how `TimeSinceAttack` already accumulates there.

### `HurtState`

`Assets/Scripts/HeroKnight/States/HurtState.cs`, following `AttackState`'s existing shape:

```csharp
using UnityEngine;

namespace HeroKnightSandbox.States
{
    public class HurtState : PlayerState
    {
        private float timer;

        public HurtState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        public override void Enter()
        {
            timer = 0f;
            Context.SetVelocityX(0f);
            Context.Animator.SetTrigger("Hurt");
        }

        public override void Tick()
        {
            timer += Time.deltaTime;
            if (timer > Context.HurtDuration)
            {
                Controller.ChangeState(Context.IsGrounded
                    ? (Mathf.Abs(Context.Controls.MoveX) > Mathf.Epsilon ? (PlayerState)Controller.Run : Controller.Idle)
                    : Controller.Fall);
            }
        }
    }
}
```

Added to `HeroKnightController`: a `public HurtState Hurt { get; private set; }` property, constructed in `Awake()` alongside the other states, following the exact pattern `Attack`/`Roll`/`Block` already use.

### `HeroKnightController.TakeDamage`

New public method on `HeroKnightController`:

```csharp
public void TakeDamage(int amount)
{
    if (context.IsInvulnerable || currentState == Hurt)
    {
        return;
    }

    context.CurrentHP -= amount;
    context.InvulnerabilityTimer = context.InvulnerabilityDuration;
    ChangeState(Hurt);
}
```

This is the enemy's only entry point into the player's state machine — `Enemy.AttackState` calls `Context.Player.TakeDamage(Context.AttackDamage)` directly, no events, no `Simulation` scheduling, consistent with both systems being self-contained. No death handling: `CurrentHP` can go to or below 0 with no special-cased behavior beyond the normal Hurt flinch — a future spec adds death/respawn.

### `AttackState` hit-detection addition

`Assets/Scripts/HeroKnight/States/AttackState.cs` gains a `hasHitThisSwing` bool (reset to `false` in `Enter()`) and a `private const float AttackHitWindow = 0.2f` (partway through the existing `ExitDelay = 0.4f` window). In `Tick()`, once `Context.TimeSinceAttack` crosses `AttackHitWindow` and `hasHitThisSwing` is still false: iterate `HeroKnightSandbox.Enemy.EnemyRegistry.All`, and for each enemy, compute `float dx = enemy.Position.x - Context.Transform.position.x`. It's in range and in front of the player when `Mathf.Abs(dx) <= Context.AttackHitRadius && Mathf.Sign(dx) == Context.FacingDirection` (matches the existing X-only, sign-based direction convention `LedgeGrabState`'s climb direction already uses — no `Vector2.Dot`/angle math needed since there's no vertical combat range). Call `enemy.TakeDamage(Context.AttackDamage)` on the first matching enemy found and set `hasHitThisSwing = true` (only one enemy is hit per swing — no multi-target cleave in this pass). This mirrors the enemy's own windup-then-single-hit pattern in `Enemy.AttackState` above.

## Scene setup

A new step in `Assets/Editor/HeroKnightSandboxSetup.cs`'s automation, `HeroKnightSandbox > 5 Build Enemies`, added to `RunAll`'s sequence after `4 Finalize Project Settings`:

- Duplicates `Assets/Bandits - Pixel Art/Demo/LightBandit.prefab` into `Assets/Prefabs/HeroKnightEnemy.prefab` (not `Enemy.prefab` — that path already belongs to a pre-existing template asset, `Platformer.Mechanics.EnemyController`-based and referenced by `SampleScene.unity`; an earlier revision of this design collided with it and silently corrupted that scene's enemies) — the same `PrefabUtility.LoadPrefabContents`/`SaveAsPrefabAsset` approach `BuildPrefab()` already uses for the player's `HeroKnight.prefab` — removing the demo `Bandit` component from the root, setting its `Rigidbody2D` to `Kinematic` (the vendor default is Dynamic-with-gravity, but `PatrolState` moves the enemy via direct transform assignment, not physics — a Dynamic body there would jitter and let the player physically shove it), and adding `HeroKnightSandbox.Enemy.EnemyController` in its place. The `GroundSensor` child and its `Sensor_Bandit` component are left as-is (inert, unused — this enemy never leaves the ground, see Non-goals).
- Creates exactly 2 enemy instances directly in `HeroKnightSandbox.unity` (not as separate prefab variants, matching this scene's existing pattern of instantiating most things inline via the automation script) — enough to test both an isolated encounter and, later, behavior when two enemies are attackable in the same area, without adding real variety. Each instance: instantiated from `Enemy.prefab`, positioned on the flat Ground platform near the player's spawn (reachable without requiring wall-slide/ledge-grab), with a separate stationary anchor `GameObject` per enemy holding a `Platformer.Mechanics.PatrolPath` (short `startPosition`/`endPosition` range), and its `EnemyController`'s `player`/`patrolPath` serialized fields wired to the scene's existing `HeroKnight` instance and that anchor's `PatrolPath`, respectively (found via the same `GameObject.Find`/hierarchy-lookup pattern `BuildScene()` already uses elsewhere in this file).
- Idempotent: re-running `5 Build Enemies` destroys and recreates any existing enemy/anchor `GameObject`s found by name before building fresh ones, rather than skipping when they already exist — so a rerun always reconnects the scene's enemies to whatever `HeroKnightEnemy.prefab` currently contains (matching `BuildEnemyPrefab()`'s own always-rebuild convention, and correctly repairing the scene if the prefab is ever rebuilt with different contents).

## Testing plan

Manual Unity Editor Play-mode pass, same style as the movement controller's Task 12, covering:

- Enemy patrols correctly between its `PatrolPath` endpoints without player nearby, facing flipped correctly to match its current movement direction, without drifting off that range over time (confirms `PatrolPath` is wired to the anchor, not the enemy's own transform — see Architecture).
- Approaching within `AttackRange` stops patrol and triggers the enemy's Attack animation; moving back out of range before the windup completes should still let the attack resolve as designed (the range check only happens once, at windup) — confirm this matches intent or tune `AttackWindup` if it feels wrong.
- Player's attack combo damages the enemy: confirm `CurrentHP` depletes per hit (visible via the `Hurt` clip playing) and it dies (`Death` clip plays, `GameObject` destroyed after `DeathDuration`) after `MaxHP` hits.
- Enemy's attack damages the player: confirm `CurrentHP` decrements, `HurtState` plays (velocity zeroed, `Hurt` Animator clip visible), and a second enemy swing within `InvulnerabilityDuration` does not double-hit.
- Mutual exclusivity: taking damage while in Attack/Roll/Block correctly interrupts into Hurt; the player cannot Attack/Roll while in Hurt.
- Confirm the vendor Animator Controller's existing player `Hurt` state already has working entry/exit transitions (it predates this session's `LedgeGrab` additions) — if it doesn't transition back out cleanly on its own, this needs the same kind of explicit trigger-based exit transition Task 11's `LedgeGrab` work added, via a new `AddHurtExitTransition`-style step in the automation script. (The *enemy*'s Animator Controller needs no such check — it's the vendor's own, already fully wired.)
- Tune by feel by adjusting `EnemyContext`/`HeroKnightContext` values: `AttackRange`, `AttackDamage` (both sides), `MaxHP` (both sides), `InvulnerabilityDuration`, `HurtDuration` (player side), `AttackWindup`/`AttackCooldown`.

## Out of scope

- The Heavy Bandit variant (same code, would just need its own prefab/override-controller pairing — deferred to keep this pass's testing surface to one enemy type).
- Enemy chase/aggro-from-a-distance behavior (attack-in-range only, no pursuit).
- Enemy jump/airborne behavior (ground-only patrol — `Grounded`/`AirSpeed`/`Jump` Animator parameters are never touched).
- Multiple enemy types or variety beyond the two placed instances.
- Player death and respawn (HP can reach 0 with no special handling beyond the normal Hurt flinch).
- Ranged/projectile attacks, on either side.
- Any integration with `Platformer.Core.Simulation` or `Platformer.Mechanics.EnemyController`/`Health`.
