using HeroKnightSandbox.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HeroKnightSandbox.EditorTools
{
    /// <summary>
    /// Shared menu-button setup: kenney_ui-pack-rpg-expansion (CC0) sprite-swap skin
    /// (buttonLong_brown / buttonLong_brown_pressed) plus the ButtonPunch hover/press
    /// animation and click sound, for every BuildButton/BuildMenuButton helper
    /// (StartScreenSetup, LevelSelectSetup, Level2Setup, HeroKnightSandboxSetup) to call.
    /// The pressed look comes from a pre-drawn sprite instead of a Color tint, so
    /// Button.transition drives a SpriteSwap rather than the ColorTint this project never
    /// wired up.
    /// </summary>
    public static class KenneyButtonSkin
    {
        private const string NormalSpritePath = "Assets/kenney_ui-pack-rpg-expansion/PNG/buttonLong_brown.png";
        private const string PressedSpritePath = "Assets/kenney_ui-pack-rpg-expansion/PNG/buttonLong_brown_pressed.png";
        private const string ClickClipPath = "Assets/Audio/button_click.mp3";

        // Both source PNGs are a rounded rect with a ~5-6px border/corner at their native
        // 190px width - 14px leaves a safe margin so 9-slicing never scales the corner art.
        private static readonly Vector4 Border = new Vector4(14f, 14f, 14f, 14f);

        public static void Apply(GameObject buttonGO)
        {
            Sprite normal = LoadSprite(NormalSpritePath);
            Sprite pressed = LoadSprite(PressedSpritePath);

            Image image = buttonGO.AddComponent<Image>();
            image.sprite = normal;
            image.type = Image.Type.Sliced;

            Button button = buttonGO.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            SpriteState state = button.spriteState;
            state.pressedSprite = pressed;
            state.highlightedSprite = normal;
            button.spriteState = state;

            buttonGO.AddComponent<AudioSource>().playOnAwake = false;
            ButtonPunch punch = buttonGO.AddComponent<ButtonPunch>();
            var punchSO = new SerializedObject(punch);
            punchSO.FindProperty("clickClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(ClickClipPath);
            punchSO.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite LoadSprite(string path)
        {
            EnsureImported(path);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureImported(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null || importer.textureType == TextureImporterType.Sprite)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = Border;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
    }
}
