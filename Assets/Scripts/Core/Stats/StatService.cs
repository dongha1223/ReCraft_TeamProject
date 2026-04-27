using System.Collections.Generic;

namespace _2D_Roguelike
{
    /// <summary>
    /// 플레이어 스탯의 유일한 계산 창구.
    /// 기본값 + Add 합산 후 Multiply 곱셈 순서로 최종값을 반환한다.
    /// 아이템/각인 효과는 반드시 이 서비스를 통해 스탯을 수정해야 한다.
    /// </summary>
    public class StatService
    {
        private readonly Dictionary<StatType, float>             _baseValues = new();
        private readonly Dictionary<StatType, List<StatModifier>> _modifiers  = new();

        // ── 기본값 설정 ──────────────────────────────────────────────

        public void SetBaseValue(StatType stat, float value)
        {
            _baseValues[stat] = value;
        }

        public float GetBaseValue(StatType stat)
        {
            return _baseValues.TryGetValue(stat, out var v) ? v : 0f;
        }

        // ── 모디파이어 추가/제거 ──────────────────────────────────────

        public void AddModifier(string sourceId, StatType stat, ModifierOperation operation, float value)
        {
            if (!_modifiers.ContainsKey(stat))
                _modifiers[stat] = new List<StatModifier>();

            _modifiers[stat].Add(new StatModifier(sourceId, operation, value));
        }

        /// <summary>특정 소스가 부여한 특정 스탯 모디파이어를 전부 제거</summary>
        public void RemoveModifiersFromSource(string sourceId, StatType stat)
        {
            if (_modifiers.TryGetValue(stat, out var list))
                list.RemoveAll(m => m.SourceId == sourceId);
        }

        /// <summary>특정 소스가 부여한 모든 스탯 모디파이어를 전부 제거 (아이템 해제용)</summary>
        public void RemoveAllModifiersFromSource(string sourceId)
        {
            foreach (var list in _modifiers.Values)
                list.RemoveAll(m => m.SourceId == sourceId);
        }

        // ── 최종값 계산 ──────────────────────────────────────────────

        /// <summary>
        /// 최종 스탯값 반환.
        /// 계산식: (baseValue + ΣAdd) × ΠMultiply
        /// </summary>
        public float GetFinalValue(StatType stat)
        {
            float baseVal = GetBaseValue(stat);

            if (!_modifiers.TryGetValue(stat, out var mods) || mods.Count == 0)
                return baseVal;

            float addSum         = 0f;
            float multiplyProduct = 1f;

            foreach (var mod in mods)
            {
                if (mod.Operation == ModifierOperation.Add)
                    addSum += mod.Value;
                else
                    multiplyProduct *= mod.Value;
            }

            return (baseVal + addSum) * multiplyProduct;
        }
    }
}
