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

        protected override void ApplyData(EnemyDataSO data)
        {
            base.ApplyData(data);
            if (data is RangedEnemyDataSO d)
            {
                _attackDamage   = d.AttackDamage;
                _knockbackForce = d.KnockbackForce;
                _windupDuration = d.WindupDuration;
                if (d.ProjectilePrefab != null) _projectilePrefab = d.ProjectilePrefab;
            }
        }

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
            // 빨간 선(전조) 시작 시점에 방향 고정
            float lockedDir = _player != null
                ? (_player.position.x > transform.position.x ? 1f : -1f)
                : (transform.localScale.x >= 0f ? 1f : -1f);

            _animator?.SetTrigger(AnimWindup);
            _windupIndicator?.SetActive(true);

            // 전조 대기 — 빙결 시 일시정지
            yield return StartCoroutine(PauseableWait(_windupDuration));

            _windupIndicator?.SetActive(false);

            // 고정된 방향으로 수평 발사 (Y 고정)
            if (_projectilePrefab != null)
            {
                Vector3 targetPos = _spawnPoint.position + new Vector3(lockedDir * 100f, 0f, 0f);

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
