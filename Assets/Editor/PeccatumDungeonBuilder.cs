
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;
using System.IO;

public static class PeccatumSceneUtils
{
    public static void ReimportTextures()
    {
        string[] paths = {
            "Assets/Textures/Dungeons/Stage1_Floor.png","Assets/Textures/Dungeons/Stage1_Wall.png",
            "Assets/Textures/Dungeons/Stage2_Floor.png","Assets/Textures/Dungeons/Stage2_Wall.png",
            "Assets/Textures/Dungeons/Stage3_Floor.png","Assets/Textures/Dungeons/Stage3_Wall.png",
            "Assets/Textures/Dungeons/Stage4_Floor.png","Assets/Textures/Dungeons/Stage4_Wall.png",
        };
        foreach (var p in paths)
        {
            var ti = AssetImporter.GetAtPath(p) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.filterMode = FilterMode.Point;
            ti.mipmapEnabled = false;
            ti.spritePixelsPerUnit = 16;
            ti.maxTextureSize = 64;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }
        Debug.Log("[PECCATUM] Textures reimported as pixel-art sprites.");
    }

    public static void BuildScene(int n, string sin, Color bg, Color amb, string korean)
    {
        string scenePath = $"Assets/Scenes/Dungeons/Stage{n}_{sin}.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 6f;
        cam.backgroundColor = bg; cam.clearFlags = CameraClearFlags.SolidColor;
        cam.tag = "MainCamera";
        cam.transform.position = new Vector3(9.5f, 5.5f, -10f);
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional; light.color = amb; light.intensity = 1.2f;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = amb * 0.4f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = bg * 0.5f;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 20f;
        RenderSettings.fogEndDistance = 40f;
        var gridGO = new GameObject("Grid");
        gridGO.AddComponent<Grid>().cellSize = Vector3.one;
        var floorGO = new GameObject("Tilemap_Floor");
        floorGO.transform.SetParent(gridGO.transform, false);
        var floorTM = floorGO.AddComponent<Tilemap>();
        var floorR = floorGO.AddComponent<TilemapRenderer>();
        floorR.sortingOrder = 0;
        var wallGO = new GameObject("Tilemap_Wall");
        wallGO.transform.SetParent(gridGO.transform, false);
        var wallTM = wallGO.AddComponent<Tilemap>();
        var wallR = wallGO.AddComponent<TilemapRenderer>();
        wallR.sortingOrder = 1;
        var floorSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Textures/Dungeons/Stage{n}_Floor.png");
        var wallSprite  = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Textures/Dungeons/Stage{n}_Wall.png");
        var floorTile = ScriptableObject.CreateInstance<Tile>();
        var wallTile  = ScriptableObject.CreateInstance<Tile>();
        if (floorSprite) floorTile.sprite = floorSprite;
        if (wallSprite)  wallTile.sprite  = wallSprite;
        floorTile.color = Color.white; wallTile.color = Color.white;
        string ftp = $"Assets/Textures/Dungeons/Stage{n}_FloorTile.asset";
        string wtp = $"Assets/Textures/Dungeons/Stage{n}_WallTile.asset";
        if (AssetDatabase.LoadAssetAtPath<Tile>(ftp) != null) AssetDatabase.DeleteAsset(ftp);
        if (AssetDatabase.LoadAssetAtPath<Tile>(wtp) != null) AssetDatabase.DeleteAsset(wtp);
        AssetDatabase.CreateAsset(floorTile, ftp);
        AssetDatabase.CreateAsset(wallTile, wtp);
        AssetDatabase.SaveAssets();
        int[,] map = DungeonMaps.GetMap(n);
        int rows = map.GetLength(0), cols = map.GetLength(1);
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                var pos = new Vector3Int(c, rows - 1 - r, 0);
                int cell = map[r, c];
                if      (cell == 1) floorTM.SetTile(pos, floorTile);
                else if (cell == 2) wallTM.SetTile(pos, wallTile);
            }
        var infoGO = new GameObject($"[Stage{n}] {sin} - {korean}");
        var info = infoGO.AddComponent<DungeonStageInfo>();
        info.stageNumber = n; info.sinName = sin;
        info.sinNameKorean = korean; info.backgroundColor = bg;
        Directory.CreateDirectory("Assets/Scenes/Dungeons");
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"[PECCATUM] Stage{n} '{korean}({sin})' saved: {scenePath}");
    }

    public static void RegisterAllScenes()
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        string[] paths = {
            "Assets/Scenes/Dungeons/Stage1_Superbia.unity",
            "Assets/Scenes/Dungeons/Stage2_Avaritia.unity",
            "Assets/Scenes/Dungeons/Stage3_Luxuria.unity",
            "Assets/Scenes/Dungeons/Stage4_Ira.unity",
        };
        foreach (var sp in paths)
        {
            bool exists = false;
            foreach (var s in list) if (s.path == sp) { exists = true; break; }
            if (!exists) list.Add(new EditorBuildSettingsScene(sp, true));
        }
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log("[PECCATUM] Build Settings updated with 4 dungeon scenes.");
    }
}

public class PeccatumDungeonBuilder
{
    [MenuItem("PECCATUM/0. Reimport Dungeon Textures")]
    public static void Step0() => PeccatumSceneUtils.ReimportTextures();

    [MenuItem("PECCATUM/1. Build Stage1 Superbia")]
    public static void Step1() => PeccatumSceneUtils.BuildScene(1,"Superbia",new Color(0.10f,0.07f,0.02f),new Color(1f,0.9f,0.4f),"교만");

    [MenuItem("PECCATUM/2. Build Stage2 Avaritia")]
    public static void Step2() => PeccatumSceneUtils.BuildScene(2,"Avaritia",new Color(0.05f,0.04f,0.01f),new Color(0.8f,0.6f,0.1f),"탐욕");

    [MenuItem("PECCATUM/3. Build Stage3 Luxuria")]
    public static void Step3() => PeccatumSceneUtils.BuildScene(3,"Luxuria",new Color(0.12f,0.02f,0.05f),new Color(1f,0.4f,0.6f),"색욕");

    [MenuItem("PECCATUM/4. Build Stage4 Ira")]
    public static void Step4() => PeccatumSceneUtils.BuildScene(4,"Ira",new Color(0.10f,0.03f,0.01f),new Color(1f,0.45f,0.1f),"분노");

    [MenuItem("PECCATUM/5. Register to Build Settings")]
    public static void Step5() => PeccatumSceneUtils.RegisterAllScenes();
}
#endif