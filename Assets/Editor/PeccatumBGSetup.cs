
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class PeccatumBGSetup
{
    [MenuItem("PECCATUM/6a. Reimport BG Textures as Sprite")]
    public static void ReimportBG()
    {
        string[] bgPaths = {
            "Assets/Textures/Dungeons/Stage1_BG.png",
            "Assets/Textures/Dungeons/Stage2_BG.png",
            "Assets/Textures/Dungeons/Stage3_BG.png",
            "Assets/Textures/Dungeons/Stage4_BG.png",
        };
        foreach (var p in bgPaths)
        {
            var ti = AssetImporter.GetAtPath(p) as TextureImporter;
            if (ti == null) { Debug.LogWarning($"[PECCATUM] No importer for {p}"); continue; }
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.filterMode = FilterMode.Bilinear;
            ti.mipmapEnabled = false;
            ti.spritePixelsPerUnit = 16;
            ti.maxTextureSize = 128;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
            Debug.Log($"[PECCATUM] Reimported as Sprite: {p}");
        }
        Debug.Log("[PECCATUM] All BG textures reimported as Sprites.");
    }

    [MenuItem("PECCATUM/6b. Apply BG to Current Scene")]
    public static void ApplyBGToCurrentScene()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        int n = 1;
        if      (scene.name.Contains("Superbia")) n = 1;
        else if (scene.name.Contains("Avaritia")) n = 2;
        else if (scene.name.Contains("Luxuria"))  n = 3;
        else if (scene.name.Contains("Ira"))      n = 4;
        else { Debug.LogWarning("[PECCATUM] Unknown scene name: " + scene.name); return; }

        string bgPath = $"Assets/Textures/Dungeons/Stage{n}_BG.png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgPath);
        if (sprite == null) { Debug.LogWarning($"[PECCATUM] Sprite not found at {bgPath}. Run 6a first!"); return; }

        var bgGO = GameObject.Find("Background");
        if (bgGO == null) { Debug.LogWarning("[PECCATUM] No Background GO found."); return; }

        var sr = bgGO.GetComponent<SpriteRenderer>();
        if (sr == null) { Debug.LogWarning("[PECCATUM] No SpriteRenderer on Background."); return; }

        sr.sprite = sprite;
        sr.sortingOrder = -10;
        sr.transform.position = new Vector3(9.5f, 5.5f, 1f);
        sr.transform.localScale = new Vector3(25f, 16f, 1f);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log($"[PECCATUM] BG sprite applied and saved for Stage{n}.");
    }
}
#endif
