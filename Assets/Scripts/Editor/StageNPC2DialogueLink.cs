using UnityEngine;
using UnityEditor;

namespace _2D_Roguelike
{
    public static class StageNPC2DialogueLink
    {
        [MenuItem("Tools/2D Roguelike/Link StageNPC2 Dialogues")]
        public static void Link()
        {
            // Painter_Dialogue.asset 로드
            var painterDialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(
                "Assets/Scripts/Core/Items/NpcSO/Painter_Dialogue.asset");
            if (painterDialogue == null) { Debug.LogError("Painter_Dialogue.asset not found"); return; }

            // NPC_01_Dialogue.asset 로드
            var healingDialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(
                "Assets/Scripts/Core/Items/NpcSO/NPC_01_Dialogue.asset");
            if (healingDialogue == null) { Debug.LogError("NPC_01_Dialogue.asset not found"); return; }

            // StageNPC2Root 찾기
            var all = Resources.FindObjectsOfTypeAll<Transform>();
            Transform stageNPC2 = null;
            foreach (var t in all)
                if (t.name == "StageNPC2Root" && t.hideFlags == HideFlags.None)
                { stageNPC2 = t; break; }

            if (stageNPC2 == null) { Debug.LogError("StageNPC2Root not found"); return; }

            int linked = 0;

            foreach (Transform child in stageNPC2)
            {
                var npc = child.GetComponent<NPCController>();
                if (npc == null) continue;

                var so = new SerializedObject(npc);

                if (child.name == "Painter")
                {
                    so.FindProperty("_dialogueData").objectReferenceValue = painterDialogue;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(npc);
                    linked++;
                    Debug.Log("[StageNPC2DialogueLink] Painter → Painter_Dialogue 연결");
                }
                else if (child.name == "NPC_01")
                {
                    so.FindProperty("_dialogueData").objectReferenceValue = healingDialogue;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(npc);
                    linked++;
                    Debug.Log("[StageNPC2DialogueLink] NPC_01 → NPC_01_Dialogue 연결");
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log($"[StageNPC2DialogueLink] 완료! {linked}개 연결됨");
            EditorUtility.DisplayDialog("완료", $"DialogueData 연결 완료! ({linked}개)", "확인");
        }
    }
}
