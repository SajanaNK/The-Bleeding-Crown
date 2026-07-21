using HeroKnightSandbox.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HeroKnightSandbox.EditorTools
{
/// <summary>
/// Builds the title/start screen scene that boots before HeroKnightSandbox.unity.
/// Run via HeroKnightSandbox/11 Build Start Screen. Safe to re-run: overwrites its
/// own output each time, same convention as HeroKnightSandboxSetup.
/// </summary>
public static class StartScreenSetup
{
    private const string ScenePath = "Assets/Scenes/StartScreen.unity";
    private const string LevelSelectSceneName = "LevelSelect";
    private const string UIFontPath = "Assets/Mod Assets/Mod Resources/Fonts/PressStart2P-Regular SDF.asset";
    private const string NaturePropsFolder = "Assets/Nature_pixel_art_assets/Prefabs/Nature_props/";
    private const string LandscapeSkyboxPath = "Assets/Nature Landscapes Free Pixel Art/nature_3/origbig.png";

    private static TMP_FontAsset UIFont => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UIFontPath);

    // Storm-gloom banding for the sky, drawn top row first (see BuildSkyGradient). No warm
    // tones - this is a "rain about to break" palette, not the earlier dusk one.
    private static readonly Color[] SkyGradientStops =
    {
        new Color(0.027f, 0.027f, 0.043f),
        new Color(0.067f, 0.078f, 0.114f),
        new Color(0.114f, 0.129f, 0.176f),
        new Color(0.161f, 0.180f, 0.231f),
        new Color(0.220f, 0.243f, 0.298f),
        new Color(0.290f, 0.310f, 0.365f),
    };

    // Dark tint multiplied onto the treeline props so they read as storm-lit silhouettes
    // rather than the bright colors they'd have in-level.
    private static readonly Color TreelineTint = new Color(0.42f, 0.45f, 0.52f, 1f);

    // Dark blue-grey multiply tint over the (bright, sunny) nature_3 mountain art so it
    // reads as storm-lit rather than clashing with the gloomy palette around it.
    private static readonly Color LandscapeTint = new Color(0.30f, 0.34f, 0.44f, 1f);

    // (Nature_props prefab number, screen-width fraction, size multiplier, wind-sway degrees).
    // Prefab numbers identified by cropping Nature_props_01.png: 27-29 are full trees,
    // 31/33/34 are the blue shrub icons, 19/21/25 are rock clusters (swayAngle 0 - rocks
    // don't sway), 39 is the signpost (also rigid).
    private static readonly (int propNumber, float anchorX, float scale, float swayAngle)[] TreelineProps =
    {
        (28, 0.04f, 1.0f, 3f),
        (31, 0.10f, 0.6f, 7f),
        (19, 0.145f, 0.5f, 0f),
        (27, 0.22f, 0.9f, 3f),
        (33, 0.29f, 0.55f, 7f),
        (21, 0.35f, 0.55f, 0f),
        (29, 0.42f, 0.7f, 3f),
        (34, 0.49f, 0.55f, 7f),
        (28, 0.58f, 0.95f, 3f),
        (31, 0.65f, 0.6f, 7f),
        (25, 0.70f, 0.5f, 0f),
        (27, 0.78f, 0.85f, 3f),
        (33, 0.85f, 0.55f, 7f),
        (29, 0.91f, 0.65f, 3f),
        (39, 0.96f, 0.7f, 0f),
    };

    // Cloud shapes (35-37), each with an anchor position and a drift speed/direction -
    // faster and darker than a calm sky, pushed along by the incoming storm.
    private static readonly (int propNumber, Vector2 anchor, float width, float driftSpeed)[] CloudProps =
    {
        (35, new Vector2(0.12f, 0.90f), 300f, 16f),
        (37, new Vector2(0.30f, 0.80f), 260f, -14f),
        (36, new Vector2(0.50f, 0.94f), 340f, 20f),
        (35, new Vector2(0.68f, 0.82f), 240f, -12f),
        (37, new Vector2(0.85f, 0.92f), 280f, 18f),
        (36, new Vector2(0.95f, 0.76f), 200f, -16f),
    };

    // Small foliage sprites reused as wind-blown debris streaking across the sky.
    private static readonly (int propNumber, float anchorY, float driftSpeed)[] WindDebrisProps =
    {
        (9, 0.55f, 260f),
        (12, 0.42f, -220f),
        (7, 0.68f, 300f),
        (10, 0.35f, -250f),
    };

    // Birds (no bird sprite exists in the project's assets - see FlyingBird, which draws a
    // simple flapping "V" chevron instead).
    private static readonly (float anchorX, float anchorY, float driftSpeed)[] BirdSpots =
    {
        (0.20f, 0.72f, 55f),
        (0.35f, 0.66f, 48f),
        (0.55f, 0.74f, -60f),
        (0.72f, 0.68f, 52f),
    };

    [MenuItem("HeroKnightSandbox/11 Build Start Screen")]
    public static void BuildStartScreen()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camGO = new GameObject("Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        // EmptyScene (unlike the gameplay scenes' DefaultGameObjects) has no Main Camera of
        // its own, so nothing here is listening for the music/UI audio - without this,
        // Unity logs "no audio listeners in the scene" and nothing is audible at all.
        camGO.AddComponent<AudioListener>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        camGO.tag = "MainCamera";

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

        ScreenTransitionSetup.Build();
        MusicSetup.Build(MusicSetup.StartingMusicPath, 1f);

        // Background built first so every later sibling (title, buttons) draws on top of it.
        BuildSkyGradient(canvasGO.transform);
        BuildLandscapeBackdrop(canvasGO.transform);
        BuildClouds(canvasGO.transform);
        BuildBirds(canvasGO.transform);
        BuildWindDebris(canvasGO.transform);
        BuildTreeline(canvasGO.transform);
        BuildCrossedSwords(canvasGO.transform);

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(canvasGO.transform, false);
        RectTransform titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 0.5f);
        titleRT.anchorMax = new Vector2(0.5f, 0.5f);
        titleRT.pivot = new Vector2(0.5f, 0.5f);
        titleRT.anchoredPosition = new Vector2(0f, 200f);
        titleRT.sizeDelta = new Vector2(1000f, 200f);
        TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.font = UIFont;
        titleText.fontSize = 96f;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.text = "The Bleeding Crown";
        Shadow titleShadow = titleGO.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0.35f, 0.02f, 0.02f, 0.85f);
        titleShadow.effectDistance = new Vector2(4f, -4f);

        GameObject playButton = BuildButton(canvasGO.transform, "PlayButton", new Vector2(0f, 0f), "Play");
        var playSO = new SerializedObject(playButton.AddComponent<LoadSceneButton>());
        playSO.FindProperty("sceneName").stringValue = LevelSelectSceneName;
        playSO.ApplyModifiedPropertiesWithoutUndo();

        BuildButton(canvasGO.transform, "QuitButton", new Vector2(0f, -110f), "Quit")
            .AddComponent<QuitGameButton>();

        // Lightning built last so its full-screen flash draws over everything else.
        BuildLightning(canvasGO.transform);

        EditorSceneManager.SaveScene(scene, ScenePath);
        LevelSelectSetup.RegisterAllScenes();

        Debug.Log("HeroKnightSandboxSetup: start screen built at " + ScenePath);
    }

    private static GameObject BuildButton(Transform parent, string name, Vector2 anchoredPos, string label)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);
        RectTransform buttonRT = buttonGO.AddComponent<RectTransform>();
        buttonRT.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRT.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRT.pivot = new Vector2(0.5f, 0.5f);
        buttonRT.anchoredPosition = anchoredPos;
        buttonRT.sizeDelta = new Vector2(280f, 80f);
        KenneyButtonSkin.Apply(buttonGO);
        Shadow buttonShadow = buttonGO.AddComponent<Shadow>();
        buttonShadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        buttonShadow.effectDistance = new Vector2(3f, -3f);

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(12f, 12f);
        textRT.offsetMax = new Vector2(-12f, -12f);
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.font = UIFont;
        text.fontSize = 42f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.92f, 0.85f, 0.70f);
        // PressStart2P's wider glyphs can wrap short labels onto two lines - see
        // HeroKnightSandboxSetup.BuildHUDLine()'s matching comment.
        text.enableWordWrapping = false;
        text.text = label;

        return buttonGO;
    }

    // Full-screen dusk gradient, drawn as flat horizontal bands (SkyGradientStops, top to
    // bottom) rather than a shader/material gradient - keeps this to plain Image components
    // like the rest of the editor-built UI, and the banding reads fine against pixel art.
    private static void BuildSkyGradient(Transform canvasTransform)
    {
        GameObject skyGO = new GameObject("SkyGradient");
        skyGO.transform.SetParent(canvasTransform, false);
        RectTransform skyRT = skyGO.AddComponent<RectTransform>();
        skyRT.anchorMin = Vector2.zero;
        skyRT.anchorMax = Vector2.one;
        skyRT.offsetMin = Vector2.zero;
        skyRT.offsetMax = Vector2.zero;

        int bandCount = SkyGradientStops.Length;
        for (int i = 0; i < bandCount; i++)
        {
            GameObject bandGO = new GameObject("Band" + i);
            bandGO.transform.SetParent(skyGO.transform, false);
            RectTransform bandRT = bandGO.AddComponent<RectTransform>();
            float top = 1f - (float)i / bandCount;
            float bottom = 1f - (float)(i + 1) / bandCount;
            bandRT.anchorMin = new Vector2(0f, bottom);
            bandRT.anchorMax = new Vector2(1f, top);
            bandRT.offsetMin = Vector2.zero;
            bandRT.offsetMax = Vector2.zero;
            Image bandImage = bandGO.AddComponent<Image>();
            bandImage.color = SkyGradientStops[i];
            bandImage.raycastTarget = false;
        }
    }

    // Full-screen mountain-valley artwork (nature_3 from the Nature Landscapes pack) laid
    // over the gradient bands, dark-tinted to fit the storm mood. If the art is missing,
    // BuildSkyGradient's plain bands are left showing underneath as a fallback.
    private static void BuildLandscapeBackdrop(Transform canvasTransform)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LandscapeSkyboxPath);
        if (sprite == null)
        {
            return;
        }

        GameObject backdropGO = new GameObject("LandscapeBackdrop");
        backdropGO.transform.SetParent(canvasTransform, false);
        RectTransform rt = backdropGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image image = backdropGO.AddComponent<Image>();
        image.sprite = sprite;
        // Stretches to fill the screen rect - a full-bleed background doesn't need to
        // preserve the source art's aspect ratio.
        image.color = LandscapeTint;
        image.raycastTarget = false;
    }

    private static void BuildClouds(Transform canvasTransform)
    {
        GameObject cloudsGO = new GameObject("Clouds");
        cloudsGO.transform.SetParent(canvasTransform, false);
        RectTransform cloudsRT = cloudsGO.AddComponent<RectTransform>();
        cloudsRT.anchorMin = Vector2.zero;
        cloudsRT.anchorMax = Vector2.one;
        cloudsRT.offsetMin = Vector2.zero;
        cloudsRT.offsetMax = Vector2.zero;

        foreach (var (propNumber, anchor, width, driftSpeed) in CloudProps)
        {
            Sprite sprite = LoadPropSprite(propNumber);
            if (sprite == null)
            {
                continue;
            }

            GameObject cloudGO = new GameObject("Cloud_" + propNumber);
            cloudGO.transform.SetParent(cloudsGO.transform, false);
            RectTransform rt = cloudGO.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            float height = width * (sprite.rect.height / sprite.rect.width);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = Vector2.zero;

            Image image = cloudGO.AddComponent<Image>();
            image.sprite = sprite;
            image.color = new Color(0.24f, 0.25f, 0.30f, 0.92f);
            image.raycastTarget = false;

            CloudDrift drift = cloudGO.AddComponent<CloudDrift>();
            drift.speed = driftSpeed;
            drift.rangeX = 900f;
        }
    }

    private static void BuildBirds(Transform canvasTransform)
    {
        GameObject birdsGO = new GameObject("Birds");
        birdsGO.transform.SetParent(canvasTransform, false);
        RectTransform birdsRT = birdsGO.AddComponent<RectTransform>();
        birdsRT.anchorMin = Vector2.zero;
        birdsRT.anchorMax = Vector2.one;
        birdsRT.offsetMin = Vector2.zero;
        birdsRT.offsetMax = Vector2.zero;

        foreach (var (anchorX, anchorY, driftSpeed) in BirdSpots)
        {
            GameObject birdGO = new GameObject("Bird");
            birdGO.transform.SetParent(birdsGO.transform, false);
            RectTransform rt = birdGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorX, anchorY);
            rt.anchorMax = new Vector2(anchorX, anchorY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            FlyingBird bird = birdGO.AddComponent<FlyingBird>();
            bird.driftSpeed = driftSpeed;
        }
    }

    private static void BuildWindDebris(Transform canvasTransform)
    {
        GameObject debrisGO = new GameObject("WindDebris");
        debrisGO.transform.SetParent(canvasTransform, false);
        RectTransform debrisRT = debrisGO.AddComponent<RectTransform>();
        debrisRT.anchorMin = Vector2.zero;
        debrisRT.anchorMax = Vector2.one;
        debrisRT.offsetMin = Vector2.zero;
        debrisRT.offsetMax = Vector2.zero;

        foreach (var (propNumber, anchorY, driftSpeed) in WindDebrisProps)
        {
            Sprite sprite = LoadPropSprite(propNumber);
            if (sprite == null)
            {
                continue;
            }

            GameObject leafGO = new GameObject("Leaf_" + propNumber);
            leafGO.transform.SetParent(debrisGO.transform, false);
            RectTransform rt = leafGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, anchorY);
            rt.anchorMax = new Vector2(0.5f, anchorY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(sprite.rect.width * 1.5f, sprite.rect.height * 1.5f);
            rt.anchoredPosition = Vector2.zero;

            Image image = leafGO.AddComponent<Image>();
            image.sprite = sprite;
            image.color = new Color(0.3f, 0.32f, 0.28f, 0.8f);
            image.raycastTarget = false;

            CloudDrift drift = leafGO.AddComponent<CloudDrift>();
            drift.speed = driftSpeed;
            drift.rangeX = 1100f;
        }
    }

    private const string SwordSpritePath = "Assets/FlexUnit/2DMedievalWeaponPack/HQ/Sprites/Weapon/Sword.png";

    private static void BuildCrossedSwords(Transform canvasTransform)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SwordSpritePath);
        if (sprite == null)
        {
            return;
        }

        GameObject swordsGO = new GameObject("CrossedSwords");
        swordsGO.transform.SetParent(canvasTransform, false);
        RectTransform swordsRT = swordsGO.AddComponent<RectTransform>();
        swordsRT.anchorMin = new Vector2(0.5f, 0f);
        swordsRT.anchorMax = new Vector2(0.5f, 0f);
        swordsRT.pivot = new Vector2(0.5f, 0f);
        // Tips sit slightly below the ground line so the blades read as driven into the
        // dirt rather than floating above it.
        swordsRT.anchoredPosition = new Vector2(0f, -18f);
        swordsRT.sizeDelta = Vector2.zero;

        BuildSword(swordsRT, sprite, 28f, 105f, -3f);
        BuildSword(swordsRT, sprite, -28f, -83f, -4f);
    }

    // tiltAngle splays the blade apart from vertical (opposite signs cross into an "X");
    // xOffset/yOffset space the two planted tips apart along the ground.
    private static void BuildSword(Transform parent, Sprite sprite, float tiltAngle, float xOffset,
        float yOffset = 0f)
    {
        GameObject swordGO = new GameObject("Sword");
        swordGO.transform.SetParent(parent, false);
        RectTransform rt = swordGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        // The source art has its tip at the top and hilt at the bottom. Pivoting on the
        // tip means rotation swings the hilt around a fixed point instead of the whole
        // sword drifting - so a 180 flip (tip now down) plants the tip exactly at
        // anchoredPosition regardless of the added tilt.
        rt.pivot = new Vector2(0.5f, 1f);
        float height = 460f;
        rt.sizeDelta = new Vector2(height * (sprite.rect.width / sprite.rect.height), height);
        rt.anchoredPosition = new Vector2(xOffset, yOffset);
        rt.localRotation = Quaternion.Euler(0f, 0f, 180f + tiltAngle);

        Image image = swordGO.AddComponent<Image>();
        image.sprite = sprite;
        image.color = new Color(0.72f, 0.74f, 0.78f, 1f);
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private static void BuildLightning(Transform canvasTransform)
    {
        GameObject lightningGO = new GameObject("LightningFlash");
        lightningGO.transform.SetParent(canvasTransform, false);
        RectTransform rt = lightningGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image image = lightningGO.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = false;

        lightningGO.AddComponent<LightningFlash>();
    }

    private static void BuildTreeline(Transform canvasTransform)
    {
        GameObject treelineGO = new GameObject("Treeline");
        treelineGO.transform.SetParent(canvasTransform, false);
        RectTransform treelineRT = treelineGO.AddComponent<RectTransform>();
        treelineRT.anchorMin = Vector2.zero;
        treelineRT.anchorMax = Vector2.one;
        treelineRT.offsetMin = Vector2.zero;
        treelineRT.offsetMax = Vector2.zero;

        foreach (var (propNumber, anchorX, scale, swayAngle) in TreelineProps)
        {
            Sprite sprite = LoadPropSprite(propNumber);
            if (sprite == null)
            {
                continue;
            }

            GameObject propGO = new GameObject("Prop_" + propNumber);
            propGO.transform.SetParent(treelineGO.transform, false);
            RectTransform rt = propGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorX, 0f);
            rt.anchorMax = new Vector2(anchorX, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(sprite.rect.width * scale * 2.4f, sprite.rect.height * scale * 2.4f);
            rt.anchoredPosition = Vector2.zero;

            Image image = propGO.AddComponent<Image>();
            image.sprite = sprite;
            image.color = TreelineTint;
            image.raycastTarget = false;

            if (swayAngle > 0f)
            {
                WindSway sway = propGO.AddComponent<WindSway>();
                sway.swayAngle = swayAngle;
                sway.swaySpeed = swayAngle >= 6f ? 1.1f : 0.6f;
            }
        }
    }

    // prefabNumber is the Nature_props_NN.prefab file number (1-40); see the matching
    // comment on HeroKnightSandboxSetup.PlaceProp for the file-number-to-sprite-index
    // mapping quirk (only prefab 40 differs, mapping to sprite index 0).
    private static Sprite LoadPropSprite(int prefabNumber)
    {
        string path = NaturePropsFolder + "Nature_props_" + prefabNumber.ToString("00") + ".prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning("StartScreenSetup: prop prefab not found at " + path);
            return null;
        }

        return prefab.GetComponent<SpriteRenderer>().sprite;
    }
}
}
