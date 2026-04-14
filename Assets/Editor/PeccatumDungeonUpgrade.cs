
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// 각 던전 씬의 타일맵을 Cainos 픽셀아트 타일셋으로 업그레이드하고
/// 횃불·장식 오브젝트를 배치합니다.
/// </summary>
public class PeccatumDungeonUpgrade
{
    const string TILE_BASE = "Assets/Cainos/Pixel Art Platformer - Village Props/Tileset Palette/TP Ground/TX Tileset Ground_";

    // ── 메뉴 진입점 ──────────────────────────────────────
    [MenuItem("PECCATUM/8a. Upgrade Stage1 Tiles + Props (교만)")]
    static void Upgrade1() { LoadAndUpgrade(2, "Stage1_Superbia"); }

    [MenuItem("PECCATUM/8b. Upgrade Stage2 Tiles + Props (탐욕)")]
    static void Upgrade2() { LoadAndUpgrade(3, "Stage2_Avaritia"); }

    [MenuItem("PECCATUM/8c. Upgrade Stage3 Tiles + Props (색욕)")]
    static void Upgrade3() { LoadAndUpgrade(4, "Stage3_Luxuria"); }

    [MenuItem("PECCATUM/8d. Upgrade Stage4 Tiles + Props (분노)")]
    static void Upgrade4() { LoadAndUpgrade(5, "Stage4_Ira"); }

    // ── 공통 업그레이드 흐름 ──────────────────────────────
    static void LoadAndUpgrade(int buildIdx, string sceneName)
    {
        EditorSceneManager.OpenScene($"Assets/Scenes/Dungeons/{sceneName}.unity", OpenSceneMode.Single);
        UpgradeCurrentScene(buildIdx, sceneName);
    }

    static void UpgradeCurrentScene(int buildIdx, string sceneName)
    {
        // ── 1. Tilemap 찾기 ──
        var floorTM = GetTilemap("Tilemap_Floor");
        var wallTM  = GetTilemap("Tilemap_Wall");
        if (floorTM == null || wallTM == null)
        {
            Debug.LogError($"[PECCATUM] Tilemap not found in {sceneName}");
            return;
        }

        // ── 2. Cainos 타일 로드 (스테이지별 다른 컬러 틴트) ──
        var floorTile = LoadTile(GetFloorTileIndex(buildIdx));
        var wallTile  = LoadTile(GetWallTileIndex(buildIdx));
        Color floorTint = GetFloorTint(buildIdx);
        Color wallTint  = GetWallTint(buildIdx);

        if (floorTile != null) floorTile.color = floorTint;
        if (wallTile  != null) wallTile.color  = wallTint;

        // ── 3. 기존 타일 위에 Cainos 타일로 덮어쓰기 ──
        int[,] map = DungeonMaps.GetMap(buildIdx == 2 ? 1 : buildIdx == 3 ? 2 : buildIdx == 4 ? 3 : 4);
        int rows = map.GetLength(0), cols = map.GetLength(1);

        floorTM.ClearAllTiles();
        wallTM.ClearAllTiles();

        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                var pos  = new Vector3Int(c, rows - 1 - r, 0);
                int cell = map[r, c];
                if      (cell == 1 && floorTile != null) floorTM.SetTile(pos, floorTile);
                else if (cell == 2 && wallTile  != null) wallTM.SetTile(pos, wallTile);
            }

        // ── 4. Deco 오브젝트 추가 ──
        PlaceDecorations(buildIdx, sceneName);

        // ── 5. 저장 ──
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[PECCATUM] ✓ {sceneName} upgraded with Cainos tiles + props.");
    }

    // ── 데코 오브젝트 배치 ──────────────────────────────────
    static void PlaceDecorations(int buildIdx, string sceneName)
    {
        // 기존 Deco 정리
        DestroyIfExists("Deco_Props");

        var propsRoot = new GameObject("Deco_Props");

        // 스테이지별 횃불/오브젝트 배치
        switch (buildIdx)
        {
            case 2: // 교만 - 황금 기둥 위 횃불
                PlaceTorch(propsRoot, new Vector3(2f,  8f, 0f), new Color(1f, 0.85f, 0.2f));
                PlaceTorch(propsRoot, new Vector3(17f, 8f, 0f), new Color(1f, 0.85f, 0.2f));
                PlaceTorch(propsRoot, new Vector3(9.5f,9f, 0f), new Color(1f, 0.95f, 0.4f));
                PlaceChest(propsRoot, new Vector3(9.5f, 4f, 0f), new Color(1f, 0.85f, 0.2f));
                break;
            case 3: // 탐욕 - 구석마다 횃불
                PlaceTorch(propsRoot, new Vector3(1f,  10f, 0f), new Color(0.9f, 0.7f, 0.1f));
                PlaceTorch(propsRoot, new Vector3(18f, 10f, 0f), new Color(0.9f, 0.7f, 0.1f));
                PlaceTorch(propsRoot, new Vector3(1f,   1f, 0f), new Color(0.7f, 0.5f, 0.05f));
                PlaceTorch(propsRoot, new Vector3(18f,  1f, 0f), new Color(0.7f, 0.5f, 0.05f));
                PlaceChest(propsRoot, new Vector3(5f,  8f, 0f), new Color(0.8f, 0.6f, 0.1f));
                PlaceChest(propsRoot, new Vector3(14f, 8f, 0f), new Color(0.8f, 0.6f, 0.1f));
                break;
            case 4: // 색욕 - 장미빛 조명
                PlaceTorch(propsRoot, new Vector3(3f,  6f, 0f), new Color(1f, 0.4f, 0.6f));
                PlaceTorch(propsRoot, new Vector3(16f, 6f, 0f), new Color(1f, 0.4f, 0.6f));
                PlaceTorch(propsRoot, new Vector3(9.5f,9f, 0f), new Color(0.9f, 0.3f, 0.5f));
                break;
            case 5: // 분노 - 화염 횃불
                PlaceTorch(propsRoot, new Vector3(4f,  5f, 0f), new Color(1f, 0.4f, 0.1f));
                PlaceTorch(propsRoot, new Vector3(15f, 5f, 0f), new Color(1f, 0.4f, 0.1f));
                PlaceTorch(propsRoot, new Vector3(4f,  7f, 0f), new Color(1f, 0.5f, 0.05f));
                PlaceTorch(propsRoot, new Vector3(15f, 7f, 0f), new Color(1f, 0.5f, 0.05f));
                break;
        }
    }

    static void PlaceTorch(GameObject parent, Vector3 pos, Color lightColor)
    {
        var go = new GameObject("Torch");
        go.transform.SetParent(parent.transform, false);
        go.transform.position = pos;

        // 스프라이트
        var sr = go.AddComponent<SpriteRenderer>();
        var torchSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Cainos/Pixel Art Platformer - Village Props/Texture/TX FX Torch Flame.png");
        if (torchSprite != null) sr.sprite = torchSprite;
        sr.sortingOrder = 5;

        // 포인트 라이트
        var lightGO = new GameObject("TorchLight");
        lightGO.transform.SetParent(go.transform, false);
        lightGO.transform.localPosition = new Vector3(0, 0.3f, 0);
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = lightColor;
        light.intensity = 1.8f;
        light.range = 5f;

        // 파티클 (작은 불꽃)
        var fxGO = new GameObject("TorchFX");
        fxGO.transform.SetParent(go.transform, false);
        fxGO.transform.localPosition = new Vector3(0, 0.2f, 0);
        var ps = fxGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(lightColor * 0.8f, Color.yellow);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        main.maxParticles = 30;
        main.gravityModifier = -0.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = 20f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;
    }

    static void PlaceChest(GameObject parent, Vector3 pos, Color tint)
    {
        var go = new GameObject("Chest");
        go.transform.SetParent(parent.transform, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Cainos/Pixel Art Platformer - Village Props/Texture/TX Chest Animation.png");
        if (sprite != null) sr.sprite = sprite;
        sr.color = tint;
        sr.sortingOrder = 3;
        go.transform.localScale = Vector3.one * 1.2f;
    }

    // ── 헬퍼 ────────────────────────────────────────────────
    static Tilemap GetTilemap(string name)
    {
        var go = GameObject.Find(name);
        return go != null ? go.GetComponent<Tilemap>() : null;
    }

    static Tile LoadTile(int index)
    {
        var path = $"{TILE_BASE}{index}.asset";
        return AssetDatabase.LoadAssetAtPath<Tile>(path);
    }

    static void DestroyIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    // 스테이지별 타일 인덱스 (Cainos 타일셋에서 어울리는 것)
    static int GetFloorTileIndex(int idx) {
        switch(idx){ case 2: return 56; case 3: return 91; case 4: return 74; default: return 112; }
    }
    static int GetWallTileIndex(int idx) {
        switch(idx){ case 2: return 4;  case 3: return 22; case 4: return 37; default: return 1; }
    }

    // 스테이지별 컬러 틴트
    static Color GetFloorTint(int idx) {
        switch(idx){
            case 2: return new Color(1.0f, 0.88f, 0.4f);   // 교만 - 황금
            case 3: return new Color(0.4f, 0.35f, 0.15f);  // 탐욕 - 어두운 금
            case 4: return new Color(0.8f, 0.4f, 0.55f);   // 색욕 - 붉은 분홍
            default:return new Color(0.6f, 0.18f, 0.05f);  // 분노 - 화염
        }
    }
    static Color GetWallTint(int idx) {
        switch(idx){
            case 2: return new Color(0.9f, 0.75f, 0.2f);
            case 3: return new Color(0.3f, 0.25f, 0.08f);
            case 4: return new Color(0.7f, 0.2f, 0.35f);
            default:return new Color(0.5f, 0.12f, 0.02f);
        }
    }
}
#endif
