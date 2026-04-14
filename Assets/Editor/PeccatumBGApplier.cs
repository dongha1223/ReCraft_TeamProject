
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class PeccatumBGApplier
{
    [MenuItem("PECCATUM/6. Apply BG Sprites to All Scenes")]
    public static void ApplyAllBG()
    {
        ApplyBG(2, "Stage1_Superbia", "Assets/Textures/Dungeons/Stage1_BG.png", new Vector3(9.5f, 5.5f, 1f), new Vector3(22f, 14f, 1f));
        ApplyBG(3, "Stage2_Avaritia", "Assets/Textures/Dungeons/Stage2_BG.png", new Vector3(9.5f, 5.5f, 1f), new Vector3(22f, 14f, 1f));
        ApplyBG(4, "Stage3_Luxuria",  "Assets/Textures/Dungeons/Stage3_BG.png", new Vector3(9.5f, 5.5f, 1f), new Vector3(22f, 14f, 1f));
        ApplyBG(5, "Stage4_Ira",      "Assets/Textures/Dungeons/Stage4_BG.png", new Vector3(9.5f, 5.5f, 1f), new Vector3(22f, 14f, 1f));
        Debug.Log("[PECCATUM] BG sprites applied to all 4 scenes.");
        EditorUtility.DisplayDialog("완료", "모든 씬에 배경 스프라이트 적용 완료!", "OK");
    }

    static void ApplyBG(int buildIndex, string sceneName, string spritePath, Vector3 pos, Vector3 scale)
    {
        var scene = EditorSceneManager.OpenScene($"Assets/Scenes/Dungeons/{sceneName}.unity", OpenSceneMode.Single);
        
        // BG 텍스처 임포트 설정
        var ti = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.filterMode = FilterMode.Bilinear;
            ti.mipmapEnabled = false;
            ti.spritePixelsPerUnit = 16;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null) { Debug.LogWarning($"[PECCATUM] Sprite not found: {spritePath}"); return; }

        // Background GO 찾기 또는 생성
        var bgGO = GameObject.Find("Background");
        if (bgGO == null) bgGO = new GameObject("Background");

        var sr = bgGO.GetComponent<SpriteRenderer>() ?? bgGO.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -10;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(scale.x, scale.y);
        bgGO.transform.position = pos;

        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[PECCATUM] BG applied to {sceneName}");
    }
}
#endif
