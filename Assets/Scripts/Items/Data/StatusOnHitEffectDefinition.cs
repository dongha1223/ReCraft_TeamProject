using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 공격 명중 시 상태이상을 부여하는 효과 정의.
    /// 아이템과 각인 모두 이 SO를 effects 리스트에 넣어 사용한다.
    ///
    /// 사용 예시:
    ///   target = BasicAttack, statusEffect.effectType = Stun, chance = 0.2
    ///   → 기본 공격 명중 시 20% 확률로 기절
    /// </summary>
    [CreateAssetMenu(menuName = "2D Roguelike/Effects/Status On Hit", fileName = "NewStatusOnHitEffect")]
    public class StatusOnHitEffectDefinition : EffectDefinition
    {
        [Tooltip("명중 시 부여할 상태이상 데이터 (타입/지속시간/틱/확률)")]
        public StatusEffectSpec statusEffect;

        [Tooltip("이 효과가 적용되는 공격 종류")]
        public OnHitTarget target;
    }
}
