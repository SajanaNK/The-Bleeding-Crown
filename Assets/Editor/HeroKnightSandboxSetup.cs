using System.Linq;
using Cinemachine;
using CodeMonkey.HealthSystemCM;
using HeroKnightSandbox;
using HeroKnightSandbox.Enemy;
using HeroKnightSandbox.Input;
using HeroKnightSandbox.Objectives;
using HeroKnightSandbox.Sensors;
using HeroKnightSandbox.UI;
using Platformer.View;
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
    private const string SlideDustPrefabPath = "Assets/Hero Knight - Pixel Art/Demo/SlideDust.prefab";
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
    private const string ProjectileSpritePath = ArcherSetup.ArrowSpritePath;
    private const string NaturePalettePath = "Assets/Nature_pixel_art_assets/palette/Nature_environment_01.prefab";
    private const string NaturePropsFolder = "Assets/Nature_pixel_art_assets/Prefabs/Nature_props/";
    private const string CompletionSoundPath = "Assets/Audio/Collectable.wav";
    private const string ConfettiPrefabPath = "Assets/Mod Assets/Particle Prefabs/ConfettiCelebration.prefab";
    private const string PlayerHealthBarPrefabPath = "Assets/CodeMonkey/HealthSystem/Prefabs/pfHealthBarUI.prefab";
    private const string EnemyHealthBarPrefabPath = "Assets/CodeMonkey/HealthSystem/Prefabs/pfHealthBarUIWorldCanvas.prefab";
    private const string TerrainTilesPrefabPath = "Assets/Prefabs/Level1_TerrainTiles.prefab";
    private const string UIFontPath = "Assets/Mod Assets/Mod Resources/Fonts/PressStart2P-Regular SDF.asset";
    private const string GroundTilePath = "Assets/Nature_pixel_art_assets/Nature_tiles_01/nature_environment_01_50.asset";
    private const string WallTilePath = "Assets/Nature_pixel_art_assets/Nature_tiles_01/nature_environment_01_67.asset";
    private const string SkyboxPath = "Assets/Nature Landscapes Free Pixel Art/nature_3/origbig.png";
    private const string JumpIconPath = "Assets/1-bit_Pixel_Icons/Sprites/Arrows_Pointer_Up_North.png";
    private const string AttackIconPath = "Assets/1-bit_Pixel_Icons/Sprites/RPG_Skill_Strike_Attack_Sword_Slash_Cleave.png";
    private const string BlockIconPath = "Assets/1-bit_Pixel_Icons/Sprites/RPG_Item_Stat_Shield_Defense_Armor.png";
    private const string RollIconPath = "Assets/1-bit_Pixel_Icons/Sprites/RPG_Skill_Dash_Dodge_Movement_Speed_Run_Sprint.png";
    private const string SettingsIconPath = "Assets/1-bit_Pixel_Icons/Sprites/Software_Options_Settings_Cogwheel_Gear_Mechanics.png";
    private const string HeartIconPath = "Assets/1-bit_Pixel_Icons/Sprites/RPG_Stat_HP_Health_Heart.png";
    private const string SkullIconPath = "Assets/1-bit_Pixel_Icons/Sprites/Warfare_Soldier_Skull_Kills_Frags.png";
    private const string FlagIconPath = "Assets/1-bit_Pixel_Icons/Sprites/Map_Markers_Flagpole.png";
    private const string CheckboxDoneIconPath = "Assets/1-bit_Pixel_Icons/Sprites/Software_Checkbox_Checkmarked_Yes_Done_Todo.png";
    private const string CheckboxEmptyIconPath = "Assets/1-bit_Pixel_Icons/Sprites/Software_Checkbox_Empty_Todo.png";

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

        // Terrain sprites from LevelBuildHelpers.CreatePlatform() below and the character's own
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
        SetClipArray(so, "attackClips", LoadClips(
            "Assets/Audio/player_attack_1.mp3", "Assets/Audio/player_attack_2.mp3",
            "Assets/Audio/player_attack_3.mp3", "Assets/Audio/player_attack_4.mp3",
            "Assets/Audio/player_attack_5.mp3"));
        SetClipArray(so, "blockClips", LoadClips(
            "Assets/Audio/player_block_1.mp3", "Assets/Audio/player_block_2.mp3",
            "Assets/Audio/player_block_3.mp3"));
        SetClipArray(so, "jumpClips", LoadClips(
            "Assets/Audio/player_jump_1.mp3", "Assets/Audio/player_jump_2.mp3"));
        SetClipArray(so, "footstepClips", LoadClips(
            "Assets/Audio/player_footstep_1.mp3", "Assets/Audio/player_footstep_2.mp3"));
        so.FindProperty("slideDustPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(SlideDustPrefabPath);
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, DestPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        Debug.Log("HeroKnightSandboxSetup: prefab built at " + DestPrefabPath);
    }

    private static AudioClip[] LoadClips(params string[] paths)
    {
        return paths.Select(AssetDatabase.LoadAssetAtPath<AudioClip>).ToArray();
    }

    internal static void SetClipArray(SerializedObject so, string propertyName, AudioClip[] clips)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        prop.arraySize = clips.Length;
        for (int i = 0; i < clips.Length; i++)
        {
            prop.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
        }
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

        BuildBackground(mainCam.transform.position);

        PhysicsMaterial2D noFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(PhysicsMaterialPath);

        GameObject terrainRoot = new GameObject("Terrain");

        // Collision footprint is owned entirely by these BoxCollider2D calls (unchanged
        // from before the Nature tileset was added) so it stays 100% reproducible via
        // RunAll() -- enemy PatrolPath anchors, ledge-grab, and spawn positions all key
        // off these same rectangles. Visuals are handled separately by a hand-painted
        // Tilemap (see CreateTerrainTilemap below) rather than by these platforms'
        // (removed) placeholder SpriteRenderers.

        // Flat run: open ground for run/roll testing. Top surface at y = 0.
        LevelBuildHelpers.CreatePlatform(terrainRoot.transform, "Ground", new Vector2(6f, -0.5f), new Vector2(24f, 1f), noFriction);

        // Raised platform: reachable by a jump. Width 4 (not 3) so both edges land on
        // whole units (x 7-11) - odd widths on an integer-center platform put the edges on
        // a half-unit boundary, which no whole tile can ever line up with when hand-
        // painting the Tile Palette (see CreateTerrainTilemap's own comment on this).
        LevelBuildHelpers.CreatePlatform(terrainRoot.transform, "JumpPlatform", new Vector2(9f, 2.5f), new Vector2(4f, 1f), noFriction);

        // Wall face: walking off the flat run's right edge (at x=18) leaves 3 full units
        // of open air (x 18-21, nothing there at any height near ground level) before the
        // wall starts -- wide enough that the character is unmistakably airborne (in
        // FallState, which is the only state that checks for wall contact) well before
        // reaching it, not stopped at the ground's edge while still grounded. The wall
        // then extends from y=-10 up to y=3.5 -- effectively bottomless within this
        // level -- so no fall trajectory across that gap can miss its vertical range
        // and slip past underneath it, whatever the exact fall depth turns out to be.
        //
        // Ledge height (3.5 above Ground's own top y=0) is capped by the player's actual
        // jump reach, not chosen freely: JumpForce 7.5 with gravityScale 1 gives a max
        // rise of v^2/2g = ~2.87 units, and WallSensor_R2 sits a further 1.15 units above
        // the character's root, so ~4.5 units above the jumping-off platform is the hard
        // ceiling for ever touching the wall at ledge height. There's no wall-jump
        // (WallSlideState only ever slows the fall, never adds height), so once sliding
        // there's no recovering a jump that started too low. 3.5 leaves ~1 unit of slack
        // for imperfect timing. (Originally 6 - unreachable; confirmed live: the player
        // could wall-slide but never reached ledge-grab range before hitting the ground.)
        LevelBuildHelpers.CreatePlatform(terrainRoot.transform, "Wall", new Vector2(21.5f, -3.25f), new Vector2(1f, 13f), noFriction,
            new Vector2(0f, -0.25f));

        // Ledge platform on top of the wall (the wall "ends" here at y=3.5).
        LevelBuildHelpers.CreatePlatform(terrainRoot.transform, "LedgePlatform", new Vector2(25f, 3f), new Vector2(6f, 1f), noFriction,
            new Vector2(0f, -0.53f));

        // Gap chain: three small stepping platforms past the first ledge climb, each a
        // separate jump (2-3 unit gaps, matching this level's existing airborne-before-
        // reaching-it convention), with a bit of height variance so it doesn't read as
        // a flat repeat of JumpPlatform. Shifted down 2.5 units from their original
        // heights to match LedgePlatform's own height fix above.
        LevelBuildHelpers.CreatePlatform(terrainRoot.transform, "GapPlatform1", new Vector2(31f, 4f), new Vector2(2f, 1f), noFriction,
            new Vector2(0f, 0.52f));
        LevelBuildHelpers.CreatePlatform(terrainRoot.transform, "GapPlatform2", new Vector2(35f, 5f), new Vector2(2f, 1f), noFriction,
            new Vector2(0f, 0.51f));
        LevelBuildHelpers.CreatePlatform(terrainRoot.transform, "GapPlatform3", new Vector2(39f, 4f), new Vector2(2f, 1f), noFriction,
            new Vector2(0f, 0.49f));

        // Gauntlet platform: hosts a Heavy Bandit + Light Bandit together (see
        // BuildHeavyEnemy()/BuildEnemies()), forcing block/roll instead of just trading
        // hits with a single enemy at a time. Also shifted down 2.5 units, matching the
        // gap chain above.
        LevelBuildHelpers.CreatePlatform(terrainRoot.transform, "GauntletPlatform", new Vector2(45f, 4f), new Vector2(6f, 1f), noFriction,
            new Vector2(0f, 0.5f));

        // Second wall + ledge climb - same 3.5-unit reachable-height budget as the first
        // climb above, this time measured from GauntletPlatform's own top (y=5) up to
        // this ledge's top (y=8). Left edge at x=51 (center 51.5, width 1) puts the gap
        // from GauntletPlatform's own right edge (x=48) at 3 units, matching the first
        // Wall's own 3-unit gap - previously at x=54.5 this was a 6-unit gap, too wide to
        // jump at all.
        LevelBuildHelpers.CreatePlatform(terrainRoot.transform, "Wall2", new Vector2(51.5f, 0f), new Vector2(1f, 16f), noFriction);
        LevelBuildHelpers.CreatePlatform(terrainRoot.transform, "LedgePlatform2", new Vector2(55f, 7.5f), new Vector2(6f, 1f), noFriction);

        // Safety-net floor, well below either wall's own bottom, in case the character
        // drops straight down through a gap without drifting into a wall at all. Widened
        // to cover the full extended span, from the original Wall's gap through past the
        // new goal zone at the far end.
        GameObject safetyNet = LevelBuildHelpers.CreatePlatform(terrainRoot.transform, "SafetyNet", new Vector2(35f, -12.5f),
            new Vector2(90f, 1f), noFriction);

        // Second, trigger-only BoxCollider2D on the same GameObject (a compound collider
        // sharing SafetyNet's own static Rigidbody2D) so standing here shows the Respawn
        // prompt (see SafetyNetTrigger/RespawnPromptUI) without affecting the solid
        // collider above. Sized taller than the solid slab and centered above its top
        // (slab top at y=-12) so it catches the player right as they land rather than
        // needing them to sink into the platform first.
        BoxCollider2D respawnZone = safetyNet.AddComponent<BoxCollider2D>();
        respawnZone.isTrigger = true;
        respawnZone.size = new Vector2(90f, 3f);
        respawnZone.offset = new Vector2(0f, 2f);
        safetyNet.AddComponent<SafetyNetTrigger>();

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

        // Hidden by default (see RespawnPromptUI.Awake()) - shown only while the player
        // is standing on the SafetyNet (see SafetyNetTrigger). Positioned above screen
        // center so it never overlaps the touch controls built below (all anchored to
        // the bottom corners).
        LevelBuildHelpers.BuildMenuButton(canvasGO.transform, "RespawnPromptButton", new Vector2(0f, 250f), "Respawn", UIFont)
            .AddComponent<RespawnPromptUI>();

        GameObject eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.AddComponent<EventSystem>();
        eventSystemGO.AddComponent<StandaloneInputModule>();

        ScreenTransitionSetup.Build();
        MusicSetup.Build(MusicSetup.GameplayMusicPath, MusicSetup.GameplayVolume);

        GameObject touchInputGO = new GameObject("TouchControls");
        touchInputGO.transform.SetParent(canvasGO.transform, false);
        TouchHeroKnightInput touchInput = touchInputGO.AddComponent<TouchHeroKnightInput>();

        VirtualJoystick joystick = BuildJoystick(canvasGO.transform);
        TouchButton jumpBtn = BuildButton(canvasGO.transform, "JumpButton", new Vector2(-100, 160), JumpIconPath);
        TouchButton attackBtn = BuildButton(canvasGO.transform, "AttackButton", new Vector2(-220, 100), AttackIconPath);
        TouchButton blockBtn = BuildButton(canvasGO.transform, "BlockButton", new Vector2(-100, 40), BlockIconPath);
        TouchButton rollBtn = BuildButton(canvasGO.transform, "RollButton", new Vector2(-220, 220), RollIconPath);

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

        // Lets HeroKnightController.Respawn() tell Cinemachine its teleport was a warp,
        // not real motion - otherwise the framing transposer spends several frames
        // whipping the camera across the whole level to "catch up" to the sudden jump.
        var cameraSO = new SerializedObject(player.GetComponent<HeroKnightController>());
        cameraSO.FindProperty("followCamera").objectReferenceValue = vcam;
        cameraSO.ApplyModifiedPropertiesWithoutUndo();

        GameObject boundsGO = new GameObject("CameraBounds");
        PolygonCollider2D bounds = boundsGO.AddComponent<PolygonCollider2D>();
        bounds.isTrigger = true;
        // Widened/heightened for the level extension - LedgePlatform2 tops out at y=8
        // (vs the original LedgePlatform's y=3.5) and the new goal zone sits past x=62.
        bounds.points = new[]
        {
            new Vector2(-10, -16),
            new Vector2(-10, 20),
            new Vector2(80, 20),
            new Vector2(80, -16),
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

    // Nature Landscapes mountain-valley art as the level's skybox: a single sprite pinned
    // to the camera via ParallaxLayer with movementScale (1,1,0), so it never scrolls
    // relative to the screen (true skybox behavior) rather than partially parallaxing -
    // simpler, and avoids needing a sprite wide enough to cover this level's full ~90-unit
    // CameraBounds span.
    private static void BuildBackground(Vector3 cameraStartPosition)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SkyboxPath);
        if (sprite == null)
        {
            Debug.LogWarning("HeroKnightSandboxSetup: skybox sprite not found at " + SkyboxPath);
            return;
        }

        GameObject skyGO = new GameObject("Skybox");
        skyGO.transform.position = new Vector3(cameraStartPosition.x, cameraStartPosition.y, 0f);

        SpriteRenderer renderer = skyGO.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        // Comfortably past the ~21x12 unit view at orthoSize 6 on a 16:9 screen, with
        // margin for wider aspect ratios - since the layer is screen-locked this only
        // needs to cover one view, not the whole level.
        float targetWidth = 30f;
        float scale = targetWidth / sprite.bounds.size.x;
        skyGO.transform.localScale = new Vector3(scale, scale, 1f);

        renderer.sortingOrder = -100;

        ParallaxLayer parallax = skyGO.AddComponent<ParallaxLayer>();
        parallax.movementScale = new Vector3(1f, 1f, 0f);
    }

    // Sets up a Grid/Tilemap and auto-paints it with the Nature tileset, using the
    // collision footprint above as the source of truth for which cells to fill (unlike
    // that footprint itself, which must stay reproducible for the enemy/ledge tuning
    // that keys off it). Hand-painting via the Tile Palette (still opened below) is
    // still possible for touch-ups; once touched-up tiles exist at
    // TerrainTilesPrefabPath (see SaveTerrainTiles() below), reruns restore that saved
    // version here instead of re-running the auto-paint - BuildScene() wipes the whole
    // scene via NewScene() every time, so without this every Run All would otherwise
    // discard any manual touch-ups.
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
        Tilemap tilemap = LevelBuildHelpers.CreateOrRestoreTerrainTilemap(
            parent, TerrainTilesPrefabPath, NaturePalettePath, "HeroKnightSandboxSetup");
        if (tilemap == null)
        {
            return;
        }

        // Auto-paints a plain default tile across every platform's cell rectangle
        // (below), rather than leaving the grid empty for hand-painting - this Nature
        // set has no distinct left/right edge-cap tiles the way many tilesets do, only
        // a long run of near-identical repeatable pieces, so one tile reused everywhere
        // reads fine and removes the need to hunt through 128 unlabeled sprites. The
        // Tile Palette is still opened (see CreateOrRestoreTerrainTilemap) in case
        // manual touch-ups are wanted after.
        TileBase groundTile = AssetDatabase.LoadAssetAtPath<TileBase>(GroundTilePath);
        TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);

        LevelBuildHelpers.PaintRect(tilemap, groundTile, -6, 17, -1, -1); // Ground
        LevelBuildHelpers.PaintRect(tilemap, groundTile, 7, 10, 2, 2); // JumpPlatform (0.5-unit overhang each side by design)
        LevelBuildHelpers.PaintRect(tilemap, wallTile, 21, 21, -10, 3); // Wall
        LevelBuildHelpers.PaintRect(tilemap, groundTile, 22, 27, 3, 3); // LedgePlatform
        LevelBuildHelpers.PaintRect(tilemap, groundTile, 30, 31, 4, 4); // GapPlatform1
        LevelBuildHelpers.PaintRect(tilemap, groundTile, 34, 35, 5, 5); // GapPlatform2
        LevelBuildHelpers.PaintRect(tilemap, groundTile, 38, 39, 4, 4); // GapPlatform3
        LevelBuildHelpers.PaintRect(tilemap, groundTile, 42, 47, 4, 4); // GauntletPlatform
        LevelBuildHelpers.PaintRect(tilemap, wallTile, 50, 51, -8, 8); // Wall2 (centered on an integer x, so it straddles two columns)
        LevelBuildHelpers.PaintRect(tilemap, groundTile, 52, 57, 8, 8); // LedgePlatform2
        LevelBuildHelpers.PaintRect(tilemap, groundTile, -10, 79, -13, -13); // SafetyNet

        Debug.Log("HeroKnightSandboxSetup: 'Ground Tiles' Tilemap auto-painted under Terrain/TerrainTiles.");
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

    private static TouchButton BuildButton(Transform canvasTransform, string name, Vector2 anchoredPos, string iconPath)
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

        AddButtonIcon(go.transform, iconPath, 60f);
        go.AddComponent<ButtonPunch>();

        return go.AddComponent<TouchButton>();
    }

    // Overlays a 1-bit icon (black outline, white fill, transparent elsewhere - no tint
    // needed) centered on a touch button, sized well within its 100x100 tap area.
    private static void AddButtonIcon(Transform buttonTransform, string iconPath, float size)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        if (sprite == null)
        {
            Debug.LogWarning("HeroKnightSandboxSetup: control icon not found at " + iconPath);
            return;
        }

        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(buttonTransform, false);
        RectTransform rt = iconGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = Vector2.zero;

        Image image = iconGO.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
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

    private static GameObject BuildEnemyPrefab(string sourcePrefabPath, string destPrefabPath, AudioClip[] attackClips)
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
        SetClipArray(enemySO, "attackClips", attackClips);
        // Shared across every enemy type (Light/Heavy Bandit, Archer) - only one clip was
        // provided, so this is the same asset either way.
        SetClipArray(enemySO, "deathClips", LoadClips("Assets/Audio/enemy_died.mp3"));
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
        GameObject enemyPrefab = BuildEnemyPrefab(EnemySourcePrefabPath, EnemyDestPrefabPath,
            LoadClips("Assets/Audio/light_enemy_attack_1.mp3", "Assets/Audio/light_enemy_attack_2.mp3"));

        // Both on the flat Ground platform (top at y=0, spans x -6..18 - see BuildScene()),
        // spread apart so one can be tested in isolation before walking further to reach
        // both at once, without needing a jump/wall-slide/ledge-grab to reach either.
        // Y matches the platform's top surface exactly (not +0.5 like the player's own
        // spawn) - Bandit sprites are bottom-pivoted (confirmed in LightBandit.prefab's
        // collider data: pivot {x:0.5, y:0}), unlike HeroKnight's center-pivoted sprite.
        CreateEnemy("Enemy_1", enemyPrefab, new Vector2(4f, 0f), new Vector2(-1.5f, 0f), new Vector2(1.5f, 0f), controller);
        CreateEnemy("Enemy_2", enemyPrefab, new Vector2(11f, 0f), new Vector2(-1.5f, 0f), new Vector2(1.5f, 0f), controller);

        // Part of the gauntlet on GauntletPlatform (top y=5, x 42..48 - see BuildScene()),
        // alongside HeavyEnemy_2 (see BuildHeavyEnemy()) - together they force block/roll
        // instead of just trading hits with a single enemy at a time.
        CreateEnemy("Enemy_3", enemyPrefab, new Vector2(43.5f, 5f), new Vector2(-1.2f, 0f), new Vector2(1.2f, 0f), controller);

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
        GameObject heavyPrefab = BuildEnemyPrefab(HeavyEnemySourcePrefabPath, HeavyEnemyDestPrefabPath,
            LoadClips("Assets/Audio/heavy_enemy_attack_1.mp3", "Assets/Audio/heavy_enemy_attack_2.mp3"));

        // Placed on JumpPlatform (center 9,2.5, size 3x1 - see BuildScene()), distinct from
        // both Light Bandits on the flat Ground platform below, so it reads as a separate,
        // tougher encounter reachable only after a jump.
        CreateEnemy("HeavyEnemy_1", heavyPrefab, new Vector2(9f, 3f), new Vector2(-1f, 0f), new Vector2(1f, 0f), controller,
            maxHP: 6, moveSpeed: 1.2f, attackDamage: 2);

        // Gauntlet encounter on GauntletPlatform (top y=5, x 42..48 - see BuildScene()),
        // paired with Enemy_3 (see BuildEnemies()).
        // Y=5 (flush with platform top), not +0.5 - Heavy Bandit's own resting offset,
        // matching HeavyEnemy_1's convention on JumpPlatform above, which differs from
        // the Light Bandit's +0.5 offset used elsewhere in this file.
        CreateEnemy("HeavyEnemy_2", heavyPrefab, new Vector2(46.5f, 5f), new Vector2(-1.2f, 0f), new Vector2(1.2f, 0f), controller,
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
        // Archer.prefab, not the melee Light Bandit prefab BuildEnemyPrefab() builds -
        // ArcherSetup.BuildAll() (re)builds its sprites/animator/prefab so this step
        // stays self-contained in RunAll(), matching BuildEnemyPrefab()'s own always-
        // rebuild convention above. "ranged" is still a per-instance toggle set below via
        // CreateEnemy(), same as before.
        ArcherSetup.BuildAll();
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArcherSetup.ArcherPrefabPath);

        // Placed further along the Ground platform (spans x -6..18) than Enemy_1/Enemy_2,
        // near the gap before the Wall, so there's open room to see it fire from range
        // before the player closes the distance.
        CreateEnemy("RangedEnemy_1", enemyPrefab, new Vector2(16f, 0f), new Vector2(-1f, 0f), new Vector2(1f, 0f), controller,
            attackRange: 3.5f, ranged: true);

        // Final guard on LedgePlatform2 (top y=8, x 52..58 - see BuildScene()), right
        // before the goal zone.
        CreateEnemy("RangedEnemy_2", enemyPrefab, new Vector2(55f, 8f), new Vector2(-1f, 0f), new Vector2(1f, 0f), controller,
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

        // LedgePlatform (top y=2.97 - collider offset by (0, -0.53), x 22..28).
        PlaceProp(decoration.transform, 18, 24f, 2.97f);
        PlaceProp(decoration.transform, 12, 26f, 2.97f);

        // Gap chain platforms (tops y=5.02, 6.01, 4.99 - each collider offset per
        // BuildScene(), see LevelBuildHelpers.CreatePlatform() calls above).
        PlaceProp(decoration.transform, 21, 31f, 5.02f);
        PlaceProp(decoration.transform, 9, 35f, 6.01f);
        PlaceProp(decoration.transform, 27, 39f, 4.99f);

        // GauntletPlatform (top y=5.0 - collider offset by (0, 0.5), x 42..48), around Enemy_3/HeavyEnemy_2.
        PlaceProp(decoration.transform, 17, 42.5f, 5.0f);
        PlaceProp(decoration.transform, 29, 47.5f, 5.0f);

        // LedgePlatform2 (top y=8, x 52..58), around RangedEnemy_2 and the goal.
        PlaceProp(decoration.transform, 18, 53f, 8f);
        PlaceProp(decoration.transform, 12, 57f, 8f);

        // SafetyNet floor (top y=-12, x -10..79) - sparse, just so it's not totally bare
        // if the player drops all the way down.
        PlaceProp(decoration.transform, 25, 10f, -12f);
        PlaceProp(decoration.transform, 15, 30f, -12f);
        PlaceProp(decoration.transform, 25, 50f, -12f);
        PlaceProp(decoration.transform, 15, 70f, -12f);

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

        GameObject existingPanel = GameObject.Find("ObjectivesPanel");
        if (existingPanel != null)
        {
            Object.DestroyImmediate(existingPanel);
        }

        GameObject existingCompletePanel = GameObject.Find("CompletePanel");
        if (existingCompletePanel != null)
        {
            Object.DestroyImmediate(existingCompletePanel);
        }

        GameObject controllerGO = new GameObject("ObjectivesController");
        controllerGO.AddComponent<ObjectivesController>();

        // Just past the right edge of LedgePlatform2 (center 55, size 6x1, top y=8 -
        // see BuildScene()), spanning y=7..10 so it's only reachable after both ledge
        // climbs and the gauntlet, not by walking underneath at ground level.
        GameObject goalZoneGO = new GameObject("GoalZone");
        goalZoneGO.transform.SetParent(terrain.transform, false);
        goalZoneGO.transform.position = new Vector3(61f, 8.5f, 0f);
        BoxCollider2D goalZoneCollider = goalZoneGO.AddComponent<BoxCollider2D>();
        goalZoneCollider.isTrigger = true;
        goalZoneCollider.size = new Vector2(2f, 3f);
        goalZoneGO.AddComponent<GoalZoneTrigger>();

        // Starts at y=-68 rather than -20 to leave room above for the player health
        // bar built by BuildPlayerHealthBar().
        GameObject objectivesPanel = BuildObjectivesPanel(canvas.transform);
        var (enemiesText, enemiesCheckbox) = BuildObjectiveRow(
            objectivesPanel.transform, "EnemiesRow", new Vector2(14f, -16f), SkullIconPath, "Defeat enemies");
        var (goalText, goalCheckbox) = BuildObjectiveRow(
            objectivesPanel.transform, "GoalRow", new Vector2(14f, -60f), FlagIconPath, "Reach the end");
        GameObject completePanel = BuildCompletePanel(canvas.transform);

        ObjectivesHUD hud = objectivesPanel.AddComponent<ObjectivesHUD>();
        var hudSO = new SerializedObject(hud);
        hudSO.FindProperty("enemiesLine").objectReferenceValue = enemiesText;
        hudSO.FindProperty("goalLine").objectReferenceValue = goalText;
        hudSO.FindProperty("enemiesCheckbox").objectReferenceValue = enemiesCheckbox;
        hudSO.FindProperty("goalCheckbox").objectReferenceValue = goalCheckbox;
        hudSO.FindProperty("checkboxDoneSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(CheckboxDoneIconPath);
        hudSO.FindProperty("checkboxEmptySprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(CheckboxEmptyIconPath);
        hudSO.FindProperty("completePanel").objectReferenceValue = completePanel;
        hudSO.FindProperty("completionSound").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(CompletionSoundPath);
        hudSO.FindProperty("confettiPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(ConfettiPrefabPath);
        hudSO.FindProperty("player").objectReferenceValue = player.transform;
        hudSO.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("HeroKnightSandboxSetup: objectives built");
    }

    // Parchment-style framed box (same sliced UISprite/tint family as the pause menu
    // buttons) behind the objectives checklist - previously just floating white text
    // directly on the transparent canvas.
    private static GameObject BuildObjectivesPanel(Transform canvasTransform)
    {
        GameObject panelGO = new GameObject("ObjectivesPanel");
        panelGO.transform.SetParent(canvasTransform, false);
        RectTransform panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0f, 1f);
        panelRT.anchorMax = new Vector2(0f, 1f);
        panelRT.pivot = new Vector2(0f, 1f);
        panelRT.anchoredPosition = new Vector2(14f, -68f);
        panelRT.sizeDelta = new Vector2(410f, 112f);
        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        panelImage.type = Image.Type.Sliced;
        panelImage.color = new Color(0.20f, 0.16f, 0.12f, 0.85f);
        panelImage.raycastTarget = false;

        return panelGO;
    }

    // One checklist row: a topic icon (skull for "defeat enemies", flag for "reach the
    // end"), the label text, and a trailing checkbox icon that ObjectivesHUD swaps
    // between empty/done sprites (replacing the old "[DONE]"/"[ ]" text suffix).
    private static (TextMeshProUGUI text, Image checkbox) BuildObjectiveRow(
        Transform parent, string rowName, Vector2 anchoredPos, string iconPath, string initialLabel)
    {
        GameObject rowGO = new GameObject(rowName);
        rowGO.transform.SetParent(parent, false);
        RectTransform rowRT = rowGO.AddComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0f, 1f);
        rowRT.anchorMax = new Vector2(0f, 1f);
        rowRT.pivot = new Vector2(0f, 1f);
        rowRT.anchoredPosition = anchoredPos;
        rowRT.sizeDelta = new Vector2(380f, 32f);

        Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        if (icon != null)
        {
            GameObject iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(rowGO.transform, false);
            RectTransform iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0f, 0.5f);
            iconRT.anchorMax = new Vector2(0f, 0.5f);
            iconRT.pivot = new Vector2(0f, 0.5f);
            iconRT.anchoredPosition = Vector2.zero;
            iconRT.sizeDelta = new Vector2(26f, 26f);
            Image iconImage = iconGO.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(rowGO.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0f, 0.5f);
        textRT.anchorMax = new Vector2(0f, 0.5f);
        textRT.pivot = new Vector2(0f, 0.5f);
        textRT.anchoredPosition = new Vector2(34f, 0f);
        textRT.sizeDelta = new Vector2(300f, 32f);
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.font = UIFont;
        // Smaller than the 18pt used elsewhere in the HUD - PressStart2P's glyphs run
        // about a fontSize's width each (see the monospace-width memory note), so 18pt
        // needed ~340px for "Defeat enemies: 0/7" and was overflowing past the checkbox
        // and the panel's own edge. 15pt keeps the same box/panel from growing enormous.
        text.fontSize = 15f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        // PressStart2P's glyphs are noticeably wider than the default font - word wrap
        // was breaking "Defeat enemies: 0/4" onto two lines at narrower box widths.
        text.enableWordWrapping = false;
        text.text = initialLabel;

        GameObject checkboxGO = new GameObject("Checkbox");
        checkboxGO.transform.SetParent(rowGO.transform, false);
        RectTransform checkboxRT = checkboxGO.AddComponent<RectTransform>();
        checkboxRT.anchorMin = new Vector2(0f, 0.5f);
        checkboxRT.anchorMax = new Vector2(0f, 0.5f);
        checkboxRT.pivot = new Vector2(0.5f, 0.5f);
        checkboxRT.anchoredPosition = new Vector2(354f, 0f);
        checkboxRT.sizeDelta = new Vector2(26f, 26f);
        Image checkboxImage = checkboxGO.AddComponent<Image>();
        checkboxImage.preserveAspect = true;
        checkboxImage.raycastTarget = false;

        return (text, checkboxImage);
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

        LevelBuildHelpers.BuildMenuButton(panelGO.transform, "RestartButton", new Vector2(0f, -100f), "Restart", UIFont)
            .AddComponent<RestartButton>();

        GameObject levelSelectButton = LevelBuildHelpers.BuildMenuButton(panelGO.transform, "LevelSelectButton", new Vector2(0f, -200f), "Level Select", UIFont);
        var levelSelectSO = new SerializedObject(levelSelectButton.AddComponent<LoadSceneButton>());
        levelSelectSO.FindProperty("sceneName").stringValue = "LevelSelect";
        levelSelectSO.ApplyModifiedPropertiesWithoutUndo();

        // FadePanel fades this in via the CanvasGroup instead of the GameObject just
        // popping active - see ObjectivesHUD's completePanel.SetVisible(true) call.
        CanvasGroup canvasGroup = panelGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        panelGO.AddComponent<FadePanel>();

        panelGO.SetActive(false);
        return panelGO;
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
        GameObject existingFrame = GameObject.Find("PlayerHealthBarFrame");
        if (existingFrame != null)
        {
            Object.DestroyImmediate(existingFrame);
        }

        // Bronze bordered frame (same sliced UISprite family as the menu buttons) around
        // a heart icon and the CodeMonkey fill bar - previously just the bare prefab's
        // flat grey/red rectangle with no frame or icon at all.
        GameObject frameGO = new GameObject("PlayerHealthBarFrame");
        frameGO.transform.SetParent(canvas.transform, false);
        RectTransform frameRT = frameGO.AddComponent<RectTransform>();
        frameRT.anchorMin = new Vector2(0f, 1f);
        frameRT.anchorMax = new Vector2(0f, 1f);
        frameRT.pivot = new Vector2(0f, 1f);
        frameRT.anchoredPosition = new Vector2(14f, -14f);
        frameRT.sizeDelta = new Vector2(240f, 44f);
        Image frameImage = frameGO.AddComponent<Image>();
        frameImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        frameImage.type = Image.Type.Sliced;
        frameImage.color = new Color(0.30f, 0.24f, 0.16f, 0.95f);

        Sprite heartSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HeartIconPath);
        if (heartSprite != null)
        {
            GameObject heartGO = new GameObject("Icon");
            heartGO.transform.SetParent(frameGO.transform, false);
            RectTransform heartRT = heartGO.AddComponent<RectTransform>();
            heartRT.anchorMin = new Vector2(0f, 0.5f);
            heartRT.anchorMax = new Vector2(0f, 0.5f);
            heartRT.pivot = new Vector2(0f, 0.5f);
            heartRT.anchoredPosition = new Vector2(10f, 0f);
            heartRT.sizeDelta = new Vector2(28f, 28f);
            Image heartImage = heartGO.AddComponent<Image>();
            heartImage.sprite = heartSprite;
            heartImage.preserveAspect = true;
            heartImage.raycastTarget = false;
        }

        GameObject barPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerHealthBarPrefabPath);
        GameObject bar = (GameObject)PrefabUtility.InstantiatePrefab(barPrefab, frameGO.transform);
        bar.name = "PlayerHealthBar";

        // Prefab's own RectTransform is centered (anchor/pivot 0.5,0.5) for its source
        // demo's layout - repin inside the frame, to the right of the heart icon.
        RectTransform barRT = bar.GetComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0f, 0.5f);
        barRT.anchorMax = new Vector2(0f, 0.5f);
        barRT.pivot = new Vector2(0f, 0.5f);
        barRT.anchoredPosition = new Vector2(46f, 0f);
        barRT.sizeDelta = new Vector2(176f, 24f);

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
        AddButtonIcon(pauseButtonGO.transform, SettingsIconPath, 60f);
        pauseButtonGO.AddComponent<ButtonPunch>();
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

        LevelBuildHelpers.BuildMenuButton(panelGO.transform, "ResumeButton", new Vector2(0f, 40f), "Resume", UIFont)
            .AddComponent<ResumeButton>();
        LevelBuildHelpers.BuildMenuButton(panelGO.transform, "RestartButton", new Vector2(0f, -60f), "Restart", UIFont)
            .AddComponent<RestartButton>();

        GameObject quitButton = LevelBuildHelpers.BuildMenuButton(panelGO.transform, "QuitButton", new Vector2(0f, -160f), "Quit to Start Screen", UIFont);
        var quitSO = new SerializedObject(quitButton.AddComponent<LoadSceneButton>());
        quitSO.FindProperty("sceneName").stringValue = "StartScreen";
        quitSO.ApplyModifiedPropertiesWithoutUndo();

        // FadePanel fades this in/out via the CanvasGroup instead of the GameObject just
        // popping active/inactive - see PauseController.SetPaused().
        CanvasGroup canvasGroup = panelGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        panelGO.AddComponent<FadePanel>();

        panelGO.SetActive(false);
        return panelGO;
    }

    [MenuItem("HeroKnightSandbox/14 Build Fps Counter")]
    public static void BuildFpsCounter()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            throw new System.Exception("Canvas instance not found in the open scene - run '3 Build Scene' first");
        }

        // Destroy-and-recreate (see CreateEnemy's own comment on this convention) so a
        // rerun never leaves a stale duplicate counter alongside a fresh one.
        GameObject existing = GameObject.Find("FpsCounter");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        LevelBuildHelpers.BuildFpsCounter(canvas.transform, UIFont);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("HeroKnightSandboxSetup: fps counter built");
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
        BuildFpsCounter();
    }
}
}
