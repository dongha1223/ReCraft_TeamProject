using UnityEngine;
using UnityEditor;

namespace _2D_Roguelike
{
    public static class StageNPC2Setup
    {
        [MenuItem("Tools/2D Roguelike/Setup StageNPC2 (HealingNPC + Painter)")]
        public static void Setup()
        {
            // StageMidNPCRoot 찾기 (비활성 포함)
            var stagesRoot = GameObject.Find("##--STAGES--##");
            if (stagesRoot == null)
            {
                // 비활성 오브젝트는 Find로 못 찾으니 전체 탐색
                var all = Resources.FindObjectsOfTypeAll<Transform>();
                foreach (var t in all)
                {
                    if (t.name == "##--STAGES--##" && t.hideFlags == HideFlags.None)
                    {
                        stagesRoot = t.gameObject;
                        break;
                    }
                }
            }

            if (stagesRoot == null) { Debug.LogError("##--STAGES--## not found"); return; }

            // StageMidNPCRoot 찾기
            Transform midNPC = null;
            foreach (Transform child in stagesRoot.transform)
                if (child.name == "StageMidNPCRoot") { midNPC = child; break; }

            if (midNPC == null) { Debug.LogError("StageMidNPCRoot not found"); return; }

            // 이미 StageNPC2Root가 있으면 삭제
            foreach (Transform child in stagesRoot.transform)
            {
                if (child.name == "StageNPC2Root")
                {
                    GameObject.DestroyImmediate(child.gameObject);
                    break;
                }
            }

            // StageMidNPCRoot 복제
            var newStage = GameObject.Instantiate(midNPC.gameObject, stagesRoot.transform);
            newStage.name = "StageNPC2Root";
            newStage.SetActive(false);

            // HolyKnight 자식 제거
            Transform holyKnight = null;
            foreach (Transform child in newStage.transform)
                if (child.name == "HolyKnight") { holyKnight = child; break; }
            if (holyKnight != null) GameObject.DestroyImmediate(holyKnight.gameObject);

            // NPC_01 위치 파악 (힐링NPC는 그대로 유지)
            Transform npc01 = null;
            foreach (Transform child in newStage.transform)
                if (child.name == "NPC_01") { npc01 = child; break; }

            // Painter 프리팹 로드
            var painterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/NPC_Prefab/Painter.prefab");
            if (painterPrefab == null) { Debug.LogError("Painter.prefab not found"); return; }

            // Painter 배치 — NPC_01 옆 오른쪽에 배치
            Vector3 painterPos = npc01 != null
                ? npc01.position + new Vector3(3f, 0f, 0f)
                : new Vector3(3f, 0f, 0f);

            var painter = (GameObject)PrefabUtility.InstantiatePrefab(painterPrefab, newStage.transform);
            painter.transform.position = painterPos;
            painter.name = "Painter";

            // StageNPC2Root를 ##--STAGES--## 의 마지막에서 StageBossRoot 앞으로 이동
            // (순서는 그냥 마지막에 두기)
            newStage.transform.SetAsLastSibling();

            EditorUtility.SetDirty(newStage);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log($"[StageNPC2Setup] StageNPC2Root 생성 완료! NPC_01 위치: {npc01?.position}, Painter 위치: {painterPos}");
            EditorUtility.DisplayDialog("완료", "StageNPC2Root 생성 완료!\nNPC_01 (힐링NPC) + Painter (화가NPC)", "확인");
        }
    }
}
