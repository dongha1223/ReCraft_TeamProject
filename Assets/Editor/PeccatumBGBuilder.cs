
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class PeccatumBGBuilder
{
    [MenuItem("PECCATUM/BG/Add Backgrounds to All Stages")]
    public static void AddAllBackgrounds()
    {
        AddBG(2, "BG_Superbia");
        AddBG(3, "BG_Avaritia");
        AddBG(4, "BG_Luxuria");
        AddBG(5, "BG_Ira");
        Debug.Log("[PECCATUM] All backgrounds added!");
    }

    static void AddBG(int buildIndex, string texName)
    {
        var path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(buildIndex);
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        // 기존 BG 오브젝트가 있으면 삭제
        var existing = GameObject.Find("Background");
        if (existing != null) Object.DestroyImmediate(existing);

        // 스프라이트 로드
        string spritePath = $"Assets/Textures/Dungeons/{texName}.png";
        var ti = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.filterMode = FilterMode.Bilinear;
            ti.mipmapEnabled = false;
            ti.maxTextureSize = 64;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

        // Background quad
        var bgGO = new GameObject("Background");
        bgGO.transform.position = new Vector3(9.5f, 5.5f, 1f); // 타일맵보다 뒤에
        bgGO.transform.localScale = new Vector3(22f, 13f, 1f);

        var sr = bgGO.AddComponent<SpriteRenderer>();
        if (sprite != null) sr.sprite = sprite;
        sr.sortingOrder = -10;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(1f, 1f);
        sr.color = new Color(1f, 1f, 1f, 0.85f);

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[PECCATUM] BG added to {scene.name}");
    }
}
#endif
