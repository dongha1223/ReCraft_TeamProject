using UnityEngine;
using UnityEditor;
using System.IO;

namespace _2D_Roguelike
{
    public static class StageNPC2Register
    {
        [MenuItem("Tools/2D Roguelike/Register 화가NPCRoot Stage")]
        public static void RegisterStage()
        {
            // 1. Stage_화가NPC.asset SO 생성 (Stage_MidNPC와 동일 설정)
            string soPath = "Assets/Scripts/Stage/StageSO/Stage_화가NPC.asset";
            StageDataSO so = AssetDatabase.LoadAssetAtPath<StageDataSO>(soPath);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<StageDataSO>();
                so.sceneName  = "Stage_화가NPC";
                so.stageType  = StageType.Start; // MidNPC와 동일 (stageType=0)
                // BGM: Stage_MidNPC와 동일한 BGM 클립 사용
                var midNpcSO = AssetDatabase.LoadAssetAtPath<StageDataSO>(
                    "Assets/Scripts/Stage/StageSO/Stage_MidNPC.asset");
                if (midNpcSO != null) so.bgm = midNpcSO.bgm;

                AssetDatabase.CreateAsset(so, soPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[StageNPC2Register] SO 생성: {soPath}");
            }
            else
            {
                Debug.Log($"[StageNPC2Register] SO 이미 존재: {soPath}");
            }

            // 2. 씬에서 StageManager 찾기
            GameObject smGO = null;
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t.name == "StageManager"
                    && t.gameObject.scene.IsValid()
                    && !EditorUtility.IsPersistent(t.gameObject))
                {
                    smGO = t.gameObject;
                    break;
                }
            }
            if (smGO == null)
            {
                Debug.LogError("[StageNPC2Register] StageManager not found in scene.");
                return;
            }

            var sm = smGO.GetComponent<StageManager>();
            if (sm == null)
            {
                Debug.LogError("[StageNPC2Register] StageManager component not found.");
                return;
            }

            // 3. 화가NPCRoot Transform 찾기
            Transform stageRoot = null;
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t.name == "화가NPCRoot"
                    && t.gameObject.scene.IsValid()
                    && !EditorUtility.IsPersistent(t.gameObject))
                {
                    stageRoot = t;
                    break;
                }
            }
            if (stageRoot == null)
            {
                Debug.LogError("[StageNPC2Register] 화가NPCRoot not found in scene.");
                return;
            }

            // SpawnPoint_MidNPC 찾기
            Transform spawnPoint = stageRoot.Find("SpawnPoint_MidNPC");
            if (spawnPoint == null)
            {
                // 자식 전체 검색
                foreach (Transform child in stageRoot)
                {
                    if (child.name.Contains("SpawnPoint"))
                    {
                        spawnPoint = child;
                        break;
                    }
                }
            }

            // 4. SerializedObject로 _stages 배열에 추가
            var soSM = new SerializedObject(sm);
            soSM.Update();

            var stagesProp = soSM.FindProperty("_stages");

            // 이미 등록됐는지 확인
            for (int i = 0; i < stagesProp.arraySize; i++)
            {
                var entry = stagesProp.GetArrayElementAtIndex(i);
                var rootProp = entry.FindPropertyRelative("root");
                if (rootProp.objectReferenceValue == stageRoot.gameObject)
                {
                    Debug.Log("[StageNPC2Register] 이미 등록되어 있습니다.");
                    EditorUtility.DisplayDialog("알림", "화가NPCRoot는 이미 StageManager에 등록되어 있습니다.", "확인");
                    return;
                }
            }

            // 배열 끝에 추가
            stagesProp.arraySize++;
            var newEntry = stagesProp.GetArrayElementAtIndex(stagesProp.arraySize - 1);
            newEntry.FindPropertyRelative("data").objectReferenceValue       = so;
            newEntry.FindPropertyRelative("root").objectReferenceValue       = stageRoot.gameObject;
            newEntry.FindPropertyRelative("spawnPoint").objectReferenceValue = spawnPoint != null ? spawnPoint.gameObject : null;

            soSM.ApplyModifiedProperties();
            EditorUtility.SetDirty(sm);

            // 5. 씬 저장
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            int idx = stagesProp.arraySize - 1;
            Debug.Log($"[StageNPC2Register] 등록 완료! index={idx}, root=화가NPCRoot, spawnPoint={spawnPoint?.name}");
            EditorUtility.DisplayDialog("완료",
                $"화가NPCRoot가 StageManager _stages[{idx}]에 등록됐습니다!", "확인");
        }
    }
}
