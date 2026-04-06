using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    public class EnemyController : EnemyBrainBase
    {
        [Header("근접 공격")]
        [SerializeField] private float _attackDamage   = 10f;
        [SerializeField] private float _knockbackForce = 4f;

        private static readonly int AnimAttack = Animator.StringToHash("Attack");

        protected override IEnumerator AttackCoroutine()
        {
            _canAttack   = false;
            _isAttacking = true;
            _animator?.SetTrigger(AnimAttack);

            // 공격 판정 (모션 중간)
            yield return new WaitForSeconds(0.25f);

            if (_player != null)
            {
                float dist = Vector2.Distance(transform.position, _player.position);
                if (dist <= _attackRange)
                {
                    _player.GetComponent<IDamageable>()?.TakeDamage(new HitInfo
                    {
                        Damage         = _attackDamage,
                        SourcePosition = transform.position,
                        KnockbackForce = _knockbackForce
                    });
                }
            }

            yield return new WaitForSeconds(_attackCooldown - 0.25f);

            _isAttacking = false;
            _canAttack   = true;
        }
    }
}
