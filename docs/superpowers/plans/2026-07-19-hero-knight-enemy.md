# Hero Knight Sandbox — Enemy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a self-contained patrol/attack melee enemy (Light Bandit) to the Hero Knight Sandbox, plus minimal player HP and a Hurt state, so the player's Attack/Block/Roll moveset can be exercised against a real opponent — following `docs/superpowers/specs/2026-07-19-hero-knight-enemy-design.md`.

**Architecture:** A new `HeroKnightSandbox.Enemy` namespace under `Assets/Scripts/HeroKnight/Enemy/`, mirroring the player's own "data-only context + `MonoBehaviour` owner + one `EnemyState` subclass per state" pattern, driving the vendor `Assets/Bandits - Pixel Art/Demo/LightBandit.prefab`'s existing, already-wired Animator instead of any custom visuals. A static `EnemyRegistry` lets the player's `AttackState` find nearby enemies without `FindObjectsOfType`. The player gains HP/`HurtState`/`TakeDamage()` additions to the existing `HeroKnightSandbox` namespace. Neither side touches `Platformer.Core.Simulation` — this stays as deliberately standalone as the rest of the sandbox.

**Tech Stack:** Unity 2022.3.41f1, URP. Reuses `Platformer.Mechanics.PatrolPath` (the template's own position-oscillator component) and the vendor `Bandits - Pixel Art` asset pack's Light Bandit prefab/Animator Controller (visuals only — the vendor `Bandit.cs`/`Sensor_Bandit.cs` demo scripts are not reused).

## Global Constraints

- Unity Editor version: `2022.3.41f1` (must match `ProjectSettings/ProjectVersion.txt`).
- New code lives under `Assets/Scripts/HeroKnight/Enemy/`, namespaced `HeroKnightSandbox.Enemy` (flat — the state classes live in an `Enemy/States/` folder but stay in the `HeroKnightSandbox.Enemy` namespace itself, not a nested `.States`, matching the design doc exactly).
- No automated test framework tasks (unchanged from the movement-controller plan — see `CLAUDE.md`). Verification is: (a) a Unity batch-mode compile check after every code task, (b) manual in-editor playtesting once Task 4 builds the enemies into the scene.
- Compile-check command (used after every code task in Tasks 1–3):

  ```bash
  "D:/Program Files/Unity/2022.3.41f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath "D:/UnityProjects/The Bleeding Crown" -logFile "D:/UnityProjects/The Bleeding Crown/Logs/compile-check.log"
  grep -i "error CS" "D:/UnityProjects/The Bleeding Crown/Logs/compile-check.log"
  ```

  Expected: the `grep` produces no output. **Close the Unity Editor GUI first** — a second instance cannot open the same project.
- Every new `.cs` file gets a paired `.meta` from Unity on the next compile — `git add` both together (already reflected in every task's commit step below).
- **`PatrolPath` placement, and why it's not a required component on the enemy:** the design doc's `EnemyController` intentionally does **not** declare `[RequireComponent(typeof(Platformer.Mechanics.PatrolPath))]`. `PatrolPath.Mover.Position` is computed as `path.transform.TransformPoint(Vector2.Lerp(startPosition, endPosition, p))` (see `Assets/Scripts/Mechanics/PatrolPath.Mover.cs:32-33`) — a *local* offset from `path.transform`. If `path.transform` were the enemy's own transform, and `PatrolState.Tick()` assigns that same transform's position from the mover every frame, the reference frame the oscillation is measured from shifts every tick, and the enemy drifts instead of patrolling a fixed range. The template's own `Platformer.Mechanics.EnemyController.cs:15` avoids this by keeping `path` as a separate serialized reference, not a required component on the moving object. Task 2 below follows that established, already-working pattern: `EnemyController` gets a `[SerializeField] private Platformer.Mechanics.PatrolPath patrolPath` field pointing at a separate, stationary anchor `GameObject` (created in Task 3's scene automation).
- **Vendor `Bandits - Pixel Art` Animator parameters used** (read from `Assets/Bandits - Pixel Art/Animations/Light Bandit/LightBandit_AnimController.controller` and the demo `Bandit.cs`): `AnimState` (int: 0=Idle, 1=CombatIdle, 2=Run), and triggers `Attack`/`Hurt`/`Death`. `Grounded`/`AirSpeed`/`Jump`/`Recover` exist but are never touched — this enemy is ground-only (see the design doc's Non-goals). Facing flips via `transform.localScale.x` (vendor `Bandit.cs` convention: moving right → `localScale.x = -1`; moving left → `localScale.x = 1`), not `SpriteRenderer.flipX`.
- **Task 4 (scene wiring) must be run by the user via the Unity Editor menu, not by an agent.** Precedent from the movement-controller plan: `Assets/Editor/HeroKnightSandboxSetup.cs`'s `[MenuItem]` methods were written by an agent but *run* by the user through the Editor's `HeroKnightSandbox` menu, who then reports console errors/screenshots back. Tasks 1–3 (all C# code, including the new automation method itself) can be done by an agentic worker with the compile-check.

---

### Task 1: Player HP and Hurt state

**Files:**
- Modify: `Assets/Scripts/HeroKnight/HeroKnightContext.cs`
- Create: `Assets/Scripts/HeroKnight/States/HurtState.cs`
- Modify: `Assets/Scripts/HeroKnight/HeroKnightController.cs`

**Interfaces:**
- Produces additions: `HeroKnightContext.MaxHP`/`.CurrentHP`/`.AttackDamage`/`.AttackHitRadius`/`.InvulnerabilityDuration`/`.HurtDuration`/`.InvulnerabilityTimer`/`.IsInvulnerable`. `HeroKnightSandbox.States.HurtState`. `HeroKnightController.Hurt` (type `PlayerState`) and `public void TakeDamage(int amount)`.

- [ ] **Step 1: Add fields to HeroKnightContext**

In `Assets/Scripts/HeroKnight/HeroKnightContext.cs`, add below the existing `public int FacingDirection = 1;` line (currently line 40), before the blank line and `IsGrounded` property:

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

- [ ] **Step 2: Write HurtState**

```csharp
// Assets/Scripts/HeroKnight/States/HurtState.cs
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

- [ ] **Step 3: Wire Hurt, HP initialization, invulnerability countdown, and TakeDamage into HeroKnightController**

In `Assets/Scripts/HeroKnight/HeroKnightController.cs`, add below the existing `public AttackState Attack { get; private set; }` line (currently line 41):

```csharp
        public HurtState Hurt { get; private set; }
```

In `Awake()`, add below the existing `Attack = new AttackState(this, context);` line (currently line 69), still inside `Awake()`:

```csharp
            Hurt = new HurtState(this, context);

            context.CurrentHP = context.MaxHP;
```

Replace the existing `Update()` method (currently lines 77–82):

```csharp
        private void Update()
        {
            context.TimeSinceAttack += Time.deltaTime;
            if (context.InvulnerabilityTimer > 0f)
            {
                context.InvulnerabilityTimer -= Time.deltaTime;
            }

            context.Animator.SetBool("Grounded", context.IsGrounded);
            currentState.Tick();
        }
```

Add a new public method below the existing `ChangeState` method (currently the last method in the class, ending at line 94):

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

- [ ] **Step 4: Compile check**

Run the Global Constraints compile-check command. Expected: no `error CS` lines.

- [ ] **Step 5: Commit**

```bash
git add "Assets/Scripts/HeroKnight/HeroKnightContext.cs" "Assets/Scripts/HeroKnight/States/HurtState.cs" "Assets/Scripts/HeroKnight/States/HurtState.cs.meta" "Assets/Scripts/HeroKnight/HeroKnightController.cs"
git commit -m "feat(hero-knight): add player HP, Hurt state, and TakeDamage"
```

---

### Task 2: Enemy state machine — context, base, registry, states, controller

**Files:**
- Create: `Assets/Scripts/HeroKnight/Enemy/EnemyContext.cs`
- Create: `Assets/Scripts/HeroKnight/Enemy/States/EnemyState.cs`
- Create: `Assets/Scripts/HeroKnight/Enemy/EnemyRegistry.cs`
- Create: `Assets/Scripts/HeroKnight/Enemy/States/PatrolState.cs`
- Create: `Assets/Scripts/HeroKnight/Enemy/States/AttackState.cs`
- Create: `Assets/Scripts/HeroKnight/Enemy/States/HurtState.cs`
- Create: `Assets/Scripts/HeroKnight/Enemy/States/DeadState.cs`
- Create: `Assets/Scripts/HeroKnight/Enemy/EnemyController.cs`

**Interfaces:**
- Consumes: `HeroKnightSandbox.HeroKnightController` (existing, referenced unqualified — `HeroKnightSandbox.Enemy` is nested inside `HeroKnightSandbox`, so no `using` is needed) — including its `public void TakeDamage(int amount)` method, added in Task 1 and called directly from `Enemy.AttackState.Tick()` below; **Task 1 must be completed first**, or this task's code will not compile. `Platformer.Mechanics.PatrolPath` (existing).
- Produces: `HeroKnightSandbox.Enemy.EnemyContext` (public fields: `Transform`, `Animator`, `PatrolPath`, `Player`, `MaxHP`, `CurrentHP`, `MoveSpeed`, `AttackRange`, `AttackDamage`, `AttackWindup`, `AttackCooldown`, `HurtDuration`, `DeathDuration`). `HeroKnightSandbox.Enemy.EnemyState` abstract base. `HeroKnightSandbox.Enemy.EnemyRegistry` static class with `Register`/`Unregister`/`All`. `HeroKnightSandbox.Enemy.PatrolState`/`AttackState`/`HurtState`/`DeadState`. `HeroKnightSandbox.Enemy.EnemyController` — a `MonoBehaviour` with serialized fields `player` (type `HeroKnightController`) and `patrolPath` (type `Platformer.Mechanics.PatrolPath`, wired to a separate anchor object — see Global Constraints), properties `Patrol`/`Attack`/`Hurt`/`Dead` (type `EnemyState`), `public void ChangeState(EnemyState next)`, `public void TakeDamage(int amount)`, `public Vector3 Position`. Task 3 (player `AttackState`) and Task 3 (scene automation) consume `EnemyRegistry.All`/`EnemyController.Position`/`.TakeDamage(int)`/the `player`/`patrolPath` serialized field names by these exact names — do not rename.

- [ ] **Step 1: Write EnemyContext**

```csharp
using UnityEngine;

namespace HeroKnightSandbox.Enemy
{
    /// <summary>
    /// Mutable data shared across all enemy states. Owned and populated by
    /// EnemyController; states read/write it but never own it.
    /// </summary>
    public class EnemyContext
    {
        public Transform Transform;
        public Animator Animator;
        public Platformer.Mechanics.PatrolPath PatrolPath;
        public HeroKnightController Player;

        public int MaxHP = 3;
        public int CurrentHP;
        public float MoveSpeed = 2.0f;
        public float AttackRange = 1.0f;
        public int AttackDamage = 1;
        public float AttackWindup = 0.4f;
        public float AttackCooldown = 0.4f;
        public float HurtDuration = 0.25f;
        public float DeathDuration = 1.0f;
    }
}
```

- [ ] **Step 2: Write EnemyState base**

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

- [ ] **Step 3: Write EnemyRegistry**

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

- [ ] **Step 4: Write PatrolState**

Moves the enemy's transform directly to the anchor's `PatrolPath.Mover` position each tick, flipping facing to match the vendor's own convention, and switches to Attack once the player is within range.

```csharp
using UnityEngine;

namespace HeroKnightSandbox.Enemy
{
    public class PatrolState : EnemyState
    {
        private Platformer.Mechanics.PatrolPath.Mover mover;

        public PatrolState(EnemyController controller, EnemyContext context) : base(controller, context) { }

        public override void Enter()
        {
            mover = Context.PatrolPath.CreateMover(Context.MoveSpeed);
            Context.Animator.SetInteger("AnimState", 2);
        }

        public override void Tick()
        {
            Vector2 target = mover.Position;
            float dx = target.x - Context.Transform.position.x;
            if (Mathf.Abs(dx) > Mathf.Epsilon)
            {
                Vector3 scale = Context.Transform.localScale;
                scale.x = dx > 0f ? -1f : 1f;
                Context.Transform.localScale = scale;
            }

            Context.Transform.position = new Vector3(target.x, target.y, Context.Transform.position.z);

            float distance = Vector2.Distance(Context.Transform.position, Context.Player.transform.position);
            if (distance < Context.AttackRange)
            {
                Controller.ChangeState(Controller.Attack);
            }
        }
    }
}
```

**Corrected after the fact:** this task's review (and the final whole-branch review) both flagged that recreating `mover` fresh on every `Enter()` restarts `PatrolPath.Mover`'s real-time-based oscillation from `PatrolPath.startPosition`, so every re-entry into Patrol from Attack/Hurt snaps the enemy back to its patrol range's start point instead of continuing from its actual current position — noted as plan-mandated at the time (matching this exact code) but deferred to playtesting to confirm it actually mattered. Confirmed during Task 4 playtesting ("enemies reset back to the end of the platform terrain" after being attacked). Fixed in `Enter()` by creating `mover` once, guarded by `if (mover == null)`, instead of unconditionally every call — since `Mover.Position` is a pure function of elapsed real time since creation, keeping the same instance alive across state transitions lets the oscillation continue naturally instead of restarting.

- [ ] **Step 5: Write AttackState**

```csharp
using UnityEngine;

namespace HeroKnightSandbox.Enemy
{
    public class AttackState : EnemyState
    {
        private float timer;
        private bool hasDealtDamage;

        public AttackState(EnemyController controller, EnemyContext context) : base(controller, context) { }

        public override void Enter()
        {
            timer = 0f;
            hasDealtDamage = false;
            Context.Animator.SetTrigger("Attack");
        }

        public override void Tick()
        {
            timer += Time.deltaTime;

            if (!hasDealtDamage && timer >= Context.AttackWindup)
            {
                hasDealtDamage = true;
                float distance = Vector2.Distance(Context.Transform.position, Context.Player.transform.position);
                if (distance < Context.AttackRange)
                {
                    Context.Player.TakeDamage(Context.AttackDamage);
                }
            }

            if (timer >= Context.AttackWindup + Context.AttackCooldown)
            {
                Controller.ChangeState(Controller.Patrol);
            }
        }
    }
}
```

- [ ] **Step 6: Write HurtState**

```csharp
using UnityEngine;

namespace HeroKnightSandbox.Enemy
{
    public class HurtState : EnemyState
    {
        private float timer;

        public HurtState(EnemyController controller, EnemyContext context) : base(controller, context) { }

        public override void Enter()
        {
            timer = 0f;
            Context.Animator.SetTrigger("Hurt");
        }

        public override void Tick()
        {
            timer += Time.deltaTime;
            if (timer >= Context.HurtDuration)
            {
                Controller.ChangeState(Controller.Patrol);
            }
        }
    }
}
```

- [ ] **Step 7: Write DeadState**

```csharp
using UnityEngine;

namespace HeroKnightSandbox.Enemy
{
    public class DeadState : EnemyState
    {
        private float timer;

        public DeadState(EnemyController controller, EnemyContext context) : base(controller, context) { }

        public override void Enter()
        {
            timer = 0f;
            Context.Animator.SetTrigger("Death");
        }

        public override void Tick()
        {
            timer += Time.deltaTime;
            if (timer >= Context.DeathDuration)
            {
                Object.Destroy(Controller.gameObject);
            }
        }
    }
}
```

- [ ] **Step 8: Write EnemyController**

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

No `[RequireComponent]` — `Animator` comes from the vendor `LightBandit.prefab` (already present on its root, wired to `LightBandit_AnimController.controller`), and `PatrolPath` deliberately lives on a separate object (see Global Constraints).

- [ ] **Step 9: Compile check**

Run the Global Constraints compile-check command. Expected: no `error CS` lines.

- [ ] **Step 10: Commit**

```bash
git add "Assets/Scripts/HeroKnight/Enemy/EnemyContext.cs" "Assets/Scripts/HeroKnight/Enemy/EnemyContext.cs.meta" "Assets/Scripts/HeroKnight/Enemy/States/EnemyState.cs" "Assets/Scripts/HeroKnight/Enemy/States/EnemyState.cs.meta" "Assets/Scripts/HeroKnight/Enemy/EnemyRegistry.cs" "Assets/Scripts/HeroKnight/Enemy/EnemyRegistry.cs.meta" "Assets/Scripts/HeroKnight/Enemy/States/PatrolState.cs" "Assets/Scripts/HeroKnight/Enemy/States/PatrolState.cs.meta" "Assets/Scripts/HeroKnight/Enemy/States/AttackState.cs" "Assets/Scripts/HeroKnight/Enemy/States/AttackState.cs.meta" "Assets/Scripts/HeroKnight/Enemy/States/HurtState.cs" "Assets/Scripts/HeroKnight/Enemy/States/HurtState.cs.meta" "Assets/Scripts/HeroKnight/Enemy/States/DeadState.cs" "Assets/Scripts/HeroKnight/Enemy/States/DeadState.cs.meta" "Assets/Scripts/HeroKnight/Enemy/EnemyController.cs" "Assets/Scripts/HeroKnight/Enemy/EnemyController.cs.meta"
git commit -m "feat(hero-knight): add enemy state machine (patrol/attack/hurt/dead)"
```

---

### Task 3: Player attack hit-detection and scene automation

**Files:**
- Modify: `Assets/Scripts/HeroKnight/States/AttackState.cs`
- Modify: `Assets/Editor/HeroKnightSandboxSetup.cs`

**Interfaces:**
- Consumes: `HeroKnightSandbox.Enemy.EnemyRegistry.All`, `EnemyController.Position`/`.TakeDamage(int)`/`EnemyController` (Task 2). `HeroKnightContext.AttackHitRadius`/`.AttackDamage`/`.FacingDirection` (existing/Task 1). `HeroKnightSandbox.HeroKnightController` (existing).
- Produces: enemy hit-detection in the player's `AttackState`. A new `[MenuItem("HeroKnightSandbox/5 Build Enemies")]` method, called from `RunAll()`, that idempotently creates 2 enemies (instantiated from a new `Assets/Prefabs/HeroKnightEnemy.prefab`, itself built from the vendor `LightBandit.prefab`) with patrol anchors in the currently open `HeroKnightSandbox.unity` scene.

- [ ] **Step 1: Add the enemy-hit check to AttackState**

Replace the full contents of `Assets/Scripts/HeroKnight/States/AttackState.cs`:

```csharp
using HeroKnightSandbox.Enemy;
using UnityEngine;

namespace HeroKnightSandbox.States
{
    public class AttackState : PlayerState
    {
        private const float ExitDelay = 0.4f;
        private const float AttackHitWindow = 0.2f;

        private bool hasHitThisSwing;

        public AttackState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        public override void Enter()
        {
            Context.SetVelocityX(0f);
            hasHitThisSwing = false;

            Context.ComboCount++;
            if (Context.ComboCount > 3 || Context.TimeSinceAttack > Context.AttackComboResetWindow)
            {
                Context.ComboCount = 1;
            }

            Context.Animator.SetTrigger("Attack" + Context.ComboCount);
            Context.TimeSinceAttack = 0f;
        }

        public override void Tick()
        {
            if (!hasHitThisSwing && Context.TimeSinceAttack > AttackHitWindow)
            {
                hasHitThisSwing = true;
                TryHitEnemy();
            }

            if (Context.Controls.AttackPressed && Context.TimeSinceAttack > Context.AttackComboWindow)
            {
                Controller.ChangeState(Controller.Attack);
                return;
            }

            if (Context.TimeSinceAttack > ExitDelay)
            {
                Controller.ChangeState(Context.IsGrounded
                    ? (Mathf.Abs(Context.Controls.MoveX) > Mathf.Epsilon ? (PlayerState)Controller.Run : Controller.Idle)
                    : Controller.Fall);
            }
        }

        private void TryHitEnemy()
        {
            foreach (EnemyController enemy in EnemyRegistry.All)
            {
                float dx = enemy.Position.x - Context.Transform.position.x;
                if (Mathf.Abs(dx) <= Context.AttackHitRadius && Mathf.Sign(dx) == Context.FacingDirection)
                {
                    enemy.TakeDamage(Context.AttackDamage);
                    return;
                }
            }
        }
    }
}
```

- [ ] **Step 2: Add the `HeroKnightSandbox.Enemy` using directive and prefab path constants to the automation script**

In `Assets/Editor/HeroKnightSandboxSetup.cs`, add below the existing `using HeroKnightSandbox;` line (currently line 4):

```csharp
using HeroKnightSandbox.Enemy;
```

Add below the existing `private const string PhysicsMaterialPath = ...;` line (currently line 42):

```csharp
    private const string EnemySourcePrefabPath = "Assets/Bandits - Pixel Art/Demo/LightBandit.prefab";
    private const string EnemyDestPrefabPath = "Assets/Prefabs/HeroKnightEnemy.prefab";
```

**Corrected after the fact:** the first implementation of this task used `Assets/Prefabs/Enemy.prefab`, which turned out to already be a pre-existing asset from the base 2D Platformer Microgame template (`Platformer.Mechanics.EnemyController`-based, referenced by `SampleScene.unity`). `PrefabUtility.SaveAsPrefabAsset` overwrote it in place, silently corrupting `SampleScene.unity`'s two enemy instances (their `PrefabInstance` modifications pointed at component `fileID`s that no longer existed once the file's contents were replaced). Found during Task 4 playtesting when checking what the Editor session had touched. Fixed by renaming the destination to a path that doesn't collide with any pre-existing asset, and restoring the original `Assets/Prefabs/Enemy.prefab` from git.

- [ ] **Step 3: Add the enemy prefab builder, enemy-instance creator, and menu method**

Add these three methods below the existing `FinalizeProjectSettings()` method (currently ends at line 486), before the existing `RunAll()` method (currently starts at line 488):

```csharp
    private static GameObject BuildEnemyPrefab()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        GameObject root = PrefabUtility.LoadPrefabContents(EnemySourcePrefabPath);

        var demoScript = root.GetComponent("Bandit");
        if (demoScript != null)
        {
            Object.DestroyImmediate(demoScript, true);
        }

        root.AddComponent<EnemyController>();

        PrefabUtility.SaveAsPrefabAsset(root, EnemyDestPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        return AssetDatabase.LoadAssetAtPath<GameObject>(EnemyDestPrefabPath);
    }

    private static void CreateEnemy(string name, GameObject enemyPrefab, Vector2 anchorPosition,
        Vector2 startOffset, Vector2 endOffset, HeroKnightController player)
    {
        // Destroy-and-recreate rather than skip-if-exists: a rerun must always reconnect
        // to the current enemyPrefab, matching BuildEnemyPrefab()'s own always-rebuild
        // convention for the player prefab.
        GameObject existingEnemy = GameObject.Find(name);
        if (existingEnemy != null)
        {
            Object.DestroyImmediate(existingEnemy);
        }

        GameObject existingAnchor = GameObject.Find(name + "_PatrolAnchor");
        if (existingAnchor != null)
        {
            Object.DestroyImmediate(existingAnchor);
        }

        GameObject anchorGO = new GameObject(name + "_PatrolAnchor");
        anchorGO.transform.position = anchorPosition;
        Platformer.Mechanics.PatrolPath patrolPath = anchorGO.AddComponent<Platformer.Mechanics.PatrolPath>();
        patrolPath.startPosition = startOffset;
        patrolPath.endPosition = endOffset;

        GameObject enemyGO = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab);
        enemyGO.name = name;
        enemyGO.transform.position = anchorPosition;

        EnemyController controller = enemyGO.GetComponent<EnemyController>();
        var so = new SerializedObject(controller);
        so.FindProperty("player").objectReferenceValue = player;
        so.FindProperty("patrolPath").objectReferenceValue = patrolPath;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("HeroKnightSandbox/5 Build Enemies")]
    public static void BuildEnemies()
    {
        GameObject player = GameObject.Find("HeroKnight");
        if (player == null)
        {
            throw new System.Exception("HeroKnight instance not found in the open scene - run '3 Build Scene' first");
        }

        HeroKnightController controller = player.GetComponent<HeroKnightController>();
        GameObject enemyPrefab = BuildEnemyPrefab();

        // Both on the flat Ground platform (top at y=0, spans x -6..18 - see BuildScene()),
        // spread apart so one can be tested in isolation before walking further to reach
        // both at once, without needing a jump/wall-slide/ledge-grab to reach either.
        CreateEnemy("Enemy_1", enemyPrefab, new Vector2(4f, 0.5f), new Vector2(-1.5f, 0f), new Vector2(1.5f, 0f), controller);
        CreateEnemy("Enemy_2", enemyPrefab, new Vector2(11f, 0.5f), new Vector2(-1.5f, 0f), new Vector2(1.5f, 0f), controller);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("HeroKnightSandboxSetup: enemies built");
    }
```

`BuildEnemyPrefab()` always rebuilds `Enemy.prefab` from the vendor source on every call (no existence check), matching `BuildPrefab()`'s own convention for the player prefab — safe to re-run, and any already-placed scene instances stay linked to the same asset path/GUID and pick up the rebuilt version automatically.

- [ ] **Step 4: Call BuildEnemies from RunAll**

Replace the existing `RunAll()` method (currently lines 488–495):

```csharp
    [MenuItem("HeroKnightSandbox/Run All")]
    public static void RunAll()
    {
        BuildPrefab();
        AddLedgeGrabToAnimator();
        BuildScene();
        FinalizeProjectSettings();
        BuildEnemies();
    }
```

- [ ] **Step 5: Compile check**

Run the Global Constraints compile-check command. Expected: no `error CS` lines. This completes all code for the enemy feature — every field/method the design doc calls for now exists.

- [ ] **Step 6: Commit**

```bash
git add "Assets/Scripts/HeroKnight/States/AttackState.cs" "Assets/Editor/HeroKnightSandboxSetup.cs"
git commit -m "feat(hero-knight): add enemy hit-detection and scene-build automation"
```

---

### Task 4: Build enemies into the scene and playtest (manual — Unity Editor GUI + Play mode)

**No agent can perform this task in this environment** — it requires the Unity Editor GUI. Everything referenced below was created in Tasks 1–3.

- [ ] **Step 1: Run the automation**
  - Open the project in the Unity Editor, with `Assets/Scenes/HeroKnightSandbox.unity` open (or run `HeroKnightSandbox > Run All` from scratch, which now includes the new step).
  - Run `HeroKnightSandbox > 5 Build Enemies` from the menu bar.
  - Confirm the Console shows `HeroKnightSandboxSetup: enemies built` with no errors, and the Hierarchy now has `Enemy_1`, `Enemy_1_PatrolAnchor`, `Enemy_2`, `Enemy_2_PatrolAnchor` under the scene root, each `Enemy_N` showing the real Light Bandit sprite (not a placeholder capsule).

- [ ] **Step 2: Playtest — Patrol**
  - Enter Play mode without approaching either enemy. Confirm each patrols back and forth between its anchor's two endpoints at a steady speed, playing its Run animation and flipping to face its current movement direction, without drifting off that range over time (this is the exact failure mode the Global Constraints section's `PatrolPath` note calls out — if you see drift, it means `patrolPath` ended up wired to the enemy's own transform instead of its anchor).

- [ ] **Step 3: Playtest — Enemy attacks player**
  - Walk the player within range of `Enemy_1`. Confirm it stops patrolling, plays its Attack animation, and after the windup delay the player's HP visibly matters (add a temporary `Debug.Log(context.CurrentHP)` in `HeroKnightController.TakeDamage` if there's no on-screen HP display yet — this design intentionally has no HUD) and the player flinches into Hurt (velocity zeroed, `Hurt` Animator clip visible).
  - Stand in range for multiple enemy attack cycles; confirm a second hit within `InvulnerabilityDuration` of the first does not double-decrement HP.

- [ ] **Step 4: Playtest — Player attacks enemy**
  - Attack combo into `Enemy_1` from the front (facing it). Confirm it plays its Hurt animation per hit, `CurrentHP` depletes (add a temporary `Debug.Log` in `EnemyController.TakeDamage` if needed), and after `MaxHP` (3) hits it plays its Death animation and its `GameObject` is destroyed once `DeathDuration` elapses.
  - Confirm attacking while facing *away* from the enemy does not hit it (the `Mathf.Sign(dx) == Context.FacingDirection` check).

- [ ] **Step 5: Playtest — mutual exclusivity and two enemies**
  - Confirm the player cannot Roll/Block while in Hurt, and taking damage while Attacking/Rolling/Blocking correctly interrupts into Hurt.
  - Walk far enough to have both `Enemy_1` and `Enemy_2` in play at once; confirm attacking hits only the intended one (the nearest one in the facing direction) and both can independently patrol/attack/die.

- [ ] **Step 6: Check the Hurt animator transition**
  - While doing Step 3, watch whether the Animator's existing player `Hurt` state transitions back out on its own once `HurtState.Tick()` changes the C# state away from Hurt. If it gets stuck showing the Hurt clip after the C# state has already moved on: open `Assets/Hero Knight - Pixel Art/Animations/HeroKnight_AnimController.controller` and check `Hurt`'s outgoing transitions — if none exist, add one Any-State-style explicit exit the same way `HeroKnightSandboxSetup.AddLedgeGrabExitTransition` already does for `LedgeGrab` (dedicated trigger parameter, `hasExitTime = false`, `duration = 0`), rather than reusing `AnimState`/`Grounded` conditions (see the comment above `AddLedgeGrabExitTransition` in that file for why those two false-fired early last time). The *enemy*'s Animator Controller needs no such check — it's the vendor's own, already fully wired and proven by the working demo.

- [ ] **Step 7: Remove temporary debug logging and tune by feel**
  - Remove any `Debug.Log` calls added in Steps 3–4.
  - Tune `EnemyContext`/`HeroKnightContext` values (`AttackRange`, `AttackDamage` both sides, `MaxHP` both sides, `InvulnerabilityDuration`, `HurtDuration` (player side), `AttackWindup`/`AttackCooldown`) if anything felt off.

- [ ] **Step 8: Commit**

```bash
git add "Assets/Scenes/HeroKnightSandbox.unity" "Assets/Prefabs/HeroKnightEnemy.prefab" "Assets/Prefabs/HeroKnightEnemy.prefab.meta"
git commit -m "feat(hero-knight): build enemies into sandbox scene and tune from playtesting"
```

(If Step 6 required an Animator Controller change, also add `Assets/Hero Knight - Pixel Art/Animations/HeroKnight_AnimController.controller` to this commit.)
