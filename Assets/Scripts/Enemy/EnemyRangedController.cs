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

            // 전조 연출 — transform 변경 없이 활성화만 (방향은 localScale.x 플립에 의해 자동 반영)
            _animator?.SetTrigger(AnimWindup);
            _windupIndicator?.SetActive(true);

            // 전조 대기 — 빙결 시 일시정지
            yield return StartCoroutine(PauseableWait(_windupDuration));

            _windupIndicator?.SetActive(false);

            // 바라보는 방향으로 수평 직선 발사
            if (_projectilePrefab != null)
            {
                Vector3 fireDir   = new Vector3(Mathf.Sign(transform.localScale.x), 0f, 0f);
                Vector3 targetPos = _spawnPoint.position + fireDir * 100f;

                var go = Instantiate(_projectilePrefab, _spawnPoint.position, Quaternion.identity);
                go.GetComponent<ProjectileBase>()?.Setup(targetPos, new HitInfo
                {
                    Damage         = _attackDamage,
                    KnockbackForce = _knockbackForce
                });
            }

            yield return new WaitForSeconds(Mathf.Max(0f, _attackCooldown - _windupDuration));

            _isAttacking = false;
            _canAttack   = true;
        }
    }
}
