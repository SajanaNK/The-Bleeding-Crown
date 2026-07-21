using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace HeroKnightSandbox.EditorTools
{
/// <summary>
/// Scene-scaffolding primitives shared between HeroKnightSandboxSetup (Level 1) and
/// Level2Setup (Level 2) - both build a terrain/UI layout from hardcoded values using
/// the same collider-platform, tilemap-restore, and menu-button construction. Extracted
/// after having to fix the same bug in both files' copies twice in one session (the
/// TerrainTiles prefab-rename bug in CreateTerrainTilemap, and adding BoxCollider2D
/// offset support to CreatePlatform). Level-specific geometry (BuildScene's actual
/// platform layout, per-level PaintRect coordinates, decoration/enemy placement) stays
/// in each level's own file - only the reusable construction code lives here.
/// </summary>
internal static class LevelBuildHelpers
{
    public static GameObject CreatePlatform(Transform parent, string name, Vector2 center, Vector2 size,
        PhysicsMaterial2D material, Vector2 offset = default)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(center.x, center.y, 0f);

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        col.offset = offset;
        col.sharedMaterial = material;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        return go;
    }

    public static void PaintRect(Tilemap tilemap, TileBase tile, int xMin, int xMax, int yMin, int yMax)
    {
        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
    }

    // Restores a previously hand-painted TerrainTiles prefab if one exists at
    // terrainTilesPrefabPath, renaming its root back to "TerrainTiles" - the prefab
    // asset's own root is named after its own asset file, not that, which is what broke
    // SaveTerrainTiles()'s Find("TerrainTiles") lookup before this rename was added.
    // Returns the Tilemap to paint into only when nothing was restored - callers should
    // skip their own PaintRect calls entirely when this returns null.
    public static Tilemap CreateOrRestoreTerrainTilemap(Transform parent, string terrainTilesPrefabPath,
        string naturePalettePath, string logPrefix)
    {
        GameObject existingTiles = AssetDatabase.LoadAssetAtPath<GameObject>(terrainTilesPrefabPath);
        if (existingTiles != null)
        {
            GameObject restored = (GameObject)PrefabUtility.InstantiatePrefab(existingTiles, parent);
            restored.name = "TerrainTiles";
            Debug.Log(logPrefix + ": restored hand-painted TerrainTiles from " + terrainTilesPrefabPath);
            return null;
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

        GameObject palette = AssetDatabase.LoadAssetAtPath<GameObject>(naturePalettePath);
        if (palette == null)
        {
            Debug.LogWarning(logPrefix + ": Nature tile palette not found at " + naturePalettePath);
        }
        else
        {
            EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
            UnityEditor.Tilemaps.GridPaintingState.palette = palette;
        }

        return tilemapGO.GetComponent<Tilemap>();
    }

    // Reusable button build: KenneyButtonSkin's sprite-swap rectangle (matching the Start
    // Screen's button style) plus a centered label and a hover/press scale animation.
    // Caller adds whichever click-behavior component (RestartButton, ResumeButton,
    // LoadSceneButton, ...) fits.
    public static GameObject BuildMenuButton(Transform parent, string name, Vector2 anchoredPos, string label,
        TMP_FontAsset font)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);
        RectTransform buttonRT = buttonGO.AddComponent<RectTransform>();
        buttonRT.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRT.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRT.pivot = new Vector2(0.5f, 0.5f);
        buttonRT.anchoredPosition = anchoredPos;
        buttonRT.sizeDelta = new Vector2(500f, 70f);
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
        text.font = font;
        // PressStart2P is monospace with a full-em advance per glyph (confirmed in the
        // font asset: m_HorizontalAdvance 51 at pointSize 51, a 1:1 ratio) - each
        // character costs roughly its own fontSize in width (see the
        // project_pressstart2p_monospace_width convention). 500w/18pt leaves comfortable
        // margin for labels up to ~21 characters (21 * 18 = 378, well under the ~476
        // usable width) - "Quit to Start Screen" is the longest label using this.
        text.enableWordWrapping = false;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.92f, 0.85f, 0.70f);
        text.fontSize = 18f;
        text.text = label;

        return buttonGO;
    }

    // Top-center "FPS: NN" overlay - the one strip of the HUD left free by the health
    // bar/objectives panel (top-left), pause button (top-right), and touch controls
    // (bottom corners). Diagnostic only, so no background panel like the other HUD
    // pieces - just a drop shadow for legibility over the skybox.
    public static void BuildFpsCounter(Transform canvasTransform, TMP_FontAsset font)
    {
        GameObject fpsGO = new GameObject("FpsCounter");
        fpsGO.transform.SetParent(canvasTransform, false);
        RectTransform fpsRT = fpsGO.AddComponent<RectTransform>();
        fpsRT.anchorMin = new Vector2(0.5f, 1f);
        fpsRT.anchorMax = new Vector2(0.5f, 1f);
        fpsRT.pivot = new Vector2(0.5f, 1f);
        fpsRT.anchoredPosition = new Vector2(0f, -14f);
        fpsRT.sizeDelta = new Vector2(160f, 30f);

        TextMeshProUGUI text = fpsGO.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = 16f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        text.text = "FPS: --";

        Shadow shadow = fpsGO.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);

        fpsGO.AddComponent<HeroKnightSandbox.UI.FpsCounter>();
    }
}
}
