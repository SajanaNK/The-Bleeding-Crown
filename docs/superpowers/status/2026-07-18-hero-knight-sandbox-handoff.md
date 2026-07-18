# Hero Knight Sandbox — Session Handoff (2026-07-18)

> Read this file first in a new session, then say: **"Read docs/superpowers/status/2026-07-18-hero-knight-sandbox-handoff.md and continue from there."**
> That's the whole resume procedure — no other file needs to be read up front. Point at the exact commands below rather than re-deriving them.

## What this project is

A movement/combat **sandbox** (no levels/enemies) for a mobile game, using a touch
joystick + action buttons, built as a full custom state-machine rewrite of the
"Hero Knight - Pixel Art" vendor asset's demo character — not the vendor demo
script, not integrated into the template's `Platformer.*`/`Simulation` framework.
Full spec: [`docs/superpowers/specs/2026-07-18-hero-knight-sandbox-design.md`](../specs/2026-07-18-hero-knight-sandbox-design.md).
Full 12-task plan: [`docs/superpowers/plans/2026-07-18-hero-knight-sandbox.md`](../plans/2026-07-18-hero-knight-sandbox.md).

Executed via **superpowers:subagent-driven-development**, working directly on
branch `hero-knight-sandbox` (no worktree).

## Status at a glance

- **Tasks 1–10 (all C# code): reviewed and merged as of commit `b621054`, but
  four of those files have SINCE been modified again this session (rounds
  1–6 below) and are currently UNCOMMITTED.** Branch `hero-knight-sandbox`.
  Modified-but-uncommitted: `Assets/Scripts/HeroKnight/HeroKnightContext.cs`,
  `HeroKnightController.cs`, `Sensors/Sensor_HeroKnight.cs`,
  `States/LedgeGrabState.cs` — all real bug fixes discovered during live
  Task 11/12 playtesting (namespace shadowing, a trigger-filter bug, a
  climb-direction bug), not scope creep. Do not re-open these further unless
  new evidence surfaces; do not revert them.
- **Task 11 (manual scene/prefab/Animator assembly): FUNCTIONALLY DONE,
  NOT YET COMMITTED.** Built via a custom Unity Editor automation script
  (`Assets/Editor/HeroKnightSandboxSetup.cs`, menu `HeroKnightSandbox > ...`)
  because the user's own Editor is open interactively, so batch-mode execution
  isn't available — the user runs each menu item themselves and reports back
  Console output / screenshots, since the agent cannot see the Editor directly.
  Every state (Idle/Run/Jump/Fall/WallSlide/LedgeGrab, Roll/Block/Attack, UI,
  camera, terrain) is now confirmed working in-game (see Round 6 below). What
  remains is cleanup (remove the temporary debug overlay) and committing
  everything — see "Known follow-up items" below.
- **Task 12 (full playtest/tuning): NOT STARTED.** Blocked on Task 11 finishing.

## Uncommitted working-tree state right now

As of round 6 (`git status --porcelain=v1 -uall`):

```
 M "Assets/Hero Knight - Pixel Art/Animations/HeroKnight_AnimController.controller"  (LedgeGrab state/params added by the automation script)
 M Assets/Scripts/HeroKnight/HeroKnightContext.cs       (round 2: fully-qualified sensor field types; round 4: LedgeClimbOffset tuning)
 M Assets/Scripts/HeroKnight/HeroKnightController.cs    (round 2: fully-qualified sensor field types; temporary OnGUI debug overlay — see below)
 M Assets/Scripts/HeroKnight/Sensors/Sensor_HeroKnight.cs  (round 3: ignore other-trigger overlaps, e.g. CameraBounds)
 M Assets/Scripts/HeroKnight/States/LedgeGrabState.cs   (round 5: climb direction follows grabbed side, not stale FacingDirection)
 M ProjectSettings/EditorBuildSettings.asset            (scene added to build settings)
 M ProjectSettings/ProjectSettings.asset                (orientation/other settings from FinalizeProjectSettings)
?? Assets/Editor/HeroKnightSandboxSetup.cs(.meta)        (the automation script itself; round 1: fully-qualified sensor type)
?? Assets/Prefabs/GroundSprite.png(.meta)                (generated placeholder ground sprite)
?? Assets/Prefabs/HeroKnight.prefab(.meta)               (generated sandbox player prefab)
?? Assets/Scenes/HeroKnightSandbox.unity(.meta)          (generated sandbox scene)
?? docs/superpowers/status/2026-07-18-hero-knight-sandbox-handoff.md  (this file)
```

**Task 11 is now functionally verified (round 6)** — commit everything above
once the temporary debug overlay is removed (next step). The four modified
`.cs` files under `Assets/Scripts/HeroKnight/` are real production bug fixes,
not tooling — they belong in their own commit separate from the Task 11
asset/automation commit (script fixes vs. generated Unity assets are
different concerns, matches this branch's existing commit granularity).

## The automation script: `Assets/Editor/HeroKnightSandboxSetup.cs`

Namespace `HeroKnightSandbox.EditorTools`. Menu items (run from the Unity Editor
menu bar, in order — or use `RunAll`):

1. `HeroKnightSandbox > 1 Build Prefab` — `BuildPrefab()`: loads the vendor's
   source prefab, strips the demo script and vendor sensor components, adds
   `HeroKnightController` + our namespaced sensors, adds two new ledge sensors,
   wires all `[SerializeField]` refs, saves to `Assets/Prefabs/HeroKnight.prefab`.
2. `HeroKnightSandbox > 2 Add LedgeGrab To Animator` — `AddLedgeGrabToAnimator()`:
   adds a `LedgeGrab` state + transitions to the vendor's Animator Controller.
3. `HeroKnightSandbox > 3 Build Scene` — `BuildScene()`: builds
   `Assets/Scenes/HeroKnightSandbox.unity` from scratch (`NewSceneMode.Single`,
   so it's safe/idempotent to re-run — no leftover duplicate geometry across
   runs), including generated ground-sprite platforms (`Terrain`), the player
   instance, the touch UI canvas, and a Cinemachine camera + confiner.
4. `HeroKnightSandbox > 4 Finalize Project Settings` — `FinalizeProjectSettings()`.
5. `HeroKnightSandbox > Run All` — runs all four in sequence.

**Safe to re-run individually.** `BuildPrefab()` always rebuilds from the
untouched vendor source prefab, so re-running it alone is enough after a
script-only fix — no need to also re-run Build Scene (the scene references the
prefab by GUID, not by value).

## The bug just fixed (root cause, now corrected — NOT YET VERIFIED BY USER)

**Symptom:** wall-slide/ledge-grab never triggered. Character would run off the
ground, fall past/into the "Wall" platform, and just keep falling flush against
it (no slide animation, X velocity zeroed by real physical contact, but state
machine stayed in `RunState`/never reached `WallSlideState`).

**Diagnosis path:** added a temporary on-screen debug overlay
(`OnGUI()` in `HeroKnightController.cs`, see below) showing live sensor states.
It revealed **all seven sensors simultaneously read `true`** (Ground, WallR1,
WallR2, WallL1, WallL2, LedgeR, LedgeL) while the character was visibly airborne
— impossible if the sensors were working as designed.

**Root cause:** inspecting the generated prefab's raw YAML showed every sensor's
`MonoBehaviour.m_Script` GUID was `2f2a8bdc7f41b704693ad497097f30dc` — the
**vendor's** global-scope `Sensor_HeroKnight` class
(`Assets/Hero Knight - Pixel Art/Demo/Sensor_HeroKnight.cs`, declared with **no
namespace**), not our own `2db2a95a40b7dce409b21b3c1a6b9735`
(`HeroKnightSandbox.Sensors.Sensor_HeroKnight`).

An **earlier session already hit this exact bug once** and "fixed" it by
wrapping the whole `HeroKnightSandboxSetup.cs` file in
`namespace HeroKnightSandbox.EditorTools { ... }`. That fix was insufficient
and the bug regressed (or never actually left): C# always resolves a bare type
name against the **global namespace's own member declarations** before it ever
falls through to a `using` import for that name, and this check happens
regardless of how deeply the *referencing* file's own namespace is nested —
wrapping our file in a namespace does nothing to shield it from a
same-named type declared with no namespace at all. Since the vendor's
`Sensor_HeroKnight` lives directly in the global namespace, every bare
`Sensor_HeroKnight` reference in the automation script was silently binding to
the vendor's class, with no compile error.

**Actual fix applied (this session):** replaced every bare `Sensor_HeroKnight`
type reference in `HeroKnightSandboxSetup.cs` with the fully-qualified
`HeroKnightSandbox.Sensors.Sensor_HeroKnight` — fully-qualified names are
resolved directly and are immune to this lookup-order shadowing regardless of
namespace nesting. Updated the file's header doc comment to explain the real
mechanism (the old comment's explanation was wrong). Left the string-based
`child.GetComponent("Sensor_HeroKnight")` call alone (matches by class name
across namespaces already, so it correctly strips whichever version is
attached to the vendor's source prefab).

## Round 2 of the same bug — production code was ALSO shadowed

After the editor-script fix above, the user re-ran `1 Build Prefab` and hit:

```
NullReferenceException: Object reference not set to an instance of an object
HeroKnightSandbox.HeroKnightContext.get_IsGrounded () (HeroKnightContext.cs:38)
HeroKnightSandbox.HeroKnightController.OnGUI () (HeroKnightController.cs:93)
```

Inspecting the regenerated `Assets/Prefabs/HeroKnight.prefab` directly showed
all 7 sensor components now correctly used our script GUID
(`2db2a95a40b7dce409b21b3c1a6b9735`) — the editor-script fix worked — **but**
`HeroKnightController`'s serialized fields (`groundSensor`, `wallSensorR1`,
etc.) were all `{fileID: 0}` (null) on the saved prefab asset itself, not just
in the scene instance.

**Cause: the exact same shadowing bug also existed in the production code.**
`HeroKnightController.cs` and `HeroKnightContext.cs` both live in
`namespace HeroKnightSandbox { ... }` and both declared their sensor fields
with the bare name `Sensor_HeroKnight` (relying on
`using HeroKnightSandbox.Sensors;`). By the identical C# lookup-order rule,
those fields' *actual compiled type* was the vendor's global-scope class, not
ours. So when the (now-fixed) editor script tried to assign our correctly-typed
sensor component into a field that Unity's compiler had typed as the vendor's
class, `SerializedProperty.objectReferenceValue`'s setter silently no-ops on
the type mismatch (no exception, no log — it just leaves the field null). That
silent failure is why every sensor field ended up empty, causing the
`NullReferenceException` in `IsGrounded` at runtime.

This also retroactively explains why jump/run/attack/block/roll worked earlier
in the session: *before* either fix, the editor script's local variables and
these two files' field types were shadowed to the **same** (vendor) type
consistently, so the mismatched-type assignment problem didn't exist yet — the
fields got wired, just to the vendor's sensor component, which happened to
produce usable (if eventually wrong-for-walls) ground detection.

**Fix applied (this session, round 2):** in both
`Assets/Scripts/HeroKnight/HeroKnightController.cs` (7 `[SerializeField]`
fields) and `Assets/Scripts/HeroKnight/HeroKnightContext.cs` (7 public
fields), changed every bare `Sensor_HeroKnight` field type to the
fully-qualified `HeroKnightSandbox.Sensors.Sensor_HeroKnight`. Verified via
grep that no other file under `Assets/Scripts/HeroKnight` references
`Sensor_HeroKnight` by bare name outside of `Sensors/Sensor_HeroKnight.cs`
itself (where it's the declaration, not a reference).

**This is a change to already-committed production code** (part of commit
`b621054`'s lineage) — when this is finally verified working, it needs its own
commit on top of `Sensor_HeroKnight`-related history, separate from the Task
11 asset commit.

## Round 3 — the actual sensor-overlap root cause (CameraBounds trigger)

After round 2's fix, the user re-ran `1 Build Prefab` (confirmed via direct
prefab inspection: sensor GUIDs correct, `HeroKnightController` fields all
resolved to non-zero `fileID`s — wiring genuinely fixed), recompiled, and
retested. No more `NullReferenceException`, but the **exact same symptom as
before either fix**: all 7 sensors (`Ground`, `WallR1/R2`, `WallL1/L2`,
`LedgeR/L`) simultaneously `true`, `Grounded: True` while visibly airborne and
falling (`Velocity: (0.00, -7.81)`), stuck in `RunState`.

**Real root cause, finally identified:** `Sensor_HeroKnight.OnTriggerEnter2D`/
`OnTriggerExit2D` don't filter by anything — they count *every* trigger
overlap. `BuildScene()` creates a `CameraBounds` `PolygonCollider2D` with
`isTrigger = true` spanning almost the entire level
(`(-10,-16), (-10,10), (44,10), (44,-16)`), used as the Cinemachine confiner's
bounding shape. Unity only requires that **one side** of a trigger pair have a
`Rigidbody2D` for `OnTrigger*2D` callbacks to fire — the character's own
`Rigidbody2D` satisfies that — so every sensor gets a permanent
`OnTriggerEnter2D` from `CameraBounds` the instant it spawns inside that huge
region, and since the character never leaves it, there's never a matching
`OnTriggerExit2D`. `m_ColCount` is therefore stuck at ≥1 (i.e. `State() ==
true`) on every sensor for the entire session, completely independent of any
real ground/wall/ledge contact. This fully explains every symptom seen in both
debug-overlay screenshots, and also retroactively explains why jump (which
only needs `Grounded == true`) "worked" early on while Fall/WallSlide (which
need `Grounded == false`) never could.

**Fix applied (this session, round 3):** `Assets/Scripts/HeroKnight/Sensors/Sensor_HeroKnight.cs`
— both `OnTriggerEnter2D` and `OnTriggerExit2D` now `return` early when
`other.isTrigger` is true, so sensors only ever react to solid level geometry
(Ground/Wall/JumpPlatform/LedgePlatform are all non-trigger; only
`CameraBounds` is a trigger in this scene). This is a deliberate, documented
deviation from the file's "verbatim namespaced copy of the vendor's script"
description (see the file's own header comment) — justified because this
project's level design (a trigger-based camera confiner overlapping the whole
level) exposes a real bug the vendor's original demo scene never hit.

**This is a pure script-logic change — no serialization/wiring involved.**
Unlike rounds 1–2, this does **not** require re-running any
`HeroKnightSandbox` menu item. Just let the Editor recompile and re-enter Play
mode directly.

**CONFIRMED WORKING.** User screenshot after this fix showed:
`State: WallSlideState`, `Grounded: False`, `WallR1: True  WallR2: True`,
`WallL1: False  WallL2: False`, `IsWallSliding: True`, velocity slowed to
`(0.00, -1.68)` (controlled slide, not free-fall). All three rounds of fix
(sensor-script namespace shadowing in the editor tool, sensor-field namespace
shadowing in production code, and the `CameraBounds` trigger swallowing every
sensor) are done and verified.

**New harmless warning surfaced, deliberately left alone:**
`'HeroKnight' AnimationEvent 'AE_SlideDust' on animation 'HeroKnight_WallSlide'
has no receiver!` — the vendor's `HeroKnight_WallSlide` clip has a baked-in
Animation Event calling a dust-VFX method that lived on the vendor's demo
script, which `BuildPrefab()` deliberately removes. Purely cosmetic (missing
particle effect), doesn't affect state-machine behavior. User chose to ignore
it rather than strip the event — do not "fix" this unless asked again.

## Immediate next step (where we left off) — CURRENT

Wall-slide is fully confirmed working (see above). **Ledge-grab is the last
unverified piece of Task 11's runtime behavior.**

Next actions, in order:
1. From `WallSlideState`, get the character to the top of the "Wall" platform
   (y=6, where the wall "ends" and `LedgePlatform` sits at `(25, 5.5)`) so the
   ledge sensors (`LedgeSensor_R`/`LedgeSensor_L`, positioned 0.5 units above
   `WallSensor_R2`/`L2`) clear the wall's top edge while the wall sensors
   below are still in contact. Per the design spec, ledge-grab triggers from
   `WallSlideState` when a wall sensor is true but the corresponding ledge
   sensor detects the ledge — check
   `Assets/Scripts/HeroKnight/States/WallSlideState.cs` and
   `LedgeGrabState.cs` for the exact condition if this needs re-deriving.
2. Confirm the character enters `LedgeGrabState` (visible in the debug
   overlay's `State:` line), and that its `Enter()` X-snap (added during the
   final-review fix, see `LedgeGrabState.cs`) positions it plausibly at the
   ledge rather than clipping into the wall.
3. Test both ledge-grab exits: **Jump = climb up onto the ledge**, **Roll =
   drop back down** (per the spec's button mapping — confirmed during
   brainstorming self-review). Confirm the Animator's `LedgeGrab` state (added
   by `AddLedgeGrabToAnimator()`) actually plays, not just the state-machine
   logic.
4. If ledge-grab doesn't trigger at all: check the ledge sensors' generated
   world positions are actually near `LedgePlatform`/the wall's top — the
   automation script derives them purely from `WallSensor_R2`/`L2`'s local
   position + 0.5 units up, so verify that lines up with where the wall
   visually ends (y=6) given the wall's total height/offset in
   `HeroKnightSandboxSetup.cs`'s `CreatePlatform` call for `"Wall"`.

If the sensor `CameraBounds`-trigger fix (round 3) somehow regresses or a
similar all-true symptom reappears for the ledge sensors specifically, apply
the same diagnostic approach: read the debug overlay's `LedgeR`/`LedgeL`
values first before assuming it's a geometry problem.

## Round 4 — ledge-grab confirmed, climb offset needed tuning

User tested ledge-grab via the Inspector-teleport shortcut (Play mode, manually
setting the `HeroKnight` Transform to ~`(20.6, 6.3, 0)` so it falls straight
into ledge range instead of relying on real platforming, which the current
level geometry likely doesn't support reaching naturally — see the "natural
platforming" note in this doc's testing guidance above).

- **Grab: confirmed working.** `State: LedgeGrabState`, `WallR1/R2: True`,
  `IsWallSliding: True`, `Velocity: (0,0)` (frozen, as designed).
- **Roll = drop: confirmed working** (implied by state transitions; not
  separately screenshotted this round, was already logic-verified).
- **Jump = climb: functionally works (state transitions to Idle, Animator
  triggers) but the landing position was off** — after climbing, the character
  ended up floating at the wall/ledge boundary, not cleanly on the shelf
  surface, requiring a second manual jump to actually land. Not a logic bug —
  `LedgeGrabState.Tick()`'s climb branch correctly does
  `Transform.position += LedgeClimbOffset` (facing-aware on X) and transitions
  to Idle; the fixed offset value in `HeroKnightContext.cs` just didn't clear
  this level's exact wall-top/ledge-shelf height.

**Fix applied (this session, round 4):** bumped
`HeroKnightContext.LedgeClimbOffset` from `(0.3f, 1.1f)` to `(0.3f, 1.6f)` in
`Assets/Scripts/HeroKnight/HeroKnightContext.cs`. Not a `[SerializeField]` —
plain C# default, so this needs only a recompile, no prefab/scene rebuild.
Deliberately biased toward slight overshoot rather than underscoot: the
Rigidbody2D goes back to `Dynamic` on state exit, so any overshoot just
results in a brief, unnoticeable gravity-settle onto the surface, whereas an
undershoot visibly strands the character at the edge (what we just saw).

**Not yet re-tested after this tuning change** — pick up here: ask for
confirmation that a single climb (no second jump needed) now lands the
character cleanly on the wall-top/`LedgePlatform` shelf. If still short,
nudge `LedgeClimbOffset.y` up further (try `2.0`); if it now overshoots
noticeably (character visibly pops up and drops back down before settling),
back it off slightly (try `1.4`). This value is level-geometry-specific
should the sandbox's wall/ledge dimensions change later.

Once confirmed, this is the last open functional item for Task 11 —
everything else (Idle/Run/Jump/Fall/WallSlide/LedgeGrab-drop, UI wiring,
camera, terrain) is already confirmed working.

## Round 5 — round 4's tuning fix was a red herring; real bug was climb direction

User retested with `LedgeClimbOffset.y = 1.6f` (round 4's fix), including a
full Play-mode stop + recompile to rule out stale code. Result was **pixel-
identical** to before the tuning change — proof the Y-magnitude wasn't the
actual problem.

**Real cause:** `LedgeGrabState.Tick()`'s climb branch computed the climb
offset's X sign from `Context.FacingDirection`, which only updates via
`UpdateFacing()` on live joystick input. The Inspector-teleport test method
(used because normal platforming can't reach the ledge — see testing guidance
above) never runs any movement code before the grab, so `FacingDirection` held
whatever stale value it last had — apparently `-1` (left) — while the
character was actually grabbing with the **right-side** wall sensors
(`WallR1/R2`, confirmed in every debug overlay). The climb then moved the
character `LedgeClimbOffset.x * -1`, i.e. further **left**, off the wall's
own footprint (`x: 21–22`) into open air — landing them floating beside the
wall, not on top of it. This also matches the earlier design-review history:
`LedgeGrabState.cs` already computes which side was grabbed once, in
`Enter()`, for the X-position snap — it just wasn't reusing that for the climb
direction too. This can also happen in **real, non-teleport gameplay**, not
just this test method: `WallSlideState` calls `UpdateFacing()` on live input,
so a player holding *away* from the wall while sliding (e.g. trying to escape
the slide) would reproduce the same wrong-direction climb.

**Fix applied (this session, round 5):** `Assets/Scripts/HeroKnight/States/LedgeGrabState.cs`
— added a `private bool grabbedRight` field, set once in `Enter()` (replacing
the local `rightLedge` var used only for the X-snap), and reused in `Tick()`'s
climb branch instead of `Context.FacingDirection` to pick the offset's sign.
Left `LedgeClimbOffset.y` at round 4's `1.6f` (harmless, still a reasonable
value) — the actual defect was direction, not magnitude.

**Not yet re-tested.** Pure script change, no `[SerializeField]` involved —
recompile only, no prefab/scene rebuild. Pick up here: ask for a fresh
Inspector-teleport ledge-grab test (stop Play mode first to guarantee a clean
recompile, same as round 4's retest), single Jump press, confirm the
character lands directly on the wall-top/`LedgePlatform` shelf without a
second jump.

## Round 6 — climb "floating" was a visual sprite-padding illusion, not a bug

After round 5's direction fix, the resting position after climbing
(`(21.30, 5.95)`) still *looked* like it was floating above the platform in
Game-view screenshots. Numeric data said otherwise (collider bottom math
landed within ~0.01 units of the true surface at y=6, `Grounded: True`,
velocity stable at exactly `(0,0)` across two separate screenshots taken at
different times — proof it wasn't still actively falling).

To settle it, compared Scene/Game-view screenshots with Gizmos enabled
(collider bounds visible as a green box) in two situations: (a) right after
climbing onto the wall-top/ledge shelf, and (b) standing at rest on the plain
`Ground` platform, nowhere near any ledge logic. **Both showed a similar small
visual gap** between the collider box and the ground sprite. Since the
ledge-climb case isn't meaningfully different from ordinary standing, the gap
isn't something `LedgeGrabState` introduces — it's a general characteristic of
the vendor's sprite frames (some Idle/pose frames have more transparent
padding below the drawn boots than others), purely cosmetic.

**Conclusion: no further code change needed.** Ledge-grab (grab, climb via
Jump, drop via Roll) is functionally confirmed correct. This closes out
Task 11's last open runtime-verification item — see the "Known follow-up
items" list below for what's left (mostly cleanup + commit + Task 12).

If this visual gap ever bothers playtesting later, it would be a Task 12
polish item (e.g. re-checking the vendor sprite import's pivot per-frame), not
a logic fix — do not re-open `LedgeGrabState.cs`/`HeroKnightContext.cs` for
this again without new evidence it's climb-specific.

## Round 7 — Task 12 polish: collider/sprite gap closed

Post-commit (`20605f7`/`21620ed`), user asked about the small visual gap
between the collider and ground noted in round 6 (confirmed cosmetic, not a
logic bug). Live-tuned the vendor's `BoxCollider2D.offset.y` (originally
`0.662`) upward in the Inspector during Play mode until the boots sat flush
on the ground; landed on `0.68f`.

**Fix applied:** baked into `BuildPrefab()` in
`Assets/Editor/HeroKnightSandboxSetup.cs` (added right after the demo-script
removal) so it survives future prefab regenerations, since `Assets/Prefabs/HeroKnight.prefab`
is always rebuilt fresh from the vendor's untouched source prefab.
**Requires re-running `HeroKnightSandbox > 1 Build Prefab`** to take effect on
the actual prefab asset (this one **is** a wiring/asset change, unlike rounds
3/5's pure script-logic fixes) — no need to rebuild the scene, it references
the prefab by GUID. **Not yet re-tested or committed** — pick up here: ask
user to re-run Build Prefab, confirm the gap looks closed on both flat ground
and the wall-top ledge, then commit
`Assets/Editor/HeroKnightSandboxSetup.cs` + `Assets/Prefabs/HeroKnight.prefab`
together (small follow-up commit on top of `21620ed`).

## Round 8 — two more Task 12 bugs found after re-testing round 7's fix

After re-running Build Prefab for round 7's collider-offset fix, user found
two further issues while re-testing ledge-grab:

1. **Animator stuck in LedgeGrab clip after climbing.** `AddLedgeGrabToAnimator()`
   only ever added an entry transition (Any State → LedgeGrab). There was no
   exit transition at all, so once the Animator entered that state it played
   the clip forever regardless of the C# state machine — confirmed the C#
   side was already correctly in `IdleState` (per round 5/6's debug overlay
   data), just the *visual* animation never left LedgeGrab until an unrelated
   second Jump press happened to force it out via the vendor's own
   `AnyState → Jump` transition. **Fix:** added
   `AddLedgeGrabExitTransition()` in `HeroKnightSandboxSetup.cs`, called for
   two explicit exits: LedgeGrab → `Idle` (condition `AnimState == 0`, set by
   `IdleState.Enter()`) and LedgeGrab → `Fall` (condition `Grounded == false`,
   kept in sync every frame by `HeroKnightController.Update()`). Idempotent
   (checks `from.transitions` before adding) so reruns are safe.
2. **Character sprite rendering behind/"inside" the wall.** Both the
   character's `SpriteRenderer` and every `CreatePlatform()` terrain sprite
   defaulted to `sortingOrder: 0` — an unresolved tie. Since `LedgeGrabState.Enter()`
   snaps the character's X right against the wall's face, part of its sprite
   legitimately overlaps the wall's rendered rectangle, and the tie-break
   sometimes drew the wall on top. **Fix:** `BuildPrefab()` now explicitly
   sets the character's `SpriteRenderer.sortingOrder = 10`, unambiguously in
   front of all terrain (still default `0`).

**Both fixes require re-running menu items** (unlike rounds 3/5's pure
script-logic changes): **`HeroKnightSandbox > 1 Build Prefab`** (sorting
order + round 7's collider offset) **and `HeroKnightSandbox > 2 Add LedgeGrab
To Animator`** (exit transitions). No need to rebuild the scene. **Not yet
re-tested.** Pick up here: ask user to re-run both menu items in order, retest
ledge-grab climb, and confirm (a) the Idle animation plays immediately after
one Jump press, no second press needed, and (b) the character sprite stays
visually in front of the wall throughout the grab/climb.

Once confirmed, commit `Assets/Editor/HeroKnightSandboxSetup.cs`,
`Assets/Prefabs/HeroKnight.prefab`, and
`"Assets/Hero Knight - Pixel Art/Animations/HeroKnight_AnimController.controller"`
together as a follow-up commit on top of `21620ed`.

## Round 9 — round 8's animator exit fix regressed further (3 jumps needed, not 2); debug overlay re-added

User re-ran both `1 Build Prefab` and `2 Add LedgeGrab To Animator` and
retested. Got worse, not better: needed **3** jump presses to reach a normal
Idle stance (previously 2). No debug overlay was available for this test
(removed before the round 7/8 commits), so diagnosis was screenshot-only and
inconclusive.

**Suspected cause (not yet confirmed):** the two exit conditions added in
round 8 — `AnimState == 0` and `Grounded == false` — are likely **already
true from before the grab even happens**, not freshly true at climb/drop
time. Neither `WallSlideState` nor `LedgeGrabState` ever change `AnimState`
(only `IdleState`/`RunState` do), so it likely still reads whatever it was
before the wall-slide began. Similarly `Grounded` is already `false`
throughout the wall-slide/grab (that's *why* the grab was reachable at all).
If both conditions read true immediately upon entering the `LedgeGrab`
animator state, Unity's `hasExitTime=false, duration=0` transitions could
fire almost immediately rather than waiting for a real climb/drop decision —
though the observed "needs multiple jumps" symptom doesn't cleanly match a
"bounces back instantly" theory either, hence not yet confirmed.

**Action taken:** re-added a temporary debug overlay to
`HeroKnightController.cs` (this is a re-add — it had been removed prior to
the `20605f7`/`21620ed` commits). This version is richer than the earlier one:
shows the **C# state name** AND the **actual currently-playing Animator clip
name** side by side (via `Animator.GetCurrentAnimatorClipInfo(0)`), plus
`AnimState`/`Grounded` animator parameter values, sensor-based `Grounded`,
`IsWallSliding`, velocity, position, and body type. This directly answers
whether the C# logic and the visual Animator are in sync or diverged, which
plain screenshots couldn't settle.

**Not yet tested.** Pick up here: ask user to let it recompile, retest the
ledge-grab climb, and screenshot the overlay at each jump press (grab moment,
1st press, 2nd press if still needed) so we can see exactly where C# state
and Animator clip disagree. Do not further modify `AddLedgeGrabToAnimator()`'s
exit-transition logic until this data comes back — round 8's fix might need
a different signal entirely (e.g. a dedicated one-shot Trigger parameter set
explicitly by `LedgeGrabState.Tick()` at the moment of climb/drop, immune to
this kind of stale-value race, instead of reusing `AnimState`/`Grounded`).

Remember: **remove this debug overlay again before the next commit**, same as
before.

## Round 10 — root cause of round 9 confirmed, switched to dedicated one-shot triggers

Debug overlay data proved the theory: at the very moment of grabbing (before
any Jump press), overlay showed `C# State: LedgeGrabState` but
`Animator Clip: HeroKnight_Idle` already playing — the LedgeGrab clip never
really got a chance to show. Root cause confirmed: `AnimState==0` and
`Grounded==false` were both already true from *before* the grab (neither
`WallSlideState` nor `LedgeGrabState` ever touch `AnimState`; `Grounded` is
false throughout the whole wall-slide/grab by construction), so both
condition-based exit transitions added in round 8 fired almost immediately on
entering the state, desyncing the visual Animator from the still-active C#
`LedgeGrabState`. The player's first Jump press was actually the real climb
input (silently consumed correctly), but looked like nothing happened since
Idle was already showing, prompting a second press that read as a genuine new
jump.

**Fix applied (round 10):**
- `Assets/Scripts/HeroKnight/States/LedgeGrabState.cs`: fires
  `Context.Animator.SetTrigger("LedgeClimb")` right before the climb's
  `ChangeState(Controller.Idle)`, and `SetTrigger("LedgeDrop")` right before
  the drop's `ChangeState(Controller.Fall)` — dedicated one-shot signals set
  only at the exact moment of decision, immune to stale-parameter races.
- `Assets/Editor/HeroKnightSandboxSetup.cs`: `AddLedgeGrabToAnimator()` now
  adds `LedgeClimb`/`LedgeDrop` trigger parameters (via new
  `AddTriggerParameterIfMissing()` helper) and wires the two exit transitions
  on those triggers (`AnimatorConditionMode.If`) instead of
  `AnimState`/`Grounded`. `AddLedgeGrabExitTransition()` now **removes any
  existing transition to the same destination before adding the new one**
  (previously it skipped if one already existed) — necessary because round
  9's already-committed Animator Controller has the old, wrong-condition
  transitions baked in from the previous run; a plain "skip if exists" would
  have left them in place forever.

**Requires re-running `HeroKnightSandbox > 2 Add LedgeGrab To Animator`**
(script-only + Animator Controller asset change — no need for `1 Build
Prefab` or scene rebuild this time). **Not yet tested.** Pick up here: ask
user to let it recompile, re-run that one menu item, retest the ledge climb,
and confirm via the debug overlay that `Animator Clip` shows `HeroKnight_LedgeGrab`
(or whatever the clip is actually named) while `C# State: LedgeGrabState`,
and only switches to Idle's clip once Jump is pressed and `C# State` changes
too — i.e. the two should now change together, not desync.

Once confirmed: remove the debug overlay again, then commit
`Assets/Editor/HeroKnightSandboxSetup.cs`,
`Assets/Scripts/HeroKnight/States/LedgeGrabState.cs`, and
`"Assets/Hero Knight - Pixel Art/Animations/HeroKnight_AnimController.controller"`
(this round's Animator wiring changes) together — plus whatever's still
pending from round 7/8 (`Assets/Prefabs/HeroKnight.prefab`, sorting-order and
collider-offset changes) if not already committed by then.

## Round 11 — hang position overlapped the wall visually

User asked (no screenshot, conceptual question) whether the ledge-grab pose
should show the character hanging outside/beside the wall rather than
overlapping into it. Correct intuition: `LedgeGrabState.Enter()`'s X-snap
used `WallSensorR2`/`WallSensorL2`'s own position directly as the character's
body-center X — that sensor sits barely outside the main collider's own edge
(by design, to detect the wall just before the body touches it), so snapping
the body's *center* there put roughly half the body's width overlapping the
wall's footprint instead of hanging beside it. The sensor position marks
where the hand should reach, not where the torso belongs.

**Fix applied:** added `HeroKnightContext.LedgeHangOffset = 0.3f` (plain
field, not `[SerializeField]`) and changed `LedgeGrabState.Enter()` to pull
the snap position back by that amount, away from the wall on whichever side
was grabbed, instead of using the raw sensor X directly.

**Not yet tested — value is a starting guess, likely needs live-tuning** the
same way `LedgeClimbOffset`/the collider offset did. Pure script change, no
menu re-run needed, just recompile. Pick up here: ask user to retest the grab
pose and report whether `0.3f` looks right, too far out, or still overlapping
— adjust `LedgeHangOffset` accordingly (same iterate-by-eye approach as
round 7's collider offset).

## Round 12 — round 10's animator fix exposed a pre-existing re-grab loop

User tested round 11's hang-offset fix (visually correct: character now hangs
clear of the wall, `Animator Clip` correctly tracks `C# State` per round 10's
fix). But found a *new* symptom: needed 2-3 jump presses again, this time
with the overlay showing `C# State: LedgeGrabState` reappearing *after* a
climb had already been initiated (position data showed a real climb offset
had been applied between screenshots, then the state was back in
`LedgeGrabState` at a new, higher position).

**Root cause:** this bug likely always existed but was invisible before round
10 (when the Animator always jumped to Idle regardless of C# state, masking
any rapid re-cycling). The climb offset (`LedgeClimbOffset.y = 1.6`) doesn't
always land the character exactly on solid ground — overlay confirmed
`Grounded: False` immediately after one climb, meaning it landed slightly
above the true resting surface. `IdleState` correctly falls back to
`FallState` when ungrounded, and during that brief residual fall the wall
sensors can re-touch the wall before the character clears them, re-satisfying
`LedgeGrabState.CanGrab()` and looping straight back in. The **drop** branch
already guards against exactly this (`WallSensorR1-L2.Disable(RegrabCooldown)`
before transitioning to Fall) — the **climb** branch never had the same
guard.

**Fix applied:** added the identical `Disable(RegrabCooldown)` calls (0.3s)
to the climb branch in `LedgeGrabState.Tick()`, right after applying the
climb offset and before the state change. Pure script change, no menu
re-run needed.

**CONFIRMED WORKING.** User recompiled and retested: single Jump press now
reliably climbs and settles in `IdleState`, no re-grab loop. This closes out
ledge-grab (grab/hang position/climb/drop) as fully correct — all of Task 11
and the Task-12-adjacent polish items found during this session's playtesting
are done.

## Known follow-up items (do these once wall-slide/ledge-grab are confirmed)

1. **Remove the temporary debug overlay.** In
   `Assets/Scripts/HeroKnight/HeroKnightController.cs`, delete the `OnGUI()`
   method (currently the last method in the class, clearly marked
   `// TEMPORARY debug overlay for diagnosing wall-sensor detection -- remove
   once wall-slide/ledge-grab are confirmed working in the sandbox.`). Do not
   commit this method.
2. ~~Finish verifying the rest of Task 11~~ **DONE (round 6):** wall-slide and
   ledge-grab (grab/climb/drop) are now confirmed working, joining the
   already-confirmed jump/attack/block/roll. All of Task 11's runtime-behavior
   verification is complete. The only still-unverified detail is whether the
   Animator's `LedgeGrab` *visual clip* itself looks right (vs. just the state
   machine being in the right logical state) — worth a glance during Task 12,
   not a blocker.
3. Commit Task 11's output per the plan's own instructions (script + generated
   prefab/scene/sprite + Animator Controller changes + ProjectSettings diffs).
   Remember `.meta` files for every new asset (project convention — see
   root `CLAUDE.md` / `AGENTS.md`).
4. Move on to **Task 12**: full playtest and tuning. Exercise every state per
   the design's testing checklist — Idle→Run→Jump→Fall→WallSlide→LedgeGrab→
   Idle/Fall, Roll, Block, 3-hit Attack combo, and mutual exclusivity between
   them — tuning feel values (`HeroKnightContext` fields: `MoveSpeed`,
   `JumpForce`, `LedgeClimbOffset`, `RollForce`, `RollDuration`,
   `AttackComboWindow`, `AttackComboResetWindow`) as needed.

## Already confirmed working (earlier in this session, before the wall bug)

- Character spawns, falls onto the ground.
- Joystick makes it run.
- Jump works, attack works, block works, roll works.
- Camera follows correctly (after the Cinemachine `m_CameraDistance` fix).
- Terrain renders correctly (after pivoting from vendor tile sprites, which
  turned out to be tiny decorative pieces, to a generated solid ground sprite).

## Other bugs already fixed this session (for context, do not re-fix)

- `NewSceneSetupMode` typo → `NewSceneMode.Single`.
- Cinemachine vcam had no Body component → added `CinemachineFramingTransposer`
  with `m_CameraDistance = 10f`.
- Vendor `EnvironmentTiles` sprites were tiny/mostly-transparent → switched to
  a generated solid `GroundSprite.png` (`SpriteMeshType.FullRect`, idempotently
  re-applied every run so a stale cached import self-heals).
- Wall/gap geometry went through two bad iterations (gap too wide → character
  fell past it; gap narrowed too far → wall became contiguous with the ground
  so the character never went airborne) before settling on the current layout:
  a genuinely-open 3-unit gap (`Ground` right edge at x=18, `Wall` left face at
  x=21) and an effectively-bottomless wall (y=6 down to y=-10) plus a
  `SafetyNet` floor at y=-12.5 as a catch-all. This geometry itself was **not**
  the cause of the wall-slide bug — the sensor-namespace bug was.

## Key file locations

| What | Path |
|---|---|
| Design spec | `docs/superpowers/specs/2026-07-18-hero-knight-sandbox-design.md` |
| Implementation plan | `docs/superpowers/plans/2026-07-18-hero-knight-sandbox.md` |
| SDD progress ledger (git-ignored scratch, may not survive `git clean`) | `.superpowers/sdd/progress.md` |
| Production code (committed) | `Assets/Scripts/HeroKnight/**` |
| Editor automation (uncommitted) | `Assets/Editor/HeroKnightSandboxSetup.cs` |
| Generated prefab (uncommitted) | `Assets/Prefabs/HeroKnight.prefab` |
| Generated scene (uncommitted) | `Assets/Scenes/HeroKnightSandbox.unity` |
| Generated ground sprite (uncommitted) | `Assets/Prefabs/GroundSprite.png` |
| Vendor source prefab (read-only reference) | `Assets/Hero Knight - Pixel Art/Demo/HeroKnight.prefab` |
| Vendor's colliding global class (do not edit) | `Assets/Hero Knight - Pixel Art/Demo/Sensor_HeroKnight.cs` |

## How the user drives testing

The user's own Unity Editor is open interactively (so no batch-mode automation
is possible — confirmed via process inspection that a real interactive Editor
PID was running). The user runs menu items / enters Play mode themselves and
reports back Console text and screenshots (via absolute Windows file paths,
e.g. `c:\Users\SajanaNK\Desktop\Other\Unity\....PNG`, read directly with the
Read tool). Prefer asking for the on-screen debug overlay's text over asking
the user to interpret Scene-view gizmos — it's proven far more reliable in
this session.
