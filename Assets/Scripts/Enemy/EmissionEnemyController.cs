using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 방사형 투사체 몬스터.
    /// 공격 범위 내 플레이어를 감지하면 여러 방향으로 투사체를 순차 발사합니다.
    /// </summary>
    public class EmissionEnemyController : EnemyBrainBase
    {
        [Header("방사형 공격")]
        [SerializeField] private float      _attackDamage     = 8f;
        [SerializeField] private float      _knockbackForce   = 2f;
        [SerializeField] private float      _windupDuration   = 0.8f;
        [Tooltip("한 번에 발사할 투사체 수 (ProjectileDirectional의 maxCount로 전달)")]
        [SerializeField] private int        _projectileCount  = 6;
        [Tooltip("투사체 사이 발사 간격 (초)")]
        [SerializeField] private float      _fireInterval     = 0.05f;

        [Header("투사체")]
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private Transform  _spawnPoint;
        [SerializeField] private GameObject _windupIndicator;

        private static readonly int AnimWindup = Animator.StringToHash("Windup");

        protected override void OnEnable()
        {
            base.OnEnable();
            _windupIndicator?.SetActive(false);
            _animator?.ResetTrigger(AnimWindup);
        }

        protected override IEnumerator AttackCoroutine()
        {
            _canAttack   = false;
            _isAttacking = true;

            // 전조 연출
            _animator?.SetTrigger(AnimWindup);
            _windupIndicator?.SetActive(true);

            yield return new WaitForSeconds(_windupDuration);

            _windupIndicator?.SetActive(false);

            // 방사형 투사체 순차 발사
            if (_projectilePrefab != null)
            {
                var hitInfo = new HitInfo
                {
                    Damage         = _attackDamage,
                    KnockbackForce = _knockbackForce
                };

                for (int i = 0; i < _projectileCount; i++)
                {
                    var go = Instantiate(_projectilePrefab, _spawnPoint.position, Quaternion.identity);
                    go.GetComponent<ProjectileBase>()?.Setup(_player, hitInfo, _projectileCount, i);

                    if (_fireInterval > 0f)
                        yield return new WaitForSeconds(_fireInterval);
                }
            }

            yield return new WaitForSeconds(Mathf.Max(0f, _attackCooldown - _windupDuration - _fireInterval * _projectileCount));

            _isAttacking = false;
            _canAttack   = true;
        }
    }
}
