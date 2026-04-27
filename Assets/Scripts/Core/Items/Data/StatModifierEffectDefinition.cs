using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 스탯 수치를 변경하는 효과.
    /// 1단계에서 사용하는 유일한 EffectDefinition 구현체.
    ///
    /// 사용 예시:
    ///   statType = AttackPower, operation = Add,      value = 15   → 공격력 +15
    ///   statType = MaxHp,       operation = Multiply, value = 1.2  → 최대 HP ×1.2
    /// </summary>
    [CreateAssetMenu(menuName = "2D Roguelike/Effects/Stat Modifier", fileName = "NewStatModifierEffect")]
    public class StatModifierEffectDefinition : EffectDefinition
    {
        public StatType          statType;
        public ModifierOperation operation;
        public float             value;
    }
}
