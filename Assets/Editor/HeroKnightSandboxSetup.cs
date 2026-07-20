using System.Linq;
using Cinemachine;
using CodeMonkey.HealthSystemCM;
using HeroKnightSandbox;
using HeroKnightSandbox.Enemy;
using HeroKnightSandbox.Input;
using HeroKnightSandbox.Objectives;
using HeroKnightSandbox.Sensors;
using HeroKnightSandbox.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace HeroKnightSandbox.EditorTools
{
/// <summary>
/// One-off automation for Task 11 of docs/superpowers/plans/2026-07-18-hero-knight-sandbox.md
/// (prefab, Animator Controller, scene, project settings). Run via:
/// Unity.exe -batchmode -nographics -quit -projectPath &lt;path&gt; -executeMethod HeroKnightSandbox.EditorTools.HeroKnightSandboxSetup.RunAll
/// Safe to re-run: each step overwrites/idempotently checks its own output.
///
/// The vendor asset pack's Demo/Sensor_HeroKnight.cs declares a global-scope class
/// named Sensor_HeroKnight (no namespace). C# always resolves a simple name against
/// the global namespace's own member declarations before it ever considers a `using`
/// import for that name — no amount of namespacing THIS file changes that, since the
/// global namespace is checked on the way out regardless of how deep this file's own
/// namespace is nested. So every Sensor_HeroKnight reference below is fully qualified
/// (HeroKnightSandbox.Sensors.Sensor_HeroKnight) rather than relying on the `using
/// HeroKnightSandbox.Sensors;` import above — that import silently loses to the
/// vendor's global class with no compile error, just wrong components wired onto the
/// prefab. (Found by inspecting the generated prefab's script GUIDs.)
///
/// Similarly, this branch's HeroKnightSandbox.Enemy.EnemyController (and its
/// HurtState/AttackState, which also share names with existing player-side classes)
/// are referenced unqualified below in reliance on this file having no `using
/// Platformer.Mechanics;` import. Platformer.Mechanics also declares its own
/// EnemyController (a different, pre-existing class from the base 2D Platformer
/// Microgame template). If a `using Platformer.Mechanics;` import is ever added here,
/// `EnemyController` becomes an ambiguous reference (CS0104) between the two types and
/// would need to be fully qualified as HeroKnightSandbox.Enemy.EnemyController at its
/// two use sites in this file (root.AddComponent&lt;EnemyController&gt;() and
/// enemyGO.GetComponent&lt;EnemyController&gt;()).
/// </summary>
public static class HeroKnightSandboxSetup
{
    private const string SourcePrefabPath = "Assets/Hero Knight - Pixel Art/Demo/HeroKnight.prefab";
    private const string DestPrefabPath = "Assets/Prefabs/HeroKnight.prefab";
    private const string ControllerPath = "Assets/Hero Knight - Pixel Art/Animations/HeroKnight_AnimController.controller";
    private const string LedgeClipPath = "Assets/Hero Knight - Pixel Art/Animations/HeroKnight_LedgeGrab.anim";
    private const string ScenePath = "Assets/Scenes/HeroKnightSandbox.unity";
    private const string PhysicsMaterialPath = "Assets/Hero Knight - Pixel Art/Environment/Walls_noFriction.physicsMaterial2D";
    private const string EnemySourcePrefabPath = "Assets/Bandits - Pixel Art/Demo/LightBandit.prefab";
    // Not "Enemy.prefab" - that path was already a pre-existing asset from the base 2D
    // Platformer Microgame template (Platformer.Mechanics.EnemyController-based, used by
    // SampleScene.unity). An earlier version of this script wrote to that same path and
    // silently overwrote the template's prefab in place, corrupting SampleScene.unity's
    // two enemy instances (their PrefabInstance modifications pointed at component
    // fileIDs that no longer existed once the file's contents were replaced). Confirmed
    // by finding SampleScene.unity's own guid reference to the original file. Fixed by
    // using a distinct filename so this script's output never collides with pre-existing
    // template assets.
    private const string EnemyDestPrefabPath = "Assets/Prefabs/HeroKnightEnemy.prefab";
    private const string HeavyEnemySourcePrefabPath = "Assets/Bandits - Pixel Art/Demo/HeavyBandit.prefab";
    private const string HeavyEnemyDestPrefabPath = "Assets/Prefabs/HeroKnightHeavyEnemy.prefab";
    private const string ProjectileSpritePath = "Assets/FlexUnit/2DMedievalWeaponPack/LQ/Sprites/Bow/Arrow.png";
    private const string NaturePalettePath = "Assets/Nature_pixel_art_assets/palette/Nature_environment_01.prefab";
    private const string NaturePropsFolder = "Assets/Nature_pixel_art_assets/Prefabs/Nature_props/";
    private const string CompletionSoundPath = "Assets/Audio/Collectable.wav";
    private const string ConfettiPrefabPath = "Assets/Mod Assets/Particle Prefabs/ConfettiCelebration.prefab";
    private const string PlayerHealthBarPrefabPath = "Assets/CodeMonkey/HealthSystem/Prefabs/pfHealthBarUI.prefab";
    private const string EnemyHealthBarPrefabPath = "Assets/CodeMonkey/HealthSystem/Prefabs/pfHealthBarUIWorldCanvas.prefab";
    private const string TerrainTilesPrefabPath = "Assets/Prefabs/Level1_TerrainTiles.prefab";
    private const string UIFontPath = "Assets/Mod Assets/Mod Resources/Fonts/PressStart2P-Regular SDF.asset";

    private static TMP_FontAsset UIFont => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UIFontPath);

    [MenuItem("HeroKnightSandbox/1 Build Prefab")]
    public static void BuildPrefab()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        GameObject root = PrefabUtility.LoadPrefabContents(SourcePrefabPath);

        var demoScript = root.GetComponent("HeroKnight");
        if (demoScript != null)
        {
            Object.DestroyImmediate(demoScript, true);
        }

        // Vendor's own BoxCollider2D offset (Y: 0.662) leaves a small but visible gap
        // between the collider's resting contact point and the sprite's drawn boots on
        // this level's flat ground/ledge surfaces. Nudged up live in the Inspector during
        // playtesting until the boots sat flush; 0.68 was the value that looked right.
        BoxCollider2D bodyCollider = root.GetComponent<BoxCollider2D>();
        bodyCollider.offset = new Vector2(bodyCollider.offset.x, 0.68f);

        // Terrain sprites from CreatePlatform() below and the character's own
        // SpriteRenderer both default to sortingOrder 0, an unresolved tie that Unity
        // breaks by an undefined/arbitrary criterion. Confirmed live during ledge-grab
        // testing: with the character's Transform positioned right at the wall's edge
        // (as LedgeGrabState's X-snap does), the terrain sprite sometimes drew in front,
        // making the character appear to render "inside" the wall. Forcing the character
        // strictly in front of all terrain removes the ambiguity.
        root.GetComponent<SpriteRenderer>().sortingOrder = 10;

        HeroKnightController controller = root.AddComponent<HeroKnightController>();

        HeroKnightSandbox.Sensors.Sensor_HeroKnight groundSensor = ReplaceSensor(root.transform.Find("GroundSensor"));
        HeroKnightSandbox.Sensors.Sensor_HeroKnight wallR1 = ReplaceSensor(root.transform.Find("WallSensor_R1"));
        HeroKnightSandbox.Sensors.Sensor_HeroKnight wallR2 = ReplaceSensor(root.transform.Find("WallSensor_R2"));
        HeroKnightSandbox.Sensors.Sensor_HeroKnight wallL1 = ReplaceSensor(root.transform.Find("WallSensor_L1"));
        HeroKnightSandbox.Sensors.Sensor_HeroKnight wallL2 = ReplaceSensor(root.transform.Find("WallSensor_L2"));

        HeroKnightSandbox.Sensors.Sensor_HeroKnight ledgeR = CreateLedgeSensor(root.transform, "LedgeSensor_R", wallR2.transform.localPosition);
        HeroKnightSandbox.Sensors.Sensor_HeroKnight ledgeL = CreateLedgeSensor(root.transform, "LedgeSensor_L", wallL2.transform.localPosition);

        var so = new SerializedObject(controller);
        so.FindProperty("groundSensor").objectReferenceValue = groundSensor;
        so.FindProperty("wallSensorR1").objectReferenceValue = wallR1;
        so.FindProperty("wallSensorR2").objectReferenceValue = wallR2;
        so.FindProperty("wallSensorL1").objectReferenceValue = wallL1;
        so.FindProperty("wallSensorL2").objectReferenceValue = wallL2;
        so.FindProperty("ledgeSensorR").objectReferenceValue = ledgeR;
        so.FindProperty("ledgeSensorL").objectReferenceValue = ledgeL;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, DestPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        Debug.Log("HeroKnightSandboxSetup: prefab built at " + DestPrefabPath);
    }

    private static HeroKnightSandbox.Sensors.Sensor_HeroKnight ReplaceSensor(Transform child)
    {
        if (child == null)
        {
            throw new System.Exception("Sensor child not found on source prefab");
        }

        // Matches by class name regardless of namespace (Unity's string-based
        // GetComponent), so this strips the vendor's global-scope Sensor_HeroKnight
        // that's attached on the source prefab.
        Component old = child.GetComponent("Sensor_HeroKnight");
        if (old != null)
        {
            Object.DestroyImmediate(old, true);
        }

        return child.gameObject.AddComponent<HeroKnightSandbox.Sensors.Sensor_HeroKnight>();
    }

    private static HeroKnightSandbox.Sensors.Sensor_HeroKnight CreateLedgeSensor(Transform parent, string name, Vector3 wallSensorLocalPos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(wallSensorLocalPos.x, wallSensorLocalPos.y + 0.5f, 0f);

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.05f;
        col.offset = new Vector2(0f, 0.1f);

        return go.AddComponent<HeroKnightSandbox.Sensors.Sensor_HeroKnight>();
    }

    [MenuItem("HeroKnightSandbox/2 Add LedgeGrab To Animator")]
    public static void AddLedgeGrabToAnimator()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            throw new System.Exception("Animator controller not found at " + ControllerPath);
        }

        AddTriggerParameterIfMissing(controller, "LedgeGrab");
        AddTriggerParameterIfMissing(controller, "LedgeClimb");
        AddTriggerParameterIfMissing(controller, "LedgeDrop");

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState state = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "LedgeGrab");
        if (state == null)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LedgeClipPath);
            if (clip == null)
            {
                throw new System.Exception("LedgeGrab clip not found at " + LedgeClipPath);
            }

            state = sm.AddState("LedgeGrab");
            state.motion = clip;

            AnimatorStateTransition t = sm.AddAnyStateTransition(state);
            t.AddCondition(AnimatorConditionMode.If, 0, "LedgeGrab");
            t.duration = 0f;
            t.exitTime = 0f;
            t.hasExitTime = false;
            t.hasFixedDuration = false;
            t.interruptionSource = TransitionInterruptionSource.None;
            t.orderedInterruption = true;
            t.canTransitionToSelf = true;
        }

        // LedgeGrab previously had no way OUT: only the AnyState entry transition above
        // was ever added, so once the Animator entered this state it stayed there
        // forever, regardless of what the C# state machine did next -- confirmed live:
        // after climbing, LedgeGrabState.Tick() correctly moves to Controller.Idle, but
        // the Animator kept playing the LedgeGrab clip until an unrelated second Jump
        // input happened to force it out via the vendor's own AnyState->Jump transition.
        //
        // First attempt used AnimState==0 / Grounded==false as the exit conditions --
        // both WRONG, confirmed live via debug overlay: neither WallSlideState nor
        // LedgeGrabState ever change AnimState, and Grounded is already false the whole
        // time we're hanging (that's how the grab became reachable), so both conditions
        // already read as "exit" from the instant LedgeGrab is entered, firing the
        // transition almost immediately -- the Animator jumped straight to Idle while
        // the C# state machine was still legitimately in LedgeGrabState waiting for
        // input. Fixed by using two dedicated one-shot Trigger parameters instead,
        // fired explicitly by LedgeGrabState.Tick() only at the moment it actually
        // decides to climb or drop -- immune to this kind of stale-value race.
        AnimatorState idleState = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "Idle");
        AnimatorState fallState = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "Fall");
        AddOneShotExitTransition(state, idleState, AnimatorConditionMode.If, "LedgeClimb", 0f);
        AddOneShotExitTransition(state, fallState, AnimatorConditionMode.If, "LedgeDrop", 0f);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log("HeroKnightSandboxSetup: LedgeGrab animator state/parameter/transition ready");
    }

    private static void AddTriggerParameterIfMissing(AnimatorController controller, string name)
    {
        bool hasParam = controller.parameters.Any(p => p.name == name && p.type == AnimatorControllerParameterType.Trigger);
        if (!hasParam)
        {
            controller.AddParameter(name, AnimatorControllerParameterType.Trigger);
        }
    }

    private static void AddOneShotExitTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, string parameter, float threshold)
    {
        if (to == null)
        {
            throw new System.Exception("Animator state not found: expected exit target for LedgeGrab (name lookup failed)");
        }

        // Remove any transition to this destination from an earlier run of this method
        // (e.g. the AnimState/Grounded-based one this replaces) rather than skip when
        // one already exists -- a rerun must always reflect the current condition logic
        // below, not silently keep whatever an earlier script version wired up.
        foreach (AnimatorStateTransition existing in from.transitions.Where(t => t.destinationState == to).ToList())
        {
            from.RemoveTransition(existing);
        }

        AnimatorStateTransition transition = from.AddTransition(to);
        transition.AddCondition(mode, threshold, parameter);
        transition.duration = 0f;
        transition.exitTime = 0f;
        transition.hasExitTime = false;
        transition.hasFixedDuration = false;
        transition.interruptionSource = TransitionInterruptionSource.None;
    }

    [MenuItem("HeroKnightSandbox/7 Add Death Respawn To Animator")]
    public static void AddDeathRespawnToAnimator()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            throw new System.Exception("Animator controller not found at " + ControllerPath);
        }

        AddTriggerParameterIfMissing(controller, "Respawn");

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState deathState = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "Death");
        AnimatorState idleState = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "Idle");
        if (deathState == null)
        {
            throw new System.Exception("Animator state not found: Death");
        }

        // Death has no outgoing transitions in the vendor controller (it's a dead end for
        // enemies, which get destroyed instead of leaving it) - see Respawn()'s comment.
        AddOneShotExitTransition(deathState, idleState, AnimatorConditionMode.If, "Respawn", 0f);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log("HeroKnightSandboxSetup: Death/Respawn animator transition ready");
    }

    [MenuItem("HeroKnightSandbox/3 Build Scene")]
    public static void BuildScene()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Camera mainCam = Camera.main;
        mainCam.orthographic = true;
        mainCam.orthographicSize = 6f;
        mainCam.transform.position = new Vector3(-3f, 2f, -10f);
        mainCam.gameObject.AddComponent<CinemachineBrain>();

        PhysicsMaterial2D noFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(PhysicsMaterialPath);

        GameObject terrainRoot = new GameObject("Terrain");

        // Collision footprint is owned entirely by these BoxCollider2D calls (unchanged
        // from before the Nature tileset was added) so it stays 100% reproducible via
        // RunAll() -- enemy PatrolPath anchors, ledge-grab, and spawn positions all key
        // off these same rectangles. Visuals are handled separately by a hand-painted
        // Tilemap (see CreateTerrainTilemap below) rather than by these platforms'
        // (removed) placeholder SpriteRenderers.

        // Flat run: open ground for run/roll testing. Top surface at y = 0.
        CreatePlatform(terrainRoot.transform, "Ground", new Vector2(6f, -0.5f), new Vector2(24f, 1f), noFriction);

        // Raised platform: reachable by a jump.
        CreatePlatform(terrainRoot.transform, "JumpPlatform", new Vector2(9f, 2.5f), new Vector2(3f, 1f), noFriction);

        // Wall face: walking off the flat run's right edge (at x=18) leaves 3 full units
        // of open air (x 18-21, nothing there at any height near ground level) before the
        // wall starts -- wide enough that the character is unmistakably airborne (in
        // FallState, which is the only state that checks for wall contact) well before
        // reaching it, not stopped at the ground's edge while still grounded. The wall
        // then extends from y=6 down to y=-10 -- effectively bottomless within this
        // level -- so no fall trajectory across that gap can miss its vertical range
        // and slip past underneath it, whatever the exact fall depth turns out to be.
        CreatePlatform(terrainRoot.transform, "Wall", new Vector2(21.5f, -2f), new Vector2(1f, 16f), noFriction);

        // Ledge platform on top of the wall (the wall "ends" here at y=6).
        CreatePlatform(terrainRoot.transform, "LedgePlatform", new Vector2(25f, 5.5f), new Vector2(6f, 1f), noFriction);

        // Safety-net floor, well below the wall's own bottom, in case the character
        // drops straight down through the gap without drifting into the wall at all.
        CreatePlatform(terrainRoot.transform, "SafetyNet", new Vector2(20f, -12.5f), new Vector2(50f, 1f), noFriction);

        CreateTerrainTilemap(terrainRoot.transform);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DestPrefabPath);
        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        player.transform.position = new Vector3(-3f, 0.5f, 0f);

        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.AddComponent<EventSystem>();
        eventSystemGO.AddComponent<StandaloneInputModule>();

        GameObject touchInputGO = new GameObject("TouchControls");
        touchInputGO.transform.SetParent(canvasGO.transform, false);
        TouchHeroKnightInput touchInput = touchInputGO.AddComponent<TouchHeroKnightInput>();

        VirtualJoystick joystick = BuildJoystick(canvasGO.transform);
        TouchButton jumpBtn = BuildButton(canvasGO.transform, "JumpButton", new Vector2(-100, 160));
        TouchButton attackBtn = BuildButton(canvasGO.transform, "AttackButton", new Vector2(-220, 100));
        TouchButton blockBtn = BuildButton(canvasGO.transform, "BlockButton", new Vector2(-100, 40));
        TouchButton rollBtn = BuildButton(canvasGO.transform, "RollButton", new Vector2(-220, 220));

        var touchInputSO = new SerializedObject(touchInput);
        touchInputSO.FindProperty("joystick").objectReferenceValue = joystick;
        touchInputSO.FindProperty("jumpButton").objectReferenceValue = jumpBtn;
        touchInputSO.FindProperty("attackButton").objectReferenceValue = attackBtn;
        touchInputSO.FindProperty("blockButton").objectReferenceValue = blockBtn;
        touchInputSO.FindProperty("rollButton").objectReferenceValue = rollBtn;
        touchInputSO.ApplyModifiedPropertiesWithoutUndo();

        var controllerSO = new SerializedObject(player.GetComponent<HeroKnightController>());
        controllerSO.FindProperty("input").objectReferenceValue = touchInput;
        controllerSO.ApplyModifiedPropertiesWithoutUndo();

        GameObject vcamGO = new GameObject("CM Player Camera");
        CinemachineVirtualCamera vcam = vcamGO.AddComponent<CinemachineVirtualCamera>();
        vcam.Follow = player.transform;
        vcam.m_Lens.OrthographicSize = 6f;

        // A vcam with no Body component just sits at its own Transform (world origin by
        // default) instead of tracking Follow, and CinemachineBrain then drives the Main
        // Camera to that same position/depth as the sprites, clipping everything invisible.
        // FramingTransposer tracks Follow in X/Y while holding a fixed Z distance.
        CinemachineFramingTransposer framingTransposer = vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
        framingTransposer.m_CameraDistance = 10f;

        GameObject boundsGO = new GameObject("CameraBounds");
        PolygonCollider2D bounds = boundsGO.AddComponent<PolygonCollider2D>();
        bounds.isTrigger = true;
        bounds.points = new[]
        {
            new Vector2(-10, -16),
            new Vector2(-10, 10),
            new Vector2(44, 10),
            new Vector2(44, -16),
        };

        CinemachineConfiner2D confiner = vcamGO.AddComponent<CinemachineConfiner2D>();
        confiner.m_BoundingShape2D = bounds;

        EditorSceneManager.SaveScene(scene, ScenePath);

        Debug.Log("HeroKnightSandboxSetup: scene built at " + ScenePath);
    }

    [MenuItem("HeroKnightSandbox/3b Save Terrain Tiles")]
    public static void SaveTerrainTiles()
    {
        GameObject terrain = GameObject.Find("Terrain");
        if (terrain == null)
        {
            throw new System.Exception("Terrain instance not found in the open scene - run '3 Build Scene' first");
        }

        Transform tiles = terrain.transform.Find("TerrainTiles");
        if (tiles == null)
        {
            throw new System.Exception("TerrainTiles not found under Terrain - run '3 Build Scene' first");
        }

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        PrefabUtility.SaveAsPrefabAsset(tiles.gameObject, TerrainTilesPrefabPath);

        Debug.Log("HeroKnightSandboxSetup: terrain tiles saved to " + TerrainTilesPrefabPath +
                   " - future '3 Build Scene' runs restore them automatically instead of starting empty");
    }

    private static void CreatePlatform(Transform parent, string name, Vector2 center, Vector2 size,
        PhysicsMaterial2D material)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(center.x, center.y, 0f);

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        col.sharedMaterial = material;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
    }

    // Sets up a Grid/Tilemap for hand-painting with the Nature tileset -- the actual
    // tile placement is done by hand in the Editor via the Tile Palette, since that's
    // this asset's intended workflow (unlike the code-generated collision footprint
    // above, which must stay reproducible for the enemy/ledge tuning that keys off
    // it). Once painted tiles exist at TerrainTilesPrefabPath (see SaveTerrainTiles()
    // below), reruns restore them here instead of leaving an empty grid -- BuildScene()
    // wipes the whole scene via NewScene() every time, so without this every Run All
    // would otherwise mean repainting from scratch.
    //
    // Grid.cellSize defaults to (1,1,1) and the tileset's sprites are imported at 48
    // pixels-per-unit for a 48x48 source size, so 1 tile == 1 Unity unit == 1 cell,
    // lining up directly with the collider rectangles above (all of which are
    // integer-aligned except JumpPlatform, whose x=9 center with width 3 spans
    // [7.5, 10.5] -- painting 4 cells (x=7..10) over-covers that platform by 0.5 unit
    // on each side rather than under-covering, since a visible edge lip is harmless but
    // a collider extending past the visible tile would look like invisible ground).
    private static void CreateTerrainTilemap(Transform parent)
    {
        GameObject existingTiles = AssetDatabase.LoadAssetAtPath<GameObject>(TerrainTilesPrefabPath);
        if (existingTiles != null)
        {
            PrefabUtility.InstantiatePrefab(existingTiles, parent);
            Debug.Log("HeroKnightSandboxSetup: restored hand-painted TerrainTiles from " + TerrainTilesPrefabPath);
            return;
        }

        GameObject gridGO = new GameObject("TerrainTiles");
        gridGO.transform.SetParent(parent, false);
        Grid grid = gridGO.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        GameObject tilemapGO = new GameObject("Ground Tiles");
        tilemapGO.transform.SetParent(gridGO.transform, false);
        tilemapGO.AddComponent<Tilemap>();
        TilemapRenderer renderer = tilemapGO.AddComponent<TilemapRenderer>();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 0;

        GameObject palette = AssetDatabase.LoadAssetAtPath<GameObject>(NaturePalettePath);
        if (palette == null)
        {
            Debug.LogWarning("HeroKnightSandboxSetup: Nature tile palette not found at " + NaturePalettePath);
        }
        else
        {
            EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
            UnityEditor.Tilemaps.GridPaintingState.palette = palette;
        }

        Debug.Log(
            "HeroKnightSandboxSetup: 'Ground Tiles' Tilemap created under Terrain/TerrainTiles. " +
            "Open Window > 2D > Tile Palette, select 'Nature_environment_01', and hand-paint these " +
            "cell rectangles to match the existing colliders:\n" +
            "  Ground: x=-6..17, y=-1 (24x1)\n" +
            "  JumpPlatform: x=7..10, y=2 (4x1, 0.5-unit overhang each side by design)\n" +
            "  Wall: x=21, y=-10..5 (1x16)\n" +
            "  LedgePlatform: x=22..27, y=5 (6x1)\n" +
            "  SafetyNet: x=-5..44, y=-13 (50x1)");
    }

    private static VirtualJoystick BuildJoystick(Transform canvasTransform)
    {
        GameObject bgGO = new GameObject("Joystick_Background");
        bgGO.transform.SetParent(canvasTransform, false);
        RectTransform bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0f);
        bgRT.anchorMax = new Vector2(0f, 0f);
        bgRT.pivot = new Vector2(0.5f, 0.5f);
        bgRT.anchoredPosition = new Vector2(180f, 180f);
        bgRT.sizeDelta = new Vector2(220f, 220f);
        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(1f, 1f, 1f, 0.25f);

        GameObject handleGO = new GameObject("Joystick_Handle");
        handleGO.transform.SetParent(bgGO.transform, false);
        RectTransform handleRT = handleGO.AddComponent<RectTransform>();
        handleRT.anchorMin = new Vector2(0.5f, 0.5f);
        handleRT.anchorMax = new Vector2(0.5f, 0.5f);
        handleRT.pivot = new Vector2(0.5f, 0.5f);
        handleRT.anchoredPosition = Vector2.zero;
        handleRT.sizeDelta = new Vector2(100f, 100f);
        Image handleImage = handleGO.AddComponent<Image>();
        handleImage.color = new Color(1f, 1f, 1f, 0.6f);

        VirtualJoystick joystick = bgGO.AddComponent<VirtualJoystick>();
        var so = new SerializedObject(joystick);
        so.FindProperty("background").objectReferenceValue = bgRT;
        so.FindProperty("handle").objectReferenceValue = handleRT;
        so.FindProperty("handleRange").floatValue = 80f;
        so.ApplyModifiedPropertiesWithoutUndo();

        return joystick;
    }

    private static TouchButton BuildButton(Transform canvasTransform, string name, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(canvasTransform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(100f, 100f);
        Image image = go.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.35f);

        return go.AddComponent<TouchButton>();
    }

    [MenuItem("HeroKnightSandbox/4 Finalize Project Settings")]
    public static void FinalizeProjectSettings()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        // LevelSelectSetup.RegisterAllScenes() is the single authoritative place for
        // Build Settings scene order across the whole StartScreen/LevelSelect/level
        // menu flow - see its own comment for why.
        LevelSelectSetup.RegisterAllScenes();

        AssetDatabase.SaveAssets();

        Debug.Log("HeroKnightSandboxSetup: project settings finalized");
    }

    private static GameObject BuildEnemyPrefab(string sourcePrefabPath, string destPrefabPath)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        GameObject root = PrefabUtility.LoadPrefabContents(sourcePrefabPath);

        var demoScript = root.GetComponent("Bandit");
        if (demoScript != null)
        {
            Object.DestroyImmediate(demoScript, true);
        }

        Rigidbody2D body = root.GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;

        EnemyController enemyController = root.AddComponent<EnemyController>();
        var enemySO = new SerializedObject(enemyController);
        enemySO.FindProperty("healthBarPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(EnemyHealthBarPrefabPath);
        enemySO.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, destPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        return AssetDatabase.LoadAssetAtPath<GameObject>(destPrefabPath);
    }

    private static void CreateEnemy(string name, GameObject enemyPrefab, Vector2 anchorPosition,
        Vector2 startOffset, Vector2 endOffset, HeroKnightController player,
        int maxHP = 3, float moveSpeed = 2.0f, int attackDamage = 1, float attackRange = 1.0f, bool ranged = false)
    {
        Sprite projectileSprite = ranged ? AssetDatabase.LoadAssetAtPath<Sprite>(ProjectileSpritePath) : null;
        // Destroy-and-recreate rather than skip-if-exists: a rerun must always reconnect
        // to the current enemyPrefab (e.g. after BuildEnemyPrefab() rebuilds it), matching
        // BuildPrefab()'s own always-rebuild convention for the player prefab. Skipping
        // when the name already existed left old instances silently pointing at a stale
        // or renamed prefab asset.
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
        so.FindProperty("maxHP").intValue = maxHP;
        so.FindProperty("moveSpeed").floatValue = moveSpeed;
        so.FindProperty("attackDamage").intValue = attackDamage;
        so.FindProperty("attackRange").floatValue = attackRange;
        so.FindProperty("ranged").boolValue = ranged;
        so.FindProperty("projectileSprite").objectReferenceValue = projectileSprite;
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
        GameObject enemyPrefab = BuildEnemyPrefab(EnemySourcePrefabPath, EnemyDestPrefabPath);

        // Both on the flat Ground platform (top at y=0, spans x -6..18 - see BuildScene()),
        // spread apart so one can be tested in isolation before walking further to reach
        // both at once, without needing a jump/wall-slide/ledge-grab to reach either.
        CreateEnemy("Enemy_1", enemyPrefab, new Vector2(4f, 0.5f), new Vector2(-1.5f, 0f), new Vector2(1.5f, 0f), controller);
        CreateEnemy("Enemy_2", enemyPrefab, new Vector2(11f, 0.5f), new Vector2(-1.5f, 0f), new Vector2(1.5f, 0f), controller);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("HeroKnightSandboxSetup: enemies built");
    }

    [MenuItem("HeroKnightSandbox/6 Build Heavy Enemy")]
    public static void BuildHeavyEnemy()
    {
        GameObject player = GameObject.Find("HeroKnight");
        if (player == null)
        {
            throw new System.Exception("HeroKnight instance not found in the open scene - run '3 Build Scene' first");
        }

        HeroKnightController controller = player.GetComponent<HeroKnightController>();
        GameObject heavyPrefab = BuildEnemyPrefab(HeavyEnemySourcePrefabPath, HeavyEnemyDestPrefabPath);

        // Placed on JumpPlatform (center 9,2.5, size 3x1 - see BuildScene()), distinct from
        // both Light Bandits on the flat Ground platform below, so it reads as a separate,
        // tougher encounter reachable only after a jump.
        CreateEnemy("HeavyEnemy_1", heavyPrefab, new Vector2(9f, 3f), new Vector2(-1f, 0f), new Vector2(1f, 0f), controller,
            maxHP: 6, moveSpeed: 1.2f, attackDamage: 2);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("HeroKnightSandboxSetup: heavy enemy built");
    }

    [MenuItem("HeroKnightSandbox/9 Build Ranged Enemy")]
    public static void BuildRangedEnemy()
    {
        GameObject player = GameObject.Find("HeroKnight");
        if (player == null)
        {
            throw new System.Exception("HeroKnight instance not found in the open scene - run '3 Build Scene' first");
        }

        HeroKnightController controller = player.GetComponent<HeroKnightController>();
        // Reuses the same Light Bandit prefab as the melee enemies - "ranged" is a
        // per-instance toggle set below via CreateEnemy(), not baked into the prefab.
        GameObject enemyPrefab = BuildEnemyPrefab(EnemySourcePrefabPath, EnemyDestPrefabPath);

        // Placed further along the Ground platform (spans x -6..18) than Enemy_1/Enemy_2,
        // near the gap before the Wall, so there's open room to see it fire from range
        // before the player closes the distance.
        CreateEnemy("RangedEnemy_1", enemyPrefab, new Vector2(16f, 0.5f), new Vector2(-1f, 0f), new Vector2(1f, 0f), controller,
            attackRange: 3.5f, ranged: true);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("HeroKnightSandboxSetup: ranged enemy built");
    }

    [MenuItem("HeroKnightSandbox/8 Build Terrain Decoration")]
    public static void BuildTerrainDecoration()
    {
        GameObject terrain = GameObject.Find("Terrain");
        if (terrain == null)
        {
            throw new System.Exception("Terrain instance not found in the open scene - run '3 Build Scene' first");
        }

        // Destroy-and-recreate (see CreateEnemy's own comment on this convention) so a
        // rerun never leaves stale duplicate prop instances alongside freshly placed ones.
        Transform existing = terrain.transform.Find("Decoration");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject decoration = new GameObject("Decoration");
        decoration.transform.SetParent(terrain.transform, false);

        // Starting-camp cluster near player spawn (-3, 0.5 - see BuildScene()), on the
        // open left stretch of Ground (top y=0) before Enemy_1's patrol zone (x 2.5-5.5).
        PlaceProp(decoration.transform, 28, -5f, 0f);
        PlaceProp(decoration.transform, 40, -4f, 0f); // pink flowering bush - sprite index 0 is prefab 40, see PlaceProp's mapping note
        PlaceProp(decoration.transform, 38, -2f, 0f);
        PlaceProp(decoration.transform, 39, -1f, 0f);

        // Between Enemy_1 and Enemy_2 (x 5.5-9.5), under/around JumpPlatform.
        PlaceProp(decoration.transform, 16, 6f, 0f);
        PlaceProp(decoration.transform, 11, 6.8f, 0f);

        // Between Enemy_2 and RangedEnemy_1 (x 12.5-15).
        PlaceProp(decoration.transform, 27, 13f, 0f);
        PlaceProp(decoration.transform, 21, 14f, 0f);
        PlaceProp(decoration.transform, 9, 13.7f, 0f);

        // Past RangedEnemy_1, marking the edge before the Wall gap (x 17-18).
        PlaceProp(decoration.transform, 17, 17.3f, 0f);
        PlaceProp(decoration.transform, 29, 17.7f, 0f);

        // LedgePlatform (top y=6, x 22..28).
        PlaceProp(decoration.transform, 18, 24f, 6f);
        PlaceProp(decoration.transform, 12, 26f, 6f);

        // SafetyNet floor (top y=-12, x -5..45) - sparse, just so it's not totally bare
        // if the player drops all the way down.
        PlaceProp(decoration.transform, 25, 10f, -12f);
        PlaceProp(decoration.transform, 15, 30f, -12f);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("HeroKnightSandboxSetup: terrain decoration built");
    }

    // prefabNumber is the Nature_props_NN.prefab file number (1-40), NOT the 0-based
    // sprite index the pack's internal fileIDs use -- prefab N's m_Sprite fileID maps to
    // sprite index (N mod 40), so prefab 40 is the only one whose file number doesn't
    // equal its sprite index (sprite index 0). Every other prop used here (1-39) has
    // fileNumber == spriteIndex, which is why this method just takes the file number
    // directly rather than re-deriving it.
    //
    // This pack's sprites all have a center pivot (0.5,0.5), not bottom, so placing a
    // prop directly at a surface's Y would sink it half its own height into the ground.
    // sprite.bounds.extents.y is already in world units (48 PPU import) and gives that
    // half-height without hardcoding per-sprite pixel dimensions.
    private static void PlaceProp(Transform parent, int prefabNumber, float x, float surfaceY)
    {
        string path = NaturePropsFolder + "Nature_props_" + prefabNumber.ToString("00") + ".prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning("HeroKnightSandboxSetup: prop prefab not found at " + path);
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
        sr.sortingOrder = 1;
        instance.transform.position = new Vector3(x, surfaceY + sr.sprite.bounds.extents.y, 0f);
    }

    [MenuItem("HeroKnightSandbox/10 Build Objectives")]
    public static void BuildObjectives()
    {
        GameObject terrain = GameObject.Find("Terrain");
        if (terrain == null)
        {
            throw new System.Exception("Terrain instance not found in the open scene - run '3 Build Scene' first");
        }

        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            throw new System.Exception("Canvas instance not found in the open scene - run '3 Build Scene' first");
        }

        GameObject player = GameObject.Find("HeroKnight");
        if (player == null)
        {
            throw new System.Exception("HeroKnight instance not found in the open scene - run '3 Build Scene' first");
        }

        // Destroy-and-recreate (see CreateEnemy's own comment on this convention) so a
        // rerun never leaves stale duplicate objectives instances alongside fresh ones.
        GameObject existingController = GameObject.Find("ObjectivesController");
        if (existingController != null)
        {
            Object.DestroyImmediate(existingController);
        }

        Transform existingGoalZone = terrain.transform.Find("GoalZone");
        if (existingGoalZone != null)
        {
            Object.DestroyImmediate(existingGoalZone.gameObject);
        }

        GameObject existingHUD = GameObject.Find("ObjectivesHUD");
        if (existingHUD != null)
        {
            Object.DestroyImmediate(existingHUD);
        }

        GameObject existingCompletePanel = GameObject.Find("CompletePanel");
        if (existingCompletePanel != null)
        {
            Object.DestroyImmediate(existingCompletePanel);
        }

        GameObject controllerGO = new GameObject("ObjectivesController");
        controllerGO.AddComponent<ObjectivesController>();

        // Just past the right edge of LedgePlatform (center 25, size 6x1, top y=6 - see
        // BuildScene()), spanning y=5..8 so it's only reachable after the ledge climb,
        // not by walking underneath at ground level.
        GameObject goalZoneGO = new GameObject("GoalZone");
        goalZoneGO.transform.SetParent(terrain.transform, false);
        goalZoneGO.transform.position = new Vector3(28f, 6.5f, 0f);
        BoxCollider2D goalZoneCollider = goalZoneGO.AddComponent<BoxCollider2D>();
        goalZoneCollider.isTrigger = true;
        goalZoneCollider.size = new Vector2(2f, 3f);
        goalZoneGO.AddComponent<GoalZoneTrigger>();

        GameObject hudGO = new GameObject("ObjectivesHUD");
        hudGO.transform.SetParent(canvas.transform, false);
        // A plain Transform here breaks anchoring for its RectTransform children below -
        // Unity's UI layout needs every ancestor up to the Canvas to be a RectTransform to
        // correctly resolve anchored positions, otherwise children land in the wrong place
        // entirely (confirmed live: EnemiesLine/GoalLine rendered down near ground level
        // instead of their intended top-left HUD position).
        RectTransform hudRT = hudGO.AddComponent<RectTransform>();
        hudRT.anchorMin = Vector2.zero;
        hudRT.anchorMax = Vector2.one;
        hudRT.offsetMin = Vector2.zero;
        hudRT.offsetMax = Vector2.zero;

        // Starts at y=-60 rather than -20 to leave room above for the player health
        // bar built by BuildPlayerHealthBar().
        TextMeshProUGUI enemiesLine = BuildHUDLine(hudGO.transform, "EnemiesLine", new Vector2(20f, -60f));
        TextMeshProUGUI goalLine = BuildHUDLine(hudGO.transform, "GoalLine", new Vector2(20f, -105f));
        GameObject completePanel = BuildCompletePanel(canvas.transform);

        ObjectivesHUD hud = hudGO.AddComponent<ObjectivesHUD>();
        var hudSO = new SerializedObject(hud);
        hudSO.FindProperty("enemiesLine").objectReferenceValue = enemiesLine;
        hudSO.FindProperty("goalLine").objectReferenceValue = goalLine;
        hudSO.FindProperty("completePanel").objectReferenceValue = completePanel;
        hudSO.FindProperty("completionSound").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(CompletionSoundPath);
        hudSO.FindProperty("confettiPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(ConfettiPrefabPath);
        hudSO.FindProperty("player").objectReferenceValue = player.transform;
        hudSO.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("HeroKnightSandboxSetup: objectives built");
    }

    private static TextMeshProUGUI BuildHUDLine(Transform parent, string name, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(600f, 40f);

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.font = UIFont;
        text.fontSize = 24f;
        text.color = Color.white;
        // PressStart2P's glyphs are noticeably wider than the default font - word wrap
        // was breaking "Defeat enemies: 0/4" and "Reach the end: [ ]" each onto two
        // lines instead of one, at the box width these were originally sized for.
        text.enableWordWrapping = false;
        text.text = name;

        return text;
    }

    private static GameObject BuildCompletePanel(Transform canvasTransform)
    {
        GameObject panelGO = new GameObject("CompletePanel");
        panelGO.transform.SetParent(canvasTransform, false);
        RectTransform panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;
        Image dim = panelGO.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.6f);

        GameObject textGO = new GameObject("CompleteText");
        textGO.transform.SetParent(panelGO.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 0.5f);
        textRT.anchorMax = new Vector2(0.5f, 0.5f);
        textRT.pivot = new Vector2(0.5f, 0.5f);
        textRT.anchoredPosition = Vector2.zero;
        textRT.sizeDelta = new Vector2(800f, 200f);

        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.font = UIFont;
        text.fontSize = 64f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.text = "Sandbox Complete!";

        BuildMenuButton(panelGO.transform, "RestartButton", new Vector2(0f, -100f), "Restart")
            .AddComponent<RestartButton>();

        GameObject levelSelectButton = BuildMenuButton(panelGO.transform, "LevelSelectButton", new Vector2(0f, -200f), "Level Select");
        var levelSelectSO = new SerializedObject(levelSelectButton.AddComponent<LoadSceneButton>());
        levelSelectSO.FindProperty("sceneName").stringValue = "LevelSelect";
        levelSelectSO.ApplyModifiedPropertiesWithoutUndo();

        panelGO.SetActive(false);
        return panelGO;
    }

    // Reusable button build: a tinted rectangle (same borderless-Image trick used
    // throughout this file) plus a centered label. Caller adds whichever click-behavior
    // component (RestartButton, ResumeButton, LoadSceneButton, ...) fits.
    private static GameObject BuildMenuButton(Transform parent, string name, Vector2 anchoredPos, string label)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);
        RectTransform buttonRT = buttonGO.AddComponent<RectTransform>();
        buttonRT.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRT.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRT.pivot = new Vector2(0.5f, 0.5f);
        buttonRT.anchoredPosition = anchoredPos;
        buttonRT.sizeDelta = new Vector2(500f, 70f);
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0.85f);
        buttonGO.AddComponent<Button>();

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(12f, 12f);
        textRT.offsetMax = new Vector2(-12f, -12f);
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.font = UIFont;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        // PressStart2P is monospace with a full-em advance per glyph (confirmed in the
        // font asset: m_HorizontalAdvance 51 at pointSize 51, a 1:1 ratio) - each
        // character costs roughly its own fontSize in width. The two earlier attempts
        // (340w/20pt fixed, then auto-sizing down to a 14pt floor) both still needed
        // more width than "Quit to Start Screen" (21 characters) had available, so the
        // overflow silently rendered off the white button and onto the dark panel
        // behind it - invisible, since the label color is black. 500w/18pt leaves
        // comfortable margin (21 * 18 = 378, well under the ~476 usable width).
        text.enableWordWrapping = false;
        text.fontSize = 18f;
        text.text = label;

        return buttonGO;
    }

    [MenuItem("HeroKnightSandbox/12 Build Player Health Bar")]
    public static void BuildPlayerHealthBar()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            throw new System.Exception("Canvas instance not found in the open scene - run '3 Build Scene' first");
        }

        GameObject player = GameObject.Find("HeroKnight");
        if (player == null)
        {
            throw new System.Exception("HeroKnight instance not found in the open scene - run '3 Build Scene' first");
        }

        // Destroy-and-recreate (see CreateEnemy's own comment on this convention) so a
        // rerun never leaves a stale duplicate bar alongside a fresh one.
        GameObject existingBar = GameObject.Find("PlayerHealthBar");
        if (existingBar != null)
        {
            Object.DestroyImmediate(existingBar);
        }

        GameObject barPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerHealthBarPrefabPath);
        GameObject bar = (GameObject)PrefabUtility.InstantiatePrefab(barPrefab, canvas.transform);
        bar.name = "PlayerHealthBar";

        // Prefab's own RectTransform is centered (anchor/pivot 0.5,0.5) for its source
        // demo's layout - repin to the HUD's top-left corner, above the objectives
        // checklist (see BuildObjectives()'s EnemiesLine/GoalLine y-offsets).
        RectTransform barRT = bar.GetComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0f, 1f);
        barRT.anchorMax = new Vector2(0f, 1f);
        barRT.pivot = new Vector2(0f, 1f);
        barRT.anchoredPosition = new Vector2(20f, -20f);

        var barSO = new SerializedObject(bar.GetComponent<HealthBarUI>());
        barSO.FindProperty("getHealthSystemGameObject").objectReferenceValue = player;
        barSO.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("HeroKnightSandboxSetup: player health bar built");
    }

    [MenuItem("HeroKnightSandbox/13 Build Pause Menu")]
    public static void BuildPauseMenu()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            throw new System.Exception("Canvas instance not found in the open scene - run '3 Build Scene' first");
        }

        // Destroy-and-recreate (see CreateEnemy's own comment on this convention) so a
        // rerun never leaves stale duplicate instances alongside fresh ones.
        GameObject existingButton = GameObject.Find("PauseButton");
        if (existingButton != null)
        {
            Object.DestroyImmediate(existingButton);
        }

        GameObject existingPanel = GameObject.Find("PausePanel");
        if (existingPanel != null)
        {
            Object.DestroyImmediate(existingPanel);
        }

        GameObject existingController = GameObject.Find("PauseController");
        if (existingController != null)
        {
            Object.DestroyImmediate(existingController);
        }

        // Top-right corner is the one free spot among the existing bottom-right touch
        // controls (Jump/Attack/Block/Roll - see BuildScene()/BuildJoystick()).
        GameObject pauseButtonGO = new GameObject("PauseButton");
        pauseButtonGO.transform.SetParent(canvas.transform, false);
        RectTransform pauseButtonRT = pauseButtonGO.AddComponent<RectTransform>();
        pauseButtonRT.anchorMin = new Vector2(1f, 1f);
        pauseButtonRT.anchorMax = new Vector2(1f, 1f);
        pauseButtonRT.pivot = new Vector2(0.5f, 0.5f);
        pauseButtonRT.anchoredPosition = new Vector2(-70f, -70f);
        pauseButtonRT.sizeDelta = new Vector2(100f, 100f);
        Image pauseButtonImage = pauseButtonGO.AddComponent<Image>();
        pauseButtonImage.color = new Color(1f, 1f, 1f, 0.35f);
        TouchButton pauseTouchButton = pauseButtonGO.AddComponent<TouchButton>();

        GameObject pausePanel = BuildPausePanel(canvas.transform);

        GameObject controllerGO = new GameObject("PauseController");
        PauseController pauseController = controllerGO.AddComponent<PauseController>();
        var pauseSO = new SerializedObject(pauseController);
        pauseSO.FindProperty("pauseButton").objectReferenceValue = pauseTouchButton;
        pauseSO.FindProperty("pausePanel").objectReferenceValue = pausePanel;
        pauseSO.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("HeroKnightSandboxSetup: pause menu built");
    }

    private static GameObject BuildPausePanel(Transform canvasTransform)
    {
        GameObject panelGO = new GameObject("PausePanel");
        panelGO.transform.SetParent(canvasTransform, false);
        RectTransform panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;
        Image dim = panelGO.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.6f);

        GameObject textGO = new GameObject("PausedText");
        textGO.transform.SetParent(panelGO.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 0.5f);
        textRT.anchorMax = new Vector2(0.5f, 0.5f);
        textRT.pivot = new Vector2(0.5f, 0.5f);
        textRT.anchoredPosition = new Vector2(0f, 180f);
        textRT.sizeDelta = new Vector2(800f, 150f);
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.font = UIFont;
        text.fontSize = 64f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.text = "Paused";

        BuildMenuButton(panelGO.transform, "ResumeButton", new Vector2(0f, 40f), "Resume")
            .AddComponent<ResumeButton>();
        BuildMenuButton(panelGO.transform, "RestartButton", new Vector2(0f, -60f), "Restart")
            .AddComponent<RestartButton>();

        GameObject quitButton = BuildMenuButton(panelGO.transform, "QuitButton", new Vector2(0f, -160f), "Quit to Start Screen");
        var quitSO = new SerializedObject(quitButton.AddComponent<LoadSceneButton>());
        quitSO.FindProperty("sceneName").stringValue = "StartScreen";
        quitSO.ApplyModifiedPropertiesWithoutUndo();

        panelGO.SetActive(false);
        return panelGO;
    }

    [MenuItem("HeroKnightSandbox/Run All")]
    public static void RunAll()
    {
        BuildPrefab();
        AddLedgeGrabToAnimator();
        AddDeathRespawnToAnimator();
        BuildScene();
        BuildTerrainDecoration();
        FinalizeProjectSettings();
        BuildEnemies();
        BuildHeavyEnemy();
        BuildRangedEnemy();
        BuildObjectives();
        BuildPlayerHealthBar();
        BuildPauseMenu();
    }
}
}
