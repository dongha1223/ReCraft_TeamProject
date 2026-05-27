using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 보스 파츠(몸통/머리/팔 등)에 부착하는 피격 수신기.
    /// IDamageable을 구현하여 데미지를 받으면 배율을 적용한 뒤
    /// 보스 루트의 EnemyStats로 포워딩한다.
    ///
    /// 설정 방법:
    ///   1. 각 파츠 GameObject에 이 컴포넌트와 Collider2D를 추가한다.
    ///   2. _damageMultiplier: 약점 파츠(머리)는 1.5 이상, 일반은 1.0.
    ///   3. _rootStats: 보스 루트의 EnemyStats를 Inspector에서 연결한다.
    /// </summary>
    public class BossPartReceiver : MonoBehaviour, IDamageable
    {
        [Tooltip("이 파츠에 가해지는 데미지 배율 (머리=약점이면 1.5 등)")]
        [SerializeField] private float _damageMultiplier = 1f;

        [Tooltip("보스 루트의 EnemyStats — Inspector에서 직접 연결")]
        [SerializeField] private EnemyStats _rootStats;

        public bool IsDead       => _rootStats != null && _rootStats.IsDead;
        public bool IsInvincible => _rootStats != null && _rootStats.IsDead;

        public void TakeDamage(HitInfo info)
        {
            if (_rootStats == null || _rootStats.IsDead) return;

            var modified = info;
            modified.Damage *= _damageMultiplier;

            // 넉백은 루트에서 처리하므로 파츠에서는 전달하지 않는다
            modified.KnockbackForce = 0f;

            _rootStats.TakeDamage(modified);
        }
    }
}
