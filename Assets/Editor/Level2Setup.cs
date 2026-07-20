using Cinemachine;
using CodeMonkey.HealthSystemCM;
using HeroKnightSandbox;
using HeroKnightSandbox.Enemy;
using HeroKnightSandbox.Input;
using HeroKnightSandbox.Objectives;
using HeroKnightSandbox.UI;
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
/// Deliberately duplicates (rather than shares) HeroKnightSandboxSetup's scene-
/// scaffolding code - see the "Level 2 code structure" design decision: with only
/// two levels so far, a shared-helper extraction would be speculative
/// generalization. Revisit if/when a Level 3 ever comes along (rule of three).
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
    private const string ProjectileSpritePath = "Assets/FlexUnit/2DMedievalWeaponPack/LQ/Sprites/Bow/Arrow.png";
    private const string NaturePalettePath = "Assets/Nature_pixel_art_assets/palette/Nature_environment_01.prefab";
    private const string NaturePropsFolder = "Assets/Nature_pixel_art_assets/Prefabs/Nature_props/";
    private const string CompletionSoundPath = "Assets/Audio/Collectable.wav";
    private const string ConfettiPrefabPath = "Assets/Mod Assets/Particle Prefabs/ConfettiCelebration.prefab";
    private const string PlayerHealthBarPrefabPath = "Assets/CodeMonkey/HealthSystem/Prefabs/pfHealthBarUI.prefab";
    private const string TerrainTilesPrefabPath = "Assets/Prefabs/Level2_TerrainTiles.prefab";

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

        PhysicsMaterial2D noFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(PhysicsMaterialPath);

        GameObject terrainRoot = new GameObject("Terrain");

        // Longer flat opening than Level 1's (28 units vs 24), both Light Bandits spread
        // further apart across it. Top surface at y = 0.
        CreatePlatform(terrainRoot.transform, "Ground", new Vector2(7f, -0.5f), new Vector2(30f, 1f), noFriction);

        // Two-step staircase (vs. Level 1's single JumpPlatform) leading up to the
        // Heavy Bandit - each step individually jumpable, so climbing it reads as a
        // deliberate sequence rather than one big leap.
        CreatePlatform(terrainRoot.transform, "StepPlatform1", new Vector2(28f, 2f), new Vector2(3f, 1f), noFriction);
        CreatePlatform(terrainRoot.transform, "StepPlatform2", new Vector2(33f, 4.5f), new Vector2(3f, 1f), noFriction);

        // Wall face after the staircase: a wider gap (x 22..26, ground ends at 22, first
        // step starts at 26.5) than Level 1's, so the player is unmistakably airborne
        // well before either landing on the steps or reaching the wall. Wall spans
        // y=6 down to y=-10, same bottomless-within-this-level convention as Level 1.
        CreatePlatform(terrainRoot.transform, "Wall", new Vector2(37f, -2f), new Vector2(1f, 16f), noFriction);

        // Ledge platform on top of the wall (wall "ends" here at y=6), guarded by the
        // Ranged Bandit right before the goal zone.
        CreatePlatform(terrainRoot.transform, "LedgePlatform", new Vector2(41f, 5.5f), new Vector2(6f, 1f), noFriction);

        // Safety-net floor, well below the wall's own bottom and wide enough to catch
        // any fall trajectory across this level's longer span.
        CreatePlatform(terrainRoot.transform, "SafetyNet", new Vector2(25f, -12.5f), new Vector2(90f, 1f), noFriction);

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

        GameObject eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.AddComponent<EventSystem>();
        eventSystemGO.AddComponent<StandaloneInputModule>();

        GameObject touchInputGO = new GameObject("TouchControls");
        touchInputGO.transform.SetParent(canvasGO.transform, false);
        TouchHeroKnightInput touchInput = touchInputGO.AddComponent<TouchHeroKnightInput>();

        VirtualJoystick joystick = BuildJoystick(canvasGO.transform);
        TouchButton jumpBtn = BuildTouchButton(canvasGO.transform, "JumpButton", new Vector2(-100, 160));
        TouchButton attackBtn = BuildTouchButton(canvasGO.transform, "AttackButton", new Vector2(-220, 100));
        TouchButton blockBtn = BuildTouchButton(canvasGO.transform, "BlockButton", new Vector2(-100, 40));
        TouchButton rollBtn = BuildTouchButton(canvasGO.transform, "RollButton", new Vector2(-220, 220));

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

        GameObject boundsGO = new GameObject("CameraBounds");
        PolygonCollider2D bounds = boundsGO.AddComponent<PolygonCollider2D>();
        bounds.isTrigger = true;
        bounds.points = new[]
        {
            new Vector2(-12, -16),
            new Vector2(-12, 10),
            new Vector2(60, 10),
            new Vector2(60, -16),
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

    private static void CreateTerrainTilemap(Transform parent)
    {
        GameObject existingTiles = AssetDatabase.LoadAssetAtPath<GameObject>(TerrainTilesPrefabPath);
        if (existingTiles != null)
        {
            PrefabUtility.InstantiatePrefab(existingTiles, parent);
            Debug.Log("Level2Setup: restored hand-painted TerrainTiles from " + TerrainTilesPrefabPath);
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
            Debug.LogWarning("Level2Setup: Nature tile palette not found at " + NaturePalettePath);
        }
        else
        {
            EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
            UnityEditor.Tilemaps.GridPaintingState.palette = palette;
        }

        Debug.Log(
            "Level2Setup: 'Ground Tiles' Tilemap created under Terrain/TerrainTiles. " +
            "Open Window > 2D > Tile Palette, select 'Nature_environment_01', and hand-paint these " +
            "cell rectangles to match the existing colliders:\n" +
            "  Ground: x=-8..22, y=-1 (30x1)\n" +
            "  StepPlatform1: x=26..29, y=1 (3x1)\n" +
            "  StepPlatform2: x=31..34, y=4 (3x1)\n" +
            "  Wall: x=36, y=-10..5 (1x16)\n" +
            "  LedgePlatform: x=38..43, y=5 (6x1)\n" +
            "  SafetyNet: x=-20..70, y=-13 (90x1)");
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

    private static TouchButton BuildTouchButton(Transform canvasTransform, string name, Vector2 anchoredPos)
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
        PlaceProp(decoration.transform, 17, 28f, 2.5f);
        PlaceProp(decoration.transform, 9, 33f, 5f);

        // LedgePlatform (top y=6, x 38..44).
        PlaceProp(decoration.transform, 18, 40f, 6f);
        PlaceProp(decoration.transform, 12, 42f, 6f);

        // SafetyNet floor - sparse.
        PlaceProp(decoration.transform, 25, 15f, -12f);
        PlaceProp(decoration.transform, 15, 45f, -12f);

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

        // Spread across the longer Ground platform (top y=0, spans x -8..22).
        CreateEnemy("Enemy_1", enemyPrefab, new Vector2(2f, 0.5f), new Vector2(-1.5f, 0f), new Vector2(1.5f, 0f), controller);
        CreateEnemy("Enemy_2", enemyPrefab, new Vector2(14f, 0.5f), new Vector2(-1.5f, 0f), new Vector2(1.5f, 0f), controller);

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
        // the staircase.
        CreateEnemy("HeavyEnemy_1", heavyPrefab, new Vector2(33f, 5.5f), new Vector2(-1f, 0f), new Vector2(1f, 0f), controller,
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
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);

        // Placed on LedgePlatform (top y=6, x 38..44), guarding the goal zone.
        CreateEnemy("RangedEnemy_1", enemyPrefab, new Vector2(41f, 6.5f), new Vector2(-1f, 0f), new Vector2(1f, 0f), controller,
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

        // Just past the right edge of LedgePlatform (center 41, size 6x1, top y=6),
        // spanning y=5..8 so it's only reachable after the ledge climb.
        GameObject goalZoneGO = new GameObject("GoalZone");
        goalZoneGO.transform.SetParent(terrain.transform, false);
        goalZoneGO.transform.position = new Vector3(44f, 6.5f, 0f);
        BoxCollider2D goalZoneCollider = goalZoneGO.AddComponent<BoxCollider2D>();
        goalZoneCollider.isTrigger = true;
        goalZoneCollider.size = new Vector2(2f, 3f);
        goalZoneGO.AddComponent<GoalZoneTrigger>();

        GameObject hudGO = new GameObject("ObjectivesHUD");
        hudGO.transform.SetParent(canvas.transform, false);

        TextMeshProUGUI enemiesLine = BuildHUDLine(hudGO.transform, "EnemiesLine", new Vector2(20f, -60f));
        TextMeshProUGUI goalLine = BuildHUDLine(hudGO.transform, "GoalLine", new Vector2(20f, -90f));
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

        Debug.Log("Level2Setup: objectives built");
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
        rt.sizeDelta = new Vector2(400f, 30f);

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = 24f;
        text.color = Color.white;
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
        text.fontSize = 64f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.text = "Level Complete!";

        BuildMenuButton(panelGO.transform, "RestartButton", new Vector2(0f, -100f), "Restart")
            .AddComponent<RestartButton>();

        GameObject levelSelectButton = BuildMenuButton(panelGO.transform, "LevelSelectButton", new Vector2(0f, -200f), "Level Select");
        var levelSelectSO = new SerializedObject(levelSelectButton.AddComponent<LoadSceneButton>());
        levelSelectSO.FindProperty("sceneName").stringValue = "LevelSelect";
        levelSelectSO.ApplyModifiedPropertiesWithoutUndo();

        panelGO.SetActive(false);
        return panelGO;
    }

    private static GameObject BuildMenuButton(Transform parent, string name, Vector2 anchoredPos, string label)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);
        RectTransform buttonRT = buttonGO.AddComponent<RectTransform>();
        buttonRT.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRT.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRT.pivot = new Vector2(0.5f, 0.5f);
        buttonRT.anchoredPosition = anchoredPos;
        buttonRT.sizeDelta = new Vector2(240f, 70f);
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0.85f);
        buttonGO.AddComponent<Button>();

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.fontSize = 36f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        text.text = label;

        return buttonGO;
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

        GameObject existingBar = GameObject.Find("PlayerHealthBar");
        if (existingBar != null)
        {
            Object.DestroyImmediate(existingBar);
        }

        GameObject barPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerHealthBarPrefabPath);
        GameObject bar = (GameObject)PrefabUtility.InstantiatePrefab(barPrefab, canvas.transform);
        bar.name = "PlayerHealthBar";

        RectTransform barRT = bar.GetComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0f, 1f);
        barRT.anchorMax = new Vector2(0f, 1f);
        barRT.pivot = new Vector2(0f, 1f);
        barRT.anchoredPosition = new Vector2(20f, -20f);

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
    }
}
}
