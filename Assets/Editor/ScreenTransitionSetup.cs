using HeroKnightSandbox.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HeroKnightSandbox.EditorTools
{
/// <summary>
/// Builds the full-screen fade overlay used for scene-load transitions (see
/// ScreenTransition). Identical in every scene with no per-level variation, so - unlike
/// the terrain-building code each scene's own Setup script owns independently - this one
/// helper is shared and called from all four (StartScreenSetup, LevelSelectSetup,
/// HeroKnightSandboxSetup, Level2Setup) rather than copy-pasted four times.
/// </summary>
public static class ScreenTransitionSetup
{
    public static void Build()
    {
        GameObject root = new GameObject("ScreenTransition");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Highest sorting order of any canvas in any scene, so the fade always draws
        // over everything else - including the Start Screen's LightningFlash.
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        root.AddComponent<GraphicRaycaster>();

        GameObject fadeGO = new GameObject("Fade");
        fadeGO.transform.SetParent(root.transform, false);
        RectTransform fadeRT = fadeGO.AddComponent<RectTransform>();
        fadeRT.anchorMin = Vector2.zero;
        fadeRT.anchorMax = Vector2.one;
        fadeRT.offsetMin = Vector2.zero;
        fadeRT.offsetMax = Vector2.zero;
        Image fadeImage = fadeGO.AddComponent<Image>();
        // Opaque black - ScreenTransition.Start() fades this out once the scene settles.
        fadeImage.color = Color.black;

        ScreenTransition transition = root.AddComponent<ScreenTransition>();
        var so = new SerializedObject(transition);
        so.FindProperty("fadeImage").objectReferenceValue = fadeImage;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
}
