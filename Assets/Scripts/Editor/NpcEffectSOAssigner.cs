using UnityEngine;
using UnityEditor;

namespace _2D_Roguelike
{
    /// <summary>
    /// 각 NPC의 DialogueData SO에 Effect SO를 자동 할당하는 에디터 유틸리티.
    /// 메뉴: Tools > 2D Roguelike > Assign NPC Effects to DialogueData
    /// </summary>
    public static class NpcEffectSOAssigner
    {
        [MenuItem("Tools/2D Roguelike/Assign NPC Effects to DialogueData")]
        public static void Assign()
        {
            int successCount = 0;

            // ── 오렌 (음유시인) — Bard_Dialogue ─────────────────────
            successCount += AssignEffects(
                "Assets/Scripts/Core/Items/NpcSO/Bard_Dialogue.asset",
                yesEffectPaths: new[] {
                    "Assets/Scripts/Core/Items/Data/NPC_Effects/Oren/Oren_Yes_MoveSpeed.asset"
                },
                noEffectPaths: new[] {
                    "Assets/Scripts/Core/Items/Data/NPC_Effects/Oren/Oren_No_AttackPower.asset"
                }
            );

            // ── 베로스 (노예 상인) — SlaveTrader_Dialogue ───────────
            successCount += AssignEffects(
                "Assets/Scripts/Core/Items/NpcSO/SlaveTrader_Dialogue.asset",
                yesEffectPaths: new[] {
                    "Assets/Scripts/Core/Items/Data/NPC_Effects/Veros/Veros_Yes_MoveSpeed.asset",
                    "Assets/Scripts/Core/Items/Data/NPC_Effects/Veros/Veros_Yes_ExpBonus.asset"
                },
                noEffectPaths: new[] {
                    "Assets/Scripts/Core/Items/Data/NPC_Effects/Veros/Veros_No_MoveSpeed.asset",
                    "Assets/Scripts/Core/Items/Data/NPC_Effects/Veros/Veros_No_DropRate.asset"
                }
            );

            // ── 화가 — NPC_01_Dialogue ───────────────────────────────
            successCount += AssignEffects(
                "Assets/Scripts/Core/Items/NpcSO/NPC_01_Dialogue.asset",
                yesEffectPaths: new string[] { },
                noEffectPaths: new[] {
                    "Assets/Scripts/Core/Items/Data/NPC_Effects/Painter/Painter_No_AttackPower.asset"
                }
            );

            // ── 가레스 (선대 비질란테) — VeteranVigilante_Dialogue ───
            successCount += AssignEffects(
                "Assets/Scripts/Core/Items/NpcSO/VeteranVigilante_Dialogue.asset",
                yesEffectPaths: new[] {
                    "Assets/Scripts/Core/Items/Data/NPC_Effects/Gareth/Gareth_Yes_ExpBonus.asset"
                },
                noEffectPaths: new[] {
                    "Assets/Scripts/Core/Items/Data/NPC_Effects/Gareth/Gareth_No_AttackPower.asset",
                    "Assets/Scripts/Core/Items/Data/NPC_Effects/Gareth/Gareth_No_AttackSpeed.asset"
                }
            );

            // ── 루시우스 (신성기사) — HolyKnight_Dialogue ───────────
            successCount += AssignEffects(
                "Assets/Scripts/Core/Items/NpcSO/HolyKnight_Dialogue.asset",
                yesEffectPaths: new string[] { },
                noEffectPaths: new[] {
                    "Assets/Scripts/Core/Items/Data/NPC_Effects/Lucius/Lucius_No_MoveSpeed.asset"
                }
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string msg = $"Effect 할당 완료! ({successCount}/5 DialogueData SO 처리됨)";
            Debug.Log("[NpcEffectSOAssigner] " + msg);
            EditorUtility.DisplayDialog("완료", msg, "확인");
        }

        private static int AssignEffects(string dialoguePath,
            string[] yesEffectPaths, string[] noEffectPaths)
        {
            var dialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(dialoguePath);
            if (dialogue == null)
            {
                Debug.LogWarning($"[NpcEffectSOAssigner] DialogueData 없음: {dialoguePath}");
                return 0;
            }

            var so = new SerializedObject(dialogue);

            // Yes Effects
            SetEffectArray(so, "_yesEffects", yesEffectPaths);
            // No Effects
            SetEffectArray(so, "_noEffects", noEffectPaths);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(dialogue);
            Debug.Log($"[NpcEffectSOAssigner] 할당 완료: {dialoguePath}");
            return 1;
        }

        private static void SetEffectArray(SerializedObject so, string propertyName, string[] paths)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogWarning($"[NpcEffectSOAssigner] 프로퍼티 없음: {propertyName}");
                return;
            }

            prop.arraySize = paths.Length;
            for (int i = 0; i < paths.Length; i++)
            {
                var effect = AssetDatabase.LoadAssetAtPath<StatModifierEffectDefinition>(paths[i]);
                if (effect == null)
                {
                    Debug.LogWarning($"[NpcEffectSOAssigner] Effect SO 없음: {paths[i]}");
                    continue;
                }
                prop.GetArrayElementAtIndex(i).objectReferenceValue = effect;
            }
        }
    }
}
