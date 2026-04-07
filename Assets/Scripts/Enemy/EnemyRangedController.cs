using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    public class EnemyRangedController : EnemyBrainBase
    {
        [Header("원거리 공격")]
        [SerializeField] private float      _attackDamage   = 8f;
        [SerializeField] private float      _knockbackForce = 3f;
        [SerializeField] private float      _windupDuration = 1f;

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

        // 공격 진입 시 플레이어 방향으로 전환 후 기본 처리
        protected override void HandleAttack()
        {
            if (_player != null)
                Flip(_player.position.x > transform.position.x ? 1f : -1f);

            base.HandleAttack();
        }

        protected override IEnumerator AttackCoroutine()
        {
            _canAttack   = false;
            _isAttacking = true;

            // 전조 연출 시작
            _animator?.SetTrigger(AnimWindup);
            _windupIndicator?.SetActive(true);

            // 전조 대기 — 빙결 시 일시정지, 해제 후 발사로 이어짐
            yield return StartCoroutine(PauseableWait(_windupDuration));

            // 전조 종료 → 투사체 발사
            _windupIndicator?.SetActive(false);

            if (_projectilePrefab != null && _player != null)
            {
                var go = Instantiate(_projectilePrefab, _spawnPoint.position, Quaternion.identity);
                go.GetComponent<ProjectileBase>()?.Setup(_player, new HitInfo
                {
                    Damage         = _attackDamage,
                    KnockbackForce = _knockbackForce
                });
            }

            // 쿨타임 잔여 대기 (windupDuration이 cooldown보다 길어지는 케이스 방지)
            yield return new WaitForSeconds(Mathf.Max(0f, _attackCooldown - _windupDuration));  // 쿨타임

            _isAttacking = false;
            _canAttack   = true;
        }
    }
}
