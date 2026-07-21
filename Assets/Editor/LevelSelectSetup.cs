using System.Linq;
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
/// Builds the level-select scene between the Start Screen and the playable levels.
/// Run via HeroKnightSandbox/12 Build Level Select. Safe to re-run: overwrites its
/// own output each time, same convention as HeroKnightSandboxSetup/StartScreenSetup.
/// </summary>
public static class LevelSelectSetup
{
    private const string ScenePath = "Assets/Scenes/LevelSelect.unity";
    private const string UIFontPath = "Assets/Mod Assets/Mod Resources/Fonts/PressStart2P-Regular SDF.asset";
    private const string LandscapeSkyboxPath = "Assets/Nature Landscapes Free Pixel Art/nature_3/origbig.png";

    private static TMP_FontAsset UIFont => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UIFontPath);

    [MenuItem("HeroKnightSandbox/14 Build Level Select")]
    public static void BuildLevelSelect()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camGO = new GameObject("Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        // See StartScreenSetup's matching comment - EmptyScene has no built-in listener.
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

        // Background built first so title/buttons draw on top of it.
        BuildBackground(canvasGO.transform);

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(canvasGO.transform, false);
        RectTransform titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 0.5f);
        titleRT.anchorMax = new Vector2(0.5f, 0.5f);
        titleRT.pivot = new Vector2(0.5f, 0.5f);
        titleRT.anchoredPosition = new Vector2(0f, 220f);
        titleRT.sizeDelta = new Vector2(1000f, 150f);
        TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.font = UIFont;
        titleText.fontSize = 72f;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        // PressStart2P's wider glyphs were wrapping this onto two lines at 1000 units -
        // see HeroKnightSandboxSetup.BuildHUDLine()'s matching comment.
        titleText.enableWordWrapping = false;
        titleText.text = "Select Level";
        Shadow titleShadow = titleGO.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
        titleShadow.effectDistance = new Vector2(3f, -3f);

        BuildLevelButton(canvasGO.transform, new Vector2(0f, 40f), "Level 1", "HeroKnightSandbox");
        BuildLevelButton(canvasGO.transform, new Vector2(0f, -60f), "Level 2", "Level2");

        GameObject backButton = BuildButton(canvasGO.transform, "BackButton", new Vector2(0f, -180f), "Back");
        var backSO = new SerializedObject(backButton.AddComponent<LoadSceneButton>());
        backSO.FindProperty("sceneName").stringValue = "StartScreen";
        backSO.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        RegisterAllScenes();

        Debug.Log("HeroKnightSandboxSetup: level select built at " + ScenePath);
    }

    // Full-screen nature_3 mountain-valley art (same piece used for the Start Screen and
    // in-level skyboxes, for visual continuity across the whole menu flow), with a light
    // dark overlay on top so the title/buttons stay legible over the busy artwork.
    private static void BuildBackground(Transform canvasTransform)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LandscapeSkyboxPath);
        if (sprite == null)
        {
            Debug.LogWarning("LevelSelectSetup: background sprite not found at " + LandscapeSkyboxPath);
            return;
        }

        GameObject backdropGO = new GameObject("Backdrop");
        backdropGO.transform.SetParent(canvasTransform, false);
        RectTransform backdropRT = backdropGO.AddComponent<RectTransform>();
        backdropRT.anchorMin = Vector2.zero;
        backdropRT.anchorMax = Vector2.one;
        backdropRT.offsetMin = Vector2.zero;
        backdropRT.offsetMax = Vector2.zero;
        Image backdropImage = backdropGO.AddComponent<Image>();
        backdropImage.sprite = sprite;
        backdropImage.raycastTarget = false;

        GameObject dimGO = new GameObject("Dim");
        dimGO.transform.SetParent(canvasTransform, false);
        RectTransform dimRT = dimGO.AddComponent<RectTransform>();
        dimRT.anchorMin = Vector2.zero;
        dimRT.anchorMax = Vector2.one;
        dimRT.offsetMin = Vector2.zero;
        dimRT.offsetMax = Vector2.zero;
        Image dimImage = dimGO.AddComponent<Image>();
        dimImage.color = new Color(0f, 0f, 0f, 0.35f);
        dimImage.raycastTarget = false;
    }

    private static void BuildLevelButton(Transform canvasTransform, Vector2 anchoredPos, string label, string sceneName)
    {
        GameObject button = BuildButton(canvasTransform, label.Replace(" ", "") + "Button", anchoredPos, label);
        var so = new SerializedObject(button.AddComponent<LoadSceneButton>());
        so.FindProperty("sceneName").stringValue = sceneName;
        so.ApplyModifiedPropertiesWithoutUndo();
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
        // PressStart2P's wider glyphs were wrapping "Level 1"/"Level 2" onto two lines -
        // see HeroKnightSandboxSetup.BuildHUDLine()'s matching comment.
        text.enableWordWrapping = false;
        text.text = label;

        return buttonGO;
    }

    // Single authoritative place for Build Settings scene order across the whole
    // menu flow - SceneManager.LoadScene(string) requires every target scene to be
    // registered here (true in Editor Play Mode, not just real builds), and with
    // four scenes now wired together by name from three different setup scripts,
    // having each script independently insert/reorder its own entry risked
    // producing a different final order depending on which setup step ran last.
    // This instead unconditionally rebuilds the whole list in one fixed order, so
    // it's safe to call from any of the setup scripts in any order.
    public static void RegisterAllScenes()
    {
        string[] orderedPaths =
        {
            "Assets/Scenes/StartScreen.unity",
            "Assets/Scenes/LevelSelect.unity",
            "Assets/Scenes/HeroKnightSandbox.unity",
            "Assets/Scenes/Level2.unity",
        };

        EditorBuildSettings.scenes = orderedPaths
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();
    }
}
}
