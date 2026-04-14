
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class PeccatumExitPlacer
{
    // 각 스테이지 출구 위치 (맵 레이아웃에서 개방된 하단 통로)
    static readonly Vector3[] exitPositions = {
        new Vector3(9.5f, 0.5f, 0f),  // Stage1: 하단 중앙
        new Vector3(9.5f, 0.5f, 0f),  // Stage2: 하단 중앙
        new Vector3(9.5f, 0.5f, 0f),  // Stage3: 하단 중앙
        new Vector3(9.5f, 0.5f, 0f),  // Stage4: 하단 중앙
    };

    [MenuItem("PECCATUM/9a. Place Exit + Manager in Stage1")]
    static void Stage1() => PlaceInScene(2, "Stage1_Superbia", 1);
    [MenuItem("PECCATUM/9b. Place Exit + Manager in Stage2")]
    static void Stage2() => PlaceInScene(3, "Stage2_Avaritia", 2);
    [MenuItem("PECCATUM/9c. Place Exit + Manager in Stage3")]
    static void Stage3() => PlaceInScene(4, "Stage3_Luxuria", 3);
    [MenuItem("PECCATUM/9d. Place Exit + Manager in Stage4")]
    static void Stage4() => PlaceInScene(5, "Stage4_Ira", 4);

    static void PlaceInScene(int buildIdx, string sceneName, int stageNum)
    {
        EditorSceneManager.OpenScene($"Assets/Scenes/Dungeons/{sceneName}.unity", OpenSceneMode.Single);

        // ── DungeonManager ──
        var existingMgr = GameObject.Find("DungeonManager");
        if (existingMgr == null)
        {
            var mgrGO = new GameObject("DungeonManager");
            var mgr = mgrGO.AddComponent<DungeonManager>();
            mgr.currentStage = stageNum;
            mgr.currentSinName = GetSinName(stageNum);
            Debug.Log($"[PECCATUM] DungeonManager placed in {sceneName}");
        }

        // ── DungeonExit ──
        var existingExit = GameObject.Find("DungeonExit");
        if (existingExit != null) Object.DestroyImmediate(existingExit);

        var exitGO = new GameObject("DungeonExit");
        exitGO.transform.position = exitPositions[stageNum - 1];
        var exit = exitGO.AddComponent<DungeonExit>();
        exit.targetStage = 0; // 다음 스테이지 자동
        exit.gizmoColor = GetExitColor(stageNum);
        exit.gizmoSize = new Vector2(2f, 1.5f);

        // BoxCollider2D 추가
        var col = exitGO.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2f, 1.5f);

        // ── 씬 저장 ──
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[PECCATUM] ✓ Exit placed at {exitPositions[stageNum-1]} in {sceneName}");
    }

    static string GetSinName(int n) {
        switch(n){ case 1: return "Superbia"; case 2: return "Avaritia"; case 3: return "Luxuria"; default: return "Ira"; }
    }
    static Color GetExitColor(int n) {
        switch(n){
            case 1: return new Color(1f, 0.9f, 0.2f, 0.5f);
            case 2: return new Color(0.8f, 0.6f, 0.1f, 0.5f);
            case 3: return new Color(1f, 0.4f, 0.6f, 0.5f);
            default: return new Color(1f, 0.4f, 0.1f, 0.5f);
        }
    }
}
#endif
