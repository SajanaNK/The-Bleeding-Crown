# Hero Knight Movement & Combat Sandbox Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a mobile-ready Hero Knight character controller (run/jump/wall-slide/ledge-grab/roll/block/3-hit attack combo) driven by on-screen touch controls, in a dedicated sandbox scene, following the design in `docs/superpowers/specs/2026-07-18-hero-knight-sandbox-design.md`.

**Architecture:** A standalone `HeroKnight` namespace (separate from the template's `Platformer.*`/`Simulation` framework) implementing an explicit state-machine player controller. Movement/combat logic lives in `PlayerState` subclasses reading a shared `HeroKnightContext`; touch input is read only through an `IHeroKnightInput` interface so the UI layer (`VirtualJoystick`, `TouchButton`, `TouchHeroKnightInput`) is fully decoupled from movement logic.

**Tech Stack:** Unity 2022.3.41f1, URP, legacy UI (`UnityEngine.UI` + `UnityEngine.EventSystems`, no Input System package), Cinemachine 2.10.1 (already a project dependency).

## Global Constraints

- Unity Editor version: `2022.3.41f1` (must match `ProjectSettings/ProjectVersion.txt`).
- New code lives under `Assets/Scripts/HeroKnight/`, namespaced `HeroKnight.*` — kept separate from `Platformer.*` per the design's explicit non-goal of integrating with `Simulation`.
- No Input System package is installed — touch input uses `UnityEngine.EventSystems` UI interfaces (`IPointerDownHandler` etc.), not `Input.*`.
- No automated test framework tasks — this project has no NUnit tests set up (confirmed in `CLAUDE.md`) and the design's Non-goals explicitly exclude adding one. Verification is: (a) a Unity batch-mode compile check for every code task, and (b) manual in-editor playtesting once the scene exists (Tasks 11–12).
- Compile-check command (used after every code task in Tasks 1–10):

  ```bash
  "D:/Program Files/Unity/2022.3.41f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath "D:/UnityProjects/The Bleeding Crown" -logFile "D:/UnityProjects/The Bleeding Crown/Logs/compile-check.log"
  grep -i "error CS" "D:/UnityProjects/The Bleeding Crown/Logs/compile-check.log"
  ```

  Expected: the `grep` produces no output (no `error CS` lines). This can take 1–3 minutes per run. **The Unity Editor GUI must be closed first** — a second instance cannot open the same project and the batch run will fail to acquire the project lock.
- **Tasks 11 and 12 require the Unity Editor GUI (scene/prefab/Canvas layout, Tilemap painting, Animator Controller graph editing) and must be performed by a human.** No Unity MCP/editor-automation tool is available in this environment for an agent to drive the Editor directly — do not attempt to hand-edit `.unity`/`.prefab`/`.controller` YAML to fake this, it's too fragile. Tasks 1–10 (all C# code) can be done by an agentic worker.

---

### Task 1: Touch input contract

**Files:**
- Create: `Assets/Scripts/HeroKnight/Input/IHeroKnightInput.cs`

**Interfaces:**
- Produces: `HeroKnight.Input.IHeroKnightInput` with members `float MoveX`, `bool JumpPressed`, `bool AttackPressed`, `bool BlockHeld`, `bool RollPressed`.

- [ ] **Step 1: Write the interface**

```csharp
namespace HeroKnight.Input
{
    /// <summary>
    /// Abstracts the Hero Knight's control source. Movement/combat states read only
    /// this interface, never a concrete input implementation, so the input source
    /// (touch today, potentially keyboard/gamepad later) can change independently.
    /// </summary>
    public interface IHeroKnightInput
    {
        float MoveX { get; }
        bool JumpPressed { get; }
        bool AttackPressed { get; }
        bool BlockHeld { get; }
        bool RollPressed { get; }
    }
}
```

- [ ] **Step 2: Compile check**

Run the Global Constraints compile-check command. Expected: no `error CS` lines.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Scripts/HeroKnight/Input/IHeroKnightInput.cs"
git commit -m "feat(hero-knight): add IHeroKnightInput contract"
```

---

### Task 2: Touch UI primitives (joystick + button)

**Files:**
- Create: `Assets/Scripts/HeroKnight/UI/VirtualJoystick.cs`
- Create: `Assets/Scripts/HeroKnight/UI/TouchButton.cs`

**Interfaces:**
- Produces: `HeroKnight.UI.VirtualJoystick` with `Vector2 Direction { get; }` (normalized, `.x` in `[-1, 1]`), and serialized fields `background`/`handle`/`handleRange` (assigned in the Editor in Task 11).
- Produces: `HeroKnight.UI.TouchButton` with `bool IsHeld { get; }` (true while pressed) and `bool WasPressedThisFrame { get; }` (true for exactly one frame on press) — the same component serves one-shot buttons (Jump/Attack/Roll use `WasPressedThisFrame`) and the hold button (Block uses `IsHeld`).

- [ ] **Step 1: Write VirtualJoystick**

```csharp
using UnityEngine;
using UnityEngine.EventSystems;

namespace HeroKnight.UI
{
    /// <summary>
    /// Drag-based on-screen joystick. Drag anywhere on `background`; the `handle`
    /// follows, clamped to `handleRange`, and Direction reports the clamped
    /// offset normalized to [-1, 1] on each axis.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;
        [SerializeField] private float handleRange = 100f;

        public Vector2 Direction { get; private set; } = Vector2.zero;

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                Vector2 clamped = Vector2.ClampMagnitude(localPoint, handleRange);
                handle.anchoredPosition = clamped;
                Direction = clamped / handleRange;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            handle.anchoredPosition = Vector2.zero;
            Direction = Vector2.zero;
        }
    }
}
```

- [ ] **Step 2: Write TouchButton**

```csharp
using UnityEngine;
using UnityEngine.EventSystems;

namespace HeroKnight.UI
{
    /// <summary>
    /// One-shot press (WasPressedThisFrame) and press-and-hold (IsHeld) in a single
    /// component, so the same class backs Jump/Attack/Roll (one-shot) and Block (hold).
    /// </summary>
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool IsHeld { get; private set; }
        public bool WasPressedThisFrame { get; private set; }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsHeld = true;
            WasPressedThisFrame = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsHeld = false;
        }

        private void LateUpdate()
        {
            // Cleared after all Update()s this frame have had a chance to observe it.
            WasPressedThisFrame = false;
        }
    }
}
```

- [ ] **Step 3: Compile check**

Run the Global Constraints compile-check command. Expected: no `error CS` lines.

- [ ] **Step 4: Commit**

```bash
git add "Assets/Scripts/HeroKnight/UI/VirtualJoystick.cs" "Assets/Scripts/HeroKnight/UI/TouchButton.cs"
git commit -m "feat(hero-knight): add virtual joystick and touch button UI primitives"
```

---

### Task 3: Touch input adapter

**Files:**
- Create: `Assets/Scripts/HeroKnight/Input/TouchHeroKnightInput.cs`

**Interfaces:**
- Consumes: `HeroKnight.UI.VirtualJoystick.Direction`, `HeroKnight.UI.TouchButton.IsHeld`/`WasPressedThisFrame` (Task 2); implements `HeroKnight.Input.IHeroKnightInput` (Task 1).
- Produces: `HeroKnight.Input.TouchHeroKnightInput`, a `MonoBehaviour` with serialized fields `joystick`, `jumpButton`, `attackButton`, `blockButton`, `rollButton` (assigned in the Editor in Task 11).

- [ ] **Step 1: Write TouchHeroKnightInput**

```csharp
using HeroKnight.UI;
using UnityEngine;

namespace HeroKnight.Input
{
    public class TouchHeroKnightInput : MonoBehaviour, IHeroKnightInput
    {
        [SerializeField] private VirtualJoystick joystick;
        [SerializeField] private TouchButton jumpButton;
        [SerializeField] private TouchButton attackButton;
        [SerializeField] private TouchButton blockButton;
        [SerializeField] private TouchButton rollButton;

        public float MoveX => joystick != null ? joystick.Direction.x : 0f;
        public bool JumpPressed => jumpButton != null && jumpButton.WasPressedThisFrame;
        public bool AttackPressed => attackButton != null && attackButton.WasPressedThisFrame;
        public bool BlockHeld => blockButton != null && blockButton.IsHeld;
        public bool RollPressed => rollButton != null && rollButton.WasPressedThisFrame;
    }
}
```

- [ ] **Step 2: Compile check**

Run the Global Constraints compile-check command. Expected: no `error CS` lines.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Scripts/HeroKnight/Input/TouchHeroKnightInput.cs"
git commit -m "feat(hero-knight): add touch input adapter"
```

---

### Task 4: Relocated sensor component

**Files:**
- Create: `Assets/Scripts/HeroKnight/Sensors/Sensor_HeroKnight.cs`

**Interfaces:**
- Produces: `HeroKnight.Sensors.Sensor_HeroKnight` with `bool State()` (true while a trigger collider overlaps it and it isn't disabled) and `void Disable(float duration)`.

- [ ] **Step 1: Write the sensor**

This is our own copy of the asset pack's `Sensor_HeroKnight.cs` (`Assets/Hero Knight - Pixel Art/Demo/Sensor_HeroKnight.cs`), placed under our own namespace so it isn't tangled with the vendor demo folder. Logic is unchanged from the original — it's a trigger-overlap counter with a timed disable, already proven by the demo (used for ground and wall detection); this same component will also back the two new ledge sensors added in Task 7.

```csharp
using UnityEngine;

namespace HeroKnight.Sensors
{
    public class Sensor_HeroKnight : MonoBehaviour
    {
        private int m_ColCount = 0;
        private float m_DisableTimer;

        private void OnEnable()
        {
            m_ColCount = 0;
        }

        public bool State()
        {
            if (m_DisableTimer > 0)
                return false;
            return m_ColCount > 0;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            m_ColCount++;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            m_ColCount--;
        }

        private void Update()
        {
            m_DisableTimer -= Time.deltaTime;
        }

        public void Disable(float duration)
        {
            m_DisableTimer = duration;
        }
    }
}
```

- [ ] **Step 2: Compile check**

Run the Global Constraints compile-check command. Expected: no `error CS` lines. (Two classes named `Sensor_HeroKnight` now exist in different namespaces — the original in the global namespace under the Demo folder, ours under `HeroKnight.Sensors` — this is not a conflict since their fully-qualified names differ.)

- [ ] **Step 3: Commit**

```bash
git add "Assets/Scripts/HeroKnight/Sensors/Sensor_HeroKnight.cs"
git commit -m "feat(hero-knight): add namespaced sensor component"
```

---

### Task 5: Foundations — context, state machine base, Idle/Run

**Files:**
- Create: `Assets/Scripts/HeroKnight/HeroKnightContext.cs`
- Create: `Assets/Scripts/HeroKnight/States/PlayerState.cs`
- Create: `Assets/Scripts/HeroKnight/States/IdleState.cs`
- Create: `Assets/Scripts/HeroKnight/States/RunState.cs`
- Create: `Assets/Scripts/HeroKnight/HeroKnightController.cs`

**Interfaces:**
- Consumes: `HeroKnight.Input.IHeroKnightInput` (Task 1), `HeroKnight.Sensors.Sensor_HeroKnight` (Task 4), `HeroKnight.Input.TouchHeroKnightInput` (Task 3, referenced by concrete type only in `HeroKnightController`'s serialized field, since Unity can't serialize interface references).
- Produces: `HeroKnight.HeroKnightContext` (public fields: `Body`, `Animator`, `SpriteRenderer`, `Transform`, `Controls`, `GroundSensor`, `MoveSpeed`, `FacingDirection`; methods `UpdateFacing()`, `SetVelocityX(float)`; property `IsGrounded`). `HeroKnight.States.PlayerState` abstract base (`Enter()`, `Tick()`, `FixedTick()`, `Exit()`, all virtual/no-op by default; `protected Controller`, `protected Context`). `HeroKnight.HeroKnightController` with public properties `Idle`, `Run` (type `PlayerState`) and `public void ChangeState(PlayerState next)`. These property names (`Idle`, `Run`, and later `Jump`/`Fall`/`WallSlide`/`LedgeGrab`/`Roll`/`Block`/`Attack`) are the exact names every later task's states use to trigger transitions — do not rename them.

- [ ] **Step 1: Write HeroKnightContext**

```csharp
using HeroKnight.Input;
using HeroKnight.Sensors;
using UnityEngine;

namespace HeroKnight
{
    /// <summary>
    /// Mutable data shared across all player states. Owned and populated by
    /// HeroKnightController; states read/write it but never own it.
    /// </summary>
    public class HeroKnightContext
    {
        public Rigidbody2D Body;
        public Animator Animator;
        public SpriteRenderer SpriteRenderer;
        public Transform Transform;
        public IHeroKnightInput Controls;

        public Sensor_HeroKnight GroundSensor;

        public float MoveSpeed = 4.0f;
        public int FacingDirection = 1;

        public bool IsGrounded => GroundSensor.State();

        public void UpdateFacing()
        {
            if (Controls.MoveX > 0)
            {
                FacingDirection = 1;
                SpriteRenderer.flipX = false;
            }
            else if (Controls.MoveX < 0)
            {
                FacingDirection = -1;
                SpriteRenderer.flipX = true;
            }
        }

        public void SetVelocityX(float x)
        {
            Body.velocity = new Vector2(x, Body.velocity.y);
        }
    }
}
```

- [ ] **Step 2: Write PlayerState base**

```csharp
namespace HeroKnight.States
{
    public abstract class PlayerState
    {
        protected readonly HeroKnightController Controller;
        protected readonly HeroKnightContext Context;

        protected PlayerState(HeroKnightController controller, HeroKnightContext context)
        {
            Controller = controller;
            Context = context;
        }

        public virtual void Enter() { }
        public virtual void Tick() { }
        public virtual void FixedTick() { }
        public virtual void Exit() { }
    }
}
```

- [ ] **Step 3: Write IdleState and RunState**

```csharp
// Assets/Scripts/HeroKnight/States/IdleState.cs
using UnityEngine;

namespace HeroKnight.States
{
    public class IdleState : PlayerState
    {
        public IdleState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        public override void Enter()
        {
            Context.SetVelocityX(0f);
            Context.Animator.SetInteger("AnimState", 0);
        }

        public override void Tick()
        {
            Context.UpdateFacing();

            if (Mathf.Abs(Context.Controls.MoveX) > Mathf.Epsilon)
            {
                Controller.ChangeState(Controller.Run);
            }
        }
    }
}
```

```csharp
// Assets/Scripts/HeroKnight/States/RunState.cs
using UnityEngine;

namespace HeroKnight.States
{
    public class RunState : PlayerState
    {
        public RunState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        public override void Enter()
        {
            Context.Animator.SetInteger("AnimState", 1);
        }

        public override void Tick()
        {
            Context.UpdateFacing();

            if (Mathf.Abs(Context.Controls.MoveX) <= Mathf.Epsilon)
            {
                Controller.ChangeState(Controller.Idle);
            }
        }

        public override void FixedTick()
        {
            Context.SetVelocityX(Context.Controls.MoveX * Context.MoveSpeed);
        }
    }
}
```

- [ ] **Step 4: Write HeroKnightController**

```csharp
using HeroKnight.Input;
using HeroKnight.Sensors;
using HeroKnight.States;
using UnityEngine;

namespace HeroKnight
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class HeroKnightController : MonoBehaviour
    {
        [SerializeField] private TouchHeroKnightInput input;
        [SerializeField] private Sensor_HeroKnight groundSensor;

        private HeroKnightContext context;
        private PlayerState currentState;

        public IdleState Idle { get; private set; }
        public RunState Run { get; private set; }

        private void Awake()
        {
            context = new HeroKnightContext
            {
                Body = GetComponent<Rigidbody2D>(),
                Animator = GetComponent<Animator>(),
                SpriteRenderer = GetComponent<SpriteRenderer>(),
                Transform = transform,
                Controls = input,
                GroundSensor = groundSensor,
            };

            Idle = new IdleState(this, context);
            Run = new RunState(this, context);
        }

        private void Start()
        {
            ChangeState(Idle);
        }

        private void Update()
        {
            currentState.Tick();
        }

        private void FixedUpdate()
        {
            currentState.FixedTick();
        }

        public void ChangeState(PlayerState next)
        {
            currentState?.Exit();
            currentState = next;
            currentState.Enter();
        }
    }
}
```

- [ ] **Step 5: Compile check**

Run the Global Constraints compile-check command. Expected: no `error CS` lines. (No scene exists yet, so this task's only verification is the compile check — playtesting starts once Task 11 builds the scene.)

- [ ] **Step 6: Commit**

```bash
git add "Assets/Scripts/HeroKnight/HeroKnightContext.cs" "Assets/Scripts/HeroKnight/States/PlayerState.cs" "Assets/Scripts/HeroKnight/States/IdleState.cs" "Assets/Scripts/HeroKnight/States/RunState.cs" "Assets/Scripts/HeroKnight/HeroKnightController.cs"
git commit -m "feat(hero-knight): add state machine foundation with Idle/Run states"
```

---

### Task 6: Jump, Fall, WallSlide

**Files:**
- Create: `Assets/Scripts/HeroKnight/States/JumpState.cs`
- Create: `Assets/Scripts/HeroKnight/States/FallState.cs`
- Create: `Assets/Scripts/HeroKnight/States/WallSlideState.cs`
- Modify: `Assets/Scripts/HeroKnight/HeroKnightContext.cs`
- Modify: `Assets/Scripts/HeroKnight/HeroKnightController.cs`
- Modify: `Assets/Scripts/HeroKnight/States/IdleState.cs`
- Modify: `Assets/Scripts/HeroKnight/States/RunState.cs`

**Interfaces:**
- Produces additions: `HeroKnightContext.JumpForce`, `.WallSensorR1/.WallSensorR2/.WallSensorL1/.WallSensorL2` (type `Sensor_HeroKnight`), `.IsWallSliding` (bool property). `HeroKnightController.Jump`/`.Fall`/`.WallSlide` (type `PlayerState`).

- [ ] **Step 1: Add fields to HeroKnightContext**

In `Assets/Scripts/HeroKnight/HeroKnightContext.cs`, add below the existing `GroundSensor` field:

```csharp
        public Sensor_HeroKnight WallSensorR1;
        public Sensor_HeroKnight WallSensorR2;
        public Sensor_HeroKnight WallSensorL1;
        public Sensor_HeroKnight WallSensorL2;
```

And below the existing `MoveSpeed` field:

```csharp
        public float JumpForce = 7.5f;
```

And below the existing `IsGrounded` property:

```csharp
        public bool IsWallSliding =>
            (WallSensorR1.State() && WallSensorR2.State()) ||
            (WallSensorL1.State() && WallSensorL2.State());
```

- [ ] **Step 2: Write JumpState**

```csharp
// Assets/Scripts/HeroKnight/States/JumpState.cs
using UnityEngine;

namespace HeroKnight.States
{
    public class JumpState : PlayerState
    {
        public JumpState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        public override void Enter()
        {
            Context.Animator.SetTrigger("Jump");
            Context.Animator.SetBool("Grounded", false);
            Context.Body.velocity = new Vector2(Context.Body.velocity.x, Context.JumpForce);
            Context.GroundSensor.Disable(0.2f);
        }

        public override void Tick()
        {
            Context.UpdateFacing();
            Context.Animator.SetFloat("AirSpeedY", Context.Body.velocity.y);

            if (Context.Body.velocity.y <= 0f)
            {
                Controller.ChangeState(Controller.Fall);
            }
        }

        public override void FixedTick()
        {
            Context.SetVelocityX(Context.Controls.MoveX * Context.MoveSpeed);
        }
    }
}
```

- [ ] **Step 3: Write FallState**

```csharp
// Assets/Scripts/HeroKnight/States/FallState.cs
using UnityEngine;

namespace HeroKnight.States
{
    public class FallState : PlayerState
    {
        public FallState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        public override void Tick()
        {
            Context.UpdateFacing();
            Context.Animator.SetFloat("AirSpeedY", Context.Body.velocity.y);

            if (Context.IsGrounded)
            {
                Context.Animator.SetBool("Grounded", true);
                Controller.ChangeState(Mathf.Abs(Context.Controls.MoveX) > Mathf.Epsilon
                    ? (PlayerState)Controller.Run
                    : Controller.Idle);
                return;
            }

            if (Context.IsWallSliding)
            {
                Controller.ChangeState(Controller.WallSlide);
            }
        }

        public override void FixedTick()
        {
            Context.SetVelocityX(Context.Controls.MoveX * Context.MoveSpeed);
        }
    }
}
```

- [ ] **Step 4: Write WallSlideState**

```csharp
// Assets/Scripts/HeroKnight/States/WallSlideState.cs
using UnityEngine;

namespace HeroKnight.States
{
    public class WallSlideState : PlayerState
    {
        private const float MaxSlideSpeed = -1.5f;

        public WallSlideState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        public override void Enter()
        {
            Context.Animator.SetBool("WallSlide", true);
        }

        public override void Exit()
        {
            Context.Animator.SetBool("WallSlide", false);
        }

        public override void Tick()
        {
            if (Context.IsGrounded)
            {
                Controller.ChangeState(Controller.Idle);
                return;
            }

            if (!Context.IsWallSliding)
            {
                Controller.ChangeState(Controller.Fall);
            }
        }

        public override void FixedTick()
        {
            float clampedY = Context.Body.velocity.y < MaxSlideSpeed ? MaxSlideSpeed : Context.Body.velocity.y;
            Context.Body.velocity = new Vector2(0f, clampedY);
        }
    }
}
```

- [ ] **Step 5: Wire the new states and sensors into HeroKnightController**

In `Assets/Scripts/HeroKnight/HeroKnightController.cs`, add below the existing `groundSensor` field:

```csharp
        [SerializeField] private Sensor_HeroKnight wallSensorR1;
        [SerializeField] private Sensor_HeroKnight wallSensorR2;
        [SerializeField] private Sensor_HeroKnight wallSensorL1;
        [SerializeField] private Sensor_HeroKnight wallSensorL2;
```

Add below the existing `Run` property:

```csharp
        public JumpState Jump { get; private set; }
        public FallState Fall { get; private set; }
        public WallSlideState WallSlide { get; private set; }
```

In `Awake()`, add `WallSensorR1 = wallSensorR1, WallSensorR2 = wallSensorR2, WallSensorL1 = wallSensorL1, WallSensorL2 = wallSensorL2,` to the `HeroKnightContext` initializer (after `GroundSensor = groundSensor,`), and add below the existing `Run = new RunState(this, context);` line:

```csharp
            Jump = new JumpState(this, context);
            Fall = new FallState(this, context);
            WallSlide = new WallSlideState(this, context);
```

- [ ] **Step 6: Add jump/fall transitions to IdleState and RunState**

In `Assets/Scripts/HeroKnight/States/IdleState.cs`, replace the `Tick()` method:

```csharp
        public override void Tick()
        {
            Context.UpdateFacing();

            if (!Context.IsGrounded)
            {
                Controller.ChangeState(Controller.Fall);
                return;
            }

            if (Context.Controls.JumpPressed)
            {
                Controller.ChangeState(Controller.Jump);
                return;
            }

            if (Mathf.Abs(Context.Controls.MoveX) > Mathf.Epsilon)
            {
                Controller.ChangeState(Controller.Run);
            }
        }
```

In `Assets/Scripts/HeroKnight/States/RunState.cs`, replace the `Tick()` method:

```csharp
        public override void Tick()
        {
            Context.UpdateFacing();

            if (!Context.IsGrounded)
            {
                Controller.ChangeState(Controller.Fall);
                return;
            }

            if (Context.Controls.JumpPressed)
            {
                Controller.ChangeState(Controller.Jump);
                return;
            }

            if (Mathf.Abs(Context.Controls.MoveX) <= Mathf.Epsilon)
            {
                Controller.ChangeState(Controller.Idle);
            }
        }
```

- [ ] **Step 7: Compile check**

Run the Global Constraints compile-check command. Expected: no `error CS` lines.

- [ ] **Step 8: Commit**

```bash
git add "Assets/Scripts/HeroKnight/States/JumpState.cs" "Assets/Scripts/HeroKnight/States/FallState.cs" "Assets/Scripts/HeroKnight/States/WallSlideState.cs" "Assets/Scripts/HeroKnight/HeroKnightContext.cs" "Assets/Scripts/HeroKnight/HeroKnightController.cs" "Assets/Scripts/HeroKnight/States/IdleState.cs" "Assets/Scripts/HeroKnight/States/RunState.cs"
git commit -m "feat(hero-knight): add jump, fall, and wall-slide states"
```

---

### Task 7: Ledge-grab

**Files:**
- Create: `Assets/Scripts/HeroKnight/States/LedgeGrabState.cs`
- Modify: `Assets/Scripts/HeroKnight/HeroKnightContext.cs`
- Modify: `Assets/Scripts/HeroKnight/HeroKnightController.cs`
- Modify: `Assets/Scripts/HeroKnight/States/FallState.cs`
- Modify: `Assets/Scripts/HeroKnight/States/WallSlideState.cs`

**Interfaces:**
- Produces: `HeroKnight.States.LedgeGrabState.CanGrab(HeroKnightContext)` (static, used by `FallState`/`WallSlideState`). `HeroKnightContext.LedgeSensorR/.LedgeSensorL` (type `Sensor_HeroKnight`), `.LedgeClimbOffset` (`Vector2`). `HeroKnightController.LedgeGrab` (type `PlayerState`).

This is the one move with no existing implementation to build on — see the design doc's "Ledge-grab (new logic)" section. Detection: while falling or wall-sliding, if a wall sensor pair is active but the ledge sensor above it is not, the wall ends here — grab it. Climb-up (Jump button) repositions the character by `LedgeClimbOffset` and goes to Idle (no dedicated climb animation exists in the asset pack — this is a deliberate snap, confirmed acceptable in the design). Drop (Roll button) restores normal physics and falls.

- [ ] **Step 1: Add fields to HeroKnightContext**

In `Assets/Scripts/HeroKnight/HeroKnightContext.cs`, add below the existing wall sensor fields:

```csharp
        public Sensor_HeroKnight LedgeSensorR;
        public Sensor_HeroKnight LedgeSensorL;
```

Add below the existing `JumpForce` field:

```csharp
        public Vector2 LedgeClimbOffset = new Vector2(0.3f, 1.1f);
```

- [ ] **Step 2: Write LedgeGrabState**

```csharp
// Assets/Scripts/HeroKnight/States/LedgeGrabState.cs
using UnityEngine;

namespace HeroKnight.States
{
    public class LedgeGrabState : PlayerState
    {
        private const float RegrabCooldown = 0.3f;

        public LedgeGrabState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        /// <summary>
        /// A ledge is grabbable when a wall sensor pair is touching a wall but the
        /// ledge sensor positioned above it is clear — the wall ends at this height.
        /// </summary>
        public static bool CanGrab(HeroKnightContext context)
        {
            bool rightLedge = context.WallSensorR1.State() && context.WallSensorR2.State() && !context.LedgeSensorR.State();
            bool leftLedge = context.WallSensorL1.State() && context.WallSensorL2.State() && !context.LedgeSensorL.State();
            return rightLedge || leftLedge;
        }

        public override void Enter()
        {
            Context.Body.bodyType = RigidbodyType2D.Kinematic;
            Context.Body.velocity = Vector2.zero;
            Context.Animator.SetTrigger("LedgeGrab");
        }

        public override void Exit()
        {
            Context.Body.bodyType = RigidbodyType2D.Dynamic;
        }

        public override void Tick()
        {
            if (Context.Controls.JumpPressed)
            {
                Vector3 offset = new Vector3(
                    Context.LedgeClimbOffset.x * Context.FacingDirection,
                    Context.LedgeClimbOffset.y,
                    0f);
                Context.Transform.position += offset;
                Controller.ChangeState(Controller.Idle);
                return;
            }

            if (Context.Controls.RollPressed)
            {
                Context.LedgeSensorR.Disable(RegrabCooldown);
                Context.LedgeSensorL.Disable(RegrabCooldown);
                Controller.ChangeState(Controller.Fall);
            }
        }
    }
}
```

- [ ] **Step 3: Wire LedgeGrab and its sensors into HeroKnightController**

In `Assets/Scripts/HeroKnight/HeroKnightController.cs`, add below the existing wall sensor fields:

```csharp
        [SerializeField] private Sensor_HeroKnight ledgeSensorR;
        [SerializeField] private Sensor_HeroKnight ledgeSensorL;
```

Add below the existing `WallSlide` property:

```csharp
        public LedgeGrabState LedgeGrab { get; private set; }
```

In `Awake()`, add `LedgeSensorR = ledgeSensorR, LedgeSensorL = ledgeSensorL,` to the `HeroKnightContext` initializer, and add below the existing `WallSlide = new WallSlideState(this, context);` line:

```csharp
            LedgeGrab = new LedgeGrabState(this, context);
```

- [ ] **Step 4: Add the ledge-grab check to FallState**

In `Assets/Scripts/HeroKnight/States/FallState.cs`, in `Tick()`, insert this check between the `IsGrounded` block and the `IsWallSliding` check:

```csharp
            if (LedgeGrabState.CanGrab(Context))
            {
                Controller.ChangeState(Controller.LedgeGrab);
                return;
            }
```

The full `Tick()` method should now read:

```csharp
        public override void Tick()
        {
            Context.UpdateFacing();
            Context.Animator.SetFloat("AirSpeedY", Context.Body.velocity.y);

            if (Context.IsGrounded)
            {
                Context.Animator.SetBool("Grounded", true);
                Controller.ChangeState(Mathf.Abs(Context.Controls.MoveX) > Mathf.Epsilon
                    ? (PlayerState)Controller.Run
                    : Controller.Idle);
                return;
            }

            if (LedgeGrabState.CanGrab(Context))
            {
                Controller.ChangeState(Controller.LedgeGrab);
                return;
            }

            if (Context.IsWallSliding)
            {
                Controller.ChangeState(Controller.WallSlide);
            }
        }
```

- [ ] **Step 5: Add the ledge-grab check to WallSlideState**

In `Assets/Scripts/HeroKnight/States/WallSlideState.cs`, replace `Tick()`:

```csharp
        public override void Tick()
        {
            if (Context.IsGrounded)
            {
                Controller.ChangeState(Controller.Idle);
                return;
            }

            if (LedgeGrabState.CanGrab(Context))
            {
                Controller.ChangeState(Controller.LedgeGrab);
                return;
            }

            if (!Context.IsWallSliding)
            {
                Controller.ChangeState(Controller.Fall);
            }
        }
```

- [ ] **Step 6: Compile check**

Run the Global Constraints compile-check command. Expected: no `error CS` lines.

- [ ] **Step 7: Commit**

```bash
git add "Assets/Scripts/HeroKnight/States/LedgeGrabState.cs" "Assets/Scripts/HeroKnight/HeroKnightContext.cs" "Assets/Scripts/HeroKnight/HeroKnightController.cs" "Assets/Scripts/HeroKnight/States/FallState.cs" "Assets/Scripts/HeroKnight/States/WallSlideState.cs"
git commit -m "feat(hero-knight): add ledge-grab state and detection"
```

---

### Task 8: Roll

**Files:**
- Create: `Assets/Scripts/HeroKnight/States/RollState.cs`
- Modify: `Assets/Scripts/HeroKnight/HeroKnightContext.cs`
- Modify: `Assets/Scripts/HeroKnight/HeroKnightController.cs`
- Modify: `Assets/Scripts/HeroKnight/States/IdleState.cs`
- Modify: `Assets/Scripts/HeroKnight/States/RunState.cs`
- Modify: `Assets/Scripts/HeroKnight/States/FallState.cs`

**Interfaces:**
- Produces: `HeroKnightContext.RollForce`/`.RollDuration`. `HeroKnightController.Roll` (type `PlayerState`).

- [ ] **Step 1: Add fields to HeroKnightContext**

In `Assets/Scripts/HeroKnight/HeroKnightContext.cs`, add below the existing `LedgeClimbOffset` field:

```csharp
        public float RollForce = 6.0f;
        public float RollDuration = 8.0f / 14.0f;
```

- [ ] **Step 2: Write RollState**

```csharp
// Assets/Scripts/HeroKnight/States/RollState.cs
using UnityEngine;

namespace HeroKnight.States
{
    public class RollState : PlayerState
    {
        private float timer;

        public RollState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        public override void Enter()
        {
            timer = 0f;
            Context.Animator.SetTrigger("Roll");
            Context.Body.velocity = new Vector2(Context.FacingDirection * Context.RollForce, Context.Body.velocity.y);
        }

        public override void Tick()
        {
            timer += Time.deltaTime;

            if (timer < Context.RollDuration)
            {
                return;
            }

            if (!Context.IsGrounded)
            {
                Controller.ChangeState(Controller.Fall);
                return;
            }

            Controller.ChangeState(Mathf.Abs(Context.Controls.MoveX) > Mathf.Epsilon
                ? (PlayerState)Controller.Run
                : Controller.Idle);
        }
    }
}
```

- [ ] **Step 3: Wire Roll into HeroKnightController**

In `Assets/Scripts/HeroKnight/HeroKnightController.cs`, add below the existing `LedgeGrab` property:

```csharp
        public RollState Roll { get; private set; }
```

In `Awake()`, add below `LedgeGrab = new LedgeGrabState(this, context);`:

```csharp
            Roll = new RollState(this, context);
```

- [ ] **Step 4: Add roll transitions to IdleState, RunState, and FallState**

In `Assets/Scripts/HeroKnight/States/IdleState.cs`, replace `Tick()`:

```csharp
        public override void Tick()
        {
            Context.UpdateFacing();

            if (Context.Controls.RollPressed)
            {
                Controller.ChangeState(Controller.Roll);
                return;
            }

            if (!Context.IsGrounded)
            {
                Controller.ChangeState(Controller.Fall);
                return;
            }

            if (Context.Controls.JumpPressed)
            {
                Controller.ChangeState(Controller.Jump);
                return;
            }

            if (Mathf.Abs(Context.Controls.MoveX) > Mathf.Epsilon)
            {
                Controller.ChangeState(Controller.Run);
            }
        }
```

In `Assets/Scripts/HeroKnight/States/RunState.cs`, replace `Tick()`:

```csharp
        public override void Tick()
        {
            Context.UpdateFacing();

            if (Context.Controls.RollPressed)
            {
                Controller.ChangeState(Controller.Roll);
                return;
            }

            if (!Context.IsGrounded)
            {
                Controller.ChangeState(Controller.Fall);
                return;
            }

            if (Context.Controls.JumpPressed)
            {
                Controller.ChangeState(Controller.Jump);
                return;
            }

            if (Mathf.Abs(Context.Controls.MoveX) <= Mathf.Epsilon)
            {
                Controller.ChangeState(Controller.Idle);
            }
        }
```

In `Assets/Scripts/HeroKnight/States/FallState.cs`, insert this check at the top of `Tick()`, before the `Context.Animator.SetFloat("AirSpeedY", ...)` line stays after it:

```csharp
        public override void Tick()
        {
            Context.UpdateFacing();
            Context.Animator.SetFloat("AirSpeedY", Context.Body.velocity.y);

            if (Context.Controls.RollPressed)
            {
                Controller.ChangeState(Controller.Roll);
                return;
            }

            if (Context.IsGrounded)
            {
                Context.Animator.SetBool("Grounded", true);
                Controller.ChangeState(Mathf.Abs(Context.Controls.MoveX) > Mathf.Epsilon
                    ? (PlayerState)Controller.Run
                    : Controller.Idle);
                return;
            }

            if (LedgeGrabState.CanGrab(Context))
            {
                Controller.ChangeState(Controller.LedgeGrab);
                return;
            }

            if (Context.IsWallSliding)
            {
                Controller.ChangeState(Controller.WallSlide);
            }
        }
```

- [ ] **Step 5: Compile check**

Run the Global Constraints compile-check command. Expected: no `error CS` lines.

- [ ] **Step 6: Commit**

```bash
git add "Assets/Scripts/HeroKnight/States/RollState.cs" "Assets/Scripts/HeroKnight/HeroKnightContext.cs" "Assets/Scripts/HeroKnight/HeroKnightController.cs" "Assets/Scripts/HeroKnight/States/IdleState.cs" "Assets/Scripts/HeroKnight/States/RunState.cs" "Assets/Scripts/HeroKnight/States/FallState.cs"
git commit -m "feat(hero-knight): add roll state"
```

---

### Task 9: Block

**Files:**
- Create: `Assets/Scripts/HeroKnight/States/BlockState.cs`
- Modify: `Assets/Scripts/HeroKnight/HeroKnightController.cs`
- Modify: `Assets/Scripts/HeroKnight/States/IdleState.cs`
- Modify: `Assets/Scripts/HeroKnight/States/RunState.cs`

**Interfaces:**
- Produces: `HeroKnightController.Block` (type `PlayerState`).

- [ ] **Step 1: Write BlockState**

```csharp
// Assets/Scripts/HeroKnight/States/BlockState.cs
using UnityEngine;

namespace HeroKnight.States
{
    public class BlockState : PlayerState
    {
        public BlockState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        public override void Enter()
        {
            Context.SetVelocityX(0f);
            Context.Animator.SetTrigger("Block");
            Context.Animator.SetBool("IdleBlock", true);
        }

        public override void Exit()
        {
            Context.Animator.SetBool("IdleBlock", false);
        }

        public override void Tick()
        {
            if (!Context.Controls.BlockHeld)
            {
                Controller.ChangeState(Mathf.Abs(Context.Controls.MoveX) > Mathf.Epsilon
                    ? (PlayerState)Controller.Run
                    : Controller.Idle);
            }
        }
    }
}
```

- [ ] **Step 2: Wire Block into HeroKnightController**

In `Assets/Scripts/HeroKnight/HeroKnightController.cs`, add below the existing `Roll` property:

```csharp
        public BlockState Block { get; private set; }
```

In `Awake()`, add below `Roll = new RollState(this, context);`:

```csharp
            Block = new BlockState(this, context);
```

- [ ] **Step 3: Add block transition to IdleState and RunState**

In `Assets/Scripts/HeroKnight/States/IdleState.cs`, insert this check as the new first check in `Tick()`, above the `RollPressed` check:

```csharp
            if (Context.Controls.BlockHeld)
            {
                Controller.ChangeState(Controller.Block);
                return;
            }
```

The full `Tick()` method should now read:

```csharp
        public override void Tick()
        {
            Context.UpdateFacing();

            if (Context.Controls.BlockHeld)
            {
                Controller.ChangeState(Controller.Block);
                return;
            }

            if (Context.Controls.RollPressed)
            {
                Controller.ChangeState(Controller.Roll);
                return;
            }

            if (!Context.IsGrounded)
            {
                Controller.ChangeState(Controller.Fall);
                return;
            }

            if (Context.Controls.JumpPressed)
            {
                Controller.ChangeState(Controller.Jump);
                return;
            }

            if (Mathf.Abs(Context.Controls.MoveX) > Mathf.Epsilon)
            {
                Controller.ChangeState(Controller.Run);
            }
        }
```

Apply the same change to `Assets/Scripts/HeroKnight/States/RunState.cs` — insert the identical `BlockHeld` check as the new first check in `Tick()`, above the `RollPressed` check (the rest of `RunState.Tick()` is unchanged from Task 8).

- [ ] **Step 4: Compile check**

Run the Global Constraints compile-check command. Expected: no `error CS` lines.

- [ ] **Step 5: Commit**

```bash
git add "Assets/Scripts/HeroKnight/States/BlockState.cs" "Assets/Scripts/HeroKnight/HeroKnightController.cs" "Assets/Scripts/HeroKnight/States/IdleState.cs" "Assets/Scripts/HeroKnight/States/RunState.cs"
git commit -m "feat(hero-knight): add block state"
```

---

### Task 10: Attack combo

**Files:**
- Create: `Assets/Scripts/HeroKnight/States/AttackState.cs`
- Modify: `Assets/Scripts/HeroKnight/HeroKnightContext.cs`
- Modify: `Assets/Scripts/HeroKnight/HeroKnightController.cs`
- Modify: `Assets/Scripts/HeroKnight/States/IdleState.cs`
- Modify: `Assets/Scripts/HeroKnight/States/RunState.cs`

**Interfaces:**
- Produces: `HeroKnightContext.TimeSinceAttack`/`.ComboCount`. `HeroKnightController.Attack` (type `PlayerState`).

- [ ] **Step 1: Add fields to HeroKnightContext**

In `Assets/Scripts/HeroKnight/HeroKnightContext.cs`, add below the existing `RollDuration` field:

```csharp
        public float TimeSinceAttack = 0f;
        public int ComboCount = 0;
```

- [ ] **Step 2: Write AttackState**

Combo thresholds (0.25s minimum between hits, 1.0s combo reset) and the "loop back to Attack1 after Attack3" rule are carried over from the original `HeroKnight.cs` demo's tuned values. The 0.4s exit timing is a starting point — tune it against the actual Attack1/2/3 clip lengths in the Animator during playtesting (Task 12).

```csharp
// Assets/Scripts/HeroKnight/States/AttackState.cs
using UnityEngine;

namespace HeroKnight.States
{
    public class AttackState : PlayerState
    {
        private const float ExitDelay = 0.4f;

        public AttackState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        public override void Enter()
        {
            Context.SetVelocityX(0f);

            Context.ComboCount++;
            if (Context.ComboCount > 3 || Context.TimeSinceAttack > 1.0f)
            {
                Context.ComboCount = 1;
            }

            Context.Animator.SetTrigger("Attack" + Context.ComboCount);
            Context.TimeSinceAttack = 0f;
        }

        public override void Tick()
        {
            if (Context.Controls.AttackPressed && Context.TimeSinceAttack > 0.25f)
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
    }
}
```

- [ ] **Step 3: Wire Attack into HeroKnightController and increment TimeSinceAttack**

In `Assets/Scripts/HeroKnight/HeroKnightController.cs`, add below the existing `Block` property:

```csharp
        public AttackState Attack { get; private set; }
```

In `Awake()`, add below `Block = new BlockState(this, context);`:

```csharp
            Attack = new AttackState(this, context);
```

In `Update()`, add the timer increment before `currentState.Tick();`:

```csharp
        private void Update()
        {
            context.TimeSinceAttack += Time.deltaTime;
            currentState.Tick();
        }
```

- [ ] **Step 4: Add attack transition to IdleState and RunState**

In `Assets/Scripts/HeroKnight/States/IdleState.cs`, insert this check as the new first check in `Tick()`, above the `BlockHeld` check:

```csharp
            if (Context.Controls.AttackPressed && Context.TimeSinceAttack > 0.25f)
            {
                Controller.ChangeState(Controller.Attack);
                return;
            }
```

The full `Tick()` method should now read:

```csharp
        public override void Tick()
        {
            Context.UpdateFacing();

            if (Context.Controls.AttackPressed && Context.TimeSinceAttack > 0.25f)
            {
                Controller.ChangeState(Controller.Attack);
                return;
            }

            if (Context.Controls.BlockHeld)
            {
                Controller.ChangeState(Controller.Block);
                return;
            }

            if (Context.Controls.RollPressed)
            {
                Controller.ChangeState(Controller.Roll);
                return;
            }

            if (!Context.IsGrounded)
            {
                Controller.ChangeState(Controller.Fall);
                return;
            }

            if (Context.Controls.JumpPressed)
            {
                Controller.ChangeState(Controller.Jump);
                return;
            }

            if (Mathf.Abs(Context.Controls.MoveX) > Mathf.Epsilon)
            {
                Controller.ChangeState(Controller.Run);
            }
        }
```

Apply the same change to `Assets/Scripts/HeroKnight/States/RunState.cs` — insert the identical `AttackPressed` check as the new first check in `Tick()`, above the `BlockHeld` check (the rest of `RunState.Tick()` is unchanged from Task 9).

- [ ] **Step 5: Compile check**

Run the Global Constraints compile-check command. Expected: no `error CS` lines. This completes all code for the sandbox — `HeroKnightController` now exposes `Idle`, `Run`, `Jump`, `Fall`, `WallSlide`, `LedgeGrab`, `Roll`, `Block`, `Attack`.

- [ ] **Step 6: Commit**

```bash
git add "Assets/Scripts/HeroKnight/States/AttackState.cs" "Assets/Scripts/HeroKnight/HeroKnightContext.cs" "Assets/Scripts/HeroKnight/HeroKnightController.cs" "Assets/Scripts/HeroKnight/States/IdleState.cs" "Assets/Scripts/HeroKnight/States/RunState.cs"
git commit -m "feat(hero-knight): add attack combo state"
```

---

### Task 11: Scene, prefab, and Animator assembly (manual — Unity Editor GUI required)

**No agent can perform this task in this environment.** It must be done by a human in the Unity Editor. Everything referenced below (`HeroKnightController`, all 9 states, `TouchHeroKnightInput`, `VirtualJoystick`, `TouchButton`, `Sensor_HeroKnight`) was created in Tasks 1–10.

- [ ] **Step 1: Build the HeroKnight prefab**
  - Duplicate `Assets/Hero Knight - Pixel Art/Demo/HeroKnight.prefab` into a new prefab at `Assets/Prefabs/HeroKnight.prefab` (independent copy, not a variant, so the vendor demo script can be freely removed).
  - On the new prefab's root, remove the `HeroKnight` (demo) component and add `HeroKnight.HeroKnightController`.
  - On each of the existing sensor children (`GroundSensor`, `WallSensor_R1`, `WallSensor_R2`, `WallSensor_L1`, `WallSensor_L2`), remove the vendor `Sensor_HeroKnight` component and add our `HeroKnight.Sensors.Sensor_HeroKnight` in its place (our `HeroKnightController` only accepts our namespaced type in its serialized fields).
  - Duplicate `WallSensor_R2` and `WallSensor_L2` to create two new children, `LedgeSensor_R` and `LedgeSensor_L`, each positioned roughly 0.5–1 unit above its counterpart (adjust by eye once the terrain ledge exists in Step 2 — the sensor should sit just above the wall's top edge).
  - On the prefab root's `HeroKnightController` component, assign `groundSensor`, `wallSensorR1`/`R2`/`L1`/`L2`, and `ledgeSensorR`/`L` to their matching children. Leave `input` empty for now — it's assigned per scene-instance in Step 4, since it lives on the Canvas, not the prefab.

- [ ] **Step 2: Build the sandbox scene**
  - Create `Assets/Scenes/HeroKnightSandbox.unity` (duplicate `SampleScene.unity` to inherit its camera/lighting/URP setup, then delete the Mr. Alien player/enemies/tokens from it, or start from a fresh 2D scene — either is fine).
  - Using the Tile Palette (Window > 2D > Tile Palette) and the `EnvironmentTiles_0`–`3` tile assets from `Assets/Hero Knight - Pixel Art/Environment/`, paint: a flat ground run, a raised platform reachable by a jump, a tall wall for wall-slide with a gap above its top edge (the grabbable ledge), and open flat ground for roll.
  - Add a `Tilemap Collider 2D` + `Composite Collider 2D` to the terrain Tilemap, and assign the `Walls_noFriction` Physics Material 2D (from `Assets/Hero Knight - Pixel Art/Environment/`) to its collider.
  - Drop the `HeroKnight` prefab (Step 1) into the scene at the start of the terrain.

- [ ] **Step 3: Build the touch UI**
  - Add a Canvas (GameObject > UI > Canvas); set Render Mode to Screen Space - Overlay; add a Canvas Scaler set to "Scale With Screen Size", reference resolution 1920x1080 (Unity auto-adds an EventSystem alongside the Canvas — required for the UI event handlers to fire).
  - Add a child GameObject with a `HeroKnight.Input.TouchHeroKnightInput` component.
  - Build the joystick: a background `Image` on the left with a `HeroKnight.UI.VirtualJoystick` component, and a child handle `Image` — assign both to the component's `background`/`handle` fields.
  - Build four button `Image`s on the right (Jump, Attack, Block, Roll), each with a `HeroKnight.UI.TouchButton` component.
  - On the `TouchHeroKnightInput` component, assign the joystick and the four buttons to their matching fields.
  - On the `HeroKnight` prefab instance's `HeroKnightController` component, assign the Canvas's `TouchHeroKnightInput` to the `input` field.

- [ ] **Step 4: Camera and orientation**
  - Add a Cinemachine Virtual Camera (GameObject > Cinemachine > Virtual Camera); set Follow to the `HeroKnight` instance.
  - Add a `PolygonCollider2D` (set as Trigger) bounding the playable terrain, and a `CinemachineConfiner2D` on the virtual camera referencing it.
  - Add the scene to Build Settings (File > Build Settings > Add Open Scenes).
  - In Edit > Project Settings > Player > Resolution and Presentation, set Default Orientation to Landscape Left (or Auto Rotation with only landscape options enabled).

- [ ] **Step 5: Add ledge-grab to the Animator Controller**
  - Open `Assets/Hero Knight - Pixel Art/Animations/HeroKnight_AnimController.controller`.
  - Add a new Trigger parameter named `LedgeGrab`.
  - Add a new state using the `HeroKnight_LedgeGrab` clip.
  - Add an "Any State → LedgeGrab" transition: condition `LedgeGrab` trigger, "Has Exit Time" unchecked, Transition Duration 0 — open the existing "Any State → Roll" transition first and copy its settings, since it's the same kind of one-shot trigger-driven transition. No further transitions are needed — `LedgeGrabState.Exit()`/every other state's `Enter()` already drives the Animator directly via `SetTrigger`/`SetBool`/`SetInteger`.

- [ ] **Step 6: Commit**

```bash
git add "Assets/Prefabs" "Assets/Scenes/HeroKnightSandbox.unity" "Assets/Scenes/HeroKnightSandbox.unity.meta" "Assets/Hero Knight - Pixel Art/Animations/HeroKnight_AnimController.controller" "ProjectSettings"
git commit -m "feat(hero-knight): assemble sandbox scene, prefab, touch UI, and ledge-grab animator state"
```

(Adjust the `git add` paths to whatever the Editor actually created/changed — check `git status` first.)

---

### Task 12: Full playtest and tuning (manual — Unity Editor Play mode)

In the Unity Editor Game view, using mouse clicks on the on-screen joystick/buttons to simulate touch, exercise every state from the design's transition table and confirm it behaves as documented:

- [ ] **Idle → Run**: drag the joystick left/right — character runs and flips to face the drag direction; release the joystick — character returns to Idle.
- [ ] **Run/Idle → Jump → Fall**: tap Jump while grounded — character jumps, `AirSpeedY` visibly drives the jump/fall blend, and it lands back into Run or Idle depending on whether the joystick is held.
- [ ] **Fall → WallSlide → Fall**: walk off the platform edge into freefall, then air-drift into the tall wall — character enters wall-slide (fall speed visibly clamped), and clears back to Fall when drifted away from the wall.
- [ ] **WallSlide/Fall → LedgeGrab → Idle (climb)**: slide down to the wall's ledge — character grabs and hangs; tap Jump — character snaps up onto the ledge into Idle. If the snap position looks wrong, adjust `LedgeClimbOffset` on the `HeroKnightContext` initializer in `HeroKnightController.cs` (or expose it as a `[SerializeField]` on `HeroKnightController` for easier Inspector tuning) and re-check.
- [ ] **LedgeGrab → Fall (drop)**: grab the ledge again, tap Roll — character drops and resumes falling.
- [ ] **Roll**: from Idle/Run and from mid-air, tap Roll — character dashes in the facing direction for the roll duration, ignoring joystick input during the roll, then returns to the correct grounded/airborne state.
- [ ] **Block**: hold the Block button — character stops and holds a block pose; release — returns to Run or Idle depending on joystick state.
- [ ] **Attack combo**: tap Attack three times within the combo window — Attack1 → Attack2 → Attack3 play in sequence; wait longer than 1.0s between taps — combo resets to Attack1; tap once and wait — character returns to Run/Idle after the attack finishes.
- [ ] **Mutual exclusivity**: confirm you cannot roll while attacking, attack while rolling, etc. — attempting another action's input while in Attack/Block/Roll should have no effect until that state exits on its own.

If any move's timing or distance feels off, tune the relevant field in `HeroKnightContext` (`MoveSpeed`, `JumpForce`, `RollForce`, `RollDuration`) or the `MaxSlideSpeed`/`ExitDelay` constants in `WallSlideState.cs`/`AttackState.cs`, then re-test. No commit is required for this task unless tuning values are changed, in which case:

```bash
git add "Assets/Scripts/HeroKnight"
git commit -m "chore(hero-knight): tune movement feel from playtesting"
```
