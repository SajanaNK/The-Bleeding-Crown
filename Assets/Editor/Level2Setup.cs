using Cinemachine;
using CodeMonkey.HealthSystemCM;
using HeroKnightSandbox;
using HeroKnightSandbox.Enemy;
using HeroKnightSandbox.Input;
using HeroKnightSandbox.Objectives;
using HeroKnightSandbox.UI;
using Platformer.View;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace HeroKnightSandbox.EditorTools
{
/// <summary>
/// Builds Level2.unity - a second playable level with a fresh terrain/enemy
/// layout, reusing the same mechanics, objectives system, HUD, health bars, and
/// pause menu as HeroKnightSandbox.unity. Run via HeroKnightSandbox/20-27, or
/// "Run All Level 2". Safe to re-run: each step overwrites its own output.
///
/// Shares its lowest-level scene-scaffolding primitives (collider platforms, tilemap
/// restore/paint, menu buttons - see LevelBuildHelpers) with HeroKnightSandboxSetup, but
/// keeps all actual level geometry/placement (BuildScene's layout, decoration and enemy
/// positions, PaintRect coordinates) duplicated rather than parameterized - that data
/// differs per level anyway, so sharing it would mean threading a large parameter list
/// through for no real reuse.
///
/// Does NOT rebuild the player/enemy prefabs or touch global project settings
/// (Animator parameters, PlayerSettings orientation, etc.) - those are one-time,
/// scene-independent setup already performed by HeroKnightSandboxSetup and are
/// simply loaded here by their existing asset paths.
/// </summary>
public static class Level2Setup
{
    private const string ScenePath = "Assets/Scenes/Level2.unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/HeroKnight.prefab";
    private const string PhysicsMaterialPath = "Assets/Hero Knight - Pixel Art/Environment/Walls_noFriction.physicsMaterial2D";
    private const string EnemyPrefabPath = "Assets/Prefabs/HeroKnightEnemy.prefab";
    private const string HeavyEnemyPrefabPath = "Assets/Prefabs/HeroKnightHeavyEnemy.prefab";
    private const string ProjectileSpritePath = ArcherSetup.ArrowSpritePath;
    private const string NaturePalettePath = "Assets/Nature_pixel_art_assets/palette/Nature_environment_01.prefab";
    private const string NaturePropsFolder = "Assets/Nature_pixel_art_assets/Prefabs/Nature_props/";
    private const string CompletionSoundPath = "Assets/Audio/Collectable.wav";
    private const string ConfettiPrefabPath = "Assets/Mod Assets/Particle Prefabs/ConfettiCelebration.prefab";
    private const string PlayerHealthBarPrefabPath = "Assets/CodeMonkey/HealthSystem/Prefabs/pfHealthBarUI.prefab";
    private const string TerrainTilesPrefabPath = "Assets/Prefabs/Level2_TerrainTiles.prefab";
    private const string UIFontPath = "Assets/Mod Assets/Mod Resources/Fonts/PressStart2P-Regular SDF.asset";
    private const string GroundTilePath = "Assets/Nature_pixel_art_assets/Nature_tiles_01/nature_environment_01_50.asset";
    private const string WallTilePath = "Assets/Nature_pixel_art_assets/Nature_tiles_01/nature_environment_01_67.asset";
    // nature_7 (rock formations) rather than nature_3 (Level 1/Start Screen/Level
    // Select's mountain valley), so Level 2 reads as a distinct, rockier area.
    private const string SkyboxPath = "Assets/Nature Landscapes Free Pixel Art/nature_7/origbig.png";
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

    [MenuItem("HeroKnightSandbox/20 Build Level 2 Scene")]
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

        // Longer flat opening than Level 1's (28 units vs 24), both Light Bandits spread
        // further apart across it. Top surface at y = 0.
        LevelBuildHelpers.CreatePlatform(terrainRoot.transform, "Ground", new Vector2(7f, -0.5f), new Vector2(30f, 1f), noFriction);

        // Two-step staircase (vs. Level 1's single JumpPlatform) leading up to the
        // Heavy Bandit - each step individually jumpable, so climbing it reads as a
        // deliberate sequence rather than one big leap. Width 4 (not 3) on both, same
        // fix as HeroKnightSandboxSetup's JumpPlatform - an odd width on an integer
        // center put both edges on a half-unit boundary that no whole tile can ever
        // line up with when hand-painting the Tile Palette.
        LevelBuildHelpers.CreatePlatform(terrainRoot.transform, "StepPlatform1", new Vector2(28f, 2f), new Vector2(4f, 1f), noFriction,
            new Vector2(0f, -0.5f));
        LevelBuildHelpers.CreatePlatform(terrainRoot.transform, "StepPlatform2", new Vector2(33f, 4.5f), new Vector2(4f, 1f), noFriction);

        // Wall face after the staircase: a wider gap (x 22..26, ground ends at 22, first
        // step now starts at 26) than Level 1's, so the player is unmistakably airborne
        // well before either landing on the steps or reaching the wall. Wall spans
        // y=-10 up to y=3.5, same bottomless-within-this-level convention as Level 1.
        //
        // Ledge height (3.5 above Ground's own top y=0) is capped by the player's actual
        // jump reach, not chosen freely - see HeroKnightSandboxSetup.BuildScene()'s
        // matching comment for the physics (max ~4.5 units above the jumping-off
        // platform, no wall-jump to recover from a jump that started too low).
        // Originally 6 - unreachable; confirmed live the same way as Level 1's.
        //
        // Center shifted 37 -> 37.5 (width stays 1) so both edges land on whole units
        // (x 37-38) instead of a half-unit boundary - as a bonus this also closes what
        // was a gap to LedgePlatform's own left edge (x=38) into a flush seam.
        LevelBuildHelpers.CreatePlatform(terrainRoot.transform, "Wall", new Vector2(37.5f, -3.25f), new Vector2(1f, 13f), noFriction,
            new Vector2(0f, -0.25f));

        // Ledge platform on top of the wall (wall "ends" here at y=3.5), guarded by the
        // Ranged Bandit right before the gap chain.
        LevelBuildHelpers.CreatePlatform(terrainRoot.transform, "LedgePlatform", new Vector2(41f, 3f), new Vector2(6f, 1f), noFriction,
            new Vector2(0f, -0.5f));

        // Gap chain: three small stepping platforms past the first ledge, mirroring
        // Level 1's extension - see HeroKnightSandboxSetup.BuildScene()'s matching
        // comment for why (separate jumps with height variance, not a flat repeat).
        // Shifted down 2.5 units from their original heights to match LedgePlatform's
        // own height fix above.
        LevelBuildHelpers.CreatePlatform(terrainRoot.transform, "GapPlatform1", new Vector2(48f, 4f), new Vector2(2f, 1f), noFriction,
            new Vector2(0f, -0.46f));
        LevelBuildHelpers.CreatePlatform(terrainRoot.transform, "GapPlatform2", new Vector2(52f, 5f), new Vector2(2f, 1f), noFriction,
            new Vector2(0f, -0.51f));
        LevelBuildHelpers.CreatePlatform(terrainRoot.transform, "GapPlatform3", new Vector2(56f, 4f), new Vector2(2f, 1f), noFriction,
            new Vector2(0f, 0.51f));

        // Second, shorter wall + ledge climb - same 3.5-unit reachable-height budget as
        // the first climb above, this time measured from GapPlatform3's own top (y=5.01)
        // up to this ledge's top (y=8), hosting the level's final encounter
        // (HeavyEnemy_2 + RangedEnemy_2 - see BuildHeavyEnemy()/BuildRangedEnemy())
        // right before the goal.
        //
        // Center shifted 61 -> 61.5 (width stays 1) so both edges land on whole units
        // (x 61-62) instead of a half-unit boundary - as a bonus this also closes what
        // was a gap to LedgePlatform2's own left edge (x=62) into a flush seam.
        LevelBuildHelpers.CreatePlatform(terrainRoot.transform, "Wall2", new Vector2(61.5f, 0.5f), new Vector2(1f, 15f), noFriction);
        LevelBuildHelpers.CreatePlatform(terrainRoot.transform, "LedgePlatform2", new Vector2(65f, 7.5f), new Vector2(6f, 1f), noFriction);

        // Safety-net floor, well below either wall's own bottom and wide enough to catch
        // any fall trajectory across this level's extended span.
        GameObject safetyNet = LevelBuildHelpers.CreatePlatform(terrainRoot.transform, "SafetyNet", new Vector2(30f, -12.5f),
            new Vector2(110f, 1f), noFriction);

        // Second, trigger-only BoxCollider2D on the same GameObject (a compound collider
        // sharing SafetyNet's own static Rigidbody2D) so standing here shows the Respawn
        // prompt (see SafetyNetTrigger/RespawnPromptUI, same as HeroKnightSandboxSetup's
        // Level 1 SafetyNet) without affecting the solid collider above.
        BoxCollider2D respawnZone = safetyNet.AddComponent<BoxCollider2D>();
        respawnZone.isTrigger = true;
        respawnZone.size = new Vector2(110f, 3f);
        respawnZone.offset = new Vector2(0f, 2f);
        safetyNet.AddComponent<SafetyNetTrigger>();

        CreateTerrainTilemap(terrainRoot.transform);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
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
        TouchButton jumpBtn = BuildTouchButton(canvasGO.transform, "JumpButton", new Vector2(-100, 160), JumpIconPath);
        TouchButton attackBtn = BuildTouchButton(canvasGO.transform, "AttackButton", new Vector2(-220, 100), AttackIconPath);
        TouchButton blockBtn = BuildTouchButton(canvasGO.transform, "BlockButton", new Vector2(-100, 40), BlockIconPath);
        TouchButton rollBtn = BuildTouchButton(canvasGO.transform, "RollButton", new Vector2(-220, 220), RollIconPath);

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

        CinemachineFramingTransposer framingTransposer = vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
        framingTransposer.m_CameraDistance = 10f;

        // Lets HeroKnightController.Respawn() tell Cinemachine its teleport was a warp,
        // not real motion - see HeroKnightSandboxSetup.BuildScene()'s matching comment.
        var cameraSO = new SerializedObject(player.GetComponent<HeroKnightController>());
        cameraSO.FindProperty("followCamera").objectReferenceValue = vcam;
        cameraSO.ApplyModifiedPropertiesWithoutUndo();

        GameObject boundsGO = new GameObject("CameraBounds");
        PolygonCollider2D bounds = boundsGO.AddComponent<PolygonCollider2D>();
        bounds.isTrigger = true;
        // Widened/heightened for the level extension - LedgePlatform2 tops out at y=11
        // (vs the original LedgePlatform's y=6) and the new goal zone sits past x=70.
        bounds.points = new[]
        {
            new Vector2(-12, -16),
            new Vector2(-12, 18),
            new Vector2(90, 18),
            new Vector2(90, -16),
        };

        CinemachineConfiner2D confiner = vcamGO.AddComponent<CinemachineConfiner2D>();
        confiner.m_BoundingShape2D = bounds;

        EditorSceneManager.SaveScene(scene, ScenePath);
        LevelSelectSetup.RegisterAllScenes();

        Debug.Log("Level2Setup: scene built at " + ScenePath);
    }

    [MenuItem("HeroKnightSandbox/20b Save Level 2 Terrain Tiles")]
    public static void SaveTerrainTiles()
    {
        GameObject terrain = GameObject.Find("Terrain");
        if (terrain == null)
        {
            throw new System.Exception("Terrain instance not found in the open scene - run '20 Build Level 2 Scene' first");
        }

        Transform tiles = terrain.transform.Find("TerrainTiles");
        if (tiles == null)
        {
            throw new System.Exception("TerrainTiles not found under Terrain - run '20 Build Level 2 Scene' first");
        }

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        PrefabUtility.SaveAsPrefabAsset(tiles.gameObject, TerrainTilesPrefabPath);

        Debug.Log("Level2Setup: terrain tiles saved to " + TerrainTilesPrefabPath +
                   " - future '20 Build Level 2 Scene' runs restore them automatically instead of starting empty");
    }

    // Nature Landscapes mountain-valley art as the level's skybox: a single sprite pinned
    // to the camera via ParallaxLayer with movementScale (1,1,0), so it never scrolls
    // relative to the screen (true skybox behavior) rather than partially parallaxing -
    // simpler, and avoids needing a sprite wide enough to cover this level's full ~100-unit
    // CameraBounds span. See HeroKnightSandboxSetup.BuildBackground for the same setup.
    private static void BuildBackground(Vector3 cameraStartPosition)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SkyboxPath);
        if (sprite == null)
        {
            Debug.LogWarning("Level2Setup: skybox sprite not found at " + SkyboxPath);
            return;
        }

        GameObject skyGO = new GameObject("Skybox");
        skyGO.transform.position = new Vector3(cameraStartPosition.x, cameraStartPosition.y, 0f);

        SpriteRenderer renderer = skyGO.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        float targetWidth = 30f;
        float scale = targetWidth / sprite.bounds.size.x;
        skyGO.transform.localScale = new Vector3(scale, scale, 1f);

        renderer.sortingOrder = -100;

        ParallaxLayer parallax = skyGO.AddComponent<ParallaxLayer>();
        parallax.movementScale = new Vector3(1f, 1f, 0f);
    }

    private static void CreateTerrainTilemap(Transform parent)
    {
        Tilemap tilemap = LevelBuildHelpers.CreateOrRestoreTerrainTilemap(
            parent, TerrainTilesPrefabPath, NaturePalettePath, "Level2Setup");
        if (tilemap == null)
        {
            return;
        }

        // Auto-paints a plain default tile across every platform's cell rectangle - see
        // HeroKnightSandboxSetup.CreateTerrainTilemap()'s matching comment for why (no
        // distinct edge-cap tiles in this set, so one reused tile reads fine). The Tile
        // Palette is still opened (see CreateOrRestoreTerrainTilemap) in case manual
        // touch-ups are wanted after.
        TileBase groundTile = AssetDatabase.LoadAssetAtPath<TileBase>(GroundTilePath);
        TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);

        LevelBuildHelpers.PaintRect(tilemap, groundTile, -8, 22, -1, -1); // Ground
        LevelBuildHelpers.PaintRect(tilemap, groundTile, 26, 29, 1, 1); // StepPlatform1
        LevelBuildHelpers.PaintRect(tilemap, groundTile, 31, 34, 4, 4); // StepPlatform2
        LevelBuildHelpers.PaintRect(tilemap, wallTile, 36, 36, -10, 3); // Wall
        LevelBuildHelpers.PaintRect(tilemap, groundTile, 38, 43, 3, 3); // LedgePlatform
        LevelBuildHelpers.PaintRect(tilemap, groundTile, 47, 48, 4, 4); // GapPlatform1
        LevelBuildHelpers.PaintRect(tilemap, groundTile, 51, 52, 5, 5); // GapPlatform2
        LevelBuildHelpers.PaintRect(tilemap, groundTile, 55, 56, 4, 4); // GapPlatform3
        LevelBuildHelpers.PaintRect(tilemap, wallTile, 60, 61, -7, 8); // Wall2 (centered on an integer x, so it straddles two columns)
        LevelBuildHelpers.PaintRect(tilemap, groundTile, 62, 67, 8, 8); // LedgePlatform2
        LevelBuildHelpers.PaintRect(tilemap, groundTile, -25, 84, -13, -13); // SafetyNet

        Debug.Log("Level2Setup: 'Ground Tiles' Tilemap auto-painted under Terrain/TerrainTiles.");
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

    private static TouchButton BuildTouchButton(Transform canvasTransform, string name, Vector2 anchoredPos, string iconPath)
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
            Debug.LogWarning("Level2Setup: control icon not found at " + iconPath);
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

    [MenuItem("HeroKnightSandbox/21 Build Level 2 Terrain Decoration")]
    public static void BuildTerrainDecoration()
    {
        GameObject terrain = GameObject.Find("Terrain");
        if (terrain == null)
        {
            throw new System.Exception("Terrain instance not found in the open scene - run '20 Build Level 2 Scene' first");
        }

        Transform existing = terrain.transform.Find("Decoration");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject decoration = new GameObject("Decoration");
        decoration.transform.SetParent(terrain.transform, false);

        // Starting area near player spawn (-3, 0.5).
        PlaceProp(decoration.transform, 28, -5f, 0f);
        PlaceProp(decoration.transform, 40, -4f, 0f);
        PlaceProp(decoration.transform, 38, -1f, 0f);

        // Between Enemy_1 and Enemy_2.
        PlaceProp(decoration.transform, 16, 8f, 0f);
        PlaceProp(decoration.transform, 11, 9f, 0f);

        // Past Enemy_2, marking the gap before the staircase.
        PlaceProp(decoration.transform, 27, 18f, 0f);
        PlaceProp(decoration.transform, 21, 20f, 0f);

        // Staircase steps.
        PlaceProp(decoration.transform, 17, 28f, 2.0f);
        PlaceProp(decoration.transform, 9, 33f, 5f);

        // LedgePlatform (top y=3.0 - collider offset by (0, -0.5), x 38..44).
        PlaceProp(decoration.transform, 18, 40f, 3.0f);
        PlaceProp(decoration.transform, 12, 42f, 3.0f);

        // Gap chain platforms (tops y=4.04, 4.99, 5.01 - each collider offset per
        // BuildScene(), see LevelBuildHelpers.CreatePlatform() calls above).
        PlaceProp(decoration.transform, 21, 48f, 4.04f);
        PlaceProp(decoration.transform, 9, 52f, 4.99f);
        PlaceProp(decoration.transform, 27, 56f, 5.01f);

        // LedgePlatform2 (top y=8, x 62..68), around the final encounter and the goal.
        PlaceProp(decoration.transform, 18, 63f, 8f);
        PlaceProp(decoration.transform, 12, 67f, 8f);

        // SafetyNet floor - sparse.
        PlaceProp(decoration.transform, 25, 15f, -12f);
        PlaceProp(decoration.transform, 15, 45f, -12f);
        PlaceProp(decoration.transform, 25, 65f, -12f);
        PlaceProp(decoration.transform, 15, 80f, -12f);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("Level2Setup: terrain decoration built");
    }

    private static void PlaceProp(Transform parent, int prefabNumber, float x, float surfaceY)
    {
        string path = NaturePropsFolder + "Nature_props_" + prefabNumber.ToString("00") + ".prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning("Level2Setup: prop prefab not found at " + path);
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
        sr.sortingOrder = 1;
        instance.transform.position = new Vector3(x, surfaceY + sr.sprite.bounds.extents.y, 0f);
    }

    private static void CreateEnemy(string name, GameObject enemyPrefab, Vector2 anchorPosition,
        Vector2 startOffset, Vector2 endOffset, HeroKnightController player,
        int maxHP = 3, float moveSpeed = 2.0f, int attackDamage = 1, float attackRange = 1.0f, bool ranged = false)
    {
        Sprite projectileSprite = ranged ? AssetDatabase.LoadAssetAtPath<Sprite>(ProjectileSpritePath) : null;

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

    [MenuItem("HeroKnightSandbox/22 Build Level 2 Enemies")]
    public static void BuildEnemies()
    {
        GameObject player = GameObject.Find("HeroKnight");
        if (player == null)
        {
            throw new System.Exception("HeroKnight instance not found in the open scene - run '20 Build Level 2 Scene' first");
        }

        HeroKnightController controller = player.GetComponent<HeroKnightController>();
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);

        // Spread across the longer Ground platform (top y=0, spans x -8..22). Y matches
        // the platform's top surface exactly (not +0.5) - Bandit sprites are bottom-
        // pivoted, see HeroKnightSandboxSetup.BuildEnemies()'s matching comment.
        CreateEnemy("Enemy_1", enemyPrefab, new Vector2(2f, 0f), new Vector2(-1.5f, 0f), new Vector2(1.5f, 0f), controller);
        CreateEnemy("Enemy_2", enemyPrefab, new Vector2(14f, 0f), new Vector2(-1.5f, 0f), new Vector2(1.5f, 0f), controller);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("Level2Setup: enemies built");
    }

    [MenuItem("HeroKnightSandbox/23 Build Level 2 Heavy Enemy")]
    public static void BuildHeavyEnemy()
    {
        GameObject player = GameObject.Find("HeroKnight");
        if (player == null)
        {
            throw new System.Exception("HeroKnight instance not found in the open scene - run '20 Build Level 2 Scene' first");
        }

        HeroKnightController controller = player.GetComponent<HeroKnightController>();
        GameObject heavyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeavyEnemyPrefabPath);

        // Placed on StepPlatform2 (center 33,4.5, size 3x1 - top y=5), the far end of
        // the staircase. Y matches the platform's top surface exactly (not +0.5) -
        // Bandit sprites are bottom-pivoted, see BuildEnemies()'s matching comment.
        CreateEnemy("HeavyEnemy_1", heavyPrefab, new Vector2(33f, 5f), new Vector2(-1f, 0f), new Vector2(1f, 0f), controller,
            maxHP: 6, moveSpeed: 1.2f, attackDamage: 2);

        // Final encounter on LedgePlatform2 (center 65,7.5, size 6x1 - top y=8),
        // paired with RangedEnemy_2 (see BuildRangedEnemy()) right before the goal.
        CreateEnemy("HeavyEnemy_2", heavyPrefab, new Vector2(63.5f, 8f), new Vector2(-1.2f, 0f), new Vector2(1.2f, 0f), controller,
            maxHP: 6, moveSpeed: 1.2f, attackDamage: 2);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("Level2Setup: heavy enemy built");
    }

    [MenuItem("HeroKnightSandbox/24 Build Level 2 Ranged Enemy")]
    public static void BuildRangedEnemy()
    {
        GameObject player = GameObject.Find("HeroKnight");
        if (player == null)
        {
            throw new System.Exception("HeroKnight instance not found in the open scene - run '20 Build Level 2 Scene' first");
        }

        HeroKnightController controller = player.GetComponent<HeroKnightController>();
        // Archer.prefab, not the melee EnemyPrefabPath (Light Bandit) - run
        // "31 Build Archer Assets" first (see ArcherSetup) to generate it.
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArcherSetup.ArcherPrefabPath);

        // Placed on LedgePlatform (top y=3.0 - collider offset by (0, -0.5), x 38..44),
        // guarding the gap chain entrance. Y matches the platform's top surface exactly
        // (not +0.5) - Bandit sprites are bottom-pivoted, see BuildEnemies()'s matching
        // comment.
        CreateEnemy("RangedEnemy_1", enemyPrefab, new Vector2(41f, 3.0f), new Vector2(-1f, 0f), new Vector2(1f, 0f), controller,
            attackRange: 3.5f, ranged: true);

        // Final encounter on LedgePlatform2 (top y=8, x 62..68), paired with
        // HeavyEnemy_2 (see BuildHeavyEnemy()) right before the goal.
        CreateEnemy("RangedEnemy_2", enemyPrefab, new Vector2(66.5f, 8f), new Vector2(-1f, 0f), new Vector2(1f, 0f), controller,
            attackRange: 3.5f, ranged: true);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("Level2Setup: ranged enemy built");
    }

    [MenuItem("HeroKnightSandbox/25 Build Level 2 Objectives")]
    public static void BuildObjectives()
    {
        GameObject terrain = GameObject.Find("Terrain");
        if (terrain == null)
        {
            throw new System.Exception("Terrain instance not found in the open scene - run '20 Build Level 2 Scene' first");
        }

        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            throw new System.Exception("Canvas instance not found in the open scene - run '20 Build Level 2 Scene' first");
        }

        GameObject player = GameObject.Find("HeroKnight");
        if (player == null)
        {
            throw new System.Exception("HeroKnight instance not found in the open scene - run '20 Build Level 2 Scene' first");
        }

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

        // Just past the right edge of LedgePlatform2 (center 65, size 6x1, top y=8),
        // spanning y=7..10 so it's only reachable after both ledge climbs, the gap
        // chain, and the final encounter.
        GameObject goalZoneGO = new GameObject("GoalZone");
        goalZoneGO.transform.SetParent(terrain.transform, false);
        goalZoneGO.transform.position = new Vector3(71f, 8.5f, 0f);
        BoxCollider2D goalZoneCollider = goalZoneGO.AddComponent<BoxCollider2D>();
        goalZoneCollider.isTrigger = true;
        goalZoneCollider.size = new Vector2(2f, 3f);
        goalZoneGO.AddComponent<GoalZoneTrigger>();

        // See HeroKnightSandboxSetup.BuildObjectivesPanel()/BuildObjectiveRow() for why
        // this is a framed panel with icon rows rather than bare floating text.
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

        Debug.Log("Level2Setup: objectives built");
    }

    // See HeroKnightSandboxSetup.BuildObjectivesPanel() - identical, duplicated per this
    // file's own stated convention of not sharing per-level scene-building code.
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

    // See HeroKnightSandboxSetup.BuildObjectiveRow() - identical, duplicated per this
    // file's own stated convention of not sharing per-level scene-building code.
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
        // See HeroKnightSandboxSetup.BuildObjectiveRow()'s matching comment - 18pt was
        // overflowing "Defeat enemies: 0/6" past the checkbox and the panel's own edge.
        text.fontSize = 15f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
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
        text.text = "Level Complete!";

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

    [MenuItem("HeroKnightSandbox/26 Build Level 2 Player Health Bar")]
    public static void BuildPlayerHealthBar()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            throw new System.Exception("Canvas instance not found in the open scene - run '20 Build Level 2 Scene' first");
        }

        GameObject player = GameObject.Find("HeroKnight");
        if (player == null)
        {
            throw new System.Exception("HeroKnight instance not found in the open scene - run '20 Build Level 2 Scene' first");
        }

        GameObject existingFrame = GameObject.Find("PlayerHealthBarFrame");
        if (existingFrame != null)
        {
            Object.DestroyImmediate(existingFrame);
        }

        // See HeroKnightSandboxSetup.BuildPlayerHealthBar()'s matching comment on the
        // bordered frame + heart icon.
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

        Debug.Log("Level2Setup: player health bar built");
    }

    [MenuItem("HeroKnightSandbox/27 Build Level 2 Pause Menu")]
    public static void BuildPauseMenu()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            throw new System.Exception("Canvas instance not found in the open scene - run '20 Build Level 2 Scene' first");
        }

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

        Debug.Log("Level2Setup: pause menu built");
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

    [MenuItem("HeroKnightSandbox/28 Build Level 2 Fps Counter")]
    public static void BuildFpsCounter()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            throw new System.Exception("Canvas instance not found in the open scene - run '20 Build Level 2 Scene' first");
        }

        GameObject existing = GameObject.Find("FpsCounter");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        LevelBuildHelpers.BuildFpsCounter(canvas.transform, UIFont);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("Level2Setup: fps counter built");
    }

    [MenuItem("HeroKnightSandbox/Run All Level 2")]
    public static void RunAll()
    {
        BuildScene();
        BuildTerrainDecoration();
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
