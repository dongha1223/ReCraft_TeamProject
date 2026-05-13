using UnityEngine;

namespace _2D_Roguelike
{
    [System.Serializable]
    public struct StatModifierSpec
    {
        public StatType stat;
        public ModifierOperation operation;
        [Tooltip("레벨당 증분값. Multiply면 0.1 = +10%/레벨, Add면 0.1 = +0.1 flat/레벨")]
        public float valuePerLevel;
    }

    [CreateAssetMenu(menuName = "2D Roguelike/Enhance Node", fileName = "EnhanceNode")]
    public class EnhanceNodeDefinition : ScriptableObject
    {
        [Tooltip("PlayerPrefs 저장 키로 사용. 고유해야 함 (예: courage_0)")]
        public string nodeId;
        public string displayName;
        [TextArea] public string description;
        public int maxLevel;
        [Tooltip("레벨 0→1, 1→2... 순서로 소모 신념량. 길이 = maxLevel")]
        public int[] costs;
        public StatModifierSpec[] statModifiers;

        public string SourceId => $"enhance_{nodeId}";

        public int GetCost(int currentLevel) =>
            (currentLevel >= 0 && currentLevel < costs.Length) ? costs[currentLevel] : 0;

        public string GetNextLevelPreview(int currentLevel)
        {
            if (currentLevel >= maxLevel || statModifiers == null || statModifiers.Length == 0)
                return "최대 레벨";
            float total = statModifiers[0].valuePerLevel * (currentLevel + 1) * 100f;
            return $"+{total:0}%";
        }
    }
}
