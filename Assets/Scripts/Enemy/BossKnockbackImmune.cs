using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 보스 전용 넉백 면역 컴포넌트.
    /// IKnockbackReceiver를 구현하되 모든 넉백을 무시한다.
    /// KnockbackReceiver 대신 이 컴포넌트를 붙이면 기존 피격 시스템과 호환을 유지하면서 넉백만 무효화된다.
    /// TODO: 그로기 게이지 시스템(강인도 소모 → 일정량 누적 시 슈퍼아머 해제) 확장 가능.
    /// </summary>
    public class BossKnockbackImmune : MonoBehaviour, IKnockbackReceiver
    {
        public bool    IsKnockedBack    => false;
        public Vector2 ExternalVelocity => Vector2.zero;

        public void ApplyKnockback(Vector2 sourcePosition, float force) { }
        public void ResetKnockback() { }
    }
}
