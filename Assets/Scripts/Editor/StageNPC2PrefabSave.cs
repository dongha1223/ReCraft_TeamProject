using UnityEngine;
using UnityEditor;

namespace _2D_Roguelike
{
    public static class StageNPC2PrefabSave
    {
        [MenuItem("Tools/2D Roguelike/Save StageNPC2Root as Prefab")]
        public static void SaveAsPrefab()
        {
            // 씬에서 StageNPC2Root 찾기 (비활성 포함)
            GameObject stageGO = null;
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t.name == "화가NPCRoot"
                    && t.gameObject.scene.IsValid()
                    && !EditorUtility.IsPersistent(t.gameObject))
                {
                    stageGO = t.gameObject;
                    break;
                }
            }

            if (stageGO == null)
            {
                Debug.LogError("[StageNPC2PrefabSave] 화가NPCRoot not found in scene.");
                EditorUtility.DisplayDialog("오류", "씬에서 화가NPCRoot를 찾을 수 없습니다.", "확인");
                return;
            }

            string prefabPath = "Assets/Scripts/Stage/StagePrefab/화가NPCRoot.prefab";

            // 저장 전 m_IsActive를 false로 (다른 스테이지처럼 비활성 상태로 저장)
            // 다른 스테이지처럼 비활성 상태로 저장
            bool wasActive = stageGO.activeSelf;
            stageGO.SetActive(false);

            bool success;
            PrefabUtility.SaveAsPrefabAssetAndConnect(
                stageGO, prefabPath, InteractionMode.UserAction, out success);

            if (success)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                Debug.Log($"[StageNPC2PrefabSave] 프리팹 저장 완료: {prefabPath}");
                EditorUtility.DisplayDialog("완료", "화가NPCRoot.prefab 저장 완료!", "확인");
            }
            else
            {
                stageGO.SetActive(wasActive);
                Debug.LogError("[StageNPC2PrefabSave] 프리팹 저장 실패!");
                EditorUtility.DisplayDialog("오류", "프리팹 저장에 실패했습니다.", "확인");
            }
        }
    }
}
