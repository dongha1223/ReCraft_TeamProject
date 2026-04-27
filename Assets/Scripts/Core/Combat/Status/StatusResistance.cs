using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 상태이상 면역 설정 컴포넌트.
    /// Inspector에서 타입별로 immune을 체크하면 해당 상태이상이 적용되지 않는다.
    /// 플레이어 / 보스 / 중간보스 프리팹에 부착해 기절·빙결 면역을 설정한다.
    /// </summary>
    public class StatusResistance : MonoBehaviour
    {
        [System.Serializable]
        public class Entry
        {
            public StatusEffectType type;
            public bool             immune;
        }

        [SerializeField] private Entry[] _entries;

        private Dictionary<StatusEffectType, bool> _immuneMap;

        private void Awake()
        {
            _immuneMap = new Dictionary<StatusEffectType, bool>();
            if (_entries == null) return;
            foreach (var e in _entries)
                _immuneMap[e.type] = e.immune;
        }

        public bool IsImmune(StatusEffectType type)
            => _immuneMap != null && _immuneMap.TryGetValue(type, out var immune) && immune;
    }
}
