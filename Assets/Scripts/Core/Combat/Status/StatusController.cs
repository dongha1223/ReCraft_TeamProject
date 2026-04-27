using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 오브젝트에 현재 걸려 있는 상태이상을 관리하는 컴포넌트.
    /// EnemyStats / PlayerStats 와 같은 GameObject에 부착한다.
    /// </summary>
    public class StatusController : MonoBehaviour
    {
        private readonly Dictionary<StatusEffectType, StatusEffectBase> _activeEffects = new();
        private StatusResistance _resistance;

        private void Awake()
        {
            _resistance = GetComponent<StatusResistance>();
        }

        private void Update()
        {
            if (_activeEffects.Count == 0) return;

            float dt = Time.deltaTime;
            List<StatusEffectType> toRemove = null;

            foreach (var pair in _activeEffects)
            {
                pair.Value.OnUpdate(dt);

                if (pair.Value.IsFinished)
                {
                    toRemove ??= new List<StatusEffectType>();
                    toRemove.Add(pair.Key);
                }
            }

            if (toRemove == null) return;

            foreach (var type in toRemove)
            {
                _activeEffects[type].OnRemove();
                _activeEffects.Remove(type);
            }
        }

        /// <summary>
        /// 상태이상을 적용한다.
        /// 면역이면 무시, 확률 실패 시 무시, 이미 걸려 있으면 갱신한다.
        /// </summary>
        public void ApplyStatus(StatusEffectSpec spec)
        {
            if (spec == null) return;
            if (_resistance != null && _resistance.IsImmune(spec.effectType)) return;
            if (Random.value > spec.chance) return;

            StatusEffectBase effect = CreateEffect(spec);
            if (effect == null) return;

            if (_activeEffects.TryGetValue(spec.effectType, out var existing))
            {
                existing.OnRefresh(spec);
            }
            else
            {
                _activeEffects[spec.effectType] = effect;
                effect.OnApply();
            }
        }

        /// <summary>
        /// 피격 이벤트를 모든 활성 상태이상에 전파한다.
        /// 빙결처럼 피격 시 해제되는 효과가 즉시 처리된다.
        /// </summary>
        public void OnHitReceived(HitInfo hitInfo)
        {
            if (_activeEffects.Count == 0) return;

            List<StatusEffectType> toRemove = null;

            foreach (var pair in _activeEffects)
            {
                pair.Value.OnHitReceived(hitInfo);

                if (pair.Value.IsFinished)
                {
                    toRemove ??= new List<StatusEffectType>();
                    toRemove.Add(pair.Key);
                }
            }

            if (toRemove == null) return;

            foreach (var type in toRemove)
            {
                _activeEffects[type].OnRemove();
                _activeEffects.Remove(type);
            }
        }

        public bool HasEffect(StatusEffectType type) => _activeEffects.ContainsKey(type);

        /// <summary>풀 반환 등 강제 초기화 시 모든 상태이상 즉시 해제</summary>
        public void ClearAll()
        {
            foreach (var effect in _activeEffects.Values)
                effect.OnRemove();
            _activeEffects.Clear();
        }

        private StatusEffectBase CreateEffect(StatusEffectSpec spec)
        {
            return spec.effectType switch
            {
                StatusEffectType.Stun   => new StunEffect(this, spec),
                StatusEffectType.Freeze => new FreezeEffect(this, spec),
                StatusEffectType.Burn   => new BurnEffect(this, spec),
                StatusEffectType.Bleed  => new BleedEffect(this, spec),
                StatusEffectType.Poison => new PoisonEffect(this, spec),
                _                       => null
            };
        }
    }
}
