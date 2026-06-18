using UnityEngine;
using UnityEditor;
using System.IO;

namespace _2D_Roguelike
{
    /// <summary>
    /// NPC 선택지 버프용 StatModifierEffectDefinition SO를 일괄 생성하는 에디터 유틸리티.
    /// 메뉴: Tools > 2D Roguelike > Generate NPC Effect SOs
    /// 이미 존재하는 SO는 덮어쓰지 않는다.
    /// </summary>
    public static class NpcEffectSOGenerator
    {
        private const string _rootFolder = "Assets/Scripts/Core/Items/Data/NPC_Effects";

        [MenuItem("Tools/2D Roguelike/Generate NPC Effect SOs")]
        public static void Generate()
        {
            EnsureFolder(_rootFolder);

            // ── 오렌 (음유시인) ──────────────────────────────────────
            EnsureFolder($"{_rootFolder}/Oren");
            Create("Oren", "Oren_No_AttackPower",  "oren_no_atk",    StatType.AttackPower, ModifierOperation.Multiply, 1.15f);
            Create("Oren", "Oren_Yes_MoveSpeed",   "oren_yes_spd",   StatType.MoveSpeed,   ModifierOperation.Multiply, 1.10f);

            // ── 베로스 (노예 상인) ───────────────────────────────────
            EnsureFolder($"{_rootFolder}/Veros");
            Create("Veros", "Veros_No_MoveSpeed",  "veros_no_spd",   StatType.MoveSpeed,  ModifierOperation.Multiply, 1.05f);
            Create("Veros", "Veros_No_DropRate",   "veros_no_drop",  StatType.DropRate,   ModifierOperation.Multiply, 1.20f);
            Create("Veros", "Veros_Yes_MoveSpeed", "veros_yes_spd",  StatType.MoveSpeed,  ModifierOperation.Multiply, 1.10f);
            Create("Veros", "Veros_Yes_ExpBonus",  "veros_yes_exp",  StatType.ExpBonus,   ModifierOperation.Multiply, 1.15f);

            // ── 화가 ─────────────────────────────────────────────────
            EnsureFolder($"{_rootFolder}/Painter");
            Create("Painter", "Painter_No_AttackPower", "painter_no_atk", StatType.AttackPower, ModifierOperation.Multiply, 1.20f);

            // ── 가레스 (선대 비질란테) ───────────────────────────────
            EnsureFolder($"{_rootFolder}/Gareth");
            Create("Gareth", "Gareth_No_AttackPower", "gareth_no_atk",  StatType.AttackPower, ModifierOperation.Multiply, 1.25f);
            Create("Gareth", "Gareth_No_AttackSpeed", "gareth_no_spd",  StatType.AttackSpeed, ModifierOperation.Multiply, 1.20f);
            Create("Gareth", "Gareth_Yes_ExpBonus",   "gareth_yes_exp", StatType.ExpBonus,    ModifierOperation.Multiply, 1.20f);

            // ── 루시우스 (신성기사) ──────────────────────────────────
            EnsureFolder($"{_rootFolder}/Lucius");
            Create("Lucius", "Lucius_No_MoveSpeed", "lucius_no_spd", StatType.MoveSpeed, ModifierOperation.Multiply, 1.15f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[NpcEffectSOGenerator] NPC Effect SO 11개 생성 완료! 경로: " + _rootFolder);
            EditorUtility.DisplayDialog("완료", "NPC Effect SO 11개 생성 완료!\n경로: " + _rootFolder, "확인");
        }

        private static void Create(string subfolder, string fileName, string effectId,
            StatType statType, ModifierOperation operation, float value)
        {
            string path = $"{_rootFolder}/{subfolder}/{fileName}.asset";
            if (File.Exists(Path.Combine(Application.dataPath, path.Replace("Assets/", ""))))
            {
                Debug.Log($"[NpcEffectSOGenerator] 이미 존재 — 건너뜀: {path}");
                return;
            }

            var so = ScriptableObject.CreateInstance<StatModifierEffectDefinition>();
            so.effectId  = effectId;
            so.statType  = statType;
            so.operation = operation;
            so.value     = value;

            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"[NpcEffectSOGenerator] 생성: {path}");
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            string name   = Path.GetFileName(folderPath);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
