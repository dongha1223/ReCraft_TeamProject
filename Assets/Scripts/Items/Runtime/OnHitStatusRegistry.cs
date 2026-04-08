using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 현재 장착된 아이템·각인이 부여하는 on-hit 상태이상 목록을 관리한다.
    /// 플레이어 GameObject에 부착한다.
    ///
    /// StatusOnHitEffectExecutor가 장착/해제 시 Register/Unregister를 호출하고,
    /// PlayerAttack·PlayerSkill이 공격 시 GetSpecsFor()로 적용할 spec 목록을 가져간다.
    /// </summary>
    public class OnHitStatusRegistry : MonoBehaviour
    {
        private readonly List<(StatusOnHitEffectDefinition def, string sourceId)> _entries = new();

        public void Register(StatusOnHitEffectDefinition def, string sourceId)
        {
            _entries.Add((def, sourceId));
        }

        public void Unregister(string sourceId)
        {
            _entries.RemoveAll(e => e.sourceId == sourceId);
        }

        /// <summary>
        /// 해당 공격 종류에 적용되는 StatusEffectSpec 배열 반환.
        /// 등록된 항목이 없으면 null 반환.
        /// </summary>
        public StatusEffectSpec[] GetSpecsFor(OnHitTarget target)
        {
            List<StatusEffectSpec> result = null;

            foreach (var (def, _) in _entries)
            {
                if (def.target != target && def.target != OnHitTarget.All) continue;

                result ??= new List<StatusEffectSpec>();
                result.Add(def.statusEffect);
            }

            return result?.ToArray();
        }
    }
}
