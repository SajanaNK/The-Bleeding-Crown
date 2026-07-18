using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using HeroKnightSandbox;
using HeroKnightSandbox.Input;
using HeroKnightSandbox.Sensors;
using HeroKnightSandbox.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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
/// </summary>
public static class HeroKnightSandboxSetup
{
    private const string SourcePrefabPath = "Assets/Hero Knight - Pixel Art/Demo/HeroKnight.prefab";
    private const string DestPrefabPath = "Assets/Prefabs/HeroKnight.prefab";
    private const string ControllerPath = "Assets/Hero Knight - Pixel Art/Animations/HeroKnight_AnimController.controller";
    private const string LedgeClipPath = "Assets/Hero Knight - Pixel Art/Animations/HeroKnight_LedgeGrab.anim";
    private const string ScenePath = "Assets/Scenes/HeroKnightSandbox.unity";
    private const string PhysicsMaterialPath = "Assets/Hero Knight - Pixel Art/Environment/Walls_noFriction.physicsMaterial2D";

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
        AddLedgeGrabExitTransition(state, idleState, AnimatorConditionMode.If, "LedgeClimb", 0f);
        AddLedgeGrabExitTransition(state, fallState, AnimatorConditionMode.If, "LedgeDrop", 0f);

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

    private static void AddLedgeGrabExitTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, string parameter, float threshold)
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

        // The vendor "EnvironmentTiles" sheet turned out to be four small, mostly-
        // transparent architectural decoration pieces (~32px each), not a repeatable
        // ground texture -- confirmed by inspecting the generated scene's tile sprite
        // reference and the source PNG. Using a generated solid sprite instead of
        // guessing at vendor tile indices.
        Sprite groundSprite = GetOrCreateGroundSprite();
        PhysicsMaterial2D noFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(PhysicsMaterialPath);

        GameObject terrainRoot = new GameObject("Terrain");

        // Flat run: open ground for run/roll testing. Top surface at y = 0.
        CreatePlatform(terrainRoot.transform, "Ground", new Vector2(6f, -0.5f), new Vector2(24f, 1f),
            groundSprite, new Color(0.35f, 0.55f, 0.3f), noFriction);

        // Raised platform: reachable by a jump.
        CreatePlatform(terrainRoot.transform, "JumpPlatform", new Vector2(9f, 2.5f), new Vector2(3f, 1f),
            groundSprite, new Color(0.5f, 0.5f, 0.5f), noFriction);

        // Wall face: walking off the flat run's right edge (at x=18) leaves 3 full units
        // of open air (x 18-21, nothing there at any height near ground level) before the
        // wall starts -- wide enough that the character is unmistakably airborne (in
        // FallState, which is the only state that checks for wall contact) well before
        // reaching it, not stopped at the ground's edge while still grounded. The wall
        // then extends from y=6 down to y=-10 -- effectively bottomless within this
        // level -- so no fall trajectory across that gap can miss its vertical range
        // and slip past underneath it, whatever the exact fall depth turns out to be.
        CreatePlatform(terrainRoot.transform, "Wall", new Vector2(21.5f, -2f), new Vector2(1f, 16f),
            groundSprite, new Color(0.45f, 0.45f, 0.5f), noFriction);

        // Ledge platform on top of the wall (the wall "ends" here at y=6).
        CreatePlatform(terrainRoot.transform, "LedgePlatform", new Vector2(25f, 5.5f), new Vector2(6f, 1f),
            groundSprite, new Color(0.5f, 0.5f, 0.5f), noFriction);

        // Safety-net floor, well below the wall's own bottom, in case the character
        // drops straight down through the gap without drifting into the wall at all.
        CreatePlatform(terrainRoot.transform, "SafetyNet", new Vector2(20f, -12.5f), new Vector2(50f, 1f),
            groundSprite, new Color(0.3f, 0.3f, 0.35f), noFriction);

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

    private static Sprite GetOrCreateGroundSprite()
    {
        const string path = "Assets/Prefabs/GroundSprite.png";
        if (!System.IO.File.Exists(path))
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, png);
            AssetDatabase.ImportAsset(path);
        }

        // Re-applied every call (not just on first creation) so a stale asset from an
        // earlier, differently-configured run of this script still ends up correct.
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 4;
        importer.filterMode = FilterMode.Point;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void CreatePlatform(Transform parent, string name, Vector2 center, Vector2 size,
        Sprite sprite, Color color, PhysicsMaterial2D material)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(center.x, center.y, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        col.sharedMaterial = material;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
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

        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(s => s.path == ScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        AssetDatabase.SaveAssets();

        Debug.Log("HeroKnightSandboxSetup: project settings finalized");
    }

    [MenuItem("HeroKnightSandbox/Run All")]
    public static void RunAll()
    {
        BuildPrefab();
        AddLedgeGrabToAnimator();
        BuildScene();
        FinalizeProjectSettings();
    }
}
}
