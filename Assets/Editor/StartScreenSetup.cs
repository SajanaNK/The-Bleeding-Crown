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

    private static TMP_FontAsset UIFont => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UIFontPath);

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
        titleText.text = "Hero Knight Sandbox";

        GameObject playButton = BuildButton(canvasGO.transform, "PlayButton", new Vector2(0f, 0f), "Play");
        var playSO = new SerializedObject(playButton.AddComponent<LoadSceneButton>());
        playSO.FindProperty("sceneName").stringValue = LevelSelectSceneName;
        playSO.ApplyModifiedPropertiesWithoutUndo();

        BuildButton(canvasGO.transform, "QuitButton", new Vector2(0f, -110f), "Quit")
            .AddComponent<QuitGameButton>();

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
        text.fontSize = 42f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        // PressStart2P's wider glyphs can wrap short labels onto two lines - see
        // HeroKnightSandboxSetup.BuildHUDLine()'s matching comment.
        text.enableWordWrapping = false;
        text.text = label;

        return buttonGO;
    }
}
}
