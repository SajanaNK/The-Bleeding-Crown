# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

This repository also has an [AGENTS.md](AGENTS.md) with the same guidance — keep both in sync if you update one.

## Project

Unity project built on Unity's **2D Platformer Microgame** template (Unity **2022.3.41f1**, URP). See [Assets/PlatformerMicrogame_README.txt](Assets/PlatformerMicrogame_README.txt) for the upstream template changelog and [Assets/Documentation/PlatformerTemplateUserGuide.pdf](Assets/Documentation/PlatformerTemplateUserGuide.pdf) for the general template user guide — don't duplicate that content here.

## Architecture

Gameplay code lives in `Assets/Scripts`, namespaced `Platformer.*` matching folder names, and follows a discrete-event-simulation pattern rather than classic MVC or ScriptableObject event channels:

| Folder | Namespace | Role |
|---|---|---|
| [Core](Assets/Scripts/Core) | `Platformer.Core` | The engine. `Simulation` (static partial class, split across `Simulation.cs` / `Simulation.Event.cs` / `Simulation.InstanceRegister.cs`) schedules and pools `Event` objects in a priority `HeapQueue`, ticked once per frame via `GameController.Update()`. `InstanceRegister<T>` is a generic singleton holder for model instances. |
| [Gameplay](Assets/Scripts/Gameplay) | `Platformer.Gameplay` | Concrete `Simulation.Event<T>` subclasses (`PlayerJumped`, `EnemyDeath`, `HealthIsZero`, ...) — one class per discrete game occurrence, each implementing `Execute()`. |
| [Mechanics](Assets/Scripts/Mechanics) | `Platformer.Mechanics` | `MonoBehaviour`s living in the scene (`PlayerController`, `EnemyController`, `Health`, `GameController`, ...). These react to input/physics and **schedule Gameplay events** rather than mutating state directly, e.g. `Schedule<PlayerJumped>().player = this;`. |
| [Model](Assets/Scripts/Model) | `Platformer.Model` | `PlatformerModel` — a single plain `[Serializable]` data-only class, fetched via `Simulation.GetModel<PlatformerModel>()`. Keep new model classes data-only; put behavior in Mechanics/Gameplay. |
| [UI](Assets/Scripts/UI) | `Platformer.UI` | Menu/HUD panel switching (`MainUIController`, `MetaGameController`). |
| [View](Assets/Scripts/View) | `Platformer.View` | Presentation-only components with no gameplay logic (`ParallaxLayer`, `AnimatedTile`). |

**When adding a new gameplay occurrence**: create a `Simulation.Event<T>` subclass in `Gameplay/`, and call it from a `Mechanics` `MonoBehaviour` via `Simulation.Schedule<T>()` — don't wire gameplay reactions directly between `MonoBehaviour`s.

Events are pooled (`Simulation.New<T>`/`eventPools`) and run in `Simulation.Tick()`, which pops every event whose `tick <= Time.time` from the `HeapQueue`, calls `ExecuteEvent()`, and returns it to its type's pool unless it was rescheduled to a later tick during execution.

## Conventions

- `GameController.Instance` is a manual static singleton (set in `OnEnable`, cleared in `OnDisable`).
- One class per file, file name matches class name, every `.cs` has a paired `.meta` (standard Unity — don't create/edit `.meta` files by hand, let the Unity editor manage them).
- Public fields are used for Inspector-exposed values (no `_`/`m_` prefixes); XML doc comments (`/// <summary>`) are used heavily on public members — match this style in new code.
- No Input System package is installed — input uses the legacy `Input.GetAxis(...)` / `Input.GetButtonDown(...)` API (see `PlayerController.Update()`).
- Default `Assembly-CSharp` compiles `Assets/Scripts`; only `Assets/Tutorials` has its own `.asmdef` ([Unity.Platformer.Tutorials.asmdef](Assets/Tutorials/Unity.Platformer.Tutorials.asmdef)).

## Build & Test

- This is a Unity Editor project — there is no CLI build/test step for an agent to run; open/build/play through the Unity Editor (2022.3.41f1).
- [com.unity.test-framework](Packages/manifest.json) is installed but **no tests currently exist** in the project. If adding tests, use the Unity Test Runner (NUnit) and place them under a new `Assets/Tests` folder with its own `.asmdef`.

## Other notable folders

- [Assets/Mod Assets](Assets/Mod%20Assets) — modding-support prefabs/materials and optional example mechanics (`PlatformerJumpPad`, `PlatformerSpeedPad`, `Jiggler`, ...). `Backend/TemplateEditorDetection.cs` sets scripting define symbols like `UNITY_TEMPLATE_PLATFORMER` used to conditionally compile mod scripts — not core game code, edit with care.
- [Assets/Editor/PatrolPathEditor.cs](Assets/Editor/PatrolPathEditor.cs) — the only custom editor tool, a gizmo/handle editor for `PatrolPath`.
- [Assets/Scenes](Assets/Scenes) currently contains only the stock `SampleScene.unity`.
