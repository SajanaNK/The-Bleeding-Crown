using System.Collections.Generic;
using System.Linq;
using HeroKnightSandbox.Enemy;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace HeroKnightSandbox.EditorTools
{
/// <summary>
/// Builds the archer's sprite/animation/prefab assets from the raw GandalfHardcore
/// Archer sheet (Assets/GandalfHardcore Archer/) - a pure visual reskin of the existing
/// Ranged Bandit: same EnemyController/RangedAttackState/Projectile behavior, just a real
/// bow-draw sprite instead of the melee Light Bandit sprite the ranged bandit currently
/// reuses. Follows the same AnimatorOverrideController-over-LightBandit_AnimController
/// pattern HeavyBandit_AnimController.overrideController already uses for its own reskin.
/// See HeroKnightSandboxSetup.BuildRangedEnemy()/Level2Setup.BuildRangedEnemy() for where
/// the resulting prefab gets placed into each scene.
/// </summary>
public static class ArcherSetup
{
    private const string SheetPath = "Assets/GandalfHardcore Archer/GandalfHardcore Archer/GandalfHardcore Archer sheet.png";
    public const string ArrowSpritePath = "Assets/GandalfHardcore Archer/GandalfHardcore Archer/arrow.png";
    private const string BaseControllerPath = "Assets/Bandits - Pixel Art/Animations/Light Bandit/LightBandit_AnimController.controller";
    private const string AnimParentFolder = "Assets/GandalfHardcore Archer";
    private const string AnimFolder = AnimParentFolder + "/Animations";
    private const string OverrideControllerPath = AnimFolder + "/Archer_AnimController.overrideController";
    private const string EnemySourcePrefabPath = "Assets/Prefabs/HeroKnightEnemy.prefab";
    public const string ArcherPrefabPath = "Assets/Prefabs/HeroKnightArcher.prefab";

    private const int CellSize = 64;
    private const int SheetHeight = 320;

    // World height matched to Light Bandit's own 48px-cell/32-PPU convention, scaled up
    // for this sheet's larger 64px cells, so the archer reads at the same in-scene size
    // as the Bandits instead of towering over them.
    private const float PixelsPerUnit = 32f * CellSize / 48f;

    // Row-major frame ranges read directly off the sheet (704x320 = an 11 col x 5 row
    // grid of 64x64 cells; only each row's leading columns below are actually populated,
    // the rest of the canvas is blank). Row 0 = idle, row 1 = the full draw-then-loose
    // cycle, row 2 = run, row 3 = hit flinch, row 4 = death/collapse.
    private static readonly int[] IdleColumns = { 0, 1, 2, 3, 4 };
    private static readonly int[] AttackColumns = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    private static readonly int[] RunColumns = { 0, 1, 2, 3, 4, 5, 6, 7 };
    private static readonly int[] HurtColumns = { 0, 1, 2, 3, 4 };
    private static readonly int[] DeathColumns = { 0, 1, 2, 3, 4, 5 };

    [MenuItem("HeroKnightSandbox/31 Build Archer Assets")]
    public static void BuildAll()
    {
        SliceSheet();
        SliceArrow();

        Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath)
            .OfType<Sprite>()
            .ToDictionary(s => s.name);

        Sprite[] idle = LoadRow(sprites, IdleColumns, "Archer_Idle");
        Sprite[] attack = LoadRow(sprites, AttackColumns, "Archer_Attack");
        Sprite[] run = LoadRow(sprites, RunColumns, "Archer_Run");
        Sprite[] hurt = LoadRow(sprites, HurtColumns, "Archer_Hurt");
        Sprite[] death = LoadRow(sprites, DeathColumns, "Archer_Death");

        if (!AssetDatabase.IsValidFolder(AnimFolder))
        {
            AssetDatabase.CreateFolder(AnimParentFolder, "Animations");
        }

        AnimationClip idleClip = BuildClip("Archer_Idle", idle,
            new[] { 0f, 0.25f, 0.5f, 0.75f, 1f }, stopTime: 1.25f, loop: true);
        AnimationClip runClip = BuildClip("Archer_Run", run,
            new[] { 0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f }, stopTime: 0.8f, loop: true);
        AnimationClip attackClip = BuildAttackClip(attack);
        AnimationClip hurtClip = BuildClip("Archer_Hurt", hurt,
            new[] { 0f, 0.05f, 0.1f, 0.15f, 0.2f }, stopTime: 0.25f, loop: false);
        AnimationClip deathClip = BuildClip("Archer_Death", death,
            new[] { 0f, 0.15f, 0.3f, 0.5f, 0.7f, 0.85f }, stopTime: 1f, loop: false);

        BuildOverrideController(idleClip, runClip, attackClip, hurtClip, deathClip);
        BuildPrefab(idle[0]);

        AssetDatabase.SaveAssets();
        Debug.Log("ArcherSetup: archer assets built");
    }

    private static void SliceSheet()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(SheetPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.alphaIsTransparency = true;

        var metaData = new List<SpriteMetaData>();
        AddRow(metaData, 0, IdleColumns, "Archer_Idle");
        AddRow(metaData, 1, AttackColumns, "Archer_Attack");
        AddRow(metaData, 2, RunColumns, "Archer_Run");
        AddRow(metaData, 3, HurtColumns, "Archer_Hurt");
        AddRow(metaData, 4, DeathColumns, "Archer_Death");
#pragma warning disable CS0618 // classic per-sprite-rect slicing API; still functional in 2022.3
        importer.spritesheet = metaData.ToArray();
#pragma warning restore CS0618

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    private static void AddRow(List<SpriteMetaData> list, int row, int[] columns, string prefix)
    {
        // Sprite rects use a bottom-left origin, but rows here were identified top-down
        // (row 0 = top of the sheet) - flip to match.
        int y = SheetHeight - (row + 1) * CellSize;
        foreach (int col in columns)
        {
            list.Add(new SpriteMetaData
            {
                name = $"{prefix}_{col}",
                rect = new Rect(col * CellSize, y, CellSize, CellSize),
                // BottomCenter, matching Light/Heavy Bandit's own pivot convention -
                // CreateEnemy() places every enemy at its patrol anchor's ground Y, which
                // only lines up with the sprite's feet if the pivot is bottom-anchored.
                alignment = (int)SpriteAlignment.BottomCenter,
                pivot = new Vector2(0.5f, 0f),
            });
        }
    }

    private static Sprite[] LoadRow(Dictionary<string, Sprite> sprites, int[] columns, string prefix)
    {
        return columns.Select(c => sprites[$"{prefix}_{c}"]).ToArray();
    }

    private static void SliceArrow()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(ArrowSpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        // Matches the FlexUnit Arrow.png this replaces, so Projectile's hitRadius
        // (tuned against that sprite's scale) still reads correctly.
        importer.spritePixelsPerUnit = 100f;
        // Center alignment/pivot (0.5, 0.5) is Sprite Single mode's own default -
        // no explicit setting needed.
        importer.alphaIsTransparency = true;

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    private static AnimationClip BuildAttackClip(Sprite[] sprites)
    {
        var times = new float[sprites.Length];
        // Frames 0..(n-2) are the draw-through-loose sequence, spaced across
        // AttackWindup (EnemyContext's default 0.4s) so the loose/release frame lands
        // exactly when RangedAttackState.Tick() actually spawns the projectile.
        int loosedCount = sprites.Length - 1;
        for (int i = 0; i < loosedCount; i++)
        {
            times[i] = i * 0.4f / (loosedCount - 1);
        }
        // Final frame is the bow-lowered recover pose, held through AttackCooldown.
        times[sprites.Length - 1] = 0.45f;

        return BuildClip("Archer_Attack", sprites, times, stopTime: 0.8f, loop: false);
    }

    private static AnimationClip BuildClip(string name, Sprite[] sprites, float[] times, float stopTime, bool loop)
    {
        var keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe { time = times[i], value = sprites[i] };
        }

        string path = $"{AnimFolder}/{name}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        bool isNew = clip == null;
        if (isNew)
        {
            clip = new AnimationClip();
        }

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.stopTime = stopTime;
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (isNew)
        {
            AssetDatabase.CreateAsset(clip, path);
        }
        else
        {
            EditorUtility.SetDirty(clip);
        }

        return clip;
    }

    private static void BuildOverrideController(AnimationClip idle, AnimationClip run, AnimationClip attack,
        AnimationClip hurt, AnimationClip death)
    {
        var baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>(BaseControllerPath);
        var overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(OverrideControllerPath);
        bool isNew = overrideController == null;
        if (isNew)
        {
            overrideController = new AnimatorOverrideController(baseController);
        }
        else
        {
            overrideController.runtimeAnimatorController = baseController;
        }

        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);
        for (int i = 0; i < overrides.Count; i++)
        {
            AnimationClip original = overrides[i].Key;
            AnimationClip replacement = original.name switch
            {
                "LightBandit_Attack" => attack,
                "LightBandit_Run" => run,
                "LightBandit_Hurt" => hurt,
                "LightBandit_Death" => death,
                // CombatIdle/Jump/Recover: vendor-only states this ground-patrol
                // EnemyController AI never triggers (see EnemyController.Update -
                // it only ever sets AnimState or fires Attack/Hurt/Death) - pointed at
                // Idle so the override list stays fully valid without needing dedicated
                // archer frames for states nothing can reach.
                _ => idle,
            };
            overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, replacement);
        }
        overrideController.ApplyOverrides(overrides);

        if (isNew)
        {
            AssetDatabase.CreateAsset(overrideController, OverrideControllerPath);
        }
        else
        {
            EditorUtility.SetDirty(overrideController);
        }
    }

    private static void BuildPrefab(Sprite defaultSprite)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(OverrideControllerPath);

        // Cloned from the already-built, already-correct HeroKnightEnemy prefab (collider
        // size/offset, Kinematic Rigidbody2D, EnemyController + health bar wiring) rather
        // than reconstructed from scratch - this is a reskin, so every non-visual part of
        // the enemy stays byte-for-byte what Light Bandit already uses. PixelsPerUnit
        // above was chosen specifically so the archer's in-scene size matches too, meaning
        // the collider needs no resizing either.
        GameObject root = PrefabUtility.LoadPrefabContents(EnemySourcePrefabPath);
        SpriteRenderer spriteRenderer = root.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = defaultSprite;
        // PatrolState/ChaseState/AttackState/RangedAttackState only ever flip facing via
        // Transform.localScale.x (never SpriteRenderer.flipX), under the shared
        // convention that positive scale.x = facing left - true for Light Bandit's
        // default art, but this sheet's archer faces right by default. A fixed flipX
        // here pre-corrects that mismatch once at the art level, so the shared,
        // melee-enemy-used state code needs no per-enemy-type awareness of which way its
        // source art originally faced.
        spriteRenderer.flipX = true;
        root.GetComponent<Animator>().runtimeAnimatorController = controller;

        // Cloned from HeroKnightEnemy.prefab, which already carries Light Bandit's own
        // attackClips - override with the archer's own sounds so it doesn't fire the
        // wrong enemy type's swing noise. deathClips is already the shared enemy_died.mp3
        // either way, but set explicitly rather than relying on that being true of
        // whatever the clone source happens to carry.
        var enemySO = new SerializedObject(root.GetComponent<EnemyController>());
        HeroKnightSandboxSetup.SetClipArray(enemySO, "attackClips", new[]
        {
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/archer_attack_1.mp3"),
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/archer_attack_2.mp3"),
        });
        HeroKnightSandboxSetup.SetClipArray(enemySO, "deathClips", new[]
        {
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/enemy_died.mp3"),
        });
        enemySO.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, ArcherPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }
}
}